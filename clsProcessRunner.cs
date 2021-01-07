using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using PRISM;
using PRISM.Logging;

namespace ProgRunnerSvc
{
    /// <summary>
    /// This class runs a single program as an external process and monitors it with an internal thread
    /// </summary>
    class clsProcessRunner
    {
        // Ignore Spelling: usr

        /// <summary>
        /// Thread states
        /// </summary>
        public enum eThreadState
        {
            No,
            ProcessBroken,
            Idle,
            ProcessStarting,
            ProcessRunning,
        }
        #region "Member Variables"

        private readonly Regex mMonoProgramMatcher;

        private readonly Process mProcess = new Process();

        private bool mWarnedInvalidRepeatMode;

        private clsProcessSettings mProgramInfo;

        private clsProcessSettings mNewProgramInfo;

        /// <summary>
        /// The internal thread used to run the monitoring code. That starts and monitors the external program.
        /// </summary>
        private Thread mThread;

        /// <summary>
        /// Flag that tells internal thread to quit monitoring external program and exit.
        /// </summary>
        private bool mThreadStopCommand;

        /// <summary>
        /// The interval (in milliseconds) for monitoring thread to wake up and check m_doCleanup.
        /// </summary>
        private int mMonitorIntervalMsec = 1000;

        private readonly object oSync = 1;

        private bool mInitialDelayApplied;

        private bool mUpdateRequired;

        private bool mWorkDirLogged;

        #endregion

        #region "Properties"

        /// <summary>
        /// Key name for this program (unique across all programs registered to run)
        /// </summary>
        public string KeyName { get; private set; }

        /// <summary>
        /// Path to the program (.exe or .bat) to run
        /// </summary>
        public string ProgramPath => mProgramInfo.ProgramPath;

        /// <summary>
        /// Arguments to pass to the program
        /// </summary>
        public string ProgramArguments => mProgramInfo.ProgramArguments;

        /// <summary>
        /// Repeat mode
        /// </summary>
        /// <remarks>Valid values are Repeat, Once, and No</remarks>
        public string RepeatMode => mProgramInfo.RepeatMode;

        /// <summary>
        /// Holdoff time, in seconds (not milliseconds)
        /// </summary>
        public int Holdoff => mProgramInfo.HoldoffSeconds;

        /// <summary>
        /// The interval (in milliseconds) for monitoring thread to wake up and check m_doCleanup.
        /// </summary>
        public int MonitoringInterval
        {
            get => mMonitorIntervalMsec;
            set
            {
                if (value < 100)
                    value = 100;
                mMonitorIntervalMsec = value;
            }
        }

        /// <summary>
        /// Process id of the currently running incarnation of the external program
        /// </summary>
        public int PID { get; private set; }

        /// <summary>
        /// Overall state of this object
        /// </summary>
        public eThreadState ThreadState { get; private set; }

        /// <summary>
        /// Exit code when process completes
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Working directory for process execution.
        /// </summary>
        /// <remarks>If empty, will determine the working directory based on ProgramPath</remarks>
        public string WorkDir => mProgramInfo.WorkDir;

        /// <summary>
        /// Determine if window should be displayed
        /// </summary>
        public bool CreateNoWindow { get; private set; }

        /// <summary>
        /// Window style to use when CreateNoWindow is False
        /// </summary>
        public ProcessWindowStyle WindowStyle { get; private set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processSettings">Process settings</param>
        /// <param name="windowStyle">Window style (defaults to Normal)</param>
        /// <param name="createNoWindow">True to create no window, false to use a normal window; defaults to false</param>
        public clsProcessRunner(clsProcessSettings processSettings, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, bool createNoWindow = false)
        {
            mMonoProgramMatcher = new Regex("^(mono(.exe)?|[^ ]+/mono(.exe)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Initialize(processSettings, windowStyle, createNoWindow);
        }

        /// <summary>
        /// Store the settings
        /// </summary>
        /// <param name="processSettings"></param>
        /// <param name="windowStyle"></param>
        /// <param name="createNoWindow"></param>
        private void Initialize(clsProcessSettings processSettings, ProcessWindowStyle windowStyle, bool createNoWindow)
        {

            ThreadState = eThreadState.No;
            KeyName = processSettings.UniqueKey;

            // Initially mProgramInfo will only contain the UniqueKey
            // mNewProgramInfo has all of the program details
            // mProgramInfo will be updated by UpdateThreadParameters in ProcessThread provided mUpdateRequired is true

            mProgramInfo = new clsProcessSettings(processSettings.UniqueKey);

            mNewProgramInfo = processSettings;

            WindowStyle = windowStyle;
            CreateNoWindow = createNoWindow;

            mUpdateRequired = true;
            StartThread();
        }

        /// <summary>
        /// Update settings for existing prog runner instance
        /// </summary>
        /// <param name="newProgramInfo">New program info</param>
        public void UpdateProcessParameters(clsProcessSettings newProgramInfo)
        {
            try
            {
                Monitor.Enter(oSync);
                mNewProgramInfo = newProgramInfo;
                mUpdateRequired = true;
                Monitor.Exit(oSync);
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Error updating process parameters: " + ex.Message);
            }
        }

        /// <summary>
        /// Start the thread
        /// </summary>
        public void StartThread()
        {
            if (ThreadState == eThreadState.No)
            {
                try
                {
                    mThread = new Thread(ProcessThread);
                    mThread.SetApartmentState(ApartmentState.STA);
                    mThread.Start();
                }
                catch (Exception ex)
                {
                    ThreadState = eThreadState.ProcessBroken;
                    LogTools.LogError("Failed to create thread: " + KeyName, ex);
                }
            }

        }

        /// <summary>
        /// Stop the thread
        /// </summary>
        public void StopThread()
        {
            if (mThread == null)
                return;

            mThreadStopCommand = true;
            if (ThreadState == eThreadState.ProcessRunning)
            {
                LogTools.LogMessage("Try to kill process: " + KeyName);

                try
                {
                    mProcess.Kill();
                }
                catch (Exception ex)
                {
                    LogTools.LogWarning("Exception killing process '" + KeyName + "': " + ex.Message);
                }

                if (!mProcess.WaitForExit(mMonitorIntervalMsec))
                {
                    LogTools.LogError("Failed to kill process '" + KeyName + "'");
                }
                else
                {
                    LogTools.LogMessage("Killed process: " + KeyName);
                }
            }

            try
            {
                mThread.Join(mMonitorIntervalMsec);
            }
            catch (Exception ex)
            {
                LogTools.LogError("Failed to wait while stopping thread '" + KeyName + "'", ex);
            }

            if (mThread.IsAlive)
            {
                LogTools.LogMessage("Try to abort thread: " + KeyName);

                try
                {
                    mThread.Abort();
                }
                catch (Exception ex)
                {
                    LogTools.LogError("Failed to stop thread '" + KeyName + "'", ex);
                }

            }

        }

        /// <summary>
        /// Capitalize the first letter of modeName
        /// </summary>
        /// <param name="modeName"></param>
        /// <returns></returns>
        private string CapitalizeMode(string modeName)
        {
            if (string.IsNullOrWhiteSpace(modeName))
                return string.Empty;

            if (modeName.Length == 1)
                return modeName.ToUpper();

            return modeName.Substring(0, 1).ToUpper() + modeName.Substring(1).ToLower();
        }

        /// <summary>
        /// Determine the best working directory path to use
        /// </summary>
        /// <param name="programInfo"></param>
        /// <returns></returns>
        private string DetermineWorkDir(clsProcessSettings programInfo)
        {
            try
            {

                if (!string.IsNullOrWhiteSpace(programInfo.WorkDir) && Directory.Exists(programInfo.WorkDir))
                    return programInfo.WorkDir;

                // Auto-determine the working directory
                // Check whether we're running a program with mono
                // Use a RegEx that matches .ProgramPath being mono or mono.exe or a path like /usr/local/bin/mono
                // Note, when using mono, in MultiProgRunner.xml set "value" to the path to mono
                // but specify the .NET Assembly to run at the start of "arguments")

                string warningMsg;

                var match = mMonoProgramMatcher.Match(programInfo.ProgramPath);
                if (match.Success && programInfo.ProgramArguments.Length > 0)
                {
                    var spaceIndex = programInfo.ProgramArguments.IndexOf(' ');
                    string dotNetAssemblyPath;

                    if (spaceIndex <= 0)
                        dotNetAssemblyPath = programInfo.ProgramArguments;
                    else
                    {
                        dotNetAssemblyPath = programInfo.ProgramArguments.Substring(0, spaceIndex);
                    }

                    var dotNetAssemblyInfo = new FileInfo(dotNetAssemblyPath);
                    if (dotNetAssemblyInfo.Directory == null)
                    {
                        warningMsg = "Unable to determine the parent directory of " + dotNetAssemblyPath;
                        LogPathChangeInfo(dotNetAssemblyPath, dotNetAssemblyInfo);
                    }
                    else if (!dotNetAssemblyInfo.Directory.Exists)
                    {
                        warningMsg = string.Format("Parent directory of {0} does not exist", dotNetAssemblyPath);
                        LogPathChangeInfo(dotNetAssemblyPath, dotNetAssemblyInfo);
                    }
                    else
                    {
                        return dotNetAssemblyInfo.Directory.FullName;
                    }
                }
                else
                {
                    var exeInfo = new FileInfo(programInfo.ProgramPath);
                    if (exeInfo.Directory == null)
                    {
                        warningMsg = "Unable to determine the parent directory of " + programInfo.ProgramPath;
                        LogPathChangeInfo(programInfo.ProgramPath, exeInfo);
                    }
                    else if (!exeInfo.Directory.Exists)
                    {
                        warningMsg = string.Format("Parent directory of {0} does not exist", programInfo.ProgramPath);
                        LogPathChangeInfo(programInfo.ProgramPath, exeInfo);
                    }
                    else
                    {
                        return exeInfo.Directory.FullName;
                    }
                }

                LogTools.LogWarning(warningMsg + "; cannot determine the working directory for " + programInfo.UniqueKey);
                return programInfo.WorkDir;
            }
            catch (Exception ex)
            {
                LogTools.LogWarning(string.Format("Error determining the working directory for {0}: {1}", programInfo.UniqueKey, ex.Message));
                return programInfo.WorkDir;
            }

        }

        /// <summary>
        /// If path1 and path2 are not an exact match
        /// </summary>
        /// <param name="fileOrDirectoryPath"></param>
        /// <param name="pathInfo"></param>
        private void LogPathChangeInfo(string fileOrDirectoryPath, FileSystemInfo pathInfo)
        {
            if (!StringsMatch(fileOrDirectoryPath, pathInfo.FullName))
                LogTools.LogMessage(String.Format("Note that {0} resolves to {1} on this system", fileOrDirectoryPath, pathInfo.FullName));

        }
        /// <summary>
        /// Start program as external process and monitor its state.
        /// </summary>
        private void ProcessThread()
        {
            const int REPEAT_HOLDOFF_SLEEP_TIME_MSEC = 1000;

            LogTools.LogMessage("Thread started: " + KeyName);

            while (true)
            {
                if (mThreadStopCommand)
                    break;

                if (mUpdateRequired)
                {
                    // Parameters have changed; update them
                    mUpdateRequired = false;

                    ThreadState = eThreadState.Idle;
                    UpdateThreadParameters(false);
                }

                if (ThreadState == eThreadState.ProcessStarting)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(mProgramInfo.ProgramPath))
                        {
                            LogTools.LogError("Error running process '" + KeyName + "': empty program path");
                            ThreadState = eThreadState.ProcessBroken;
                            return;
                        }

                        mProcess.StartInfo.FileName = mProgramInfo.ProgramPath;

                        var workingDirectory = DetermineWorkDir(mProgramInfo);

                        mProcess.StartInfo.WorkingDirectory = workingDirectory;

                        mProcess.StartInfo.Arguments = mProgramInfo.ProgramArguments;
                        mProcess.StartInfo.CreateNoWindow = CreateNoWindow;

                        if (mProcess.StartInfo.CreateNoWindow)
                            mProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        else
                            mProcess.StartInfo.WindowStyle = WindowStyle;

                        if (mProgramInfo.DelaySeconds > 0 && !mInitialDelayApplied)
                        {
                            LogTools.LogMessage(string.Format(
                                                    "Delaying {0} seconds before starting {1}",
                                                    mProgramInfo.DelaySeconds, mProgramInfo.UniqueKey));

                            ConsoleMsgUtils.SleepSeconds(mProgramInfo.DelaySeconds);
                        }
                        mInitialDelayApplied = true;

                        mProcess.Start();
                        ThreadState = eThreadState.ProcessRunning;
                        PID = mProcess.Id;
                        LogTools.LogMessage(string.Format("Started: {0}, pID={1}", KeyName, PID));

                        if (!mWorkDirLogged)
                        {
                            LogTools.LogMessage(string.Format("Working directory for {0} is {1}", KeyName, mProcess.StartInfo.WorkingDirectory));
                            mWorkDirLogged = true;
                        }

                        while (!(mThreadStopCommand || mProcess.HasExited))
                        {
                            mProcess.WaitForExit(mMonitorIntervalMsec);
                        }

                        if (mThreadStopCommand)
                        {
                            LogTools.LogMessage("Stopped: " + KeyName);
                            break;
                        }

                        if (StringsMatch(mProgramInfo.RepeatMode, "Repeat"))
                        {
                            LogTools.LogMessage("Waiting: " + KeyName);
                        }
                        else
                        {
                            LogTools.LogMessage("Stopped: " + KeyName);
                        }

                    }
                    catch (Exception ex)
                    {
                        LogTools.LogError("Error running process '" + KeyName + "'", ex);
                        ThreadState = eThreadState.ProcessBroken;
                        return;
                    }

                    try
                    {
                        PID = 0;
                        ExitCode = mProcess.ExitCode;
                        mProcess.Close();
                        ThreadState = eThreadState.Idle;

                        if (StringsMatch(mProgramInfo.RepeatMode, "Repeat"))
                        {
                            // Process has exited; but its mode is repeat
                            // Wait for Holdoff seconds, then set ThreadState to eThreadState.ProcessStarting
                            var holdoffStartTime = DateTime.UtcNow;

                            while (true)
                            {
                                SleepMilliseconds(REPEAT_HOLDOFF_SLEEP_TIME_MSEC);

                                if (mUpdateRequired)
                                {
                                    // Update the current values for mProgramInfo.RepeatMode, mProgramInfo.HoldoffSeconds, and mProgramInfo.DelaySeconds
                                    // However, don't set mUpdateRequired to False since we're not updating the other parameters at this time
                                    UpdateThreadParameters(true);

                                    if (!StringsMatch(mProgramInfo.RepeatMode, "Repeat"))
                                        break;
                                }

                                if (DateTime.UtcNow.Subtract(holdoffStartTime).TotalSeconds >= mProgramInfo.HoldoffSeconds)
                                    break;
                            }

                            if (StringsMatch(mProgramInfo.RepeatMode, "Repeat"))
                            {
                                if (ThreadState == eThreadState.Idle)
                                    ThreadState = eThreadState.ProcessStarting;
                            }
                            else
                            {
                                ThreadState = eThreadState.Idle;
                            }

                        }
                        else
                        {
                            SleepMilliseconds(mMonitorIntervalMsec);
                        }

                    }
                    catch (ThreadAbortException)
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        return;
                    }
                    catch (Exception ex2)
                    {
                        LogTools.LogError("Error waiting to restart process '" + KeyName + "'", ex2);
                        ThreadState = eThreadState.ProcessBroken;
                        return;
                    }

                }
                else
                {
                    SleepMilliseconds(mMonitorIntervalMsec);
                }
            }

            ThreadState = eThreadState.No;
            LogTools.LogMessage("Thread stopped: " + KeyName);

        }

        /// <summary>
        /// Sleep for the specified number of milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        private static void SleepMilliseconds(int milliseconds)
        {
            ConsoleMsgUtils.SleepSeconds(milliseconds / 1000.0);
        }

        /// <summary>
        /// Compare two strings, optionally ignoring case
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="ignoreCase"></param>
        /// <returns>True if a match, false if not a match</returns>
        private static bool StringsMatch(string item1, string item2, bool ignoreCase = true)
        {
            return string.Equals(item1, item2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Update thread parameters
        /// </summary>
        /// <param name="onlyUpdateRepeatHoldoffAndDelay">When true, only update Repeat, Holdoff, and Delay</param>
        private void UpdateThreadParameters(bool onlyUpdateRepeatHoldoffAndDelay)
        {
            try
            {
                Monitor.Enter(oSync);

                if (string.IsNullOrWhiteSpace(mNewProgramInfo.ProgramPath))
                    mNewProgramInfo.ProgramPath = string.Empty;

                if (string.IsNullOrWhiteSpace(mNewProgramInfo.ProgramArguments))
                    mNewProgramInfo.ProgramArguments = string.Empty;

                if (string.IsNullOrWhiteSpace(mNewProgramInfo.RepeatMode))
                    mNewProgramInfo.RepeatMode = "No";

                if (string.IsNullOrWhiteSpace(mNewProgramInfo.WorkDir))
                    mNewProgramInfo.WorkDir = string.Empty;

                if (mNewProgramInfo.HoldoffSeconds < 1)
                    mNewProgramInfo.HoldoffSeconds = 1;

                // Make sure the first letter of RepeatMode is capitalized and the other letters are lowercase
                mNewProgramInfo.RepeatMode = CapitalizeMode(mNewProgramInfo.RepeatMode);

                if (!onlyUpdateRepeatHoldoffAndDelay)
                {
                    if (string.IsNullOrWhiteSpace(mNewProgramInfo.ProgramPath))
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to empty program name");
                    }

                    var exeFileInfo = new FileInfo(mNewProgramInfo.ProgramPath);
                    if (!exeFileInfo.Exists)
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to missing program file: " + mNewProgramInfo.ProgramPath);
                        LogPathChangeInfo(mNewProgramInfo.ProgramPath, exeFileInfo);
                    }
                }

                if (StringsMatch(mNewProgramInfo.RepeatMode, "Repeat") ||
                    StringsMatch(mNewProgramInfo.RepeatMode, "Once") ||
                    StringsMatch(mNewProgramInfo.RepeatMode, "No"))
                {
                    mWarnedInvalidRepeatMode = false;
                }
                else
                {
                    if (onlyUpdateRepeatHoldoffAndDelay)
                    {
                        // Only updating the Repeat and Holdoff values
                        // Log the error (if not yet logged)
                        if (!mWarnedInvalidRepeatMode)
                        {
                            mWarnedInvalidRepeatMode = true;
                            LogTools.LogError("Invalid \"run\" value for process '" + KeyName + "': " + mNewProgramInfo.RepeatMode + "; valid values are Repeat, Once, and No");
                        }

                        mNewProgramInfo.RepeatMode = mProgramInfo.RepeatMode;
                    }
                    else
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to incorrect \"run\" value of '" + mNewProgramInfo.RepeatMode + "'; valid values are Repeat, Once, and No");
                    }
                }

                if (!onlyUpdateRepeatHoldoffAndDelay)
                {
                    mInitialDelayApplied = false;
                    mWorkDirLogged = false;
                    if (ThreadState == eThreadState.Idle)
                    {
                        if (StringsMatch(mNewProgramInfo.RepeatMode, "Repeat"))
                        {
                            ThreadState = eThreadState.ProcessStarting;
                        }
                        else if (StringsMatch(mNewProgramInfo.RepeatMode, "Once"))
                        {
                            if (!string.Equals(mProgramInfo.ProgramPath, mNewProgramInfo.ProgramPath))
                            {
                                ThreadState = eThreadState.ProcessStarting;
                            }
                            else if (!string.Equals(mProgramInfo.ProgramArguments, mNewProgramInfo.ProgramArguments))
                            {
                                ThreadState = eThreadState.ProcessStarting;
                            }
                            else
                            {
                                if (StringsMatch(mProgramInfo.RepeatMode, "No") || StringsMatch(mProgramInfo.RepeatMode, "Repeat"))
                                {
                                    ThreadState = eThreadState.ProcessStarting;
                                }
                                else
                                {
                                    if (mProgramInfo.HoldoffSeconds != mNewProgramInfo.HoldoffSeconds)
                                    {
                                        ThreadState = eThreadState.ProcessStarting;
                                    }
                                }
                            }
                        }
                    }
                }

                mProgramInfo.ProgramPath = mNewProgramInfo.ProgramPath;
                mProgramInfo.ProgramArguments = mNewProgramInfo.ProgramArguments;
                mProgramInfo.WorkDir = mNewProgramInfo.WorkDir;

                mProgramInfo.RepeatMode = mNewProgramInfo.RepeatMode;
                mProgramInfo.HoldoffSeconds = mNewProgramInfo.HoldoffSeconds;
                mProgramInfo.DelaySeconds = mNewProgramInfo.DelaySeconds;

                ExitCode = 0;
                Monitor.Exit(oSync);
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Error updating thread parameters: " + ex.Message);
            }
        }

    }
}
