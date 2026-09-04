using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests;

[DataContract]
public class ValidationTestDto
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string Name { get; set; }
}

[TestFixture]
public class ValidationModernizationTests
{
    [Test]
    public void ValidatorCache_NullGuards()
    {
        Assert.That(ValidatorCache.GetValidator(null, null), Is.Null);
        Assert.That(ValidatorCache.GetValidator(null, typeof(ValidationTestDto)), Is.Null);
        Assert.That(ValidatorCache<ValidationTestDto>.GetValidator(null), Is.Null);
    }

    [Test]
    public void MultiRuleSetValidatorSelector_NullGuards()
    {
        MultiRuleSetValidatorSelector selector = null;
        Assert.DoesNotThrow(() => selector = new MultiRuleSetValidatorSelector(null));
        Assert.That(selector, Is.Not.Null);
        Assert.That(selector.CanExecute(null, "Prop", null), Is.True);
    }

    [Test]
    public void ValidationResultExtensions_NullGuards()
    {
        ValidationResult nullResult = null;
        var errorResultFromNull = nullResult.ToErrorResult();
        Assert.That(errorResultFromNull, Is.Not.Null);
        Assert.That(errorResultFromNull.Errors, Is.Empty);

        var exceptionFromNull = nullResult.ToException();
        Assert.That(exceptionFromNull, Is.Not.Null);
        Assert.That(exceptionFromNull.Violations, Is.Empty);

        var emptyResult = new ValidationResult();
        var errorResult = emptyResult.ToErrorResult();
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult.Errors, Is.Empty);

        var resultWithNull = new ValidationResult(new[] { (ValidationFailure)null });
        var errorResultWithNull = resultWithNull.ToErrorResult();
        Assert.That(errorResultWithNull, Is.Not.Null);
        Assert.That(errorResultWithNull.Errors, Is.Empty);

        var resultWithCustomState = new ValidationResult(new[] {
            new ValidationFailure("Prop", "Error") { CustomState = new Dictionary<string, string> { ["foo"] = "bar" } }
        });
        var errWithCustom = resultWithCustomState.ToErrorResult();
        Assert.That(errWithCustom.Errors[0].Meta?["foo"], Is.EqualTo("bar"));

        var resultWithoutCustomState = new ValidationResult(new[] {
            new ValidationFailure("Prop", "Error")
        });
        var errWithoutCustom = resultWithoutCustomState.ToErrorResult();
        Assert.That(errWithoutCustom.Errors[0].Meta, Is.Null);
    }

    [Test]
    public async Task MemoryValidationSource_NullGuards()
    {
        var source = new MemoryValidationSource();
        Assert.That(source.GetValidationRules(null), Is.Empty);

        var allRules = await source.GetAllValidateRulesAsync(null);
        Assert.That(allRules, Is.Empty);

        Assert.DoesNotThrow(() => source.SaveValidationRules(null));
        Assert.DoesNotThrow(() => source.SaveValidationRules(new List<ValidationRule>()));

        var rulesByIds = await source.GetValidateRulesByIdsAsync(null);
        Assert.That(rulesByIds, Is.Empty);

        Assert.DoesNotThrowAsync(async () => await source.DeleteValidationRulesAsync(null));
    }

    [Test]
    public void ValidateScripts_NullGuards()
    {
        var scripts = new ValidateScripts();
        Assert.DoesNotThrow(() => scripts.HasRole(null));
        Assert.DoesNotThrow(() => scripts.HasRoles(null));
        Assert.DoesNotThrow(() => scripts.HasAnyRole(null));
        Assert.DoesNotThrow(() => scripts.HasPermission(null));
        Assert.DoesNotThrow(() => scripts.HasPermissions(null));
        Assert.DoesNotThrow(() => scripts.RegularExpression(null));
    }

    [Test]
    public void ValidatorUtils_NullGuards()
    {
        Assert.That(ValidatorUtils.Init(null, null), Is.Null);
        var mockTypeValidator = new IsAuthenticatedValidator();
        Assert.That(mockTypeValidator.Init(null), Is.SameAs(mockTypeValidator));
    }

    [Test]
    public void Validators_TypeChecks_NullGuards()
    {
        Assert.That(Validators.HasValidateRequestAttributes(null), Is.False);
        Assert.That(Validators.HasValidateAttributes(null), Is.False);
    }

    [Test]
    public void ExecOnceOnly_NullGuards()
    {
        Assert.Throws<ArgumentNullException>(() => new ExecOnceOnly(null, (Type)null, "corr-1"));
        Assert.Throws<ArgumentNullException>(() => new ExecOnceOnly(null, (Type)null, (Guid?)Guid.NewGuid()));
    }
}
