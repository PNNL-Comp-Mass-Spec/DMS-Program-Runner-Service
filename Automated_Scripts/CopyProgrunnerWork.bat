set FullPath=\\%1\DMS_Programs\MultiProgRunnerSvc

if exist %FullPath% (goto StartWork)

echo.
echo Path not found: %FullPath%
goto Done

:StartWork

echo.
echo Updating %FullPath%

if exist %FullPath%\logging.config del %FullPath%\logging.config
if exist %FullPath%\log4net.dll del %FullPath%\log4net.dll

xcopy \\Proto-2\past\Software\MultiProgRunnerSvc\Bin\ProgRunnerSvc.exe %FullPath% /D /Y
xcopy \\Proto-2\past\Software\MultiProgRunnerSvc\Bin\PRISM.dll %FullPath% /D /Y
xcopy \\Proto-2\past\Software\MultiProgRunnerSvc\Bin\README.md %FullPath% /D /Y

:Done
