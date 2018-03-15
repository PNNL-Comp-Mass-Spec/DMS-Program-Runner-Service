using System.ServiceProcess;

namespace ProgRunnerSvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new ProgRunner()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
