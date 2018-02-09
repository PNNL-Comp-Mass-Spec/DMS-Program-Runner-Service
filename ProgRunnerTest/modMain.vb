Option Strict On

Imports System.Threading

Module modMain

    Sub Main()
        Dim myProgRunner As New ProgRunnerSvc.clsMainProg()

        ' FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Start")

        ' Start the service running
        myProgRunner.StartAllProgRunners()

        ' Wait for 15 seconds
        Thread.Sleep(15 * 1000)

        ' Stop the service
        myProgRunner.StopAllProgRunners()

        ' FileLogger.WriteLog(BaseLogger.LogLevels.INFO, "Stop")

    End Sub

End Module
