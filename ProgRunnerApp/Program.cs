using System;
using System.Reflection;
using System.Threading;
using ProgRunnerSvc;
using PRISM;

namespace ProgRunnerApp
{
    /// <summary>
    /// This program starts an instance of the DMS Program Runner service
    /// </summary>
    internal static class Program
    {
        // Ignore Spelling: cron

        public const string PROGRAM_DATE = "November 22, 2022";

        /// <summary>
        /// The main entry point for the service
        /// </summary>
        private static int Main(string[] args)
        {
            var exeName = Assembly.GetEntryAssembly()?.GetName().Name;

            var parser = new CommandLineParser<ProgRunnerOptions>(exeName, GetAppVersion())
            {
                ProgramInfo = "This program starts an instance of the DMS Program Runner",
                ContactInfo = "Program written by Matthew Monroe for PNNL (Richland, WA)" + Environment.NewLine +
                              "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                              "Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics"
            };

            ProgRunnerOptions options;

            if (args.Length == 0)
            {
                options = new ProgRunnerOptions();
            }
            else
            {
                var result = parser.ParseArgs(args);
                options = result.ParsedResults;

                if (!result.Success || !options.Validate())
                {
                    if (parser.CreateParamFileProvided)
                    {
                        return 0;
                    }

                    // Delay for 750 msec in case the user double-clicked this file from within Windows Explorer (or started the program via a shortcut)
                    Thread.Sleep(750);

                    return -1;
                }
            }

            try
            {
                if (options.MaxRuntimeMinutes > 0)
                {
                    Console.WriteLine(
                        "Starting the DMSProgramRunner; will exit after {0} minute{1}",
                        options.MaxRuntimeMinutes, options.MaxRuntimeMinutes == 1 ? string.Empty : "s");
                }
                else
                {
                    Console.WriteLine("Starting the DMSProgramRunner; will run indefinitely");
                }

                Console.WriteLine();

                var myProgRunner = new MainProg();
                if (myProgRunner.StartupAborted)
                {
                    // An error message has already been logged
                    // Using a return code of 0 to prevent cron from sending a mail message every 5 minutes
                    return 0;
                }

                // Start the ProgRunner
                myProgRunner.StartAllProgRunners();

                MonitorProgRunner(myProgRunner, options);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Unable to start ProgRunner: " + ex.Message);
                return -1;
            }
        }

        private static void MonitorProgRunner(MainProg myProgRunner, ProgRunnerOptions options)
        {
            try
            {
                var continueLooping = true;
                var startTime = DateTime.UtcNow;

                while (continueLooping)
                {
                    // Wait for 20 seconds
                    ConsoleMsgUtils.SleepSeconds(20);

                    if (options.MaxRuntimeMinutes > 0 && DateTime.UtcNow.Subtract(startTime).TotalMinutes > options.MaxRuntimeMinutes)
                        continueLooping = false;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error monitoring the ProgRunner", ex);
            }

            try
            {
                // Stop the ProgRunner
                myProgRunner.StopAllProgRunners();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error stopping the ProgRunner", ex);
            }
        }

        private static string GetAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }
    }
}
