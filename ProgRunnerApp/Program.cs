using System;
using System.Collections.Generic;
using System.IO;
using ProgRunnerSvc;
using PRISM;

namespace ProgRunnerApp
{
    /// <summary>
    /// This program starts an instance of the DMS Program Runner
    /// </summary>
    class Program
    {
        public const string PROGRAM_DATE = "March 16, 2018";

        static int mMaxRuntimeMinutes;

        static int Main(string[] args)
        {

            mMaxRuntimeMinutes = 0;

            // Parse the command line arguments
            var commandLineParser = new clsParseCommandLine();

            var success = false;

            if (commandLineParser.ParseCommandLine())
            {
                if (SetOptionsUsingCommandLineParameters(commandLineParser))
                    success = true;
            }
            else
            {
                if (commandLineParser.NonSwitchParameterCount + commandLineParser.ParameterCount == 0 && !commandLineParser.NeedToShowHelp)
                {
                    // No arguments were provided; that's OK
                    success = true;
                }
            }

            if (!success || commandLineParser.NeedToShowHelp)
            {
                ShowProgramHelp();
                return -1;
            }

            try
            {
                if (mMaxRuntimeMinutes > 0)
                    Console.WriteLine("Starting the DMSProgramRunner; will exit after {0} minutes", mMaxRuntimeMinutes);
                else
                    Console.WriteLine("Starting the DMSProgramRunner; will run indefinitely");

                Console.WriteLine();

                var myProgRunner = new clsMainProg("ProgRunnerApp");
                if (myProgRunner.StartupAborted)
                    return -1;

                // Start the ProgRunner
                myProgRunner.StartAllProgRunners();

                MonitorProgRunner(myProgRunner);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Unable to start ProgRunner: " + ex.Message);
                return -1;
            }

        }

        private static void MonitorProgRunner(clsMainProg myProgRunner)
        {
            try
            {
                var continueLooping = true;
                var startTime = DateTime.UtcNow;

                while (continueLooping)
                {
                    // Wait for 60 seconds
                    ConsoleMsgUtils.SleepSeconds(60);

                    if (mMaxRuntimeMinutes > 0 && DateTime.UtcNow.Subtract(startTime).TotalMinutes > mMaxRuntimeMinutes)
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
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false
            var lstValidParameters = new List<string> { "RunTime" };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in commandLineParser.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid commmand line parameters", badArguments);

                    return false;
                }

                if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    var runtimeMinutesText = commandLineParser.RetrieveNonSwitchParameter(0);

                    if (int.TryParse(runtimeMinutesText, out var runtimeMinutes))
                    {
                        mMaxRuntimeMinutes = runtimeMinutes;
                    }
                }

                return GetParamInt(commandLineParser, "RunTime", ref mMaxRuntimeMinutes);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static bool GetParamInt(clsParseCommandLine commandLineParser, string paramName, ref int paramValue)
        {
            if (!commandLineParser.RetrieveValueForParameter(paramName, out var paramValueText))
            {
                // Leave paramValue unchanged
                return true;
            }

            if (string.IsNullOrWhiteSpace(paramValueText))
            {
                ShowErrorMessage("/" + paramName + " does not have a value");
                return false;
            }

            // Update paramValue
            if (int.TryParse(paramValueText, out paramValue))
            {
                return true;
            }

            ShowErrorMessage("Error converting " + paramValueText + " to an integer for parameter /" + paramName);
            return false;
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowErrorMessage(string title, IEnumerable<string> items)
        {
            ConsoleMsgUtils.ShowErrors(title, items);
        }

        private static void ShowProgramHelp()
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                Console.WriteLine();
                Console.WriteLine("This program starts an instance of the DMS Program Runner");
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + exeName);
                Console.WriteLine(" [/RunTime:Minutes]");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "By default this program will run indefinitely. " +
                                      "Optionally use /RunTime to specify a maximum runtime, in minutes"));
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2018");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov");
                Console.WriteLine();

                // Delay for 1 second in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                ConsoleMsgUtils.SleepSeconds(1);

            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax", ex);
            }

        }
    }
}
