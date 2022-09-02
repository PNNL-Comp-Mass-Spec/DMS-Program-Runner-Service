@echo off

cd C:\DMS_Programs\DMSProgramRunner\Scripts
REM Run the updater script, with the necessary parameter to ignore script signing.
REM Also pipe all output to the specified file.
powershell -ExecutionPolicy RemoteSigned .\ProgRunnerSvcUpdater.ps1 *>&1 > ..\LastUpdate.log
