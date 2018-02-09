@echo off
echo About to update the DMS ProgRunner service
echo using the latest version on Proto-2
pause

@echo on
c:
cd \dms_programs\MultiProgRunnerSvc

net stop progrunner

if exist log4net.dll del logging.config log4net.dll
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\ProgRunnerSvc.exe . /D /Y
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\PRISM.dll . /D /Y
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\README.md . /D /Y
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\UpgradeProgrunner.bat . /D /Y

net start progrunner
