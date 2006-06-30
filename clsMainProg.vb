Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports PRISM.Logging
Imports PRISM.Files
Imports PRISM.Processes

Public Class clsMainProg

	Private WithEvents m_FileWatcher As New FileSystemWatcher()	'abv 10/13/2003
    Private Const m_IniFileName As String = "MultiProgRunner.xml" 'abv 10/13/2003
    Private m_IniFileChanged As Boolean = False 'abv 10/13/2003
    Private m_FileChangedAt As DateTime = DateTime.Now ' jee
    ' This timer checks for INI file changes - jee
    Private m_Timer As System.Threading.Timer = New System.Threading.Timer(AddressOf TimerExpired, Nothing, 5000, 5000) ' jee
	Private m_LogFileName As String	 'dac added
	Private m_IniFileNamePath As String	 'dac added

	' list of active program runners
	Private m_prs As New ArrayList()
	Private m_prToDelete As New ArrayList()	'abv a list of programs to be deleted from m_prs list
    Private mLogger As clsQueLogger 'jee made logger presistent over the whole life of the service

	' start all prog runners after load?
	Dim m_autoStart As Boolean = False

    '====[prog runner list utilities]===================================

	Private Function CheckProgRunnerList(ByVal name As String) As clsProgRunner
		Dim pr As clsProgRunner
		CheckProgRunnerList = Nothing
		For Each pr In m_prs
			If pr.Name = name Then
				CheckProgRunnerList = pr
			End If
		Next
    End Function


    ' abv: '====[MarkProgRunners ( true or false)]===================================
    Private Sub MarkProgRunners(ByVal value As Boolean)
        Dim pr As clsProgRunner
        For Each pr In m_prs
            pr.ProgMark = value
        Next
    End Sub

    ' abv: DeleteNonexistentProgRunners
    Private Sub DeleteNonexistentProgRunners()
        Dim pr As clsProgRunner
        For Each pr In m_prs
            If pr.ProgMark = False Then
                m_prToDelete.Add(pr)   ' pr will be remove from the array list
            End If
        Next
        For Each pr In m_prToDelete
            m_prs.Remove(pr)
        Next
        m_prToDelete.Clear()
    End Sub

    ' create a new instance of prog runner class and add it to master list
    Private Sub MakeNewProgRunner(ByVal name As String, ByVal prog As String, ByVal args As String, ByVal repeat As String, ByVal holdOff As Double)
        Dim pr As clsProgRunner = CheckProgRunnerList(name)
        If Not pr Is Nothing Then
            Log("Program runner object already exists with name '" & name & "'")
        Else
            pr = New clsProgRunner
            AddHandler pr.ProgChanged, AddressOf ProgRunnerStateChanged
            UpdateProgRunner("New", pr, name, prog, args, repeat, holdOff)
            m_prs.Add(pr)
        End If
    End Sub

    ' update existing instance of prog runner class
    Private Sub UpdateProgRunner(ByVal type As String, ByVal pr As clsProgRunner, ByVal name As String, ByVal prog As String, ByVal args As String, ByVal repeat As String, ByVal holdOff As Double)
        Log(type & " " & name & ", prog->" & prog & ", args->" & args & ", run->" & repeat & ", holdOff->" & holdOff)
        pr.Name = name
        pr.Program = prog
        pr.Arguments = args
        pr.Repeat = repeat  '(repeat = "Repeat", don't run = "No", run once = "Once")
        pr.RepeatHoldOffTime = holdOff
        pr.ProgMark = True  'abv : set the progstate true
    End Sub

    ' Code not used 
    'Private Sub DeleteProgRunner(ByVal name As String)
    '    Dim pr As clsProgRunner = CheckProgRunnerList(name)
    '    If Not pr Is Nothing Then
    '        If pr.State <> 0 Then
    '            Log("Cannot delete '" & pr.Name & "' unless it is in idle state")
    '        Else
    '            Log("Delete " & pr.Name)
    '            m_prs.Remove(pr)
    '        End If
    '    End If
    'End Sub

    '====[handlers for application events]==============================

    Private Sub ProgRunnerStateChanged(ByVal obj As clsProgRunner)
        Dim s As String = "ProgRunner:" & obj.Name & ", state->" & obj.StateName & ", processID-->" & obj.PID.ToString
        Log(s)
    End Sub

    Private Sub Log(ByVal s As String)
        mLogger.PostEntry(s, ILogger.logMsgType.logNormal, True)
    End Sub

    '====[command helpers]==============================================

    Private Sub BuildProgRunnersFromFile()
        ' open ini file
        Dim ifr As IniFileReader = GetINIFileReader()

        Dim pgs As Specialized.StringCollection = ifr.AllKeysInSection("programs")

        'Dim s As String, p As String, a As String, r As String, h As String
        'abv changed:

        Dim s As String, p As String, a As String, r As String, h As String

        If pgs.Count = 0 Then
            Log("Found no program sections")
        End If

        For Each s In pgs
            p = ifr.GetIniValue("programs", s)
            a = ifr.GetCustomIniAttribute("programs", s, "arguments")
            r = ifr.GetCustomIniAttribute("programs", s, "run")
            h = ifr.GetCustomIniAttribute("programs", s, "holdoff")
            MakeNewProgRunner(s, p, a, r, h)
        Next
        '				PopulateListView()
    End Sub

    'abv : ' update existing instance of prog runner class from the file
    Private Sub UpdateProgRunnersFromFile()
        Dim pr As clsProgRunner
        Dim ifr As IniFileReader
        Dim pgs As Specialized.StringCollection
        Dim s As String, p As String, a As String, r As String, h As String

        Try
            ifr = GetINIFileReader()
            pgs = ifr.AllKeysInSection("programs")
        Catch ex As Exception
            mLogger.PostError("Creating IniFileReader", ex, True)
        End Try
        MarkProgRunners(False)

        For Each s In pgs
            p = ifr.GetIniValue("programs", s)
            a = ifr.GetCustomIniAttribute("programs", s, "arguments")
            r = ifr.GetCustomIniAttribute("programs", s, "run")
            h = ifr.GetCustomIniAttribute("programs", s, "holdoff")
            pr = CheckProgRunnerList(s)
            If pr Is Nothing Then
                MakeNewProgRunner(s, p, a, r, h)
            Else
                UpdateProgRunner("Update", pr, s, p, a, r, h)
            End If
        Next
        DeleteNonexistentProgRunners()
    End Sub

    ' Not used
    'Private Sub SaveProgRunnersToFile()
    '    ' open ini file
    '    Dim ifr As IniFileReader = GetINIFileReader()

    '    ' clear section of existing items
    '    ifr.SetIniValue("programs", Nothing, Nothing)

    '    ' for each prog runner in master list - make new item and set its custom attributes
    '    Dim pr As clsProgRunner
    '    For Each pr In m_prs
    '        ifr.SetIniValue("programs", pr.Name, pr.Program)
    '        ifr.SetCustomIniAttribute("programs", pr.Name, "arguments", pr.Arguments)
    '        ifr.SetCustomIniAttribute("programs", pr.Name, "run", pr.Repeat)
    '        ifr.SetCustomIniAttribute("programs", pr.Name, "holdoff", pr.RepeatHoldOffTime.ToString)
    '    Next

    '    ' save file
    '    ifr.OutputFilename = m_IniFileNamePath
    '    ifr.Save()
    'End Sub


    ' interpret the command line arguments given in cmdArgs
    Private Sub InterpretProgramCommandLineArgs(ByVal cmdArgs As String)
        '				StatusTxt.Text = "args: " & cmdArgs
        Dim separators As String = " "
        Dim a As String
        For Each a In cmdArgs.Split(separators.ToCharArray)
            Select Case a
                Case "/autostart"
                    m_autoStart = True
            End Select
        Next
    End Sub

    ' start up all prog runners if flag set
    Public Sub StartAllProgRunners(ByVal flag As Boolean)
        If Not flag Then Exit Sub
        Dim pr As clsProgRunner
        Try
            For Each pr In m_prs
                pr.StartAndMonitorProgram()
                System.Threading.Thread.Sleep(1000)
            Next
        Catch ex As Exception
            mLogger.PostError("Failed to StartAllProgRunners", ex, True)
        End Try
    End Sub

    ' stop all prog runners
    Public Sub StopAllProgRunners(ByVal kill As Boolean)
        Dim pr As clsProgRunner
        For Each pr In m_prs
            Log("===== " & Now().ToString & ": Stopping " & pr.Name & " =====")
            pr.StopMonitoringProgram(kill)
        Next
        Log("===== " & Now().ToString & ": MultiProgRunner stopped" & " =====")
    End Sub

    '====[handlers for class events]=====================================

    Public Sub New()
        '		Dim Fi As FileInfo		'abv modify		'dac removed
        Dim fi As New FileInfo(Application.ExecutablePath)

        m_IniFileNamePath = Path.Combine(fi.DirectoryName, m_IniFileName)  'dac added
        m_LogFileName = GetLogFileName()  'dac added
        mLogger = New clsQueLogger(New clsFileLogger(m_LogFileName))
        Try
            Log("===== " & Now().ToString & ": MultiProgRunner V " & Application.ProductVersion & " started" & " =====")
            'InterpretProgramCommandLineArgs(Microsoft.VisualBasic.Command())			'Temporary hack
            BuildProgRunnersFromFile()
            '		StartAllProgRunners(m_autoStart)
            'Set up the FileWatcher to detect setup file changes
            fi = New FileInfo(Application.ExecutablePath)
            With m_FileWatcher
                .BeginInit()
                .Path = fi.DirectoryName
                .IncludeSubdirectories = False
                .Filter = m_IniFileName
                .NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.Size
                .EndInit()
                .EnableRaisingEvents = True
            End With
        Catch ex As Exception
            mLogger.PostError("Failed to initialize clsMainProg", ex, True)
        End Try
    End Sub

    Protected Overrides Sub Finalize()
        Try
            StopAllProgRunners(True)
        Catch ex As Exception
            mLogger.PostError("Failed to StopAllProgRunners", ex, True)
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    'abv 10/13/2003
    Private Sub m_FileWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles m_FileWatcher.Changed
        m_IniFileChanged = True
        ' Whenever a file changes, we get a whole stream of events, not just one. - jee
        ' This lets us wait for all the file changed events to come in before acting. - jee
        m_FileChangedAt = DateTime.Now ' - jee
    End Sub

    ' This timer checks for INI file changes and updates the ProgRunner objects - jee
    Private Sub TimerExpired(ByVal o As Object)
        If m_IniFileChanged Then
            ' Check that it has been more than one second since the last file change
            If DateTime.Now > (m_FileChangedAt.Add(New TimeSpan(0, 0, 1))) Then
                m_IniFileChanged = False
                UpdateProgRunnersFromFile()
            End If
        End If
    End Sub

    ' This routine makes all references to the INI file consistent - jee
    Private Function GetINIFileReader() As IniFileReader
        Return New IniFileReader(m_IniFileNamePath, mLogger, True)
    End Function

    Private Function GetLogFileName() As String
        'Retrieves log file name from setup file
        Return GetINIFileReader().GetIniValue("logging", "logfilename")
    End Function
End Class
