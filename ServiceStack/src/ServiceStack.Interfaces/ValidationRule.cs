#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack;

public interface IValidateRule
{
    string Validator { get; set; }
    string? Condition { get; set; }
    string? ErrorCode { get; set; }
    string? Message { get; set; }
}

public class ValidateRule : IValidateRule 
{
    public string Validator { get; set; }
    public string? Condition { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

public interface IValidationSource
{
    IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type);
}

public interface IValidationSourceAdmin
{
    List<ValidationRule> GetAllValidateRules();
    Task<List<ValidationRule>> GetAllValidateRulesAsync();
    Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName);
    void SaveValidationRules(List<ValidationRule> validateRules);
    Task SaveValidationRulesAsync(List<ValidationRule> validateRules);
    Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids);
    Task DeleteValidationRulesAsync(params int[] ids);
    Task ClearCacheAsync();
}

/// <summary>
/// Data persistence Model 
/// </summary>
public class ValidationRule : ValidateRule
{
    [AutoIncrement]
    public int Id { get; set; }
        
    /// <summary>
    /// The name of the Type 
    /// </summary>
    [Required]
    public string Type { get; set; }
        
    /// <summary>
    /// The property field for Property Validators, null for Type Validators 
    /// </summary>
    public string? Field { get; set; }
        
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
        
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
        
    public string? SuspendedBy { get; set; }
    [Index]
    public DateTime? SuspendedDate { get; set; }

    public string? Notes { get; set; }

    protected bool Equals(ValidationRule other)
    {
        return Id == other.Id &&
               Type == other.Type && Field == other.Field &&
               CreatedBy == other.CreatedBy && Nullable.Equals(CreatedDate, other.CreatedDate) &&
               ModifiedBy == other.ModifiedBy && Nullable.Equals(ModifiedDate, other.ModifiedDate) &&
               SuspendedBy == other.SuspendedBy && Nullable.Equals(SuspendedDate, other.SuspendedDate) &&
               Notes == other.Notes;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ValidationRule) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id;
            hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Field != null ? Field.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (CreatedBy != null ? CreatedBy.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CreatedDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (ModifiedBy != null ? ModifiedBy.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ ModifiedDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (SuspendedBy != null ? SuspendedBy.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ SuspendedDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (Notes != null ? Notes.GetHashCode() : 0);
            return hashCode;
        }
    }
}