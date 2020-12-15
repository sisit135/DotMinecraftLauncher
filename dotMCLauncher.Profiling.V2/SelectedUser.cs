using Newtonsoft.Json;

namespace DotMinecraftLauncher.Profiling.V2
{
    public class SelectedUser : Serializable
    {
        [JsonProperty("account")]
        public string SelectedGuid { get; set; }

        [JsonProperty("profile")]
        public string SelectedProfile { get; set; }
    }
}