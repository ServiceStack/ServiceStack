using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack;

/// <summary>
/// Useful base Data Model class for entities you want to maintain Audit information for.
/// Property names match conventions used to populate Audit info in [AutoApply(Behavior.Audit*)] 
/// </summary>
[DataContract]
public abstract class AuditBase
{
    [DataMember(Order = 1)]
    public DateTime CreatedDate { get; set; }

    [DataMember(Order = 2), Required]
    public string CreatedBy { get; set; }

    [DataMember(Order = 3)]
    public DateTime ModifiedDate { get; set; }

    [DataMember(Order = 4), Required]
    public string ModifiedBy { get; set; }

    [DataMember(Order = 5), Index] //Check if Deleted
    public DateTime? DeletedDate { get; set; }

    [DataMember(Order = 6)]
    public string DeletedBy { get; set; }
}