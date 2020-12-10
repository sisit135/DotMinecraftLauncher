using System;

namespace dotMCLauncher
{
    /// <summary>
    /// Provide methods for performing google cloud task
    /// </summary>
    public class GCSService
    {
        public string ProjectId = "plenary-hangout-286411";

        public static void InitialGCSServices()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "Project-62b4458afebf.json", EnvironmentVariableTarget.Process);
        }
    }
}