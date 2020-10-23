using System;

namespace ServiceStack.Model
{
    public interface IAudit 
    {
        DateTime CreatedDate { get; set; }
        string CreatedBy { get; set; }
        DateTime ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
        DateTime? SoftDeletedDate { get; set; }
        string SoftDeletedBy { get; set; }
    }
}