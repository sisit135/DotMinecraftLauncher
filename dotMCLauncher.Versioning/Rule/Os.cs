using Newtonsoft.Json;

namespace DotMinecraftLauncher.Versioning
{
    public class Os
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}