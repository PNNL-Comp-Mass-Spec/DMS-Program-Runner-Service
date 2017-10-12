'*********************************************************************************************************
' Written by Dave Clark and Matthew Monroe for the US Department of Energy
' Pacific Northwest National Laboratory, Richland, WA
' Copyright 2009, Battelle Memorial Institute
' Created 01/01/2009
'*********************************************************************************************************

Option Strict On

Imports System.Collections.Generic
Imports System.IO
Imports System.Text.RegularExpressions
Imports log4net.Appender
Imports log4net

'This assembly attribute tells Log4Net where to find the config file
<Assembly: Config.XmlConfigurator(ConfigFile:="Logging.config", Watch:=True)>

Public Class clsLogTools


#Region "Constants"

    Private Const FILE_LOGGER As String = "FileLogger"
    Private Const LOG_FILE_APPENDER As String = "RollingFileAppender"

    ''' <summary>
    ''' Date format for log file names
    ''' </summary>
    ''' <remarks>
    ''' Log files are auto-named by log4net and the name is auto-updated daily
    ''' Configuration is defined in logging.config via the RollingFileAppender entry
    ''' </remarks>
    Public Const LOG_FILE_DATECODE As String = "MM-dd-yyyy"

    Private Const LOG_FILE_MATCH_SPEC As String = "??-??-????"

    Private Const LOG_FILE_DATE_REGEX As String = "(?<Month>\d+)-(?<Day>\d+)-(?<Year>\d{4,4})"

    Private Const LOG_FILE_EXTENSION As String = ".txt"

#End Region

#Region "Enums"
    ''' <summary>
    ''' Log levels
    ''' </summary>
    Public Enum LogLevels
        ''' <summary>
        ''' Debug message
        ''' </summary>
        DEBUG = 5

        ''' <summary>
        ''' Informational message
        ''' </summary>
        INFO = 4

        ''' <summary>
        ''' Warning message
        ''' </summary>
        WARN = 3

        ''' <summary>
        ''' Error message
        ''' </summary>
        [ERROR] = 2

        ''' <summary>
        ''' Fatal error message
        ''' </summary>
        FATAL = 1
    End Enum

    ''' <summary>
    ''' Log types
    ''' </summary>
    Public Enum LoggerTypes
        ''' <summary>
        ''' Log to a log file
        ''' </summary>
        LogFile

        ''' <summary>
        ''' Log to the database and to the log file
        ''' </summary>
        LogDb

        ''' <summary>
        ''' Log to the system event log and to the log file
        ''' </summary>
        LogSystem
    End Enum

#End Region

#Region "Module variables"

    ''' <summary>
    ''' File Logger (RollingFileAppender)
    ''' </summary>
    ''' <remarks>
    ''' Log files are auto-named by log4net and the name is auto-updated daily
    ''' Configuration is defined in logging.config via the RollingFileAppender entry
    ''' </remarks>
    Private Shared ReadOnly m_FileLogger As ILog = LogManager.GetLogger(FILE_LOGGER)

    ''' <summary>
    ''' Database logger
    ''' </summary>
    Private Shared ReadOnly m_DbLogger As ILog = LogManager.GetLogger("DbLogger")

    ''' <summary>
    ''' System event log logger
    ''' </summary>
    Private Shared ReadOnly m_SysLogger As ILog = LogManager.GetLogger("SysLogger")
    Private Shared m_MostRecentErrorMessage As String = String.Empty
#End Region

#Region "Properties"
    ''' <summary>
    ''' Tells calling program file debug status
    ''' </summary>
    ''' <returns>TRUE if debug level enabled for file logger; FALSE otherwise</returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property FileLogDebugEnabled As Boolean
        Get
            Return m_FileLogger.IsDebugEnabled
        End Get
    End Property

    Public Shared ReadOnly Property MostRecentErrorMessage As String
        Get
            Return m_MostRecentErrorMessage
        End Get
    End Property
#End Region

#Region "Methods"
    ''' <summary>
    ''' Writes a message to the logging system
    ''' </summary>
    ''' <param name="LoggerType">Type of logger to use</param>
    ''' <param name="LogLevel">Level of log reporting</param>
    ''' <param name="message">Message to be logged</param>
    ''' <remarks></remarks>
    Public Shared Sub WriteLog(loggerType As LoggerTypes, logLevel As LogLevels, message As String)
        WriteLogWork(loggerType, logLevel, message, Nothing)
    End Sub

    ''' <summary>
    ''' Overload to write a message and exception to the logging system
    ''' </summary>
    ''' <param name="loggerType">Type of logger to use</param>
    ''' <param name="logLevel">Level of log reporting</param>
    ''' <param name="message">Message to be logged</param>
    ''' <param name="ex">Exception to be logged</param>
    ''' <remarks></remarks>
    Public Shared Sub WriteLog(loggerType As LoggerTypes, logLevel As LogLevels, message As String, ex As Exception)
        WriteLogWork(loggerType, logLevel, message, ex)
    End Sub

    ''' <summary>
    ''' Overload to write a message and exception to the logging system
    ''' </summary>
    ''' <param name="loggerType">Type of logger to use</param>
    ''' <param name="logLevel">Level of log reporting</param>
    ''' <param name="message">Message to be logged</param>
    ''' <param name="ex">Exception to be logged</param>
    ''' <remarks></remarks>
    Private Shared Sub WriteLogWork(loggerType As LoggerTypes, logLevel As LogLevels, message As String, ex As Exception)

        Dim myLogger As ILog

        'Establish which logger will be used
        Select Case loggerType
            Case LoggerTypes.LogDb
                ' Note that the Logging.config should have the DbLogger logging to both the database and the rolling file
                myLogger = m_DbLogger
                message = Net.Dns.GetHostName() & ": " & message
            Case LoggerTypes.LogFile
                myLogger = m_FileLogger
            Case LoggerTypes.LogSystem
                myLogger = m_SysLogger
            Case Else
                Throw New Exception("Invalid logger type specified")
        End Select

        If myLogger Is Nothing Then
            Exit Sub
        End If

        'Send the log message
        Select Case logLevel
            Case LogLevels.DEBUG
                If myLogger.IsDebugEnabled Then
                    If ex Is Nothing Then
                        myLogger.Debug(message)
                    Else
                        myLogger.Debug(message, ex)
                    End If
                End If
            Case LogLevels.ERROR
                If myLogger.IsErrorEnabled Then
                    If ex Is Nothing Then
                        myLogger.Error(message)
                    Else
                        myLogger.Error(message, ex)
                    End If
                End If
            Case LogLevels.FATAL
                If myLogger.IsFatalEnabled Then
                    If ex Is Nothing Then
                        myLogger.Fatal(message)
                    Else
                        myLogger.Fatal(message, ex)
                    End If
                End If
            Case LogLevels.INFO
                If myLogger.IsInfoEnabled Then
                    If ex Is Nothing Then
                        myLogger.Info(message)
                    Else
                        myLogger.Info(message, ex)
                    End If
                End If
            Case LogLevels.WARN
                If myLogger.IsWarnEnabled Then
                    If ex Is Nothing Then
                        myLogger.Warn(message)
                    Else
                        myLogger.Warn(message, ex)
                    End If
                End If
            Case Else
                Throw New Exception("Invalid log level specified")
        End Select

        If logLevel <= LogLevels.ERROR Then
            m_MostRecentErrorMessage = message
        End If

    End Sub

    ''' <summary>
    ''' Changes the base log file name
    ''' </summary>
    ''' <param name="relativeFilePath">Log file base name and path (relative to program folder)</param>
    ''' <remarks></remarks>
    Public Shared Sub ChangeLogFileName(relativeFilePath As String)

        'Get a list of appenders
        Dim appendList = FindAppenders(LOG_FILE_APPENDER)
        If appendList Is Nothing Then
            WriteLog(LoggerTypes.LogSystem, LogLevels.WARN, "Unable to change file name. No appender found")
            Return
        End If

        For Each selectedAppender In appendList

            Dim appenderToChange = TryCast(selectedAppender, FileAppender)
            If appenderToChange Is Nothing Then
                WriteLog(LoggerTypes.LogSystem, LogLevels.ERROR, "Unable to convert appender since not a FileAppender")
                Return
            End If

            'Change the file name and activate change
            appenderToChange.File = relativeFilePath
            appenderToChange.ActivateOptions()
        Next
    End Sub

    ''' <summary>
    ''' Gets the specified appender
    ''' </summary>
    ''' <param name="appenderName">Name of appender to find</param>
    ''' <returns>List(IAppender) objects if found; NOTHING otherwise</returns>
    ''' <remarks></remarks>
    Private Shared Function FindAppenders(appenderName As String) As IEnumerable(Of IAppender)

        'Get a list of the current loggers
        Dim LoggerList() As ILog = LogManager.GetCurrentLoggers()
        If LoggerList.GetLength(0) < 1 Then Return Nothing

        'Create a List of appenders matching the criteria for each logger
        Dim retList As New List(Of IAppender)
        For Each TestLogger As ILog In LoggerList
            For Each TestAppender As IAppender In TestLogger.Logger.Repository.GetAppenders()
                If TestAppender.Name = appenderName Then retList.Add(TestAppender)
            Next
        Next

        'Return the list of appenders, if any found
        If retList.Count > 0 Then
            Return retList
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Sets the file logging level via an integer value (Overloaded)
    ''' </summary>
    ''' <param name="logLevel">Integer corresponding to level (1-5, 5 being most verbose</param>
    ''' <remarks></remarks>
    Public Shared Sub SetFileLogLevel(logLevel As Integer)

        Dim logLevelEnumType As Type = GetType(LogLevels)

        'Verify input level is a valid log level
        If Not [Enum].IsDefined(logLevelEnumType, logLevel) Then
            WriteLog(LoggerTypes.LogFile, LogLevels.ERROR, "Invalid value specified for level: " & logLevel.ToString)
            Return
        End If

        'Convert input integer into the associated enum
        Dim logLevelEnum = DirectCast([Enum].Parse(logLevelEnumType, logLevel.ToString), LogLevels)
        SetFileLogLevel(logLevelEnum)

    End Sub

    ''' <summary>
    ''' Sets file logging level based on enumeration (Overloaded)
    ''' </summary>
    ''' <param name="logLevel">LogLevels value defining level (Debug is most verbose)</param>
    ''' <remarks></remarks>
    Public Shared Sub SetFileLogLevel(logLevel As LogLevels)

        Dim logger = DirectCast(m_FileLogger.Logger, Repository.Hierarchy.Logger)

        Select Case logLevel
            Case LogLevels.DEBUG
                logger.Level = logger.Hierarchy.LevelMap("DEBUG")
            Case LogLevels.ERROR
                logger.Level = logger.Hierarchy.LevelMap("ERROR")
            Case LogLevels.FATAL
                logger.Level = logger.Hierarchy.LevelMap("FATAL")
            Case LogLevels.INFO
                logger.Level = logger.Hierarchy.LevelMap("INFO")
            Case LogLevels.WARN
                logger.Level = logger.Hierarchy.LevelMap("WARN")
        End Select
    End Sub
#End Region

End Class

