Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Xml
Imports System

Public Class clsMainProg
	Private Const SETTINGS_FILE_UPDATE_DELAY_MSEC As Integer = 1500

	Private Const m_IniFileName As String = "MultiProgRunner.xml"
    Private ReadOnly m_IniFileNamePath As String = String.Empty

	Private WithEvents m_FileWatcher As New FileSystemWatcher()

    ''' <summary>
    ''' Keys are the program name; values are the ProcessRunner object
    ''' </summary>
    ''' <remarks></remarks>
    Private ReadOnly m_ProgRunners As New Dictionary(Of String, clsProcessRunner)

    ' When the .XML settings file is changed, mUpdateSettingsFromFile is set to True and mUpdateSettingsRequestTime is set to the current date/time
    ' Timer looks for mSettingsFileUpdateTimer being true, and after 1500 milliseconds has elapsed, it calls UpdateSettingsFromFile
    Private mUpdateSettingsFromFile As Boolean
    Private mUpdateSettingsRequestTime As DateTime
    Private WithEvents mSettingsFileUpdateTimer As Timers.Timer

    Public Sub New()
        Dim fi As New FileInfo(Application.ExecutablePath)
        m_IniFileNamePath = Path.Combine(fi.DirectoryName, m_IniFileName)
        Try
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "MultiProgRunner v" & Application.ProductVersion & " started")
            'Set up the FileWatcher to detect setup file changes
            With m_FileWatcher
                .BeginInit()
                .Path = fi.DirectoryName
                .IncludeSubdirectories = False
                .Filter = m_IniFileName
                .NotifyFilter = NotifyFilters.LastWrite
                .EndInit()
                .EnableRaisingEvents = True
            End With
            AddHandler m_FileWatcher.Changed, AddressOf m_FileWatcher_Changed

            m_ProgRunners.Clear()

            mUpdateSettingsRequestTime = System.DateTime.UtcNow
            mSettingsFileUpdateTimer = New Timers.Timer(250)
            mSettingsFileUpdateTimer.Start()

        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to initialize clsMainProg")
        End Try
    End Sub

    Public Sub StartAllProgRunners()
        UpdateProgRunnersFromFile(False)
    End Sub

    Private Sub UpdateProgRunnersFromFile(ByVal blnPassXMLFileParsingExceptionsToCaller As Boolean)

        Dim lstProgramSettings As List(Of clsProcessSettings)

        Try
            If String.IsNullOrWhiteSpace(m_IniFileNamePath) Then Exit Sub

            lstProgramSettings = GetProcesses(m_IniFileNamePath)

        Catch ex As Exception
            If blnPassXMLFileParsingExceptionsToCaller Then
                Throw
            Else
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error reading parameter file '" & m_IniFileNamePath & "': " & ex.Message)
            End If
            Exit Sub
        End Try

        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Updating from file")

        ' Make a list of the currently running progrunners
        ' Keys are the UniqueKey for each progrunner, value is initially False but is set to true for each manager processed
        Dim lstProgRunners = New Dictionary(Of String, Boolean)
        For Each uniqueProgramKey As String In m_ProgRunners.Keys
            lstProgRunners.Add(uniqueProgramKey, False)
        Next

        Dim threadsProcessed = 0
        Dim oRandom = New Random()

        For Each oProcessSettings As clsProcessSettings In lstProgramSettings

            threadsProcessed += 1

            Dim uniqueProgramKey = oProcessSettings.UniqueKey
            If String.IsNullOrWhiteSpace(uniqueProgramKey) Then
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Ignoring empty program key in the Programs section")
                Continue For
            End If

            Try
                If Not m_ProgRunners.ContainsKey(uniqueProgramKey) Then
                    ' New entry
                    Dim oCProcessRunner As clsProcessRunner = New clsProcessRunner(oProcessSettings)
                    lstProgRunners.Add(uniqueProgramKey, True)

                    m_ProgRunners.Add(uniqueProgramKey, oCProcessRunner)
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Added program '" & uniqueProgramKey & "'")

                    If threadsProcessed < lstProgramSettings.Count Then
                        ' Delay between 1 and 2 seconds before continuing
                        ' We do this so that the ProgRunner doesn't start a bunch of processes all at once
                        Dim delayTimeMsec = oRandom.Next(1000, 2000)
                        Thread.Sleep(delayTimeMsec)
                    End If

                Else
                    ' Updated entry
                    Dim oCProcessRunner As clsProcessRunner = m_ProgRunners.Item(uniqueProgramKey)
                    oCProcessRunner.UpdateProcessParameters(oProcessSettings)
                    lstProgRunners(uniqueProgramKey) = True

                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Updated program '" & uniqueProgramKey & "'")
                End If
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error in UpdateProgRunnersFromFile updating process '" & uniqueProgramKey & "': " & ex.Message)
            End Try

        Next

        Try
            'Remove disappeared processes
            Dim lstProcessesToStop As New List(Of String)

            For Each progRunnerEntry As KeyValuePair(Of String, clsProcessRunner) In m_ProgRunners
                Dim enabled As Boolean = False
                If lstProgRunners.TryGetValue(progRunnerEntry.Key, enabled) Then
                    If Not enabled Then
                        lstProcessesToStop.Add(progRunnerEntry.Key)
                    End If
                End If                
            Next

            For Each uniqueProgramKey In lstProcessesToStop
                m_ProgRunners.Item(uniqueProgramKey).StopThread()
                m_ProgRunners.Remove(uniqueProgramKey)
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Deleted program '" & uniqueProgramKey & "'")
            Next

        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error in UpdateProgRunnersFromFile removing old processes: " & ex.Message)
        End Try

    End Sub

    Public Sub StopAllProgRunners()
        For Each oCKeyValuePair As KeyValuePair(Of String, clsProcessRunner) In m_ProgRunners
            m_ProgRunners.Item(oCKeyValuePair.Key).StopThread()
        Next
        m_ProgRunners.Clear()
        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "MultiProgRunner stopped")
    End Sub

    Protected Overrides Sub Finalize()
        Try
            StopAllProgRunners()
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to StopAllProgRunners")
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    Private Sub m_FileWatcher_Changed(ByVal sender As Object, ByVal e As FileSystemEventArgs) Handles m_FileWatcher.Changed
        mUpdateSettingsFromFile = True
        mUpdateSettingsRequestTime = DateTime.UtcNow
    End Sub

    Private Sub UpdateSettingsFromFile()
        Const MAX_READ_ATTEMPTS As Integer = 3

        For iTime As Integer = 1 To MAX_READ_ATTEMPTS
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "File changed")

            'When file was written program gets few events.
            'During some events XML reader can't open file. So use try-catch
            Try
                UpdateProgRunnersFromFile(True)
                Exit For
            Catch ex As Exception
                If iTime < MAX_READ_ATTEMPTS Then
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error reading XML file (will try again): " & ex.Message)
                Else
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error reading XML file (tried " & MAX_READ_ATTEMPTS.ToString & " times): " & ex.Message)
                End If
            End Try

            Thread.Sleep(1000)
        Next
    End Sub

    ''' <summary>
    ''' If the XML reader tries to read a file that is being updated, an error can occur
    ''' This function only has Try/Catch blocks when reading specific entries within a section
    ''' The calling function is expected to catch and handle other errors
    ''' </summary>
    ''' <param name="strIniFilePath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetProcesses(ByVal strIniFilePath As String) As List(Of clsProcessSettings)
        Dim lstProgramSettings As New List(Of clsProcessSettings)

        Dim strSectionName As String = ""

        Using oReader = XmlReader.Create(New FileStream(strIniFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

            While oReader.Read()
                Select Case oReader.NodeType
                    Case XmlNodeType.Element
                        If oReader.Name = "section" Then
                            Try
                                strSectionName = oReader.GetAttribute("name")
                            Catch ex As Exception
                                ' Section element doesn't have a "name" attribute; set strSectionName to ""
                                strSectionName = String.Empty

                                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error parsing XML Config file: " & ex.Message)
                            End Try

                            If strSectionName Is Nothing Then strSectionName = String.Empty
                            Continue While
                        End If

                        ' Expected format:
                        '   <section name="programs">
                        '     <item key="Analysis1" value="C:\DMS_Programs\AnalysisToolManager1\StartManager1.bat" arguments="" run="Repeat" holdoff="300" />

                        If oReader.Depth = 2 AndAlso strSectionName = "programs" AndAlso oReader.Name = "item" Then

                            Dim strKeyName As String = ""

                            Try

                                strKeyName = oReader.GetAttribute("key")

                                If String.IsNullOrWhiteSpace(strKeyName) Then
                                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Empty key name; ignoring entry")
                                End If

                                Dim oProgramSettings As clsProcessSettings = New clsProcessSettings(strKeyName)

                                oProgramSettings.ProgramPath = GetAttributeSafe(oReader, "value")
                                oProgramSettings.ProgramArguments = GetAttributeSafe(oReader, "arguments")
                                oProgramSettings.RepeatMode = GetAttributeSafe(oReader, "run", "Once")

                                Dim holdOffSecondsText = GetAttributeSafe(oReader, "holdoff", "10")
                                Dim holdOffSeconds As Integer

                                If Not Integer.TryParse(holdOffSecondsText, holdOffSeconds) Then
                                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Invalid ""Holdoff"" value for process '" & strKeyName & "': " & holdOffSecondsText & "; this value must be an integer (defining the holdoff time, in seconds).  Will assume 300")
                                    holdOffSeconds = 300
                                End If
                                oProgramSettings.HoldoffSeconds = holdOffSeconds

                                lstProgramSettings.Add(oProgramSettings)

                            Catch ex As Exception
                                ' Ignore this entry
                                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error parsing XML Config file for key " & strKeyName & ": " & ex.Message)
                            End Try

                            Continue While

                        End If


                    Case XmlNodeType.EndElement
                        If oReader.Name = "section" Then
                            strSectionName = String.Empty
                        End If

                End Select
            End While

        End Using

        Return lstProgramSettings

    End Function

	Private Function GetAttributeSafe(ByVal oCXmlReader As XmlReader, ByVal strAttributeName As String) As String
		Return GetAttributeSafe(oCXmlReader, strAttributeName, String.Empty)
	End Function

	Private Function GetAttributeSafe(ByVal oCXmlReader As XmlReader, ByVal strAttributeName As String, ByVal strDefaultValue As String) As String

        Dim strValue As String

		Try
			strValue = oCXmlReader.GetAttribute(strAttributeName)
			If strValue Is Nothing Then strValue = strDefaultValue
		Catch ex As Exception
			strValue = strDefaultValue
		End Try

		Return strValue
	End Function

    Private Sub mSettingsFileUpdateTimer_Elapsed(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs) Handles mSettingsFileUpdateTimer.Elapsed
        If mUpdateSettingsFromFile Then
            If DateTime.UtcNow.Subtract(mUpdateSettingsRequestTime).TotalMilliseconds >= SETTINGS_FILE_UPDATE_DELAY_MSEC Then
                mUpdateSettingsFromFile = False

                UpdateSettingsFromFile()
            End If
        End If
    End Sub
End Class

