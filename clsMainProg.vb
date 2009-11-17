Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Xml
Imports System.Security
Imports System

Public Class clsMainProg
    Private Const SETTINGS_FILE_UPDATE_DELAY_MSEC As Integer = 1500

    Private Const m_IniFileName As String = "MultiProgRunner.xml"
    Private m_IniFileNamePath As String = String.Empty

    Private WithEvents m_FileWatcher As New FileSystemWatcher()

    Private m_DProgRunners As New Dictionary(Of String, CProcessRunner)

    ' When the .XML settings file is changed, mUpdateSettingsFromFile is set to True and mUpdateSettingsRequestTime is set to the current date/time
    ' Timer looks for mSettingsFileUpdateTimer being true, and after 1500 milliseconds has elapsed, it calls UpdateSettingsFromFile
    Private mUpdateSettingsFromFile As Boolean
    Private mUpdateSettingsRequestTime As DateTime
    Private WithEvents mSettingsFileUpdateTimer As System.Timers.Timer

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

            m_DProgRunners.Clear()

            mSettingsFileUpdateTimer = New System.Timers.Timer(250)
            mSettingsFileUpdateTimer.Start()

        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to initialize clsMainProg")
        End Try
    End Sub

    Public Sub StartAllProgRunners()
        UpdateProgRunnersFromFile(False)
    End Sub

    Private Sub UpdateProgRunnersFromFile(ByVal blnPassXMLFileParsingExceptionsToCaller As Boolean)
        Dim ACProcessSettings As CProcessSettings()

        Try
            If m_IniFileNamePath = Nothing OrElse m_IniFileNamePath.Length = 0 Then Exit Sub

            ACProcessSettings = StrFGetProcesses(m_IniFileNamePath)
        Catch ex As Exception
            If blnPassXMLFileParsingExceptionsToCaller Then
                Throw ex
            Else
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error reading parameter file '" & m_IniFileNamePath & "': " & ex.Message)
            End If
            Exit Sub
        End Try

        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Updating from file")
        'Mark for possible removing
        For Each oCKeyValuePair As KeyValuePair(Of String, CProcessRunner) In m_DProgRunners
            oCKeyValuePair.Value.bIsUpdatedFromFile = False
        Next

        For Each oCProcessSettings As CProcessSettings In ACProcessSettings
            Dim StrKey As String = oCProcessSettings.StrKey
            Try
                If Not m_DProgRunners.ContainsKey(StrKey) Then
                    Dim oCProcessRunner As CProcessRunner = New CProcessRunner(StrKey, oCProcessSettings.StrProgram, _
                            oCProcessSettings.StrArguments, oCProcessSettings.StrRepeat, oCProcessSettings.StrHoldOff)
                    oCProcessRunner.bIsUpdatedFromFile = True
                    m_DProgRunners.Add(StrKey, oCProcessRunner)
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Added program '" & StrKey & "'")
                Else
                    Dim oCProcessRunner As CProcessRunner = m_DProgRunners.Item(StrKey)
                    oCProcessRunner.vFUpdateProcessParameters(oCProcessSettings.StrProgram, oCProcessSettings.StrArguments, _
                            oCProcessSettings.StrRepeat, oCProcessSettings.StrHoldOff)
                    oCProcessRunner.bIsUpdatedFromFile = True
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Updated program '" & StrKey & "'")
                End If
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error in UpdateProgRunnersFromFile updating process '" & StrKey & "': " & ex.Message)
            End Try
        Next

        Try
            'Remove disappeared processes
            Dim oLKeysForRemoving As New List(Of String)
            For Each oCKeyValuePair As KeyValuePair(Of String, CProcessRunner) In m_DProgRunners
                If oCKeyValuePair.Value.bIsUpdatedFromFile = False Then
                    oLKeysForRemoving.Add(oCKeyValuePair.Key)
                End If
            Next
            For Each StrKey As String In oLKeysForRemoving
                m_DProgRunners.Item(StrKey).vFStopThread()
                m_DProgRunners.Remove(StrKey)
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Deleted program '" & StrKey & "'")
            Next
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error in UpdateProgRunnersFromFile removing old processes: " & ex.Message)
        End Try

    End Sub

    Public Sub StopAllProgRunners()
        For Each oCKeyValuePair As KeyValuePair(Of String, CProcessRunner) In m_DProgRunners
            m_DProgRunners.Item(oCKeyValuePair.Key).vFStopThread()
        Next
        m_DProgRunners.Clear()
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

    Private Sub m_FileWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles m_FileWatcher.Changed
        mUpdateSettingsFromFile = True
        mUpdateSettingsRequestTime = System.DateTime.Now()
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

    Public Class CProcessSettings
        Public StrKey As String
        Public StrProgram As String
        Public StrArguments As String
        Public StrRepeat As String
        Public StrHoldOff As String
    End Class

    ''' <summary>
    ''' If the XML reader tries to read a file that is being updated, an error can occur
    ''' This function only has Try/Catch blocks when reading specific entries within a section
    ''' The calling function is expected to catch and handle other errors
    ''' </summary>
    ''' <param name="strIniFilePath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function StrFGetProcesses(ByVal strIniFilePath As String) As CProcessSettings()
        Dim oLProgramsSettings As New List(Of CProcessSettings)
        Dim oCXmlReader As XmlReader

        Dim strSectionName As String = ""
        Dim strKeyName As String = ""

        oCXmlReader = XmlReader.Create(New System.IO.FileStream(strIniFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        While oCXmlReader.Read()
            Select Case oCXmlReader.NodeType
                Case XmlNodeType.Element
                    If oCXmlReader.Name = "section" Then
                        Try
                            strSectionName = oCXmlReader.GetAttribute("name")
                        Catch ex As Exception
                            ' Section element doesn't have a "name" attribute; set strSectionName to ""
                            strSectionName = String.Empty

                            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error parsing XML Config file: " & ex.Message)
                        End Try

                        If strSectionName Is Nothing Then strSectionName = String.Empty
                    Else

                        ' Need to debug this against the MultiProgRunner.xml file on Pub-14
                        ' Make sure this line is skipped:
                        ' <comment><itemDisabled key="XTandAnalysis" value="C:\DMS_Programs\DMS5\AnalysisManagerXTandem\AnalysisManagerProg.exe" arguments="" run="Repeat" holdoff="300" /></comment>

                        If oCXmlReader.Depth = 2 AndAlso strSectionName = "programs" AndAlso oCXmlReader.Name = "item" Then

                            Try

                                strKeyName = oCXmlReader.GetAttribute("key")
                                Dim oCProgramSettings As CProcessSettings = New CProcessSettings()
                                oCProgramSettings.StrKey = strKeyName
                                oCProgramSettings.StrProgram = GetAttributeSafe(oCXmlReader, "value")
                                oCProgramSettings.StrArguments = GetAttributeSafe(oCXmlReader, "arguments")
                                oCProgramSettings.StrRepeat = GetAttributeSafe(oCXmlReader, "run", "Once")
                                oCProgramSettings.StrHoldOff = GetAttributeSafe(oCXmlReader, "holdoff", 10)
                                oLProgramsSettings.Add(oCProgramSettings)

                            Catch ex As Exception
                                ' Ignore this entry
                                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error parsing XML Config file: " & ex.Message)
                            End Try

                        End If

                    End If

                Case XmlNodeType.EndElement
                    If oCXmlReader.Name = "section" Then
                        strSectionName = String.Empty
                    End If

            End Select
        End While
        oCXmlReader.Close()
        Return oLProgramsSettings.ToArray()
    End Function

    Private Function GetAttributeSafe(ByVal oCXmlReader As XmlReader, ByVal strAttributeName As String) As String
        Return GetAttributeSafe(oCXmlReader, strAttributeName, String.Empty)
    End Function

    Private Function GetAttributeSafe(ByVal oCXmlReader As XmlReader, ByVal strAttributeName As String, ByVal strDefaultValue As String) As String

        Dim strValue As String
        strValue = strDefaultValue

        Try
            strValue = oCXmlReader.GetAttribute(strAttributeName)
            If strValue Is Nothing Then strValue = strDefaultValue
        Catch ex As Exception
            strValue = strDefaultValue
        End Try

        Return strValue
    End Function

    Private Sub mSettingsFileUpdateTimer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles mSettingsFileUpdateTimer.Elapsed
        If mUpdateSettingsFromFile Then
            If System.DateTime.Now.Subtract(mUpdateSettingsRequestTime).TotalMilliseconds >= SETTINGS_FILE_UPDATE_DELAY_MSEC Then
                mUpdateSettingsFromFile = False

                UpdateSettingsFromFile()
            End If
        End If
    End Sub
End Class

