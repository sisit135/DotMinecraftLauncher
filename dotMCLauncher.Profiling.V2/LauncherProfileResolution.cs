using Newtonsoft.Json;

namespace DotMinecraftLauncher.Profiling.V2
{
    public class LauncherProfileResolution : Serializable
    {
        [JsonProperty("width")]
        public int Width { get; set; } = 854;

        [JsonProperty("height")]
        public int Height { get; set; } = 481;
    }
}