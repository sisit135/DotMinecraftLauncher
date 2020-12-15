using Newtonsoft.Json;

namespace DotMinecraftLauncher.Versioning
{
    public class Features
    {
        [JsonProperty("has_custom_resolution")]
        public bool IsForCustomResolution { get; set; }

        [JsonProperty("is_demo_user")]
        public bool IsForDemoUser { get; set; }
    }
}