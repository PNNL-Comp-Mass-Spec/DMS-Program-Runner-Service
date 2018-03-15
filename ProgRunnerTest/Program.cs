
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

            // Wait for 60 seconds
            System.Threading.Thread.Sleep(300 * 1000);

            // Stop the service
            myProgRunner.StopAllProgRunners();

            // FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Stop")
        }
    }
}
