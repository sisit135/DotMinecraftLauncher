using System;

namespace DotMinecraftLauncher
{
    /// <summary>
    /// The game client exception
    /// </summary>
    public class MinecraftException : Exception
    {
        /// <summary>
        /// When game crashed (Exit code -1)
        /// </summary>
        ///
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

    public class LauncherException : Exception
    {
        public class InvalidSessionException : Exception
        {
            public InvalidSessionException(string message)
                : base(String.Format("Invalid session token: {0}", message))
            {
            }
        }

        public class FailedToDownloadException : Exception
        {
            public FailedToDownloadException(string message)
                : base(String.Format("File: {0} failed to download", message))
            {
            }
        }
    }
}