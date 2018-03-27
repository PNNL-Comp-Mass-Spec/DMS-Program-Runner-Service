# As described at http://prismwiki.pnl.gov/wiki/Remote_Server_Management
# you must run this from one of these servers (login as user memadmin or svc-dms and start powershell as an Administrator)
# 130.20.225.2  Gigasax
# 130.20.225.3	Proteinseqs
# 130.20.225.10	Pogo
# 130.20.225.11	Roadrunner
# 130.20.225.12	Daffy
#
# Syntax for overriding the defaults
# .\ParallelExecuteScript.ps1 -MaxThreads 90 -ScriptFile .\StopProgRunner.ps1 -ComputerList .\Computers.txt
#

Param($ScriptFile = $(Read-Host "Enter the script file"),
    $ComputerList = $(Read-Host "Enter the Location of the computerlist"),
    $MaxThreads = 100,
    $SleepTimer = 500,
    $MaxWaitAtEnd = 600,
    $OutputType = "Text")       # "Text" or "GridView"

# Populate an array using the computer names in the text file
# While reading, removing empty lines and lines that start with #
# Finally, sort the computer names
$Computers = Get-Content $ComputerList | ? {$_.trim() -ne "" } | ? {$_.trim().Substring(0,1) -ne "#" } | sort -uniq

"Killing existing jobs . . ."
Get-Job | Remove-Job -Force
"Done."
 
$i = 0
$ExecutionStart = (Get-Date)
$EffectiveMaxThreads = $MaxThreads

ForEach ($Computer in $Computers) {

	If ([string]::IsNullOrEmpty($Computer))
		{continue}

	$RunningThreads = $(Get-Job -state running).count
    While ($(Get-Job -state running).count -ge $EffectiveMaxThreads){
        Write-Progress  -Activity "Creating Server List" `
                        -Status "Waiting for threads to close" `
                        -CurrentOperation "$i threads created - $($(Get-Job -state running).count) threads open" `
                        -PercentComplete ($i / $Computers.count * 100)
        Start-Sleep -Milliseconds $SleepTimer

		$ElapsedTime = NEW-TIMESPAN –Start $ExecutionStart –End (Get-Date)

		# Bump up the effective MaxThreads value as more time elapses
		$EffectiveMaxThreads = $MaxThreads + [math]::floor($ElapsedTime.TotalSeconds / 60)
    }
 
    #"Starting job - $Computer"
    $i++
    Start-Job -FilePath $ScriptFile -ArgumentList $Computer -Name $Computer | Out-Null
    Write-Progress  -Activity "Creating Server List" `
                -Status "Starting Threads" `
                -CurrentOperation "$i threads created - $($(Get-Job -state running).count) threads open" `
                -PercentComplete ($i / $Computers.count * 100)
   
}
 
$Complete = Get-date
 
While ($(Get-Job -State Running).count -gt 0){
    $ComputersStillRunning = ""
    ForEach ($System  in $(Get-Job -state running)){$ComputersStillRunning += ", $($System.name)"}
    $ComputersStillRunning = $ComputersStillRunning.Substring(2)
    Write-Progress  -Activity "Creating Server List" `
                    -Status "$($(Get-Job -State Running).count) threads remaining" `
                    -CurrentOperation "$ComputersStillRunning" `
                    -PercentComplete ($(Get-Job -State Completed).count / $(Get-Job).count * 100)
    If ($(New-TimeSpan $Complete $(Get-Date)).totalseconds -ge $MaxWaitAtEnd){"Killing all jobs still running . . .";Get-Job -State Running | Remove-Job -Force}
    Start-Sleep -Milliseconds $SleepTimer
}
 
"Reading all jobs"
 
If ($OutputType -eq "Text"){
    ForEach($Job in Get-Job){
        "$($Job.Name)"
        "****************************************"
        Receive-Job $Job
        " "
    }
}
ElseIf($OutputType -eq "GridView"){
    Get-Job | Receive-Job | Select-Object * -ExcludeProperty RunspaceId | out-gridview
   
}