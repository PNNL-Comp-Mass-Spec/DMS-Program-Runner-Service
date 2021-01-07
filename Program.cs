using System.ServiceProcess;

namespace ProgRunnerSvc
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new ProgRunner()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
