using System;
using System.Diagnostics;
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

        private readonly Process mProcess = new Process();
        private bool mWarnedInvalidRepeatMode;

        /// <summary>
        /// The internal thread used to run the monitoring code. That starts and monitors the external program.
        /// </summary>
        private Thread m_Thread;

        /// <summary>
        /// Flag that tells internal thread to quit monitoring external program and exit.
        /// </summary>
        private bool m_ThreadStopCommand;

        /// <summary>
        /// The interval (in milliseconds) for monitoring thread to wake up and check m_doCleanup.
        /// </summary>
        private int m_monitorInterval = 1000;

        /// <summary>
        /// Key name for this program (unique across all programs registered to run)
        /// </summary>
        public string KeyName { get; private set; }

        private clsProcessSettings m_ProgramInfo;

        private clsProcessSettings m_NewProgramInfo;

        /// <summary>
        /// Path to the program (.exe) to run
        /// </summary>
        public string ProgramPath => m_ProgramInfo.ProgramPath;

        /// <summary>
        /// Arguments to pass to the program
        /// </summary>
        public string ProgramArguments => m_ProgramInfo.ProgramArguments;

        /// <summary>
        /// Repeat mode
        /// </summary>
        /// <remarks>Valid values are Repeat, Once, and No</remarks>
        public string RepeatMode => m_ProgramInfo.RepeatMode;

        /// <summary>
        /// Holdoff time, in seconds (not milliseconds)
        /// </summary>
        public int Holdoff => m_ProgramInfo.HoldoffSeconds;

        private readonly object oSync = 1;

        private bool mUpdateRequired;

        public int MonitoringInterval
        {
            get => m_monitorInterval;
            set
            {
                if (value < 100)
                    value = 100;
                m_monitorInterval = value;
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
        public string WorkDir { get; private set; }

        /// <summary>
        /// Determine if window should be displayed
        /// </summary>
        public bool CreateNoWindow { get; private set; }

        /// <summary>
        /// Window style to use when CreateNoWindow is False
        /// </summary>
        public ProcessWindowStyle WindowStyle { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processSettings">Process settings</param>
        /// <param name="createNoWindow">True to create no window, false to use a normal window</param>
        public clsProcessRunner(clsProcessSettings processSettings, bool createNoWindow = false)
        {
            Initialize(processSettings, WorkDir, ProcessWindowStyle.Normal, createNoWindow);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processSettings">Process settings</param>
        /// <param name="workingDirectory">Working directory path</param>
        /// <param name="createNoWindow">True to create no window, false to use a normal window</param>
        public clsProcessRunner(clsProcessSettings processSettings, string workingDirectory, bool createNoWindow = false)
        {
            Initialize(processSettings, workingDirectory, ProcessWindowStyle.Normal, createNoWindow);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processSettings">Process settings</param>
        /// <param name="windowStyle">Window style</param>
        /// <param name="workingDirectory">Working directory path</param>
        /// <param name="createNoWindow">True to create no window, false to use a normal window</param>
        public clsProcessRunner(clsProcessSettings processSettings, ProcessWindowStyle windowStyle, string workingDirectory, bool createNoWindow = false)
        {
            Initialize(processSettings, workingDirectory, windowStyle, createNoWindow);
        }

        private void Initialize(clsProcessSettings processSettings, string workingDirectory, ProcessWindowStyle windowStyle, bool createNoWindow)
        {

            ThreadState = eThreadState.No;
            KeyName = processSettings.UniqueKey;
            m_ProgramInfo = new clsProcessSettings(KeyName);

            m_NewProgramInfo = processSettings;

            WorkDir = workingDirectory;
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
                m_NewProgramInfo = newProgramInfo;
                mUpdateRequired = true;
                Monitor.Exit(oSync);
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowWarning("Error updating process parameters: " + ex.Message);
            }
        }

        public void StartThread()
        {
            if (ThreadState == eThreadState.No)
            {
                try
                {
                    m_Thread = new Thread(ProcessThread);
                    m_Thread.SetApartmentState(ApartmentState.STA);
                    m_Thread.Start();
                }
                catch (Exception ex)
                {
                    ThreadState = eThreadState.ProcessBroken;
                    LogTools.LogError("Failed to create thread: " + KeyName, ex);
                }
            }

        }

        public void StopThread()
        {
            if (ThreadState == eThreadState.ProcessBroken || ThreadState == eThreadState.No)
            {
                return;
            }

            m_ThreadStopCommand = true;
            if (ThreadState == eThreadState.ProcessRunning)
            {
                LogTools.LogMessage("Try to kill process: " + KeyName);


                try
                {
                    mProcess.Kill();
                }
                //catch (System.ComponentModel.Win32Exception ex)
                //{
                //     ThrowConditionalException(ex, "Caught Win32Exception while trying to kill process.");
                //}
                //catch (System.InvalidOperationException ex)
                //{
                //     ThrowConditionalException(ex, "Caught InvalidOperationException while trying to kill thread.");
                //}
                catch (Exception ex)
                {
                    LogTools.LogWarning("Exception killing process '" + KeyName + "': " + ex.Message);
                }


                if (!mProcess.WaitForExit(m_monitorInterval))
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
                m_Thread.Join(m_monitorInterval);
            }
            catch (Exception ex)
            {
                LogTools.LogError("Failed to wait while stopping thread '" + KeyName + "'", ex);
            }

            if (m_Thread.IsAlive)
            {
                LogTools.LogMessage("Try to abort thread: " + KeyName);

                try
                {
                    m_Thread.Abort();
                }
                catch (Exception ex)
                {
                    LogTools.LogError("Failed to stop thread '" + KeyName + "'", ex);
                }


            }


        }

        private string CapitalizeMode(string modeName)
        {
            if (string.IsNullOrWhiteSpace(modeName))
                return string.Empty;

            if (modeName.Length == 1)
                return modeName.ToUpper();

            return modeName.Substring(0, 1).ToUpper() + modeName.Substring(1).ToLower();
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
                if (m_ThreadStopCommand)
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
                        if (string.IsNullOrWhiteSpace(m_ProgramInfo.ProgramPath))
                        {
                            LogTools.LogError("Error running process '" + KeyName + "': empty program path");
                            ThreadState = eThreadState.ProcessBroken;
                            return;
                        }


                        mProcess.StartInfo.FileName = m_ProgramInfo.ProgramPath;
                        mProcess.StartInfo.WorkingDirectory = WorkDir;
                        mProcess.StartInfo.Arguments = m_ProgramInfo.ProgramArguments;
                        mProcess.StartInfo.CreateNoWindow = CreateNoWindow;

                        if (mProcess.StartInfo.CreateNoWindow)
                            mProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        else
                            mProcess.StartInfo.WindowStyle = WindowStyle;

                        mProcess.Start();
                        ThreadState = eThreadState.ProcessRunning;
                        PID = mProcess.Id;
                        LogTools.LogMessage("Started: " + KeyName + ", pID=" + PID);

                        while (!(m_ThreadStopCommand || mProcess.HasExited))
                        {
                            mProcess.WaitForExit(m_monitorInterval);
                        }

                        if (m_ThreadStopCommand)
                        {
                            LogTools.LogMessage("Stopped: " + KeyName);
                            break;
                        }

                        if (m_ProgramInfo.RepeatMode == "Repeat")
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
                        if (m_ProgramInfo.RepeatMode == "Repeat")
                        {
                            // Process has exited; but its mode is repeat
                            // Wait for m_Holdoff seconds, then set ThreadState to eThreadState.ProcessStarting
                            var dtHoldoffStartTime = DateTime.UtcNow;

                            while (true)
                            {
                                Thread.Sleep(REPEAT_HOLDOFF_SLEEP_TIME_MSEC);

                                if (mUpdateRequired)
                                {
                                    // Update the current values for m_ProgramInfo.RepeatMode and m_ProgramInfo.HoldoffSeconds
                                    // However, don't set mUpdateRequired to False since we're not updating the other parameters
                                    UpdateThreadParameters(true);

                                    if (m_ProgramInfo.RepeatMode != "Repeat")
                                        break;
                                }

                                if (DateTime.UtcNow.Subtract(dtHoldoffStartTime).TotalSeconds >= m_ProgramInfo.HoldoffSeconds)
                                    break;
                            }


                            if (m_ProgramInfo.RepeatMode == "Repeat")
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
                            Thread.Sleep(m_monitorInterval);
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
                    Thread.Sleep(m_monitorInterval);
                }
            }

            ThreadState = eThreadState.No;
            LogTools.LogMessage("Thread stopped: " + KeyName);

        }

        private void UpdateThreadParameters(bool updateRepeatAndHoldoffOnly)
        {
            try
            {
                Monitor.Enter(oSync);

                if (string.IsNullOrWhiteSpace(m_NewProgramInfo.ProgramPath))
                    m_NewProgramInfo.ProgramPath = string.Empty;

                if (string.IsNullOrWhiteSpace(m_NewProgramInfo.ProgramArguments))
                    m_NewProgramInfo.ProgramArguments = string.Empty;

                if (string.IsNullOrWhiteSpace(m_NewProgramInfo.RepeatMode))
                    m_NewProgramInfo.RepeatMode = "No";

                if (m_NewProgramInfo.HoldoffSeconds < 1)
                    m_NewProgramInfo.HoldoffSeconds = 1;


                // Make sure the first letter of StrNewRepeat is capitalized and the other letters are lowercase
                m_NewProgramInfo.RepeatMode = CapitalizeMode(m_NewProgramInfo.RepeatMode);

                if (!updateRepeatAndHoldoffOnly)
                {
                    if (string.IsNullOrWhiteSpace(m_NewProgramInfo.ProgramPath))
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to empty program name");
                    }

                    if (!System.IO.File.Exists(m_NewProgramInfo.ProgramPath))
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to missing program file: " + m_NewProgramInfo.ProgramPath);
                    }
                }

                if (m_NewProgramInfo.RepeatMode == "Repeat" || m_NewProgramInfo.RepeatMode == "Once" || m_NewProgramInfo.RepeatMode == "No")
                {
                    mWarnedInvalidRepeatMode = false;
                }
                else
                {
                    if (updateRepeatAndHoldoffOnly)
                    {
                        // Only updating the Repeat and Holdoff values
                        //Log the error (if not yet logged)
                        if (!mWarnedInvalidRepeatMode)
                        {
                            mWarnedInvalidRepeatMode = true;
                            LogTools.LogError("Invalid \"run\" value for process '" + KeyName + "': " + m_NewProgramInfo.RepeatMode + "; valid values are Repeat, Once, and No");
                        }

                        m_NewProgramInfo.RepeatMode = m_ProgramInfo.RepeatMode;
                    }
                    else
                    {
                        ThreadState = eThreadState.ProcessBroken;
                        LogTools.LogError("Process '" + KeyName + "' failed due to incorrect \"run\" value of '" + m_NewProgramInfo.RepeatMode + "'; valid values are Repeat, Once, and No");
                    }
                }

                if (!updateRepeatAndHoldoffOnly)
                {
                    if (ThreadState == eThreadState.Idle)
                    {
                        if (m_NewProgramInfo.RepeatMode == "Repeat")
                        {
                            ThreadState = eThreadState.ProcessStarting;
                        }
                        else if (m_NewProgramInfo.RepeatMode == "Once")
                        {
                            if (m_ProgramInfo.ProgramPath != m_NewProgramInfo.ProgramPath)
                            {
                                ThreadState = eThreadState.ProcessStarting;
                            }
                            else if (m_ProgramInfo.ProgramArguments != m_NewProgramInfo.ProgramArguments)
                            {
                                ThreadState = eThreadState.ProcessStarting;
                            }
                            else
                            {
                                if (m_ProgramInfo.RepeatMode == "No" || m_ProgramInfo.RepeatMode == "Repeat")
                                {
                                    ThreadState = eThreadState.ProcessStarting;
                                }
                                else
                                {
                                    if (m_ProgramInfo.HoldoffSeconds != m_NewProgramInfo.HoldoffSeconds)
                                    {
                                        ThreadState = eThreadState.ProcessStarting;
                                    }
                                }
                            }
                        }
                    }
                }


                m_ProgramInfo.ProgramPath = m_NewProgramInfo.ProgramPath;
                m_ProgramInfo.ProgramArguments = m_NewProgramInfo.ProgramArguments;
                m_ProgramInfo.RepeatMode = m_NewProgramInfo.RepeatMode;
                m_ProgramInfo.HoldoffSeconds = m_NewProgramInfo.HoldoffSeconds;

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
