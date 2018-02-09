@echo off
echo About to uninstall the DMS ProgRunner service
echo then install the latest version on Proto-2
pause

@echo on
c:
cd \dms_programs\MultiProgRunnerSvc

net stop progrunner
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\installutil /u ProgRunnerSvc.exe

del logging.config log4net.dll
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\ProgRunnerSvc.exe . /D /Y
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\PRISM.dll . /D /Y
xcopy \\proto-2\past\Software\Pub_Setup_Files\DMS_Programs\MultiProgRunnerSvc\README.md . /D /Y

C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\installutil ProgRunnerSvc.exe
