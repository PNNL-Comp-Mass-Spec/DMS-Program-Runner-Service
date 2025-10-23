@echo off

echo About to update the DMS Program Runner (aka Program Runner Service)
echo by copying to \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\DMSProgramRunner
echo Are you sure you want to continue?
if not "%1"=="NoPause" pause

@echo On

xcopy /d /y ..\bin\ProgRunnerSvc.exe \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\DMSProgramRunner\Update\
xcopy /d /y ..\bin\*.dll             \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\DMSProgramRunner\Update\

@echo off
if not "%1"=="NoPause" pause

echo The DMS Update Manager will auto-update the
echo DMS Program Runner Service on each computer,
echo provided no analysis jobs are running
