using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using PRISM;
using PRISM.FileProcessor;
using PRISM.Logging;

namespace ProgRunnerSvc
{
    public class clsMainProg : IDisposable
    {
        private const int SETTINGS_FILE_UPDATE_DELAY_MSEC = 1500;

        private const string XML_PARAM_FILE_NAME = "MultiProgRunner.xml";

        private readonly string mXmlParamFilePath = string.Empty;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FileSystemWatcher mFileWatcher;

        /// <summary>
        /// Keys are the program name; values are the ProcessRunner object
        /// </summary>
        /// <remarks></remarks>
        private readonly Dictionary<string, clsProcessRunner> mProgRunners;

        // When the .XML settings file is changed, mUpdateSettingsFromFile is set to True and mUpdateSettingsRequestTime is set to the current date/time
        // Timer looks for mSettingsFileUpdateTimer being true, and after 1500 milliseconds has elapsed, it calls UpdateSettingsFromFile
        private bool mUpdateSettingsFromFile;

        private DateTime mUpdateSettingsRequestTime;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly System.Timers.Timer mSettingsFileUpdateTimer;

        /// <summary>
        /// Startup Aborted is set to True if another instance was detected
        /// </summary>
        public bool StartupAborted { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMainProg()
        {

            try
            {
                var fi = new FileInfo(ProcessFilesOrFoldersBase.GetAppPath());
                if (fi.DirectoryName == null)
                    throw new DirectoryNotFoundException("Cannot determine the parent directory of " + fi.FullName);

                mXmlParamFilePath = Path.Combine(fi.DirectoryName, XML_PARAM_FILE_NAME);

                var logFileNameBase = Path.Combine("Logs", "ProgRunner");
                LogTools.CreateFileLogger(logFileNameBase);

                var appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                LogTools.LogMessage("=== MultiProgRunner v" + appVersion + " started =====");

                if (!File.Exists(mXmlParamFilePath))
                {
                    LogTools.LogWarning("XML Parameter file not found: " + mXmlParamFilePath);
                    LogTools.LogMessage("Please create the file then restart this application or service");
                    LogTools.FlushPendingMessages();
                    ConsoleMsgUtils.SleepSeconds(1);

                    StartupAborted = true;
                    return;
                }

                var multipleProgRunnersFound = ProgRunnerAlreadyRunning(out var existingProcessId, out var existingProcessName);
                if (multipleProgRunnersFound)
                {
                    var msg = string.Format(
                        "Aborting initialization of this Program Runner because process {0} with pID {1} is already running",
                        existingProcessName, existingProcessId);

                    LogTools.LogWarning(msg);
                    LogTools.FlushPendingMessages();
                    ConsoleMsgUtils.SleepSeconds(1);

                    StartupAborted = true;
                    return;
                }

                // Set up the FileWatcher to detect setup file changes
                mFileWatcher = new FileSystemWatcher();
                mFileWatcher.BeginInit();
                mFileWatcher.Path = fi.DirectoryName;
                mFileWatcher.IncludeSubdirectories = false;
                mFileWatcher.Filter = XML_PARAM_FILE_NAME;
                mFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                mFileWatcher.EndInit();
                mFileWatcher.EnableRaisingEvents = true;

                mFileWatcher.Changed += FileWatcher_Changed;

                mProgRunners = new Dictionary<string, clsProcessRunner>();
                mProgRunners.Clear();

                mUpdateSettingsRequestTime = DateTime.UtcNow;
                mSettingsFileUpdateTimer = new System.Timers.Timer(250);
                mSettingsFileUpdateTimer.Elapsed += SettingsFileUpdateTimer_Elapsed;
                mSettingsFileUpdateTimer.Start();

            }
            catch (Exception ex)
            {
                LogTools.LogError("Failed to initialize clsMainProg", ex);
            }
        }

        public void StartAllProgRunners()
        {
            UpdateProgRunnersFromFile(false);
        }

        private void UpdateProgRunnersFromFile(bool passXMLFileParsingExceptionsToCaller)
        {
            List<clsProcessSettings> programSettings;

            try
            {
                if (string.IsNullOrWhiteSpace(mXmlParamFilePath))
                    return;

                programSettings = GetProgRunnerSettings(mXmlParamFilePath);
            }
            catch (Exception ex)
            {
                if (passXMLFileParsingExceptionsToCaller)
                    throw;

                LogTools.LogError("Error reading parameter file '" + mXmlParamFilePath + "'", ex);
                return;
            }

            LogTools.LogMessage("Updating from file");

            // Make a list of the currently running prog runners
            // Keys are the UniqueKey for each prog runner, value is initially False but is set to true for each manager processed
            var progRunners = new Dictionary<string, bool>();

            foreach (var uniqueProgramKey in mProgRunners.Keys)
            {
                progRunners.Add(uniqueProgramKey, false);
            }

            var threadsProcessed = 0;
            var oRandom = new Random();

            foreach (var settingsEntry in programSettings)
            {

                threadsProcessed += 1;

                var uniqueProgramKey = settingsEntry.UniqueKey;
                if (string.IsNullOrWhiteSpace(uniqueProgramKey))
                {
                    LogTools.LogError("Ignoring empty program key in the Programs section");
                    continue;
                }

                try
                {
                    if (!mProgRunners.ContainsKey(uniqueProgramKey))
                    {
                        // New entry
                        var oCProcessRunner = new clsProcessRunner(settingsEntry);

                        progRunners.Add(uniqueProgramKey, true);

                        mProgRunners.Add(uniqueProgramKey, oCProcessRunner);
                        LogTools.LogMessage(string.Format(
                                                "Added program '{0}': {1} {2}",
                                                uniqueProgramKey, settingsEntry.ProgramPath, settingsEntry.ProgramArguments));

                        if (threadsProcessed < programSettings.Count)
                        {
                            // Delay between 1 and 2 seconds before continuing
                            // We do this so that the ProgRunner doesn't start a bunch of processes all at once
                            var delayTimeSec = 1 + oRandom.NextDouble();
                            ConsoleMsgUtils.SleepSeconds(delayTimeSec);
                        }

                    }
                    else
                    {
                        // Updated entry
                        var oCProcessRunner = mProgRunners[uniqueProgramKey];
                        oCProcessRunner.UpdateProcessParameters(settingsEntry);
                        progRunners[uniqueProgramKey] = true;

                        LogTools.LogMessage("Updated program '" + uniqueProgramKey + "'");
                    }
                }
                catch (Exception ex)
                {
                    LogTools.LogError("Error in UpdateProgRunnersFromFile updating process '" + uniqueProgramKey + "': " + ex.Message);
                }

            }

            try
            {
                // Remove disappeared processes
                var processesToStop = new List<string>();

                foreach (var progRunnerEntry in mProgRunners)
                {
                    if (progRunners.TryGetValue(progRunnerEntry.Key, out var enabled))
                    {
                        if (!enabled)
                        {
                            processesToStop.Add(progRunnerEntry.Key);
                        }
                    }
                }

                foreach (var uniqueProgramKey in processesToStop)
                {
                    mProgRunners[uniqueProgramKey].StopThread();
                    mProgRunners.Remove(uniqueProgramKey);
                    LogTools.LogMessage("Deleted program '" + uniqueProgramKey + "'");
                }

            }
            catch (Exception ex)
            {
                LogTools.LogError("Error in UpdateProgRunnersFromFile removing old processes: " + ex.Message);
            }

        }

        public void StopAllProgRunners()
        {
            foreach (var progRunnerName in mProgRunners.Keys)
            {
                mProgRunners[progRunnerName].StopThread();
            }

            mProgRunners.Clear();
            LogTools.LogMessage("MultiProgRunner stopped");
        }

        private List<clsProcessSettings> GetProgRunnerSettings(string xmlFilePath)
        {

            var programSettings = new List<clsProcessSettings>();

            var sectionName = "";

            if (string.IsNullOrWhiteSpace(xmlFilePath) || !File.Exists(xmlFilePath))
                return programSettings;

            using (var fileStream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = XmlReader.Create(fileStream))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "section")
                            {
                                try
                                {
                                    sectionName = reader.GetAttribute("name");
                                }
                                catch (Exception ex)
                                {
                                    // Section element doesn't have a "name" attribute; set sectionName to ""
                                    sectionName = string.Empty;

                                    LogTools.LogError("Error parsing XML Config file: " + ex.Message);
                                }

                                if (sectionName == null)
                                {
                                    sectionName = string.Empty;
                                }

                                continue;
                            }

                            // Expected format:
                            //  <section name="programs">
                            //    <item key="Analysis1" value="C:\DMS_Programs\AnalysisToolManager1\StartManager1.bat" arguments="" run="Repeat" holdoff="300" />

                            if (reader.Depth == 2 && sectionName == "programs" && reader.Name == "item")
                            {

                                var keyName = "";

                                try
                                {
                                    keyName = reader.GetAttribute("key");

                                    if (string.IsNullOrWhiteSpace(keyName))
                                    {
                                        LogTools.LogError("Empty key name; ignoring entry");
                                    }

                                    var oProgramSettings = new clsProcessSettings(keyName)
                                    {
                                        ProgramPath = GetAttributeSafe(reader, "value"),
                                        ProgramArguments = GetAttributeSafe(reader, "arguments"),
                                        RepeatMode = GetAttributeSafe(reader, "run", "Once"),
                                        WorkDir = GetAttributeSafe(reader, "workdir", "")
                                    };

                                    var holdOffSecondsText = GetAttributeSafe(reader, "holdoff", "300");

                                    if (!int.TryParse(holdOffSecondsText, out var holdOffSeconds))
                                    {
                                        LogTools.LogError("Invalid \"holdoff\" value for process '" + keyName + "': " + holdOffSecondsText +
                                                          "; this value must be an integer (defining the holdoff time, in seconds).  Will assume 300");
                                        holdOffSeconds = 300;
                                    }

                                    var delaySecondsText = GetAttributeSafe(reader, "delay", "0");

                                    if (!int.TryParse(delaySecondsText, out var delaySeconds))
                                    {
                                        LogTools.LogError("Invalid \"delay\" value for process '" + keyName + "': " + delaySecondsText +
                                                          "; this value must be an integer (defining the delay time, in seconds, " +
                                                          "after the prog runner first starts, that it will wait to start this process).  Will assume 0");
                                        delaySeconds = 0;
                                    }

                                    oProgramSettings.DelaySeconds = delaySeconds;

                                    oProgramSettings.HoldoffSeconds = holdOffSeconds;

                                    programSettings.Add(oProgramSettings);
                                }
                                catch (Exception ex)
                                {
                                    // Ignore this entry
                                    LogTools.LogError("Error parsing XML Config file for key " + keyName + ": " + ex.Message);
                                }
                            }
                            break;

                        case XmlNodeType.EndElement:

                            if (reader.Name == "section")
                                sectionName = string.Empty;

                            break;

                    }

                }
            }

            return programSettings;
        }

        private string GetAttributeSafe(XmlReader reader, string attributeName)
        {
            return GetAttributeSafe(reader, attributeName, string.Empty);
        }

        private string GetAttributeSafe(XmlReader reader, string attributeName, string defaultValue)
        {
            try
            {
                var value = reader.GetAttribute(attributeName);
                return value ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private bool ProgRunnerAlreadyRunning(out int existingProcessId, out string existingProcessName)
        {
            const int MAX_ATTEMPTS = 3;
            const int DELAY_TIME_SECONDS = 2;

            var iteration = 0;
            while (true)
            {
                iteration++;

                var multipleProgRunnersFound = ProgRunnerAlreadyRunningWork(out existingProcessId, out existingProcessName);
                if (!multipleProgRunnersFound)
                    return false;

                var currentProcess = Process.GetCurrentProcess();
                if (currentProcess.Id >= existingProcessId || iteration >= MAX_ATTEMPTS)
                    return true;

                // Wait 2 seconds, then check again
                // This is done to handle cases where two program runners start at the same time

                ConsoleMsgUtils.ShowWarning(string.Format(
                                                "Multiple instances of the Program Runner were found; " +
                                                "waiting {0} seconds then checking again since this instance's ProgramID ({1}) is less than {2}",
                                                DELAY_TIME_SECONDS, currentProcess.Id, existingProcessId));

                ConsoleMsgUtils.SleepSeconds(DELAY_TIME_SECONDS);
            }

        }

        /// <summary>
        /// Examine all running processes to see if the ProgRunner is already running
        /// </summary>
        /// <returns>True if multiple program runners are found, otherwise false</returns>
        private bool ProgRunnerAlreadyRunningWork(out int existingProcessId, out string existingProcessName)
        {
            existingProcessId = 0;
            existingProcessName = string.Empty;

            var sysInfo = new SystemInfo();

            // Check running processes to assure that only one instance of the ProgRunner is running at a given time
            var currentProcesses = sysInfo.GetProcesses();

            var progRunnerInstances = new List<ProcessInfo>();

            try
            {

                var appPath = ProcessFilesOrFoldersBase.GetAppPath();
                var exeName = Path.GetFileName(appPath);

                foreach (var process in currentProcesses.Values)
                {

                    if (string.Equals(process.ExeName, exeName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Matched the exe name of the process to exeName
                        progRunnerInstances.Add(process);
                    }
                    else if (string.Equals(process.ExeName, "mono", StringComparison.OrdinalIgnoreCase) &&
                             exeName != null &&
                             process.ArgumentList.Count > 0 &&
                             process.ArgumentList[0].IndexOf(exeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Running mono, and the first argument contains exeName
                        progRunnerInstances.Add(process);
                    }

                    // Uncomment to see details of each process
                    // Console.WriteLine(process.ToStringVerbose());

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error examining running processes: " + ex.Message);
            }

            if (progRunnerInstances.Count <= 1)
            {
                return false;
            }


            // Abort starting this ProgRunner since another instance is already running
            var currentProcess = Process.GetCurrentProcess();

            foreach (var runningProcess in progRunnerInstances)
            {
                if (runningProcess.ProcessID != currentProcess.Id)
                {
                    existingProcessId = runningProcess.ProcessID;
                    if (string.Equals(runningProcess.ProcessName, "mono", StringComparison.OrdinalIgnoreCase) && runningProcess.ArgumentList.Count > 0)
                        existingProcessName = string.Format("\"{0} {1}\"", runningProcess.ProcessName, runningProcess.ArgumentList[0]);
                    else
                        existingProcessName = runningProcess.ProcessName;

                    break;
                }
            }

            return true;

        }

        private void UpdateSettingsFromFile()
        {

            const int MAX_READ_ATTEMPTS = 3;

            for (var iteration = 1; iteration <= MAX_READ_ATTEMPTS; iteration++)
            {

                LogTools.LogMessage("File changed");

                // When file was written program gets few events.
                // During some events XML reader can't open file. So use try-catch
                try
                {
                    UpdateProgRunnersFromFile(true);
                    break;
                }
                catch (Exception ex)
                {

                    if (iteration < MAX_READ_ATTEMPTS)
                        LogTools.LogError("Error reading XML file (will try again): " + ex.Message);
                    else
                        LogTools.LogError(string.Format("Error reading XML file (tried {0} times): {1}", MAX_READ_ATTEMPTS, ex.Message));

                }

                ConsoleMsgUtils.SleepSeconds(1);

            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    StopAllProgRunners();
                }
                catch (Exception ex)
                {
                    LogTools.LogError("Failed to StopAllProgRunners", ex);
                }
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            mUpdateSettingsFromFile = true;
            mUpdateSettingsRequestTime = DateTime.UtcNow;
        }

        private void SettingsFileUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (mUpdateSettingsFromFile)
            {
                if (DateTime.UtcNow.Subtract(mUpdateSettingsRequestTime).TotalMilliseconds >= SETTINGS_FILE_UPDATE_DELAY_MSEC)
                {
                    mUpdateSettingsFromFile = false;
                    UpdateSettingsFromFile();
                }
            }
        }

    }
}
