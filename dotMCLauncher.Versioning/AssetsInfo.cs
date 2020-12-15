using Newtonsoft.Json;

namespace DotMinecraftLauncher.Versioning
{
    public class AssetsInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}