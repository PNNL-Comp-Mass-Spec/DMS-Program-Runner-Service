Option Strict On

Imports System.Threading


Module modMain
    Sub Main()
        Dim MyProgRunner As New ProgRunnerSvc.clsMainProg()

        'clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Start")

        'Start the service running
        MyProgRunner.StartAllProgRunners()

        ' Wait for 15 seconds
        Thread.Sleep(15 * 1000)

        'Stop the service
        MyProgRunner.StopAllProgRunners()
        MyProgRunner = Nothing

        'clsLogTools.WriteLog(clsLogTools.LoggerTypes.LogFile, clsLogTools.LogLevels.INFO, "Stop")

    End Sub
End Module
