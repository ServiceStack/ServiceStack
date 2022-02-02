Imports NUnit.Framework

Public Class Table
    Public Property Id As Integer
    public Property IsActive As Boolean
    public Property IsDeleted As Boolean
End Class

<TestFixture> _
Public Class VbTests
    
    <Test> _
    Public Sub Test_and_Expression()
        
        OrmLiteUtils.PrintSql()
 
        Dim dbFactory as New OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        Using db As IDbConnection = dbFactory.Open()
            
            db.CreateTable(Of Table)
            
            Dim q = db.From(Of Table) _
                .Where(Function(t) t.IsDeleted = False And t.IsActive = False)
            
            Dim results = db.Select(q)
            
        End Using
        
    End Sub
    
End Class