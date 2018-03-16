﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
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

        private readonly string mIniFileNamePath = string.Empty;

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
        /// Constructor
        /// </summary>
        public clsMainProg()
        {

            try
            {
                var fi = new FileInfo(ProcessFilesOrFoldersBase.GetAppPath());
                if (fi.DirectoryName == null)
                    throw new DirectoryNotFoundException("Cannot determine the parent directory of " + fi.FullName);

                mIniFileNamePath = Path.Combine(fi.DirectoryName, XML_PARAM_FILE_NAME);

                const string logFileNameBase = @"Logs\ProgRunner";
                LogTools.CreateFileLogger(logFileNameBase);

                var appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                LogTools.LogMessage("=== MultiProgRunner v" + appVersion + " started =====");

                if (!File.Exists(mIniFileNamePath))
                {
                    LogTools.LogWarning("XML Parameter file not found: " + mIniFileNamePath);
                    LogTools.LogMessage("Settings will be loaded once the XML parameter file is created");
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
                if (string.IsNullOrWhiteSpace(mIniFileNamePath))
                    return;

                programSettings = GetProgRunnerSettings(mIniFileNamePath);
            }
            catch (Exception ex)
            {
                if (passXMLFileParsingExceptionsToCaller)
                    throw;

                LogTools.LogError("Error reading parameter file '" + mIniFileNamePath + "'", ex);
                return;
            }

            LogTools.LogMessage("Updating from file");

            // Make a list of the currently running progrunners
            // Keys are the UniqueKey for each progrunner, value is initially False but is set to true for each manager processed
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
                        LogTools.LogMessage("Added program '" + uniqueProgramKey + "'");

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

        private List<clsProcessSettings> GetProgRunnerSettings(string iniFilePath)
        {

            var programSettings = new List<clsProcessSettings>();

            var sectionName = "";

            if (string.IsNullOrWhiteSpace(iniFilePath) || !File.Exists(iniFilePath))
                return programSettings;

            using (var reader = XmlReader.Create(new FileStream(iniFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
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
                                        RepeatMode = GetAttributeSafe(reader, "run", "Once")
                                    };

                                    var holdOffSecondsText = GetAttributeSafe(reader, "holdoff", "10");

                                    if (!int.TryParse(holdOffSecondsText, out var holdOffSeconds))
                                    {
                                        LogTools.LogError("Invalid \"Holdoff\" value for process '" + keyName + "': " + holdOffSecondsText +
                                                          "; this value must be an integer (defining the holdoff time, in seconds).  Will assume 300");
                                        holdOffSeconds = 300;
                                    }

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