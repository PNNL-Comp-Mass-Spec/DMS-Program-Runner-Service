using PRISM;

namespace ProgRunnerApp
{
    /// <summary>
    /// Program runner Options
    /// </summary>
    public class ProgRunnerOptions
    {
        [Option("MaxRuntimeMinutes", "Runtime",
            ArgPosition = 1,
            HelpText = "Maximum runtime, in minutes; use 0 to run indefinitely")]
        public int MaxRuntimeMinutes { get; set; }

        public bool Validate()
        {
            if (MaxRuntimeMinutes < 0)
                MaxRuntimeMinutes = 0;

            return true;
        }
    }
}
