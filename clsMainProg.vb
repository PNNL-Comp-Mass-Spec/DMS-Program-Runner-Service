Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports System.Xml
Imports PRISM.Logging

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

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        Try
            Dim fi As New FileInfo(Application.ExecutablePath)
            m_IniFileNamePath = Path.Combine(fi.DirectoryName, m_IniFileName)

            Const logFileNameBase = "Logs\ProgRunner"
            LogTools.CreateFileLogger(logFileNameBase, BaseLogger.LogLevels.INFO)

            LogTools.LogMessage("=== MultiProgRunner v" & Application.ProductVersion & " started =====")

            ' Set up the FileWatcher to detect setup file changes
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

            mUpdateSettingsRequestTime = DateTime.UtcNow
            mSettingsFileUpdateTimer = New Timers.Timer(250)
            mSettingsFileUpdateTimer.Start()

        Catch ex As Exception
            LogTools.LogError("Failed to initialize clsMainProg")
        End Try
    End Sub

    Public Sub StartAllProgRunners()
        UpdateProgRunnersFromFile(False)
    End Sub

    Private Sub UpdateProgRunnersFromFile(blnPassXMLFileParsingExceptionsToCaller As Boolean)

        Dim lstProgramSettings As List(Of clsProcessSettings)

        Try
            If String.IsNullOrWhiteSpace(m_IniFileNamePath) Then Exit Sub

            lstProgramSettings = GetProcesses(m_IniFileNamePath)

        Catch ex As Exception
            If blnPassXMLFileParsingExceptionsToCaller Then
                Throw
            Else
                LogTools.LogError("Error reading parameter file '" & m_IniFileNamePath & "'", ex)
            End If
            Exit Sub
        End Try

        LogTools.LogMessage("Updating from file")

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
                LogTools.LogError("Ignoring empty program key in the Programs section")
                Continue For
            End If

            Try
                If Not m_ProgRunners.ContainsKey(uniqueProgramKey) Then
                    ' New entry
                    Dim oCProcessRunner = New clsProcessRunner(oProcessSettings)
                    lstProgRunners.Add(uniqueProgramKey, True)

                    m_ProgRunners.Add(uniqueProgramKey, oCProcessRunner)
                    LogTools.LogMessage("Added program '" & uniqueProgramKey & "'")

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

                    LogTools.LogMessage("Updated program '" & uniqueProgramKey & "'")
                End If
            Catch ex As Exception
                LogTools.LogError("Error in UpdateProgRunnersFromFile updating process '" & uniqueProgramKey & "': " & ex.Message)
            End Try

        Next

        Try
            ' Remove disappeared processes
            Dim lstProcessesToStop As New List(Of String)

            For Each progRunnerEntry As KeyValuePair(Of String, clsProcessRunner) In m_ProgRunners
                Dim enabled = False
                If lstProgRunners.TryGetValue(progRunnerEntry.Key, enabled) Then
                    If Not enabled Then
                        lstProcessesToStop.Add(progRunnerEntry.Key)
                    End If
                End If
            Next

            For Each uniqueProgramKey In lstProcessesToStop
                m_ProgRunners.Item(uniqueProgramKey).StopThread()
                m_ProgRunners.Remove(uniqueProgramKey)
                LogTools.LogMessage("Deleted program '" & uniqueProgramKey & "'")
            Next

        Catch ex As Exception
            LogTools.LogError("Error in UpdateProgRunnersFromFile removing old processes: " & ex.Message)
        End Try

    End Sub

    Public Sub StopAllProgRunners()
        For Each oCKeyValuePair As KeyValuePair(Of String, clsProcessRunner) In m_ProgRunners
            m_ProgRunners.Item(oCKeyValuePair.Key).StopThread()
        Next
        m_ProgRunners.Clear()
        LogTools.LogMessage("MultiProgRunner stopped")
    End Sub

    Protected Overrides Sub Finalize()
        Try
            StopAllProgRunners()
        Catch ex As Exception
            LogTools.LogError("Failed to StopAllProgRunners")
        Finally
            MyBase.Finalize()
        End Try
    End Sub

    ''' <summary>
    ''' If the XML reader tries to read a file that is being updated, an error can occur
    ''' This function only has Try/Catch blocks when reading specific entries within a section
    ''' The calling function is expected to catch and handle other errors
    ''' </summary>
    ''' <param name="strIniFilePath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetProcesses(strIniFilePath As String) As List(Of clsProcessSettings)
        Dim lstProgramSettings As New List(Of clsProcessSettings)

        Dim strSectionName = ""

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

                                LogTools.LogError("Error parsing XML Config file: " & ex.Message)
                            End Try

                            If strSectionName Is Nothing Then strSectionName = String.Empty
                            Continue While
                        End If

                        ' Expected format:
                        '   <section name="programs">
                        '     <item key="Analysis1" value="C:\DMS_Programs\AnalysisToolManager1\StartManager1.bat" arguments="" run="Repeat" holdoff="300" />

                        If oReader.Depth = 2 AndAlso strSectionName = "programs" AndAlso oReader.Name = "item" Then

                            Dim strKeyName = ""

                            Try

                                strKeyName = oReader.GetAttribute("key")

                                If String.IsNullOrWhiteSpace(strKeyName) Then
                                    LogTools.LogError("Empty key name; ignoring entry")
                                End If

                                Dim oProgramSettings = New clsProcessSettings(strKeyName) With {
                                    .ProgramPath = GetAttributeSafe(oReader, "value"),
                                    .ProgramArguments = GetAttributeSafe(oReader, "arguments"),
                                    .RepeatMode = GetAttributeSafe(oReader, "run", "Once")
                                }

                                Dim holdOffSecondsText = GetAttributeSafe(oReader, "holdoff", "10")
                                Dim holdOffSeconds As Integer

                                If Not Integer.TryParse(holdOffSecondsText, holdOffSeconds) Then
                                    LogTools.LogError("Invalid ""Holdoff"" value for process '" & strKeyName & "': " & holdOffSecondsText & "; this value must be an integer (defining the holdoff time, in seconds).  Will assume 300")
                                    holdOffSeconds = 300
                                End If
                                oProgramSettings.HoldoffSeconds = holdOffSeconds

                                lstProgramSettings.Add(oProgramSettings)

                            Catch ex As Exception
                                ' Ignore this entry
                                LogTools.LogError("Error parsing XML Config file for key " & strKeyName & ": " & ex.Message)
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

    Private Function GetAttributeSafe(oCXmlReader As XmlReader, strAttributeName As String) As String
        Return GetAttributeSafe(oCXmlReader, strAttributeName, String.Empty)
    End Function

    Private Function GetAttributeSafe(oCXmlReader As XmlReader, strAttributeName As String, strDefaultValue As String) As String

        Dim strValue As String

        Try
            strValue = oCXmlReader.GetAttribute(strAttributeName)
            If strValue Is Nothing Then strValue = strDefaultValue
        Catch ex As Exception
            strValue = strDefaultValue
        End Try

        Return strValue
    End Function

    Private Sub UpdateSettingsFromFile()
        Const MAX_READ_ATTEMPTS = 3

        For iTime = 1 To MAX_READ_ATTEMPTS
            LogTools.LogMessage("File changed")

            ' When file was written program gets few events.
            ' During some events XML reader can't open file. So use try-catch
            Try
                UpdateProgRunnersFromFile(True)
                Exit For
            Catch ex As Exception
                If iTime < MAX_READ_ATTEMPTS Then
                    LogTools.LogError("Error reading XML file (will try again): " & ex.Message)
                Else
                    LogTools.LogError("Error reading XML file (tried " & MAX_READ_ATTEMPTS.ToString & " times): " & ex.Message)
                End If
            End Try

            Thread.Sleep(1000)
        Next
    End Sub

    Private Sub m_FileWatcher_Changed(sender As Object, e As FileSystemEventArgs) Handles m_FileWatcher.Changed
        mUpdateSettingsFromFile = True
        mUpdateSettingsRequestTime = DateTime.UtcNow
    End Sub

    Private Sub mSettingsFileUpdateTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles mSettingsFileUpdateTimer.Elapsed
        If mUpdateSettingsFromFile Then
            If DateTime.UtcNow.Subtract(mUpdateSettingsRequestTime).TotalMilliseconds >= SETTINGS_FILE_UPDATE_DELAY_MSEC Then
                mUpdateSettingsFromFile = False

                UpdateSettingsFromFile()
            End If
        End If
    End Sub
End Class

