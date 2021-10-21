using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Script;

namespace ServiceStack
{
    public class ValidateScripts : ScriptMethods
    {
        public static HashSet<string> RequiredValidators { get; } = new() {
            nameof(NotNull),
            nameof(NotEmpty),
        };
        
        public static ValidateScripts Instance = new();

        //Note: Can't use singleton validators in-case ErrorCode/Messages are customized 

        public IPropertyValidator Null() => new NullValidator();
        public IPropertyValidator Empty() => new EmptyValidator(null);
        public IPropertyValidator Empty(object defaultValue) => new EmptyValidator(defaultValue);
        public IPropertyValidator Equal(object value) => new EqualValidator(value);
        public IPropertyValidator NotNull() => new NotNullValidator();
        public IPropertyValidator NotEmpty() => new NotEmptyValidator(null);
        public IPropertyValidator NotEmpty(object defaultValue) => new NotEmptyValidator(defaultValue);
        public IPropertyValidator NotEqual(object value) => new NotEqualValidator(value);

        public IPropertyValidator CreditCard() => new CreditCardValidator();
        public IPropertyValidator Email() => new AspNetCoreCompatibleEmailValidator();

        public IPropertyValidator Length(int min, int max) => new LengthValidator(min, max);
        public IPropertyValidator ExactLength(int length) => new ExactLengthValidator(length);
        public IPropertyValidator MaximumLength(int max) => new MaximumLengthValidator(max);
        public IPropertyValidator MinimumLength(int min) => new MinimumLengthValidator(min);

        public IPropertyValidator InclusiveBetween(IComparable from, IComparable to) =>
            new InclusiveBetweenValidator(from, to);

        public IPropertyValidator ExclusiveBetween(IComparable from, IComparable to) =>
            new ExclusiveBetweenValidator(from, to);

        public IPropertyValidator LessThan(int value) => new LessThanValidator(value);
        public IPropertyValidator LessThanOrEqual(int value) => new LessThanOrEqualValidator(value);
        public IPropertyValidator GreaterThan(int value) => new GreaterThanValidator(value);
        public IPropertyValidator GreaterThanOrEqual(int value) => new GreaterThanOrEqualValidator(value);

        public IPropertyValidator ScalePrecision(int scale, int precision) =>
            new ScalePrecisionValidator(scale, precision);

        public IPropertyValidator RegularExpression(string regex) =>
            new RegularExpressionValidator(regex, RegexOptions.Compiled);

        public IPropertyValidator Enum(Type enumType) => new EnumValidator(enumType);
        
        public ITypeValidator IsAuthenticated() => new IsAuthenticatedValidator();
        public ITypeValidator IsAuthenticated(string provider) => new IsAuthenticatedValidator(provider);
        public ITypeValidator HasRole(string role) => new HasRolesValidator(role);
        public ITypeValidator HasRoles(string[] roles) => new HasRolesValidator(roles);
        public ITypeValidator HasPermission(string permission) => new HasPermissionsValidator(permission);
        public ITypeValidator HasPermissions(string[] permission) => new HasPermissionsValidator(permission);
        public ITypeValidator IsAdmin() => new HasRolesValidator(RoleNames.Admin);
    }
    
}