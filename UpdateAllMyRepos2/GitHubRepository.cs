using Newtonsoft.Json;

namespace UpdateAllMyRepos2
{
  public class GitHubRepository
  {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }

    [JsonProperty("clone_url")]
    public string CloneUrl { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("private")]
    public bool IsPrivate { get; set; }
  }
}
