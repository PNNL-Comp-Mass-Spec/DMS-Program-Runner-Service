if exist %1 (goto StartWork)

echo.
echo Path not found: %1
goto Done

:StartWork

xcopy \\Proto-2\past\Software\MultiProgRunnerSvc\Bin\*.exe %1 /D /Y
xcopy \\Proto-2\past\Software\MultiProgRunnerSvc\Bin\*.dll %1 /D /Y

:Done