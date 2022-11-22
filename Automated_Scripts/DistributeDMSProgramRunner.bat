@echo off

echo About to update the DMS Program Runner (aka Program Runner Service)
echo by copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\DMSProgramRunner
echo Are you sure you want to continue?
if not "%1"=="NoPause" pause

@echo On

xcopy /d /y ..\bin\ProgRunnerSvc.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\DMSProgramRunner\Update\
xcopy /d /y ..\bin\*.dll             \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\DMSProgramRunner\Update\

@echo off
if not "%1"=="NoPause" pause

echo The DMS Update Manager will auto-update the
echo DMS Program Runner Service on each computer,
echo provided no analysis jobs are running
