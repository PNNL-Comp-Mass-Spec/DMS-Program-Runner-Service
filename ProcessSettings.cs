﻿
namespace ProgRunnerSvc
{
    internal class ProcessSettings
    {
        /// <summary>
        /// Delay, in seconds, that the ProgRunner service will wait
        /// before first starting this program after the service starts,
        /// or after the XML file is reloaded
        /// </summary>
        public int DelaySeconds { get; set; }

        /// <summary>
        /// Holdoff time, in seconds (not milliseconds)
        /// </summary>
        public int HoldoffSeconds { get; set; }

        /// <summary>
        /// Path to the program (.exe or .bat) to run
        /// </summary>
        public string ProgramPath { get; set; }

        /// <summary>
        /// Arguments to pass to the program
        /// </summary>
        public string ProgramArguments { get; set; }

        /// <summary>
        /// Repeat mode
        /// </summary>
        /// <remarks>Valid values are Repeat, Once, and No</remarks>
        public string RepeatMode { get; set; }

        /// <summary>
        /// Unique name for this program
        /// </summary>
        public string UniqueKey { get; }

        /// <summary>
        /// Working directory path
        /// </summary>
        /// <remarks>If empty, the working directory is determined using ProgramPath</remarks>
        public string WorkDir { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProcessSettings(string uniqueKey)
        {
            UniqueKey = uniqueKey;
        }

        public override string ToString()
        {
            return UniqueKey;
        }
    }
}
