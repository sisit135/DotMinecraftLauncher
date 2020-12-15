using Newtonsoft.Json;

namespace DotMinecraftLauncher.Profiling.V2
{
    public class AuthenticationProfile : Serializable
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}