# DMS Program Runner

The DMS Program Runner (MultiProgRunner) is a Windows Service that can be configured
to start specific programs on a regular interval. In the Proteomics Research Information and Management System (PRISM)
the DMS Program runner Service is used to run manager programs at regular intervals.

## Installation

```
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\installutil ProgRunnerSvc.exe
```

Once installed, configure the user to run the service as (for example svc-dms).
Next, set the service to start automatically.

## Updating

To update the DMS Program Runner to a new version, stop the service, 
update the executable and PRISM.dll, then start the service.

## Uninstalling

```
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\installutil /u ProgRunnerSvc.exe
```

## Example Configuration File

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sections>
  <section name="logging">
    <item key="logfilename" value="C:\DMS_Programs\MultiProgRunnerSvc\Logs\ProgRunner" />
  </section>
  <section name="programs">
    <item key="AnalysisToolManager1" value="C:\DMS_Programs\AnalysisToolManager1\StartManager1.bat" arguments="" run="Repeat" holdoff="90" />
    <item key="AnalysisToolManager2" value="C:\DMS_Programs\AnalysisToolManager2\StartManager2.bat" arguments="" run="Repeat" holdoff="90" />
    
    <item key="FolderCreateMan" value="C:\DMS_Programs\FolderCreateMan\PkgFolderCreateManager.exe" arguments="" run="Repeat" holdoff="30" />
    
    <item key="InstDirScanner" value="C:\DMS_Programs\InstDirScanner\DMS_InstDirScanner.exe" arguments="" run="Repeat" holdoff="180" />

    <item key="StatusUpdate" value="C:\DMS_Programs\StatusMessageDBUpdater\StatusMessageDBUpdater.exe" arguments="" run="Repeat" holdoff="60" />
    <item key="StatusUpdateCTM" value="C:\DMS_Programs\StatusMessageDBUpdaterCTM\StatusMessageDBUpdater.exe" arguments="" run="Repeat" holdoff="60" />

    <item key="CapTaskMan" value="C:\DMS_Programs\CaptureTaskManager\StartCTM1.bat" arguments="" run="Repeat" holdoff="90" />
    <item key="CapTaskMan2" value="C:\DMS_Programs\CaptureTaskManager_2\StartCTM2.bat" arguments="" run="Repeat" holdoff="90" />
    <item key="SpaceMan" value="C:\DMS_Programs\SpaceManager\Space_Manager.exe" arguments="" run="Repeat" holdoff="3600" />
  </section>
</sections>
```

## Troubleshooting

If the DMS Program Runner has problems running programs, confirm that the RunAs command works.

```
runas /u:pnl\svc-dms cmd
Enter the password for pnl\svc-dms:
Attempting to start cmd as user "pnl\svc-dms" ...
```

If the command fails with error "Access is denied":
```
RUNAS ERROR: Unable to run - cmd
5: Access is denied.
```

Try starting the "Secondary Logon Service" and try again.
In addition, set the "Secondary Logon Service" to start automatically


## Contacts

Written by Dave Clark and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: proteomics@pnnl.gov \
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics

## License

The DMS Program Runner is licensed under the Apache License, Version 2.0; 
you may not use this program except in compliance with the License.  You may obtain 
a copy of the License at https://opensource.org/licenses/Apache-2.0
