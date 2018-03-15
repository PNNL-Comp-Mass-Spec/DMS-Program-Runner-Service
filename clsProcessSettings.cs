
namespace ProgRunnerSvc
{
    class clsProcessSettings
    {

        protected string m_UniqueKey;

        public string UniqueKey => m_UniqueKey;

        public string ProgramPath { get; set; }
        public string ProgramArguments { get; set; }
        public string RepeatMode { get; set; }
        public int HoldoffSeconds  { get; set; }

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
