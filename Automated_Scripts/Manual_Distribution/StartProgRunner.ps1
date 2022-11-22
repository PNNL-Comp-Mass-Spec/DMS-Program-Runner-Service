
Param($Computer = $(Read-Host "Enter the name of the computer"))

# Valid status is running or stopped
Get-Service -name ProgRunner -ComputerName $Computer | set-service -status running
