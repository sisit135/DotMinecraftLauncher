using System;

namespace dotMCLauncher
{
    /// <summary>
    /// The game client exception
    /// </summary>
    public class MinecraftCrachedException : Exception
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
}