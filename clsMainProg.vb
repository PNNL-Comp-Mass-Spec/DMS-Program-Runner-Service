Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Xml
Imports System.Security
Imports System

Public Class clsMainProg
    Private Const m_IniFileName As String = "MultiProgRunner.xml"
    Private m_IniFileNamePath As String = String.Empty

    Private WithEvents m_FileWatcher As New FileSystemWatcher()
    Private oDateTime As DateTime = DateTime.Now

    Private m_DProgRunners As New Dictionary(Of String, CProcessRunner)

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
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to initialize clsMainProg")
        End Try
    End Sub
    Public Sub StartAllProgRunners()
        UpdateProgRunnersFromFile()
    End Sub

    Private Sub UpdateProgRunnersFromFile()
        Dim ACProcessSettings As CProcessSettings()

        Try
            If m_IniFileNamePath = Nothing OrElse m_IniFileNamePath.Length = 0 Then Exit Sub

            ACProcessSettings = StrFGetProcesses(m_IniFileNamePath)
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error reading parameter file '" & m_IniFileNamePath & "': " & ex.Message)
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
        Dim iTimes As Integer = 3
        For iTime As Integer = 0 To iTimes
            If DateTime.Compare(oDateTime, DateTime.Now.AddSeconds(-1)) < 0 Then
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "File changed")
                'When file was written program gets few events.
                'During some events XML reader can't open file. So use try-catch
                Try
                    UpdateProgRunnersFromFile()
                    oDateTime = DateTime.Now
                    Exit For
                Catch
                    If iTime = iTimes Then
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "XML reader exception")
                    End If
                End Try
                Thread.Sleep(1000)
            End If
        Next
    End Sub
    '<sections>
    '   ...
    '   <section name="programs">
    '       <item key="Andrei" value="Notepad.exe" arguments="" run="No" holdoff="0" /> 
    '       <item key="Robert" value="Notepad.exe" arguments="C:\tmp\junk1.txt" run="Once" holdoff="25" />
    '       <item key="Robert" value="Notepad.exe" arguments="C:\tmp\junk1.txt" run="Repeat" holdoff="25" />
    '   </section>
    '   ...
    '</sections>
    Public Class CProcessSettings
        Public StrKey As String
        Public StrProgram As String
        Public StrArguments As String
        Public StrRepeat As String
        Public StrHoldOff As String
    End Class

    Private Function StrFGetProcesses(ByVal strIniFilePath As String) As CProcessSettings()
        Dim oLProgramsSettings As New List(Of CProcessSettings)
        Dim oCXmlReader As XmlReader
        oCXmlReader = XmlReader.Create(New System.IO.FileStream(strIniFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        While oCXmlReader.Read()
            Select Case oCXmlReader.NodeType
                Case XmlNodeType.Element
                    Dim StrKey As String = oCXmlReader.GetAttribute("key")
                    If oCXmlReader.Name = "item" And StrKey <> "logfilename" Then
                        Dim oCProgramSettings As CProcessSettings = New CProcessSettings()
                        oCProgramSettings.StrKey = StrKey
                        oCProgramSettings.StrProgram = oCXmlReader.GetAttribute("value")
                        oCProgramSettings.StrArguments = oCXmlReader.GetAttribute("arguments")
                        oCProgramSettings.StrRepeat = oCXmlReader.GetAttribute("run")
                        oCProgramSettings.StrHoldOff = oCXmlReader.GetAttribute("holdoff")
                        oLProgramsSettings.Add(oCProgramSettings)
                    End If
            End Select
        End While
        oCXmlReader.Close()
        Return oLProgramsSettings.ToArray()
    End Function
End Class

