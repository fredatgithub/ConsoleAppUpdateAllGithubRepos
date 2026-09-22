using System;
using System.IO;
using System.Threading.Tasks;
using UpdateAllMyRepos2.Properties;

namespace UpdateAllMyRepos2
{
  internal class Program
  {
    static async Task Main()
    {

      string token = "github_pat_xxxxxxxxxxxxxxxxx";
      token = ReadTokenFile("token.txt");

      string backupDirectory = Settings.Default.BackupGitDirectory; // @"D:\GitHubBackup";
      // on demande à l'utilisateur si le chemin de sauvegarde est correct
      Console.WriteLine($"Le chemin de sauvegarde est : {backupDirectory}");
      Console.WriteLine("Appuyez sur une touche pour continuer l'application ou Ctrl+C pour annuler ici et changer le chemin dans le fichier de config...");
      Console.ReadKey();

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

    private static string ReadTokenFile(string filename)
    {
      // read the file and return the token
      if (!File.Exists(filename))
      {
        Console.WriteLine($"Le fichier {filename} n'existe pas. Veuillez insérer votre token Github dans le fichier {filename}");
        // on crée le fichier vide
        try
        {
          File.WriteAllText(filename, string.Empty);
        }
        catch (Exception)
        {
          Console.WriteLine($"Impossible de créer le fichier {filename}. Veuillez vérifier les permissions.");
        }

        Environment.Exit(1);
      }

      return File.ReadAllText(filename).Trim();
    }
  }
}
