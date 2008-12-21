Public Class MainForm
    Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents myLabel As System.Windows.Forms.Label
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.myLabel = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'myLabel
        '
        Me.myLabel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.myLabel.Font = New System.Drawing.Font("Tahoma", 24.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.myLabel.Name = "myLabel"
        Me.myLabel.Size = New System.Drawing.Size(304, 118)
        Me.myLabel.TabIndex = 0
        Me.myLabel.Text = "Hello Windows Forms"
        Me.myLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'MainForm
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(304, 118)
        Me.Controls.AddRange(New System.Windows.Forms.Control() {Me.myLabel})
        Me.Name = "MainForm"
        Me.Text = "Hello Windows Forms"
        Me.ResumeLayout(False)

    End Sub

#End Region

End Class
