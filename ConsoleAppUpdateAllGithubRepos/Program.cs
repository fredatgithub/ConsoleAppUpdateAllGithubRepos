using System;
using System.Reflection;

namespace ConsoleAppUpdateAllGithubRepos
{
  internal class Program
  {
    static void Main()
    {
      Action<string> display = Console.WriteLine;
      display("Update all Github repositories");
      display($"Version {GetApplicationVersion()}");
      display("This program will update all your Github repositories in the current directory.");
      display("Please make sure you have git installed and configured.");
      display("Please make sure you have a valid Github token set in the GITHUB_TOKEN environment variable.");
      display("Please make sure you have a valid Github username set in the GITHUB_USERNAME environment variable.");
      display("Please make sure you have a valid Github email set in the GITHUB_EMAIL environment variable.");


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
