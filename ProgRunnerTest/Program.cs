
using PRISM;

namespace ProgRunnerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var myProgRunner = new ProgRunnerSvc.clsMainProg();

            // FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Start");

            // Start the service running
            myProgRunner.StartAllProgRunners();

            // Wait for 120 seconds
            ConsoleMsgUtils.SleepSeconds(120);

            // Stop the service
            myProgRunner.StopAllProgRunners();

            // FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Stop")
        }
    }
}
