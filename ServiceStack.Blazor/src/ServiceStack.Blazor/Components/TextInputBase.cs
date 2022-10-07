#pragma warning disable CA1822 // Mark members as static

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public abstract class TextInputBase : ApiComponentBase
{
    protected EventHandler<Microsoft.AspNetCore.Components.Forms.ValidationStateChangedEventArgs> _validationStateChangedHandler;
    protected bool _hasInitializedParameters;
    protected bool _previousParsingAttemptFailed;
    protected Type? _nullableUnderlyingType;

    protected TextInputBase()
    {
        _validationStateChangedHandler = OnValidateStateChanged;
    }

    protected override string StateClasses(string? valid = null, string? invalid = null) => UseStatus?.FieldError(Id!) == null
        ? valid ?? ""
        : invalid ?? "";

    protected override string CssClass(string? valid = null, string? invalid = null) =>
        CssUtils.ClassNames(StateClasses(valid, invalid), @class);

    [Parameter]
    public virtual string? Id { get; set; }

    [Parameter]
    public string Size { get; set; } = "md";

    [Parameter]
    public string? placeholder { get; set; }

    /// <summary>
    /// Additional help text for Input Control, defaults to split Pascal Case Id.
    /// Set to empty string "" to hide
    /// </summary>
    [Parameter]
    public string? Help { get; set; }

    /// <summary>
    /// Label assigned to the Input Control, defaults to split Pascal Case Id.
    /// Set to empty string "" to hide
    /// </summary>
    [Parameter]
    public string? Label { get; set; }
    [Parameter] public string? LabelClass { get; set; }

    [Parameter] public string? type { get; set; }
    protected virtual string UseType => type ?? "text";
    protected bool IsCheckbox => UseType == "checkbox";
    protected bool IsSelect => UseType == "select";


    public bool HasErrorField => UseStatus.HasErrorField(Id!);
    public ResponseError? ErrorField => UseStatus.FieldError(Id!);
    public string? ErrorFieldMessage => UseStatus.FieldError(Id!)?.Message;

    protected virtual string UseLabel => Label ?? TextUtils.Humanize(Id!);

    protected virtual string UsePlaceholder => placeholder ?? UseLabel;

    protected virtual string UseHelp => Help ?? "";


    //from: https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/InputBase.cs

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public virtual IReadOnlyDictionary<string, object>? IncludeAttributes => SanitizeAttributes(AdditionalAttributes);

    /// <summary>
    /// Gets the <see cref="FieldIdentifier"/> for the bound value.
    /// </summary>
    protected internal Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { get; set; }

    protected void OnValidateStateChanged(object? sender, Microsoft.AspNetCore.Components.Forms.ValidationStateChangedEventArgs eventArgs)
    {
        UpdateAdditionalValidationAttributes();

        StateHasChanged();
    }

    protected void UpdateAdditionalValidationAttributes()
    {
    }

    public static bool SanitizeAttribute(string name) => name == "@bind" || name.StartsWith("@bind:");

    public static IReadOnlyDictionary<string, object>? SanitizeAttributes(IReadOnlyDictionary<string, object>? attrs)
    {
        if (attrs == null) return null;
        var safeAttrs = new Dictionary<string, object>();
        foreach (var attr in attrs)
        {
            if (SanitizeAttribute(attr.Key))
                continue;
            safeAttrs[attr.Key] = attr.Value;
        }
        return safeAttrs;
    }
}

public abstract class TextInputBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : TextInputBase
{

    /// <summary>
    /// Gets or sets the value of the input. This should be used with two-way binding.
    /// </summary>
    /// <example>
    /// @bind-Value="model.PropertyName"
    /// </example>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets a callback that updates the bound value.
    /// </summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets an expression that identifies the bound value.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// Gets or sets the current value of the input.
    /// </summary>
    protected TValue? CurrentValue
    {
        get => Value;
        set
        {
            var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, Value);
            if (hasChanged)
            {
                Value = value;
                _ = ValueChanged.InvokeAsync(Value);
                //EditContext?.NotifyFieldChanged(FieldIdentifier);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current value of the input, represented as a string.
    /// </summary>
    protected string? CurrentValueAsString
    {
        get => FormatValueAsString(CurrentValue);
        set
        {
            //_parsingValidationMessages?.Clear();

            bool parsingFailed;

            if (_nullableUnderlyingType != null && string.IsNullOrEmpty(value))
            {
                // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
                // Then all subclasses get nullable support almost automatically (they just have to
                // not reject Nullable<T> based on the type itself).
                parsingFailed = false;
                CurrentValue = default!;
            }
            else if (TryParseValueFromString(value, out var parsedValue, out var validationErrorMessage))
            {
                parsingFailed = false;
                CurrentValue = parsedValue!;
            }
            else
            {
                parsingFailed = true;

                // EditContext may be null if the input is not a child component of EditForm.
                if (UseStatus is not null)
                {
                    UseStatus!.AddFieldError(Id!, validationErrorMessage);
                }
            }

            // We can skip the validation notification if we were previously valid and still are
            if (parsingFailed || _previousParsingAttemptFailed)
            {
                _previousParsingAttemptFailed = parsingFailed;
            }
        }
    }

    /// <summary>
    /// Formats the value as a string. Derived classes can override this to determine the formating used for <see cref="CurrentValueAsString"/>.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A string representation of the value.</returns>
    protected virtual string? FormatValueAsString(TValue? value)
        => value?.ToString();

    /// <summary>
    /// Parses a string to create an instance of <typeparamref name="TValue"/>. Derived classes can override this to change how
    /// <see cref="CurrentValueAsString"/> interprets incoming values.
    /// </summary>
    /// <param name="value">The string value to be parsed.</param>
    /// <param name="result">An instance of <typeparamref name="TValue"/>.</param>
    /// <param name="validationErrorMessage">If the value could not be parsed, provides a validation error message.</param>
    /// <returns>True if the value could be parsed; otherwise false.</returns>
    protected virtual bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        try
        {
            result = value.ConvertTo<TValue>();
            validationErrorMessage = "";
            return true;
        }
        catch (Exception e)
        {
            result = default;
            validationErrorMessage = $"The {Id} field is not valid. ({e.Message})";
            return false;
        }
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!_hasInitializedParameters)
        {
            // This is the first run
            // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInit to call base.OnInit()
            if (ValueExpression == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                                                    $"parameter. Normally this is provided automatically when using 'bind-Value'.");
            }

            FieldIdentifier = Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValueExpression);
            if (Id == null)
                Id = FieldIdentifier.FieldName;

            _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));
            _hasInitializedParameters = true;
        }

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.Empty);
    }
}