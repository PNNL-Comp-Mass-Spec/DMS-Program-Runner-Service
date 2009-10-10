Option Strict On

Imports System.IO
Imports System.Threading


Module Module1
    Sub Main()
        Dim MyProgRunner As New ProgRunnerSvc.clsMainProg()

        'clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Start")

        'Start the service running
        MyProgRunner.StartAllProgRunners()

        ' Wait for 10 minutes seconds
        Thread.Sleep(10 * 60 * 1000)

        'Stop the service
        MyProgRunner.StopAllProgRunners()
        MyProgRunner = Nothing

        'clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Stop")

    End Sub
End Module
