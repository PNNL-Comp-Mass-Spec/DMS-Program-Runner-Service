using PRISM.FileProcessor;
using PRISM.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ProgRunnerSvc
{
    internal class SelfUpdater : IDisposable
    {
        // Ignore Spelling: workdir

        // Delay 30 seconds when a file update is detected, to allow plenty of time to copy all dependencies too.
        private const int FileUpdateDelayMsec = 30000;

        // TODO: Pull this value from some config file instead
        private const string SelfUpdateScriptRelativePath = "Scripts\\ProgRunnerSvcUpdater.bat";
        private const string SelfUpdateFailedFileName = "UpdateFailed.txt";

        private readonly string mSelfUpdateScriptFilePath = string.Empty;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FileSystemWatcher mFileWatcher;

        // When the service executable file in the update path is changed, mUpdateService is set to True and mUpdateServiceRequestTime is set to the current date/time
        // Timer looks for mServiceUpdateTimer being true, and after 30 seconds has elapsed, it calls CheckForUpdate
        private bool mUpdateService;

        private DateTime mUpdateServiceRequestTime;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly System.Timers.Timer mServiceUpdateTimer;

        private readonly FileInfo mAppFileInfo;
        private readonly Version mAppVersion;
        private readonly string mUpdateExePath;
        private readonly string mUpdateFailedPath;

        public bool UpdateAvailable { get; private set; }
        public bool UpdateStarted { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SelfUpdater()
        {
            try
            {
                mAppFileInfo = new FileInfo(ProcessFilesOrDirectoriesBase.GetAppPath());
                if (mAppFileInfo.DirectoryName == null)
                    throw new DirectoryNotFoundException("Cannot determine the parent directory of " + mAppFileInfo.FullName);

                mAppVersion = Assembly.GetExecutingAssembly().GetName().Version;

                mSelfUpdateScriptFilePath = Path.Combine(mAppFileInfo.DirectoryName, SelfUpdateScriptRelativePath);
                mUpdateFailedPath = Path.Combine(mAppFileInfo.DirectoryName, SelfUpdateFailedFileName);

                if (!File.Exists(mSelfUpdateScriptFilePath))
                {
                    LogTools.LogWarning("Self-update " + mSelfUpdateScriptFilePath);
                    LogTools.LogMessage("Self-Updates will be disabled");
                    LogTools.FlushPendingMessages();

                    return;
                }

                var updatePath = Path.Combine(mAppFileInfo.DirectoryName, "Update");

                if (!Directory.Exists(updatePath))
                {
                    try
                    {
                        Directory.CreateDirectory(updatePath);
                    }
                    catch (Exception)
                    {
                        LogTools.LogWarning("Self-update 'Update' folder is missing and could not be created.");
                        LogTools.LogMessage("Self-Updates will be disabled");
                        LogTools.FlushPendingMessages();

                        return;
                    }
                }

                mUpdateExePath = Path.Combine(updatePath, mAppFileInfo.Name);

                // Set up the FileWatcher to detect setup file changes
                mFileWatcher = new FileSystemWatcher();
                mFileWatcher.BeginInit();
                mFileWatcher.Path = updatePath;
                mFileWatcher.IncludeSubdirectories = true;
                mFileWatcher.Filter = mAppFileInfo.Name;
                mFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                mFileWatcher.EndInit();
                mFileWatcher.EnableRaisingEvents = true;

                mFileWatcher.Changed += FileWatcher_Changed;

                mUpdateServiceRequestTime = DateTime.UtcNow;
                mServiceUpdateTimer = new System.Timers.Timer(1000);
                mServiceUpdateTimer.Elapsed += ServiceFileUpdateTimer_Elapsed;
                mServiceUpdateTimer.Start();

                CheckForUpdate();
            }
            catch (Exception ex)
            {
                LogTools.LogError("Failed to initialize SelfUpdater", ex);
            }
        }

        public void DoUpdate()
        {
            if (UpdateStarted)
            {
                return;
            }

            UpdateStarted = true;

            var startInfo = new ProcessStartInfo("cmd.exe", $"/c \"{mSelfUpdateScriptFilePath}\"")
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private void CheckForUpdate()
        {
            var fi = new FileInfo(mUpdateExePath);
            if (!fi.Exists)
            {
                // No updated service exe available, so don't update.
                UpdateAvailable = false;
                return;
            }

            var failedFi = new FileInfo(mUpdateFailedPath);
            if (failedFi.Exists && failedFi.LastWriteTimeUtc > fi.LastWriteTimeUtc)
            {
                // A previous update failed, and the updated exe is not newer than the timestamp on the flag file.
                UpdateAvailable = false;
                return;
            }

            var fileVersion = FileVersionInfo.GetVersionInfo(fi.FullName);
            var updateVersion = new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart, fileVersion.FilePrivatePart);

            UpdateAvailable = fi.Length != mAppFileInfo.Length || !mAppVersion.Equals(updateVersion);
            if (UpdateAvailable)
            {
                LogTools.LogMessage("Service update found: Current: {0} ({1}), new: {2} ({3})", mAppVersion, mAppFileInfo.Length, updateVersion, fi.Length);
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
                mServiceUpdateTimer?.Dispose();
                mFileWatcher?.Dispose();
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            mUpdateService = true;
            mUpdateServiceRequestTime = DateTime.UtcNow;
        }

        private void ServiceFileUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (mUpdateService && DateTime.UtcNow.Subtract(mUpdateServiceRequestTime).TotalMilliseconds >= FileUpdateDelayMsec)
            {
                mUpdateService = false;
                CheckForUpdate();
            }
        }
    }
}
