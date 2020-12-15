using System;

namespace DotMinecraftLauncher
{
    /// <summary>
    /// The game client exception
    /// </summary>
    public class MinecraftException : Exception
    {
        public class MinecraftCrashedException : Exception
        {
            public int ExitCode { get; set; }

            public string Error { get; set; }

            public MinecraftCrashedException(string message)
                : base(String.Format("Minecraft Crashed: {0}", message))
            {
            }
        }
    }

    /// <summary>
    /// When game crashed (Exit code -1)
    /// </summary>
    
}