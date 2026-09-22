using System;

namespace UpdateAllMyRepos2
{
  public class RepositorySyncResult
  {
    public GitHubRepository Repository { get; set; }

    public RepositorySyncStatus Status { get; set; }

    public string Message { get; set; }

    public Exception Exception { get; set; }
  }
}
