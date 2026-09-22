using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json;

namespace UpdateAllMyRepos2
{
  public class GitHubClient: IDisposable
  {
    private readonly HttpClient _httpClient;
    private readonly string _token;

    public GitHubClient(string token)
    {
      if (string.IsNullOrWhiteSpace(token))
        throw new ArgumentException(
            "Le token GitHub est obligatoire.",
            nameof(token));

      _token = token;

      _httpClient = new HttpClient
      {
        BaseAddress = new Uri("https://api.github.com/")
      };

      _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubBackup/1.0");

      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

      _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<List<GitHubRepository>> GetRepositoriesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      var repositories = new List<GitHubRepository>();

      const int perPage = 100;
      int page = 1;

      while (true)
      {
        string url = $"user/repos?per_page={perPage}" + $"&page={page}" + "&affiliation=owner,collaborator,organization_member";

        using (HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken))
        {
          string json = await response.Content.ReadAsStringAsync();

          if (!response.IsSuccessStatusCode)
          {
            throw new Exception( $"GitHub API : {(int)response.StatusCode}\n" + json);
          }

          var pageRepositories = JsonConvert.DeserializeObject<List<GitHubRepository>>(json);

          if (pageRepositories == null || pageRepositories.Count == 0)
          {
            break;
          }

          repositories.AddRange(pageRepositories);

          if (pageRepositories.Count < perPage)
          {
            break;
          }

          page++;
        }
      }

      return repositories;
    }

    public async Task SyncRepositoriesAsync(IEnumerable<GitHubRepository> repositories, string destinationDirectory, int maxParallelism, IProgress<RepositorySyncProgress> progress = null, CancellationToken cancellationToken = default(CancellationToken))
    {
      if (repositories == null)
      {
        throw new ArgumentNullException(nameof(repositories));
      }

      if (string.IsNullOrWhiteSpace(destinationDirectory))
      {
        throw new ArgumentException("Le répertoire de destination est obligatoire.", nameof(destinationDirectory));
      }

      if (maxParallelism < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(maxParallelism));
      }

      Directory.CreateDirectory(destinationDirectory);

      var repositoryList = repositories.ToList();

      int total = repositoryList.Count;
      int completed = 0;

      using (var semaphore = new SemaphoreSlim(maxParallelism))
      {
        var tasks = repositoryList.Select(
            async repository =>
            {
              await semaphore.WaitAsync(cancellationToken);

              try
              {
                cancellationToken.ThrowIfCancellationRequested();

                var result =await SyncRepositoryAsync(
                            repository,
                            destinationDirectory,
                            progress,
                            cancellationToken);

                int current =Interlocked.Increment(ref completed);

                progress?.Report(new RepositorySyncProgress
                        {
                          Repository = repository,
                          Current = current,
                          Total = total,
                          Percent = CalculatePercent(current, total),
                          Status = result.Status,
                          Message = result.Message
                        });

                return result;
              }
              finally
              {
                semaphore.Release();
              }
            });

        await Task.WhenAll(tasks);
      }
    }

    private async Task<RepositorySyncResult> SyncRepositoryAsync(GitHubRepository repository, string destinationDirectory, IProgress<RepositorySyncProgress> progress, CancellationToken cancellationToken)
    {
      var result = new RepositorySyncResult
      {
        Repository = repository,
        Status = RepositorySyncStatus.Waiting
      };

      string repositoryPath = Path.Combine(destinationDirectory, repository.Name);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        // --------------------------------------------------
        // Le repository existe déjà
        // --------------------------------------------------

        if (Directory.Exists(repositoryPath) && Repository.IsValid(repositoryPath))
        {
          result.Status = RepositorySyncStatus.Pulling;

          progress?.Report(new RepositorySyncProgress
              {
                Repository = repository,
                Status = result.Status,
                Message = "Pull en cours..."
              });

          await PullRepositoryAsync(repositoryPath, cancellationToken);

          result.Status = RepositorySyncStatus.Completed;

          result.Message = "Repository mis à jour.";

          return result;
        }

        // --------------------------------------------------
        // Le repository n'existe pas
        // --------------------------------------------------

        Directory.CreateDirectory(destinationDirectory);

        result.Status = RepositorySyncStatus.Cloning;

        progress?.Report(new RepositorySyncProgress
            {
              Repository = repository,
              Status = result.Status,
              Message = "Clone en cours..."
            });

        await CloneRepositoryAsync(
            repository,
            repositoryPath,
            progress,
            cancellationToken);

        result.Status = RepositorySyncStatus.Completed;

        result.Message = "Repository cloné.";

        return result;
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception exception)
      {
        result.Status = RepositorySyncStatus.Failed;

        result.Message = exception.Message;
        result.Exception = exception;

        return result;
      }
    }

    private async Task CloneRepositoryAsync(
    GitHubRepository repository,
    string destinationPath,
    IProgress<RepositorySyncProgress> progress,
    CancellationToken cancellationToken)
    {
      if (repository == null)
        throw new ArgumentNullException(nameof(repository));

      if (string.IsNullOrWhiteSpace(repository.CloneUrl))
      {
        throw new InvalidOperationException(
            $"L'URL de clonage est absente pour " +
            $"le repository '{repository.FullName}'.");
      }

      await Task.Run(() =>
      {
        var cloneOptions = new CloneOptions();

        cloneOptions.FetchOptions.CredentialsProvider =
            (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials
                {
                  Username = "x-access-token",
                  Password = _token
                };

        cloneOptions.FetchOptions.OnTransferProgress =
            transferProgress =>
            {
              cancellationToken.ThrowIfCancellationRequested();

              int percent = 0;

              if (transferProgress.TotalObjects > 0)
              {
                percent =
                    (int)(
                        transferProgress.ReceivedObjects * 100.0 /
                        transferProgress.TotalObjects);
              }

              progress?.Report(
                  new RepositorySyncProgress
                  {
                    Repository = repository,
                    Status =
                          RepositorySyncStatus.Cloning,
                    Percent = percent,
                    Message =
                          $"Clone : {percent}%"
                  });

              return true;
            };

        Repository.Clone(
            repository.CloneUrl,
            destinationPath,
            cloneOptions);

      }, cancellationToken);
    }

    private async Task CloneRepositoryAsync2(
        GitHubRepository repository,
        string destinationPath,
        IProgress<RepositorySyncProgress> progress,
        CancellationToken cancellationToken)
    {
      await Task.Run(() =>
      {
        var cloneOptions = new CloneOptions();

        cloneOptions.FetchOptions.CredentialsProvider =
            (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials
                {
                  Username = "x-access-token",
                  Password = _token
                };

        cloneOptions.FetchOptions.OnTransferProgress = transferProgress =>
    {
      cancellationToken.ThrowIfCancellationRequested();

      int percent = 0;

      if (transferProgress.TotalObjects > 0)
      {
        percent = (int)(transferProgress.ReceivedObjects * 100.0 / transferProgress.TotalObjects);
      }

      progress?.Report(new RepositorySyncProgress
          {
            Repository = repository,
            Status = RepositorySyncStatus.Cloning,
            Percent = percent,
            Message = $"Clone : {percent}%"
          });

      return true;
    };
        Console.WriteLine($"Repository : {repository.FullName}");

        Console.WriteLine($"clone_url  : {repository.CloneUrl}");

        Console.WriteLine($"html_url   : {repository.HtmlUrl}");
        Repository.Clone(repository.CloneUrl, destinationPath, cloneOptions);
      }, cancellationToken);
    }

    private async Task PullRepositoryAsync(string repositoryPath, CancellationToken cancellationToken)
    {
      await Task.Run(() =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        using (var repository = new Repository(repositoryPath))
        {
          var signature = new Signature("GitHubBackup", "backup@localhost", DateTimeOffset.Now);

          var pullOptions = new PullOptions
          {
            FetchOptions = new FetchOptions
                  {
                    CredentialsProvider =
                          (url,usernameFromUrl, types) =>
                              new UsernamePasswordCredentials
                              {
                                Username = "x-access-token",
                                Password = _token
                              }
                  }
          };

          Commands.Pull(repository, signature, pullOptions);
        }

      }, cancellationToken);
    }

    private static int CalculatePercent(int current, int total)
    {
      if (total <= 0)
      {
        return 100;
      }

      return (int)(current * 100.0 / total);
    }

    public void Dispose()
    {
      _httpClient.Dispose();
    }
  }
}
