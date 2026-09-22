namespace UpdateAllMyRepos2
{
  public class GitHubRepository
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string HtmlUrl { get; set; }
    public string CloneUrl { get; set; }
    public string Description { get; set; }
    public bool RepoPrivate { get; set; }
  }
}
