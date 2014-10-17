psservice \\proto-3 stop progrunner
psservice \\proto-4 stop progrunner
psservice \\proto-5 stop progrunner
psservice \\proto-7 stop progrunner
psservice \\proto-8 stop progrunner
psservice \\proto-9 stop progrunner
psservice \\proto-10 stop progrunner
psservice \\proto-11 stop progrunner
pause

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-3\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-4\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-5\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-7\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-8\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-9\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-10\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-11\DMS_Programs\MultiProgRunnerSvc_NET\ProgRunnerSvc.exe /Y
pause

psservice \\proto-3 start progrunner
psservice \\proto-4 start progrunner
psservice \\proto-5 start progrunner
psservice \\proto-7 start progrunner
psservice \\proto-8 start progrunner
psservice \\proto-9 start progrunner
psservice \\proto-10 start progrunner
psservice \\proto-11 start progrunner
