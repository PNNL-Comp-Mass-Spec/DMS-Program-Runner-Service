using System.ServiceProcess;

namespace ProgRunnerSvc
{
    public partial class ProgRunner : ServiceBase
    {
        private readonly clsMainProg MyProgRunner;

        public ProgRunner()
        {
            InitializeComponent();
            MyProgRunner = new clsMainProg();
        }

        /// <summary>
        /// Start the service
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            MyProgRunner.StartAllProgRunners();
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        protected override void OnStop()
        {
            MyProgRunner.StopAllProgRunners();
            // MyProgRunner.Dispose();
        }
    }
}
