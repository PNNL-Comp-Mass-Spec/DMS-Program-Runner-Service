#!/bin/bash

cd /opt/DMS_Programs/MultiProgRunnerSvc/
/usr/bin/flock -n /tmp/DMSProgRunner.lockfile /usr/bin/mono ProgRunnerApp.exe

