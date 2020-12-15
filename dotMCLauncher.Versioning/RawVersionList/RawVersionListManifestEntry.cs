using Newtonsoft.Json;

namespace DotMinecraftLauncher.Versioning
{
    public class RawVersionListManifestEntry : Version
    {
        [JsonProperty("url")]
        public string ManifestUrl { get; set; }
    }
}