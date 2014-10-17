echo.
echo About to stop the progrunner service
pause

:StopManagers

psservice \\proto-3 stop progrunner
psservice \\proto-4 stop progrunner
psservice \\proto-5 stop progrunner
psservice \\proto-6 stop progrunner
psservice \\proto-7 stop progrunner
psservice \\proto-8 stop progrunner
psservice \\proto-9 stop progrunner
psservice \\proto-10 stop progrunner
psservice \\proto-11 stop progrunner

rem psservice \\seqcluster1 stop progrunner
rem psservice \\seqcluster2 stop progrunner
rem psservice \\seqcluster5 stop progrunner

psservice \\peaks1 stop progrunner
psservice \\Pub-24 stop progrunner
psservice \\Pub-26 stop progrunner
psservice \\Pub-27 stop progrunner
psservice \\Pub-28 stop progrunner
psservice \\Pub-29 stop progrunner
psservice \\Pub-30 stop progrunner
psservice \\Pub-31 stop progrunner
psservice \\Pub-32 stop progrunner
psservice \\Pub-33 stop progrunner

psservice \\Pub-36 stop progrunner
psservice \\Pub-37 stop progrunner
psservice \\Pub-38 stop progrunner
psservice \\Pub-39 stop progrunner
psservice \\Pub-40 stop progrunner
psservice \\Pub-41 stop progrunner

psservice \\Pub-44 stop progrunner
psservice \\Pub-45 stop progrunner
psservice \\Pub-46 stop progrunner
psservice \\Pub-47 stop progrunner
psservice \\Pub-48 stop progrunner
psservice \\Pub-49 stop progrunner
psservice \\Pub-50 stop progrunner
psservice \\Pub-51 stop progrunner
psservice \\Pub-52 stop progrunner
psservice \\Pub-53 stop progrunner
psservice \\Pub-54 stop progrunner
psservice \\Pub-55 stop progrunner
psservice \\Pub-56 stop progrunner
psservice \\Pub-57 stop progrunner
psservice \\Pub-58 stop progrunner
psservice \\Pub-59 stop progrunner
psservice \\Pub-60 stop progrunner
psservice \\Pub-61 stop progrunner
psservice \\Pub-62 stop progrunner
psservice \\Pub-63 stop progrunner
psservice \\Pub-64 stop progrunner
psservice \\Pub-65 stop progrunner
psservice \\Pub-66 stop progrunner
psservice \\Pub-67 stop progrunner
psservice \\Pub-68 stop progrunner
psservice \\Pub-69 stop progrunner
psservice \\Pub-70 stop progrunner
psservice \\Pub-71 stop progrunner
psservice \\Pub-72 stop progrunner
psservice \\Pub-73 stop progrunner
psservice \\Pub-74 stop progrunner
psservice \\Pub-75 stop progrunner
psservice \\Pub-76 stop progrunner
psservice \\Pub-77 stop progrunner
psservice \\Pub-78 stop progrunner
psservice \\Pub-79 stop progrunner
psservice \\Pub-80 stop progrunner
psservice \\Pub-81 stop progrunner
psservice \\Pub-82 stop progrunner
psservice \\Pub-83 stop progrunner
psservice \\Pub-84 stop progrunner
psservice \\Pub-85 stop progrunner
psservice \\Pub-86 stop progrunner
psservice \\Pub-87 stop progrunner
psservice \\Pub-88 stop progrunner
psservice \\Pub-89 stop progrunner
psservice \\Pub-90 stop progrunner
psservice \\Pub-91 stop progrunner
psservice \\Pub-92 stop progrunner
psservice \\Pub-93 stop progrunner
psservice \\Pub-94 stop progrunner
psservice \\Pub-95 stop progrunner
psservice \\Pub-96 stop progrunner
psservice \\Pub-97 stop progrunner

psservice mallard stop progrunner

echo.
echo Now wait 60 seconds for all of the progrunner instances to stop
pause

:CopyExe

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-3\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-4\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-5\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-6\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-7\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-8\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-9\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-10\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\proto-11\DMS_Programs\MultiProgRunnerSvc\ /Y

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\seqcluster1\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\seqcluster2\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\seqcluster5\DMS_Programs\MultiProgRunnerSvc\ /Y

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\peaks1\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-24\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-26\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-27\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-28\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-29\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-30\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-31\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-32\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-33\DMS_Programs\MultiProgRunnerSvc\ /Y

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-36\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-37\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-38\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-39\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-40\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-41\DMS_Programs\MultiProgRunnerSvc\ /Y

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-44\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-45\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-46\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-47\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-48\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-49\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-50\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-51\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-52\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-53\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-54\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-55\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-56\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-57\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-58\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-59\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-60\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-61\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-62\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-63\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-64\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-65\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-66\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-67\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-68\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-69\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-70\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-71\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-72\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-73\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-74\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-75\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-76\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-77\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-78\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-79\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-80\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-81\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-82\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-83\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-84\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-85\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-86\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-87\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-88\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-89\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-90\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-91\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-92\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-93\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-94\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-95\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-96\DMS_Programs\MultiProgRunnerSvc\ /Y
xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\Pub-97\DMS_Programs\MultiProgRunnerSvc\ /Y

xcopy \\Roadrunnerold\PAST\Software\MultiProgRunnerSvc\ProgRunnerSvc.exe \\mallard\DMS_Programs\MultiProgRunnerSvc\ /Y

echo.
echo About to start the progrunner service on each machine
pause

:StartManagers

psservice \\proto-3 start progrunner
psservice \\proto-4 start progrunner
psservice \\proto-5 start progrunner
psservice \\proto-6 start progrunner
psservice \\proto-7 start progrunner
psservice \\proto-8 start progrunner
psservice \\proto-9 start progrunner
psservice \\proto-10 start progrunner
psservice \\proto-11 start progrunner

psservice \\seqcluster1 start progrunner
psservice \\seqcluster2 start progrunner
psservice \\seqcluster5 start progrunner

psservice \\peaks1 start progrunner
psservice \\Pub-24 start progrunner
psservice \\Pub-26 start progrunner
psservice \\Pub-27 start progrunner
psservice \\Pub-28 start progrunner
psservice \\Pub-29 start progrunner
psservice \\Pub-30 start progrunner
psservice \\Pub-31 start progrunner
psservice \\Pub-32 start progrunner
psservice \\Pub-33 start progrunner

psservice \\Pub-36 start progrunner
psservice \\Pub-37 start progrunner
psservice \\Pub-38 start progrunner
psservice \\Pub-39 start progrunner
psservice \\Pub-40 start progrunner
psservice \\Pub-41 start progrunner

psservice \\Pub-44 start progrunner
psservice \\Pub-45 start progrunner
psservice \\Pub-46 start progrunner
psservice \\Pub-47 start progrunner
psservice \\Pub-48 start progrunner
psservice \\Pub-49 start progrunner
psservice \\Pub-50 start progrunner
psservice \\Pub-51 start progrunner
psservice \\Pub-52 start progrunner
psservice \\Pub-53 start progrunner
psservice \\Pub-54 start progrunner
psservice \\Pub-55 start progrunner
psservice \\Pub-56 start progrunner
psservice \\Pub-57 start progrunner
psservice \\Pub-58 start progrunner
psservice \\Pub-59 start progrunner
psservice \\Pub-60 start progrunner
psservice \\Pub-61 start progrunner
psservice \\Pub-62 start progrunner
psservice \\Pub-63 start progrunner
psservice \\Pub-64 start progrunner
psservice \\Pub-65 start progrunner
psservice \\Pub-66 start progrunner
psservice \\Pub-67 start progrunner
psservice \\Pub-68 start progrunner
psservice \\Pub-69 start progrunner
psservice \\Pub-70 start progrunner
psservice \\Pub-71 start progrunner
psservice \\Pub-72 start progrunner
psservice \\Pub-73 start progrunner
psservice \\Pub-74 start progrunner
psservice \\Pub-75 start progrunner
psservice \\Pub-76 start progrunner
psservice \\Pub-77 start progrunner
psservice \\Pub-78 start progrunner
psservice \\Pub-79 start progrunner
psservice \\Pub-80 start progrunner
psservice \\Pub-81 start progrunner
psservice \\Pub-82 start progrunner
psservice \\Pub-83 start progrunner
psservice \\Pub-84 start progrunner
psservice \\Pub-85 start progrunner
psservice \\Pub-86 start progrunner
psservice \\Pub-87 start progrunner
psservice \\Pub-88 start progrunner
psservice \\Pub-89 start progrunner
psservice \\Pub-90 start progrunner
psservice \\Pub-91 start progrunner
psservice \\Pub-92 start progrunner
psservice \\Pub-93 start progrunner
psservice \\Pub-94 start progrunner
psservice \\Pub-95 start progrunner
psservice \\Pub-96 start progrunner
psservice \\Pub-97 start progrunner

psservice \\Mallard start progrunner

