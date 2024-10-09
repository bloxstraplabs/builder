using System.Text.Json.Serialization;

namespace Bloxstrap.Builder
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = null!;
    }
}