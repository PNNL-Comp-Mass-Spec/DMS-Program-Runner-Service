
using PRISM;
using System;

namespace ProgRunnerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var myProgRunner = new ProgRunnerSvc.clsMainProg("ProgRunnerTest");

                // FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Start");

                // Start the service running
                myProgRunner.StartAllProgRunners();

                // Wait for 120 seconds
                ConsoleMsgUtils.SleepSeconds(120);

                // Stop the service
                myProgRunner.StopAllProgRunners();

                // FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Stop")
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to start ProgRunner");
            }

        }
    }
}
