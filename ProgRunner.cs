using System;
using System.ServiceProcess;

namespace ProgRunnerSvc
{
    public partial class ProgRunner : ServiceBase
    {
        private readonly clsMainProg mProgRunner;
        private readonly bool mAbortStart;

        public ProgRunner()
        {
            InitializeComponent();

            try
            {
                mProgRunner = new clsMainProg("ProgRunnerSvc");
                mAbortStart = mProgRunner.StartupAborted;
            }
            catch (Exception)
            {
                mAbortStart = true;
            }
        }

        /// <summary>
        /// Start the service
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            if (mAbortStart)
            {
                Stop();
                return;
            }

            try
            {
                mProgRunner.StartAllProgRunners();
            }
            catch (Exception)
            {
                Stop();
            }
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                mProgRunner.StopAllProgRunners();
            }
            catch (Exception)
            {
                // Ignore errors
            }

        }
    }
}
