Option Strict On

Imports System.Threading
Imports System.IO
Imports System.Diagnostics

' This class runs a single program as an external process
' and monitors it with an internal thread
'
''' <summary> This class runs a single program as an external process and monitors it with an internal thread. </summary>
Public Class CProcessRunner

    ''' <summary> 
    ''' Thread states. 
    ''' </summary>
    Public Enum EThreadState
        No
        ProcessBroken
        Idle
        ProcessStarting
        ProcessRunning
    End Enum

    ''' <summary>
    ''' overall state of this object
    ''' </summary>
    Public m_state As EThreadState


    ''' <summary>
    ''' Used to start and monitor the external program. 
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Process As New Process

    ''' <summary> 
    ''' The process id of the currently running incarnation of the external program. 
    ''' </summary>
    Private m_pid As Integer

    ''' <summary>
    ''' The internal thread used to run the monitoring code. That starts and monitors the external program. 
    ''' </summary>
    Private m_Thread As Thread

    ''' <summary>
    ''' Flag that tells internal thread to quit monitoring external program and exit. 
    ''' </summary>
    Private m_ThreadStopCommand As Boolean = False

    ''' <summary>
    ''' The interval for monitoring thread to wake up and check m_doCleanup.
    ''' </summary>
    Private m_monitorInterval As Integer = 5000 ' (miliseconds)

    ''' <summary>
    ''' Exit code returned by completed process.
    ''' </summary>
    Private m_ExitCode As Integer

    ''' <summary>
    ''' Parameters for external program. 
    ''' </summary>
    Private m_StrKeyName As String
    Public m_StrProgramName As String
    Public m_StrProgramArguments As String
    Public m_StrRepeat As String = "No"
    Public m_Holdoff As Single            ' Holdoff time, in seconds (not milliseconds)
    Private m_WorkDir As String
    Private m_CreateNoWindow As Boolean
    Private m_WindowStyle As System.Diagnostics.ProcessWindowStyle
    Private oSync As Object = 1

    Private bUpdate As Boolean = False
    Public StrNewProgramName As String
    Public StrNewProgramArguments As String
    Public StrNewRepeat As String
    Public StrNewHoldoff As String

    ''' <summary> 
    ''' How often (milliseconds) internal monitoring thread checks status of external program.
    ''' </summary>
    Public Property MonitoringInterval() As Integer
        Get
            Return m_monitorInterval
        End Get
        Set(ByVal Value As Integer)
            m_monitorInterval = Value
        End Set
    End Property

    Public bIsUpdatedFromFile As Boolean

    ''' <summary> 
    ''' Process id of currently running external program's process. 
    ''' </summary>
    Public ReadOnly Property PID() As Integer
        Get
            Return m_pid
        End Get
    End Property

    ''' <summary> 
    ''' Current state of prog runner (as number). 
    ''' </summary>
    Public ReadOnly Property State() As EThreadState
        Get
            Return m_state
        End Get
    End Property

    ''' <summary> 
    ''' Whether prog runner will restart external program after it exits. 
    ''' </summary>
    Public Property Repeat() As String
        Get
            Return m_StrRepeat
        End Get
        Set(ByVal Value As String)
            If Value <> Nothing Then
               m_StrRepeat = CapitalizeMode(Value)
            End If
        End Set
    End Property

    ''' <summary> 
    ''' Time (seconds) that prog runner waits to restart external program after it exits. 
    ''' </summary>
    Public Property RepeatHoldOffTime() As Single
        Get
            Return m_Holdoff
        End Get
        Set(ByVal Value As Single)
            m_Holdoff = Value
        End Set
    End Property

    ''' <summary> 
    ''' Name of this progrunner.
    '''  </summary>
    Public Property Name() As String
        Get
            Return m_StrKeyName
        End Get
        Set(ByVal Value As String)
            m_StrKeyName = Value
        End Set
    End Property

    ''' <summary> 
    ''' Exit code when process completes. 
    ''' </summary>
    Public ReadOnly Property ExitCode() As Integer
        Get
            Return m_ExitCode
        End Get
    End Property

    ''' <summary> 
    ''' Working directory for process execution. 
    ''' </summary>
    Public Property WorkDir() As String
        Get
            Return m_WorkDir
        End Get
        Set(ByVal Value As String)
            m_WorkDir = Value
        End Set
    End Property

    ''' <summary> 
    ''' Determine if window should be displayed. 
    ''' </summary>
    Public Property CreateNoWindow() As Boolean
        Get
            Return m_CreateNoWindow
        End Get
        Set(ByVal Value As Boolean)
            m_CreateNoWindow = Value
        End Set
    End Property

    ''' <summary> 
    ''' Window style to use when CreateNoWindow is False. 
    ''' </summary>
    Public Property WindowStyle() As System.Diagnostics.ProcessWindowStyle
        Get
            Return m_WindowStyle
        End Get
        Set(ByVal Value As System.Diagnostics.ProcessWindowStyle)
            m_WindowStyle = Value
        End Set
    End Property

    ''' <summary> 
    ''' Initializes a new instance of the clsProgRunner class. 
    ''' </summary>
    Public Sub New()
        m_WorkDir = ""
        m_CreateNoWindow = False
        m_ExitCode = -12354  'Unreasonable value in case I missed setting it somewhere
    End Sub

    ''' <summary>
    ''' Instantiate new prog runner instance
    ''' </summary>
    ''' <param name="StrProcessRunnerName">Process runner name</param>
    ''' <param name="StrProgramName">Program name</param>
    ''' <param name="StrProgramArguments">Program arguments</param>
    ''' <param name="StrRepeat">Repeat mode.  Can be Repeat, Once, or No</param>
    ''' <param name="StrHoldoff">Holdoff time (in seconds) between when program exits to when it restarts if the Repeat mode is Repeat</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal StrProcessRunnerName As String, ByVal StrProgramName As String, ByVal StrProgramArguments As String, ByVal StrRepeat As String, ByVal StrHoldoff As String)
        m_state = EThreadState.No
        m_StrKeyName = StrProcessRunnerName
        StrNewProgramName = StrProgramName
        StrNewProgramArguments = StrProgramArguments
        StrNewRepeat = StrRepeat
        StrNewHoldoff = StrHoldoff
        bUpdate = True
        vFStartThread()
    End Sub

    ''' <summary> 
    ''' Update settings for existing prog runner instance
    ''' </summary>
    ''' <param name="StrProgramName">Program name</param>
    ''' <param name="StrProgramArguments">Program arguments</param>
    ''' <param name="StrRepeat">Repeat mode.  Can be Repeat, Once, or No</param>
    ''' <param name="StrHoldoff">Holdoff time (in seconds) between when program exits to when it restarts if the Repeat mode is Repeat</param>
    Public Sub vFUpdateProcessParameters(ByVal StrProgramName As String, ByVal StrProgramArguments As String, ByVal StrRepeat As String, ByVal StrHoldoff As String)
        Try
            Monitor.Enter(oSync)
            StrNewProgramName = StrProgramName
            StrNewProgramArguments = StrProgramArguments
            StrNewRepeat = StrRepeat
            StrNewHoldoff = StrHoldoff
            bUpdate = True
            Monitor.Exit(oSync)
        Catch
            bUpdate = bUpdate
        End Try
    End Sub

    ''' <summary> 
    ''' Creates a new thread and starts code that runs and monitors a program in it. 
    ''' </summary>
    Public Sub vFStartThread()
        If m_state = EThreadState.No Then
            Try
                Dim m_ThreadStart As New ThreadStart(AddressOf Me.vFProcessThread)
                m_Thread = New Thread(m_ThreadStart)
                m_Thread.Start()
            Catch ex As Exception
                m_state = EThreadState.ProcessBroken
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to create thread: " & m_StrKeyName)
            End Try
        End If
    End Sub

    ''' <summary> 
    ''' Causes monitoring thread to exit on its next monitoring cycle. 
    ''' </summary>
    Public Sub vFStopThread()
        If m_state = EThreadState.ProcessBroken Or m_state = EThreadState.No Then
            Exit Sub
        End If

        m_ThreadStopCommand = True
        If m_state = EThreadState.ProcessRunning Then
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Try to kill process: " & m_StrKeyName)
            Try
                m_Process.Kill()
                'Catch ex As System.ComponentModel.Win32Exception
                'ThrowConditionalException(CType(ex, Exception), "Caught Win32Exception while trying to kill process.")
                'Catch ex As System.InvalidOperationException
                'ThrowConditionalException(CType(ex, Exception), "Caught InvalidOperationException while trying to kill thread.")
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.WARN, "Exception killing process '" & m_StrKeyName & "': " & ex.Message)
            End Try
            If Not m_Process.WaitForExit(m_monitorInterval) Then
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to kill process '" & m_StrKeyName & "'")
            Else
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Killed process: " & m_StrKeyName)
            End If
        End If

        Try
            m_Thread.Join(m_monitorInterval)
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to wait while stopping thread '" & m_StrKeyName & "': " & ex.Message)
        End Try

        If m_Thread.IsAlive() = True Then
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Try to abort thread: " & m_StrKeyName)
            Try
                m_Thread.Abort()
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to stop thread '" & m_StrKeyName & "': " & ex.Message)
            End Try

        End If
    End Sub

    Private Function CapitalizeMode(ByVal strMode As String) As String

        If strMode Is Nothing Then
            strMode = String.Empty
        Else
            If strMode.Length = 1 Then
                strMode = strMode.ToUpper
            ElseIf strMode.Length > 1 Then
                strMode = strMode.Substring(0, 1).ToUpper & strMode.Substring(1).ToLower
            End If
        End If

        Return strMode

    End Function

    ''' <summary> 
    ''' Start program as external process and monitor its state. 
    ''' </summary>
    Private Sub vFProcessThread()
        Const REPEAT_HOLDOFF_SLEEP_TIME_MSEC As Integer = 1000

        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Thread started: " & m_StrKeyName)
        Do
            If m_ThreadStopCommand = True Then
                Exit Do
            End If

            If bUpdate Then
                ' Parameters have changed; update them
                bUpdate = False

                m_state = EThreadState.Idle
                UpdateThreadParameters(False)
            End If

            If m_state = EThreadState.ProcessStarting Then
                Try
                    With m_Process.StartInfo
                        .FileName = m_StrProgramName
                        .WorkingDirectory = m_WorkDir
                        .Arguments = m_StrProgramArguments
                        .CreateNoWindow = m_CreateNoWindow
                        If .CreateNoWindow Then
                            .WindowStyle = ProcessWindowStyle.Hidden
                        Else
                            .WindowStyle = m_WindowStyle
                        End If
                    End With
                    m_Process.Start()
                    m_state = EThreadState.ProcessRunning
                    m_pid = m_Process.Id
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Started: " & m_StrKeyName & ", pID=" & m_pid.ToString)

                    While Not (m_ThreadStopCommand Or m_Process.HasExited)
                        m_Process.WaitForExit(m_monitorInterval)
                    End While

                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Stopped: " & m_StrKeyName)
                    If m_ThreadStopCommand = True Then
                        Exit Do
                    End If

                    m_pid = 0
                    m_ExitCode = m_Process.ExitCode
                    m_Process.Close()
                    m_state = EThreadState.Idle
                    If m_StrRepeat = "Repeat" Then
                        ' Process has exited; but its mode is repeat
                        ' Wait for m_Holdoff seconds, then set m_state to EThreadState.ProcessStarting

                        Dim dtHoldoffStartTime As DateTime
                        dtHoldoffStartTime = System.DateTime.Now
                        Do
                            System.Threading.Thread.Sleep(REPEAT_HOLDOFF_SLEEP_TIME_MSEC)

                            If bUpdate Then
                                ' Update the current values for m_StrRepeat and m_Holdoff
                                ' However, don't set bUpdate to False since we're not updating the other parameters
                                UpdateThreadParameters(True)

                                If m_StrRepeat <> "Repeat" Then Exit Do
                            End If
                        Loop While System.DateTime.Now.Subtract(dtHoldoffStartTime).TotalSeconds < m_Holdoff

                        If m_StrRepeat = "Repeat" Then
                            If m_state = EThreadState.Idle Then
                                m_state = EThreadState.ProcessStarting
                            End If
                        Else
                            m_state = EThreadState.Idle
                        End If
                    Else
                        System.Threading.Thread.Sleep(m_monitorInterval)
                    End If
                Catch ex As Exception
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to start process: " & m_StrKeyName)
                    m_state = EThreadState.ProcessBroken
                    Exit Sub
                End Try
            Else
                System.Threading.Thread.Sleep(m_monitorInterval)
            End If
        Loop
        m_state = EThreadState.No
        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Thread stopped: " & m_StrKeyName)
    End Sub

    Private Sub UpdateThreadParameters(ByVal blnUpdateRepeatAndHoldoffOnly As Boolean)
        Static blnWarnedInvalidRepeatMode As Boolean
        Static blnWarnedInvalidHoldoffInterval As Boolean

        Try
            Monitor.Enter(oSync)

            If StrNewProgramName Is Nothing Then StrNewProgramName = String.Empty
            If StrNewProgramArguments Is Nothing Then StrNewProgramArguments = String.Empty
            If StrNewRepeat Is Nothing Then StrNewRepeat = String.Empty
            If StrNewHoldoff Is Nothing Then StrNewHoldoff = String.Empty

            ' Make sure the first letter of StrNewRepeat is capitalized and the other letters are lowercase
            StrNewRepeat = CapitalizeMode(StrNewRepeat)

            If Not blnUpdateRepeatAndHoldoffOnly Then
                If StrNewProgramName.Length = 0 Then
                    m_state = EThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_StrKeyName & "' failed due to empty program name")
                End If

                If Not System.IO.File.Exists(StrNewProgramName) Then
                    m_state = EThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_StrKeyName & "' failed due to missing program file: " & StrNewProgramName)
                End If
            End If


            If (StrNewRepeat = "Repeat" OrElse StrNewRepeat = "Once" OrElse StrNewRepeat = "No") Then
                blnWarnedInvalidRepeatMode = False
            Else
                If blnUpdateRepeatAndHoldoffOnly Then
                    ' Only updating the Repeat and Holdoff values
                    ' Log the error (if not yet logged)
                    If Not blnWarnedInvalidRepeatMode Then
                        blnWarnedInvalidRepeatMode = True
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Invalid ""run"" value for process '" & m_StrKeyName & "': " & StrNewRepeat & "; valid values are Repeat, Once, and Off")
                    End If
                    StrNewRepeat = m_StrRepeat
                Else
                    m_state = EThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_StrKeyName & "' failed due to incorrect ""run"" value of '" & StrNewRepeat & "'; valid values are Repeat, Once, and Off")
                End If
            End If

            Dim sngNewHoldOff As Single = m_monitorInterval
            If System.Single.TryParse(StrNewHoldoff, sngNewHoldOff) Then
                blnWarnedInvalidHoldoffInterval = False
            Else
                If blnUpdateRepeatAndHoldoffOnly Then
                    ' Only updating the Repeat and Holdoff values
                    ' Log the error (if not yet logged)
                    If Not blnWarnedInvalidHoldoffInterval Then
                        blnWarnedInvalidHoldoffInterval = True
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Invalid ""Holdoff"" value for process '" & m_StrKeyName & "': " & StrNewHoldoff & "; this value must be an integer (defining the holdoff time, in seconds)")
                    End If
                    sngNewHoldOff = m_Holdoff
                Else
                    m_state = EThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_StrKeyName & "' failed due to incorrect ""Holdoff"" value of '" & StrNewHoldoff & "'; this value must be an integer (defining the holdoff time, in seconds)")
                End If
            End If

            If Not blnUpdateRepeatAndHoldoffOnly Then
                If m_state = EThreadState.Idle Then
                    If StrNewRepeat = "Repeat" Then
                        m_state = EThreadState.ProcessStarting
                    ElseIf StrNewRepeat = "Once" Then
                        If m_StrProgramName <> StrNewProgramName Then
                            m_state = EThreadState.ProcessStarting
                        ElseIf m_StrProgramArguments <> StrNewProgramArguments Then
                            m_state = EThreadState.ProcessStarting
                        Else
                            If m_StrRepeat = "No" Or m_StrRepeat = "Repeat" Then
                                m_state = EThreadState.ProcessStarting
                            Else
                                If m_Holdoff <> sngNewHoldOff Then
                                    m_state = EThreadState.ProcessStarting
                                End If
                            End If
                        End If
                    End If
                End If
            End If
           

            m_StrProgramName = StrNewProgramName
            m_StrProgramArguments = StrNewProgramArguments
            m_StrRepeat = StrNewRepeat
            m_Holdoff = sngNewHoldOff
            m_WorkDir = ""
            m_CreateNoWindow = False
            m_ExitCode = -12354  'Unreasonable value in case I missed setting it somewhere
            Monitor.Exit(oSync)
        Catch
            bUpdate = bUpdate
        End Try

    End Sub

End Class