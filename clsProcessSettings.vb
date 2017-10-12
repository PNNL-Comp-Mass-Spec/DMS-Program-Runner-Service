Public Class clsProcessSettings

    Protected m_UniqueKey As String = "Undefined"
    Public ReadOnly Property UniqueKey As String
        Get
            Return m_UniqueKey
        End Get
    End Property

    Public Property ProgramPath As String
    Public Property ProgramArguments As String
    Public Property RepeatMode As String
    Public Property HoldoffSeconds As Integer

    Public Sub New(uniqueKeyText As String)
        m_UniqueKey = uniqueKeyText
    End Sub

    Public Overrides Function ToString() As String
        Return m_UniqueKey
    End Function
End Class
