using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppUpdateAllGithubRepos
{
  internal class Program
  {
    static async Task Main()
    {
      Action<string> display = Console.WriteLine;
      display("Update all Github repositories");
      display($"Version {GetApplicationVersion()}");
      display("This program will update all your Github repositories in the current directory.");
      display("Please make sure you have git installed and configured.");
      display("Please make sure you have a valid Github token set in the GITHUB_TOKEN environment variable.");
      display("Please make sure you have a valid Github username set in the GITHUB_USERNAME environment variable.");
      display("Please make sure you have a valid Github email set in the GITHUB_EMAIL environment variable.");

      string token = "ghp_xxxxxxxxxxxxxxxxxxxx";

      var github = new GitHubClient(token);

      List<GitHubRepository> repositories = await github.GetAllRepositoriesAsync();

      foreach (var repo in repositories)
      {
        Console.WriteLine(repo.FullName);
      }

      display("Press any key to continue...");
      Console.ReadKey();
    }

    private static string GetApplicationVersion()
    {
      Version version = Assembly.GetEntryAssembly()?.GetName().Version; 
      return version?.ToString() ?? "Version inconnue";
    }
  }
}
