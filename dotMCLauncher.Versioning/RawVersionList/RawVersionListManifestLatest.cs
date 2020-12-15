using Newtonsoft.Json;

namespace DotMinecraftLauncher.Versioning
{
    public class RawVersionListManifestLatest
    {
        [JsonProperty("release")]
        public string Release { get; set; }

        [JsonProperty("snapshot")]
        public string Snapshot { get; set; }
    }
}