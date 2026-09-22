using System;
using System.Threading.Tasks;

namespace UpdateAllMyRepos2
{
  internal class Program
  {
    static async Task Main()
    {
      string token = "github_pat_xxxxxxxxxxxxxxxxx";

      string backupDirectory = @"D:\GitHubBackup";

      using (var github = new GitHubClient(token))
      {
        Console.WriteLine("Récupération des repositories...");

        var repositories = await github.GetRepositoriesAsync();

        Console.WriteLine($"{repositories.Count} repositories trouvés.");

        var progress =
            new Progress<RepositorySyncProgress>(
                p =>
                {
                  Console.WriteLine(
                      $"[{p.Current}/{p.Total}] " +
                      $"{p.Repository.FullName} - " +
                      $"{p.Status} - " +
                      $"{p.Message}");
                });

        await github.SyncRepositoriesAsync(
            repositories,
            backupDirectory,
            maxParallelism: 3,
            progress: progress);

        Console.WriteLine();
        Console.WriteLine("Sauvegarde terminée.");
        Console.WriteLine("Pressez une touche pour quitter...");
        Console.ReadKey();
      }
    }
  }
}
