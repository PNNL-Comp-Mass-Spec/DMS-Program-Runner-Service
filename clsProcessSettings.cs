
namespace ProgRunnerSvc
{
    internal class clsProcessSettings
    {

        protected string m_UniqueKey;

        /// <summary>
        /// Unique name for this program
        /// </summary>
        public string UniqueKey => m_UniqueKey;

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
        /// Holdoff time, in seconds (not milliseconds)
        /// <summary>
        /// Working directory path
        /// </summary>
        public int HoldoffSeconds  { get; set; }
        /// <remarks>If empty, the working directory is determined using ProgramPath</remarks>
        public string WorkDir { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsProcessSettings(string uniqueKey)
        {
            m_UniqueKey = uniqueKey;
        }

        public override string ToString()
        {
            return m_UniqueKey;
        }
    }
}
