$serviceName = "progrunner"

$basePath = "C:\DMS_Programs\DMSProgramRunner"
$updatePath = Join-Path $basePath "Update"
$backupName = "Previous"
$backupPath = Join-Path $basePath $backupName
$flagFileName = "UpdateFailed.txt"
$flagFilePath = Join-Path $basePath $flagFileName
$exeName = "ProgRunnerSvc.exe"
$exePath = Join-Path $basePath $exeName

If ( -not (Test-Path -Path (Join-Path $updatePath $exeName) -PathType Leaf))
{
    Write-Output "No update file; exiting."
    Exit
}

$svc = $(Get-Service $serviceName)
#Write-Output $state
# Write-Output, OutputFile: No 'Append' to clear the existing contents
Write-Output "Test Status: Stop Service (if needed)"
If ($svc.Status -ne [ServiceProcess.ServiceControllerStatus]::Stopped -and $svc.Status -ne [ServiceProcess.ServiceControllerStatus]::StopPending)
{
    Try
    {
        Stop-Service $serviceName
        Start-Sleep -Seconds 5
    }
    Catch
    {}
}


Write-Output "Test Status: Wait for service to stop (if needed)"
While ((Get-Service $serviceName).Status -eq [ServiceProcess.ServiceControllerStatus]::StopPending)
{
    Start-Sleep -Seconds 5
}


Write-Output "Test Status: Safety check: Make sure service is now stopped, exit if not"
If ((Get-Service $serviceName).Status -ne [ServiceProcess.ServiceControllerStatus]::Stopped)
{
    # Error; service is not stopped, even after the above checks; that's an error.
    Exit
}

#sc config <service name> binPath= <binary path>
#Note: the space after binPath= is important.
# You can also query the current configuration using:
#sc qc <service name>


Write-Output "Check Service Config: get execution path"
$svcConfig = $(sc.exe qc $serviceName)

$currentExePath = ""
ForEach ($line in $svcConfig)
{
    $line = $line.Trim()
    If ($line.StartsWith("BINARY_PATH_NAME"))
    {
        #Write-Output $line
        $currentExePath = $line.Substring(16).Trim(' ', ':', '"')
        #Write-Output $currentExePath
    }
}


Write-Output "Check Service Config: update execution path (if needed)"
$pathUpdated = $false
If ( -not ($exePath.Equals($currentExePath, [StringComparison]::OrdinalIgnoreCase)))
{
    # Update the service binary path
    $pathUpdated = $true
    sc.exe config $serviceName binPath= "$($exePath)"

    # Query to check for the updated binary path
    sc.exe qc $serviceName
}


Write-Output "Update: Backup current binaries"
If (Test-Path -Path $backupPath -PathType Container)
{
    # delete the backup files, but not the folder itself
    Get-ChildItem -Path $backupPath -File | ForEach-Object { $_.Delete() }
}
Else
{
    New-Item -Path $basePath -Name $backupName -ItemType Directory
}

Get-ChildItem -Path $basePath -File | Where-Object { $_.Extension -in ".exe",".dll",".pdb" } | Move-Item -Destination $backupPath -Force


Write-Output "Update: Copy new binaries from 'Update' folder"
Get-ChildItem -Path $updatePath -File | Where-Object { $_.Extension -in ".exe",".dll",".pdb" } | Copy-Item -Destination $basePath -Force


Write-Output "Post-Update: Start service"
Try
{
    Start-Service $serviceName
    Start-Sleep -Seconds 5
}
Catch
{}


Write-Output "Post-Update: Wait for service to start (if needed)"
While ((Get-Service $serviceName).Status -eq [ServiceProcess.ServiceControllerStatus]::StartPending)
{
    Start-Sleep -Seconds 5
}


Write-Output "Post-Update: Check and handle service start failure"
If ((Get-Service $serviceName).Status -ne [ServiceProcess.ServiceControllerStatus]::Running)
{
    # Error; service is not running, even after the above checks; that's an error.
    #Restore the previous configuration...
    If ($pathUpdated)
    {
        sc.exe config $serviceName binPath= "$($currentExePath)"
    }

    If (Test-Path -Path $backupPath -PathType Container)
    {
        Get-ChildItem -Path $updatePath -File | Where-Object { $_.Extension -in ".exe",".dll",".pdb" } | Copy-Item -Destination $basePath -Force
    }

    # create flag file
    If ( -not (Test-Path -Path $flagFilePath -PathType Leaf))
    {
        # TODO: Update the timestamp on the file if it does exist.
        New-Item -ItemType "file" -Path $basePath -Name $flagFileName
    }

    Write-Output "Post-Update: Update failed, files restored, starting service..."
    # Start service again...
    Try
    {
        Start-Service $serviceName
        Start-Sleep -Seconds 5
    }
    Catch
    {}
}
ElseIf (Test-Path -Path $flagFilePath -PathType Leaf)
{
    Remove-Item -Path $flagFilePath
    # delete the backup files, but not the folder itself
    Get-ChildItem -Path $backupPath -File | ForEach-Object { $_.Delete() }
}

Write-Output "Update finished"
