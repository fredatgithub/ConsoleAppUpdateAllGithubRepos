using System;
using System.Threading.Tasks;

namespace UpdateAllMyRepos
{
  internal class Program
  {
    static async Task Main()
    {
      string token = "github_pat_xxxxxxxxxxxxxxxxx";

      using (var github = new GitHubClient(token))
      {
        var repositories = await github.GetRepositoriesAsync();

        foreach (var repository in repositories)
        {
          Console.WriteLine(
              $"{repository.FullName} - " +
              $"{(repository.RepoPrivate ? "Privé" : "Public")}");
        }
      }
    }
  }
}
