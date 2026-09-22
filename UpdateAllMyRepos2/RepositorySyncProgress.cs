namespace UpdateAllMyRepos2
{
  public class RepositorySyncProgress
  {
    public GitHubRepository Repository { get; set; }

    public RepositorySyncStatus Status { get; set; }

    public int Current { get; set; }

    public int Total { get; set; }

    public int Percent { get; set; }

    public string Message { get; set; }
  }
}
