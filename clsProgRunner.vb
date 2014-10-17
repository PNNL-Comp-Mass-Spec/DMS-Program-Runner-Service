Option Strict On

Imports System.Threading
Imports System.IO
Imports System.Diagnostics

''' <summary>
''' This class runs a single program as an external process and monitors it with an internal thread. 
''' </summary>
''' <remarks></remarks>
Public Class clsProcessRunner

    ''' <summary> 
    ''' Thread states. 
    ''' </summary>
    Public Enum eThreadState
        No
        ProcessBroken
        Idle
        ProcessStarting
        ProcessRunning
    End Enum

    Protected m_state As eThreadState

    ''' <summary>
    ''' Overall state of this object
    ''' </summary>
    Public ReadOnly Property ThreadState As eThreadState
        Get
            Return m_state
        End Get
    End Property

    ''' <summary>
    ''' Used to start and monitor the external program. 
    ''' </summary>
    ''' <remarks></remarks>
    Private ReadOnly m_Process As New Process

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
    ''' The interval (in milliseconds) for monitoring thread to wake up and check m_doCleanup.
    ''' </summary>
    Private m_monitorInterval As Integer = 1000

    ''' <summary>
    ''' Exit code returned by completed process.
    ''' </summary>
    Private m_ExitCode As Integer

    Protected m_KeyName As String

    ''' <summary>
    ''' Key name for this program (unique across all programs registered to run)
    ''' </summary>
    Public ReadOnly Property KeyName As String
        Get
            Return m_KeyName
        End Get
    End Property

    Protected m_ProgramInfo As clsProcessSettings
    Protected m_NewProgramInfo As clsProcessSettings

    ''' <summary>
    ''' Path to the program (.exe) to run
    ''' </summary>
    Public ReadOnly Property ProgramPath As String
        Get
            Return m_ProgramInfo.ProgramPath
        End Get
    End Property

    ''' <summary>
    ''' Arguments to pass to the program
    ''' </summary>
    Public ReadOnly Property ProgramArguments As String
        Get
            Return m_ProgramInfo.ProgramArguments
        End Get
    End Property

    ''' <summary>
    ''' Repeat mode, valid values are Repeat, Once, and No
    ''' </summary>
    Public ReadOnly Property RepeatMode As String
        Get
            Return m_ProgramInfo.RepeatMode
        End Get
    End Property

    ''' <summary>
    ''' Holdoff time, in seconds (not milliseconds)
    ''' </summary>
    Public ReadOnly Property Holdoff As Integer
        Get
            Return m_ProgramInfo.HoldoffSeconds
        End Get
    End Property

    Private m_WorkDir As String
    Private m_CreateNoWindow As Boolean
    Private m_WindowStyle As ProcessWindowStyle
    Private ReadOnly oSync As Object = 1

    Private mUpdateRequired As Boolean = False

    ''' <summary> 
    ''' How often (milliseconds) internal monitoring thread checks status of external program.
    ''' </summary>
    Public Property MonitoringInterval() As Integer
        Get
            Return m_monitorInterval
        End Get
        Set(ByVal Value As Integer)
            If Value < 100 Then Value = 100
            m_monitorInterval = Value
        End Set
    End Property

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
    Public ReadOnly Property State() As eThreadState
        Get
            Return m_state
        End Get
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
    Public ReadOnly Property WorkDir() As String
        Get
            Return m_WorkDir
        End Get
    End Property

    ''' <summary> 
    ''' Determine if window should be displayed. 
    ''' </summary>
    Public ReadOnly Property CreateNoWindow() As Boolean
        Get
            Return m_CreateNoWindow
        End Get
    End Property

    ''' <summary> 
    ''' Window style to use when CreateNoWindow is False. 
    ''' </summary>
    Public ReadOnly Property WindowStyle() As ProcessWindowStyle
        Get
            Return m_WindowStyle
        End Get
    End Property

    ''' <summary>
    ''' Instantiate new process runner instance
    ''' </summary>
    ''' <param name="processSettings">Process settings</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal processSettings As clsProcessSettings)
        Initialize(processSettings, m_WorkDir, ProcessWindowStyle.Normal, False)
    End Sub

    ''' <summary>
    ''' Instantiate new process runner instance
    ''' </summary>
    ''' <param name="processSettings">Process settings</param>
    ''' <param name="workingDirectory">Working directory path</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal processSettings As clsProcessSettings, ByVal workingDirectory As String)
        Initialize(processSettings, workingDirectory, ProcessWindowStyle.Normal, False)
    End Sub

    ''' <summary>
    ''' Instantiate new process runner instance
    ''' </summary>
    ''' <param name="processSettings">Process settings</param>
    ''' <param name="workingDirectory">Working directory path</param>
    ''' <param name="bCreateNoWindow">True to create no window, false to use a normal window</param>
    ''' <remarks></remarks>
    Public Sub New(
      ByVal processSettings As clsProcessSettings,
      ByVal workingDirectory As String,
      ByVal bCreateNoWindow As Boolean)
        Initialize(processSettings, workingDirectory, ProcessWindowStyle.Normal, bCreateNoWindow)
    End Sub

    ''' <summary>
    ''' Instantiate new process runner instance
    ''' </summary>
    ''' <param name="processSettings">Process settings</param>
    ''' <param name="workingDirectory">Working directory path</param>
    ''' <param name="eWindowStyle">Window style</param>
    ''' <param name="bCreateNoWindow">True to create no window, false to use windowStyle</param>
    ''' <remarks></remarks>
    Public Sub New(
      ByVal processSettings As clsProcessSettings,
      ByVal workingDirectory As String,
      ByVal eWindowStyle As ProcessWindowStyle,
      ByVal bCreateNoWindow As Boolean)
        Initialize(processSettings, workingDirectory, eWindowStyle, bCreateNoWindow)
    End Sub

    Protected Sub Initialize(
        ByVal processSettings As clsProcessSettings,
        ByVal workingDirectory As String,
        ByVal eWindowStyle As ProcessWindowStyle,
        ByVal bCreateNoWindow As Boolean)

        m_state = eThreadState.No
        m_KeyName = processSettings.UniqueKey
        m_ProgramInfo = New clsProcessSettings(m_KeyName)

        m_NewProgramInfo = processSettings

        m_WorkDir = workingDirectory
        m_WindowStyle = eWindowStyle
        m_CreateNoWindow = bCreateNoWindow

        mUpdateRequired = True
        StartThread()
    End Sub

    ''' <summary> 
    ''' Update settings for existing prog runner instance
    ''' </summary>
    ''' <param name="newProgramInfo">New program info</param>
    ''' <remarks>Key name is ignored in newProgramInfo</remarks>
    Public Sub UpdateProcessParameters(ByVal newProgramInfo As clsProcessSettings)
        Try
            Monitor.Enter(oSync)
            m_NewProgramInfo = newProgramInfo
            mUpdateRequired = True
            Monitor.Exit(oSync)
        Catch
            mUpdateRequired = mUpdateRequired
        End Try
    End Sub

    ''' <summary> 
    ''' Creates a new thread and starts code that runs and monitors a program in it. 
    ''' </summary>
    Public Sub StartThread()
        If m_state = eThreadState.No Then
            Try
                Dim m_ThreadStart As New ThreadStart(AddressOf Me.ProcessThread)
                m_Thread = New Thread(m_ThreadStart)
                m_Thread.Start()
            Catch ex As Exception
                m_state = eThreadState.ProcessBroken
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to create thread: " & m_KeyName)
            End Try
        End If
    End Sub

    ''' <summary> 
    ''' Causes monitoring thread to exit on its next monitoring cycle. 
    ''' </summary>
    Public Sub StopThread()
        If m_state = eThreadState.ProcessBroken Or m_state = eThreadState.No Then
            Exit Sub
        End If

        m_ThreadStopCommand = True
        If m_state = eThreadState.ProcessRunning Then
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Try to kill process: " & m_KeyName)
            Try
                m_Process.Kill()
                'Catch ex As System.ComponentModel.Win32Exception
                'ThrowConditionalException(CType(ex, Exception), "Caught Win32Exception while trying to kill process.")
                'Catch ex As System.InvalidOperationException
                'ThrowConditionalException(CType(ex, Exception), "Caught InvalidOperationException while trying to kill thread.")
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.WARN, "Exception killing process '" & m_KeyName & "': " & ex.Message)
            End Try
            If Not m_Process.WaitForExit(m_monitorInterval) Then
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to kill process '" & m_KeyName & "'")
            Else
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Killed process: " & m_KeyName)
            End If
        End If

        Try
            m_Thread.Join(m_monitorInterval)
        Catch ex As Exception
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to wait while stopping thread '" & m_KeyName & "': " & ex.Message)
        End Try

        If m_Thread.IsAlive() = True Then
            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Try to abort thread: " & m_KeyName)
            Try
                m_Thread.Abort()
            Catch ex As Exception
                clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Failed to stop thread '" & m_KeyName & "': " & ex.Message)
            End Try

        End If
    End Sub

    Private Function CapitalizeMode(ByVal strMode As String) As String

        If strMode Is Nothing Then
            strMode = String.Empty
        Else
            If strMode.Length = 1 Then
                strMode = strMode.ToUpper()
            ElseIf strMode.Length > 1 Then
                strMode = strMode.Substring(0, 1).ToUpper() & strMode.Substring(1).ToLower()
            End If
        End If

        Return strMode

    End Function

    ''' <summary> 
    ''' Start program as external process and monitor its state. 
    ''' </summary>
    Private Sub ProcessThread()
        Const REPEAT_HOLDOFF_SLEEP_TIME_MSEC As Integer = 1000

        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Thread started: " & m_KeyName)
        Do
            If m_ThreadStopCommand = True Then
                Exit Do
            End If

            If mUpdateRequired Then
                ' Parameters have changed; update them
                mUpdateRequired = False

                m_state = eThreadState.Idle
                UpdateThreadParameters(False)
            End If

            If m_state = eThreadState.ProcessStarting Then
                Try
                    If String.IsNullOrWhiteSpace(m_ProgramInfo.ProgramPath) Then
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error running process '" & m_KeyName & "': empty program path")
                        m_state = eThreadState.ProcessBroken
                        Exit Sub
                    End If

                    With m_Process.StartInfo
                        .FileName = m_ProgramInfo.ProgramPath
                        .WorkingDirectory = m_WorkDir
                        .Arguments = m_ProgramInfo.ProgramArguments
                        .CreateNoWindow = m_CreateNoWindow
                        If .CreateNoWindow Then
                            .WindowStyle = ProcessWindowStyle.Hidden
                        Else
                            .WindowStyle = m_WindowStyle
                        End If
                    End With
                    m_Process.Start()
                    m_state = eThreadState.ProcessRunning
                    m_pid = m_Process.Id
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Started: " & m_KeyName & ", pID=" & m_pid.ToString)

                    While Not (m_ThreadStopCommand Or m_Process.HasExited)
                        m_Process.WaitForExit(m_monitorInterval)
                    End While

                    If m_ThreadStopCommand = True Then
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Stopped: " & m_KeyName)
                        Exit Do
                    Else
                        If m_ProgramInfo.RepeatMode = "Repeat" Then
                            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Waiting: " & m_KeyName)
                        Else
                            clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Stopped: " & m_KeyName)
                        End If
                    End If
                Catch ex As Exception
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error running process '" & m_KeyName & "': " & ex.Message)
                    m_state = eThreadState.ProcessBroken
                    Exit Sub
                End Try

                Try
                    m_pid = 0
                    m_ExitCode = m_Process.ExitCode
                    m_Process.Close()
                    m_state = eThreadState.Idle
                    If m_ProgramInfo.RepeatMode = "Repeat" Then
                        ' Process has exited; but its mode is repeat
                        ' Wait for m_Holdoff seconds, then set m_state to eThreadState.ProcessStarting

                        Dim dtHoldoffStartTime = DateTime.UtcNow
                        Do
                            Thread.Sleep(REPEAT_HOLDOFF_SLEEP_TIME_MSEC)

                            If mUpdateRequired Then
                                ' Update the current values for m_ProgramInfo.RepeatMode and m_ProgramInfo.HoldoffSeconds
                                ' However, don't set mUpdateRequired to False since we're not updating the other parameters
                                UpdateThreadParameters(True)

                                If m_ProgramInfo.RepeatMode <> "Repeat" Then Exit Do
                            End If
                        Loop While DateTime.UtcNow.Subtract(dtHoldoffStartTime).TotalSeconds < m_ProgramInfo.HoldoffSeconds

                        If m_ProgramInfo.RepeatMode = "Repeat" Then
                            If m_state = eThreadState.Idle Then
                                m_state = eThreadState.ProcessStarting
                            End If
                        Else
                            m_state = eThreadState.Idle
                        End If
                    Else
                        Thread.Sleep(m_monitorInterval)
                    End If
                Catch ex1 As ThreadAbortException
                    m_state = eThreadState.ProcessBroken
                    Exit Sub
                Catch ex2 As Exception
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Error waiting to restart process '" & m_KeyName & "': " & ex2.Message)
                    m_state = eThreadState.ProcessBroken
                    Exit Sub
                End Try
            Else
                Thread.Sleep(m_monitorInterval)
            End If
        Loop
        m_state = eThreadState.No
        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Thread stopped: " & m_KeyName)
    End Sub

    Private Sub UpdateThreadParameters(ByVal blnUpdateRepeatAndHoldoffOnly As Boolean)
        Static blnWarnedInvalidRepeatMode As Boolean

        Try
            Monitor.Enter(oSync)

            With m_NewProgramInfo
                If String.IsNullOrWhiteSpace(.ProgramPath) Then .ProgramPath = String.Empty
                If String.IsNullOrWhiteSpace(.ProgramArguments) Then .ProgramArguments = String.Empty
                If String.IsNullOrWhiteSpace(.RepeatMode) Then .RepeatMode = "No"
                If .HoldoffSeconds < 1 Then .HoldoffSeconds = 1
            End With

            ' Make sure the first letter of StrNewRepeat is capitalized and the other letters are lowercase
            m_NewProgramInfo.RepeatMode = CapitalizeMode(m_NewProgramInfo.RepeatMode)

            If Not blnUpdateRepeatAndHoldoffOnly Then
                If String.IsNullOrWhiteSpace(m_NewProgramInfo.ProgramPath) Then
                    m_state = eThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_KeyName & "' failed due to empty program name")
                End If

                If Not File.Exists(m_NewProgramInfo.ProgramPath) Then
                    m_state = eThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_KeyName & "' failed due to missing program file: " & m_NewProgramInfo.ProgramPath)
                End If
            End If


            If (m_NewProgramInfo.RepeatMode = "Repeat" OrElse m_NewProgramInfo.RepeatMode = "Once" OrElse m_NewProgramInfo.RepeatMode = "No") Then
                blnWarnedInvalidRepeatMode = False
            Else
                If blnUpdateRepeatAndHoldoffOnly Then
                    ' Only updating the Repeat and Holdoff values
                    ' Log the error (if not yet logged)
                    If Not blnWarnedInvalidRepeatMode Then
                        blnWarnedInvalidRepeatMode = True
                        clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Invalid ""run"" value for process '" & m_KeyName & "': " & m_NewProgramInfo.RepeatMode & "; valid values are Repeat, Once, and No")
                    End If
                    m_NewProgramInfo.RepeatMode = m_ProgramInfo.RepeatMode
                Else
                    m_state = eThreadState.ProcessBroken
                    clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.ERROR, "Process '" & m_KeyName & "' failed due to incorrect ""run"" value of '" & m_NewProgramInfo.RepeatMode & "'; valid values are Repeat, Once, and No")
                End If
            End If

            If Not blnUpdateRepeatAndHoldoffOnly Then
                If m_state = eThreadState.Idle Then
                    If m_NewProgramInfo.RepeatMode = "Repeat" Then
                        m_state = eThreadState.ProcessStarting
                    ElseIf m_NewProgramInfo.RepeatMode = "Once" Then
                        If m_ProgramInfo.ProgramPath <> m_NewProgramInfo.ProgramPath Then
                            m_state = eThreadState.ProcessStarting
                        ElseIf m_ProgramInfo.ProgramArguments <> m_NewProgramInfo.ProgramArguments Then
                            m_state = eThreadState.ProcessStarting
                        Else
                            If m_ProgramInfo.RepeatMode = "No" OrElse m_ProgramInfo.RepeatMode = "Repeat" Then
                                m_state = eThreadState.ProcessStarting
                            Else
                                If m_ProgramInfo.HoldoffSeconds <> m_NewProgramInfo.HoldoffSeconds Then
                                    m_state = eThreadState.ProcessStarting
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            m_ProgramInfo.ProgramPath = m_NewProgramInfo.ProgramPath
            m_ProgramInfo.ProgramArguments = m_NewProgramInfo.ProgramArguments
            m_ProgramInfo.RepeatMode = m_NewProgramInfo.RepeatMode
            m_ProgramInfo.HoldoffSeconds = m_NewProgramInfo.HoldoffSeconds

            m_ExitCode =0
            Monitor.Exit(oSync)
        Catch
            mUpdateRequired = mUpdateRequired
        End Try

    End Sub

End Class