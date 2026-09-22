using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UpdateAllMyRepos
{
  public class GitHubClient2: IDisposable
  {
    private readonly HttpClient _httpClient;
    private readonly string _token;

    public GitHubClient2(string token)
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

      // GitHub exige un User-Agent
      _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
          "MonApplication/1.0");

      _httpClient.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Bearer", _token);

      _httpClient.DefaultRequestHeaders.Accept.Add(
          new MediaTypeWithQualityHeaderValue(
              "application/vnd.github+json"));
    }

    /// <summary>
    /// Récupère tous les repositories accessibles
    /// par l'utilisateur GitHub authentifié.
    /// </summary>
    public async Task<List<GitHubRepository>> GetRepositoriesAsync(
        CancellationToken cancellationToken = default(CancellationToken))
    {
      var repositories = new List<GitHubRepository>();

      const int perPage = 100;
      int page = 1;

      while (true)
      {
        string url =
            $"user/repos?per_page={perPage}&page={page}&affiliation=owner,collaborator,organization_member";

        using (HttpResponseMessage response =
            await _httpClient.GetAsync(url, cancellationToken))
        {
          string json = await response.Content.ReadAsStringAsync();

          if (!response.IsSuccessStatusCode)
          {
            throw new Exception(
                $"Erreur GitHub ({(int)response.StatusCode}) : {json}");
          }

          var pageRepositories =
              JsonConvert.DeserializeObject<List<GitHubRepository>>(json);

          if (pageRepositories == null ||
              pageRepositories.Count == 0)
          {
            break;
          }

          repositories.AddRange(pageRepositories);

          // Moins de 100 résultats signifie que
          // nous sommes arrivés à la dernière page.
          if (pageRepositories.Count < perPage)
            break;

          page++;
        }
      }

      return repositories;
    }

    /// <summary>
    /// Clone un repository GitHub dans le répertoire spécifié.
    /// </summary>
    public async Task CloneRepositoryAsync(
        GitHubRepository repository,
        string destinationDirectory,
        CancellationToken cancellationToken = default(CancellationToken))
    {
      if (repository == null)
        throw new ArgumentNullException(nameof(repository));

      if (string.IsNullOrWhiteSpace(destinationDirectory))
        throw new ArgumentException(
            "Le répertoire de destination est obligatoire.",
            nameof(destinationDirectory));

      Directory.CreateDirectory(destinationDirectory);

      string repositoryDirectory =
          Path.Combine(
              destinationDirectory,
              repository.Name);

      if (Directory.Exists(repositoryDirectory))
      {
        throw new IOException(
            $"Le répertoire existe déjà : {repositoryDirectory}");
      }

      // On utilise une URL HTTPS avec authentification Git
      // via GIT_ASKPASS plutôt que de mettre le token
      // directement dans l'URL.
      string gitUrl = repository.CloneUrl;

      string arguments =
          $"clone \"{gitUrl}\" \"{repositoryDirectory}\"";

      await RunGitAsync(
          arguments,
          cancellationToken);
    }

    private async Task RunGitAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "git.exe",
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
      };

      // Permet à Git de demander le token sans
      // l'exposer dans la ligne de commande.
      startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
      startInfo.EnvironmentVariables["GIT_ASKPASS"] =
          "git-askpass-helper";

      using (var process = new Process())
      {
        process.StartInfo = startInfo;

        process.Start();

        Task<string> outputTask =
            process.StandardOutput.ReadToEndAsync();

        Task<string> errorTask =
            process.StandardError.ReadToEndAsync();

        await Task.Run(
            () => process.WaitForExit(),
            cancellationToken);

        string output = await outputTask;
        string error = await errorTask;

        if (process.ExitCode != 0)
        {
          throw new Exception(
              $"Erreur lors du clone Git.\n{error}");
        }
      }
    }

    public void Dispose()
    {
      _httpClient.Dispose();
    }
  }
}
