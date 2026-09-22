using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleAppUpdateAllGithubRepos
{
  public class GitHubClient
  {
    private readonly HttpClient _httpClient;

    public GitHubClient(string token)
    {
      _httpClient = new HttpClient();

      _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonApplication/1.0");

      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<List<GitHubRepository>> GetAllRepositoriesAsync()
    {
      var repositories = new List<GitHubRepository>();

      int page = 1;
      const int perPage = 100;

      while (true)
      {
        string url = $"https://api.github.com/user/repos?per_page={perPage}&page={page}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        var pageRepositories = JsonConvert.DeserializeObject<List<GitHubRepository>>(json);

        if (pageRepositories == null || pageRepositories.Count == 0)
          break;

        repositories.AddRange(pageRepositories);

        if (pageRepositories.Count < perPage)
          break;

        page++;
      }

      return repositories;
    }
  }
}
