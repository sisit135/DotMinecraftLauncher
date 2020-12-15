using Newtonsoft.Json.Linq;

namespace DotMinecraftLauncher.Versioning
{
    public class SingleArgument : Argument
    {
        public SingleArgument()
        {
            Type = ArgumentType.SINGLE;
        }

        public JToken Value { get; set; }
    }
}