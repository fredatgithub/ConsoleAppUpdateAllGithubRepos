using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json;

namespace UpdateAllMyRepos
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

      _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonApplication/1.0");

      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

      _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<List<GitHubRepository>> GetRepositoriesAsync(
        CancellationToken cancellationToken = default(CancellationToken))
    {
      var repositories = new List<GitHubRepository>();

      const int perPage = 100;
      int page = 1;

      while (true)
      {
        string url =
            $"user/repos?per_page={perPage}&page={page}" +
            "&affiliation=owner,collaborator,organization_member";

        using (var response = await _httpClient.GetAsync(url, cancellationToken))
        {
          string json = await response.Content.ReadAsStringAsync();

          if (!response.IsSuccessStatusCode)
          {
            throw new Exception($"GitHub : {(int)response.StatusCode}\n{json}");
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

    public async Task CloneRepositoryAsync(GitHubRepository repository, string destinationDirectory, CancellationToken cancellationToken = default(CancellationToken))
    {
      if (repository == null)
        throw new ArgumentNullException(nameof(repository));

      Directory.CreateDirectory(destinationDirectory);

      string localPath = Path.Combine(destinationDirectory, repository.Name);

      if (Directory.Exists(localPath))
      {
        throw new IOException(
            $"Le répertoire existe déjà : {localPath}");
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

        Repository.Clone(
            repository.CloneUrl,
            localPath,
            cloneOptions);

      }, cancellationToken);
    }

    public void Dispose()
    {
      _httpClient.Dispose();
    }
  }
}
