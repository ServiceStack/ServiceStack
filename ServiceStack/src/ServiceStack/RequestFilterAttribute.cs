using System;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Base class to create request filter attributes only for specific HTTP methods (GET, POST...)
/// </summary>
public abstract class RequestFilterAttribute : AttributeBase, IHasRequestFilter
{
    public int Priority { get; set; }

    public ApplyTo ApplyTo { get; set; }

    public RequestFilterAttribute() : this(ApplyTo.All) {}

    /// <summary>
    /// Creates a new <see cref="RequestFilterAttribute"/>
    /// </summary>
    /// <param name="applyTo">Defines when the filter should be executed</param>
    public RequestFilterAttribute(ApplyTo applyTo) => ApplyTo = applyTo;

    public void RequestFilter(IRequest req, IResponse res, object requestDto)
    {
        ApplyTo httpMethod = req.HttpMethodAsApplyTo();
        if (ApplyTo.Has(httpMethod))
            this.Execute(req, res, requestDto);
    }

    /// <summary>
    /// This method is only executed if the HTTP method matches the <see cref="ApplyTo"/> property.
    /// </summary>
    /// <param name="req">The http request wrapper</param>
    /// <param name="res">The http response wrapper</param>
    /// <param name="requestDto">The request DTO</param>
    public abstract void Execute(IRequest req, IResponse res, object requestDto);

    /// <summary>
    /// Create a ShallowCopy of this instance.
    /// </summary>
    /// <returns></returns>
    public virtual IRequestFilterBase Copy() => (IRequestFilterBase)MemberwiseClone();
}

/// <summary>
/// Base class to create request filter attributes only for specific HTTP methods (GET, POST...)
/// </summary>
public abstract class RequestFilterAsyncAttribute : AttributeBase, IHasRequestFilterAsync
{
    public int Priority { get; set; }

    public ApplyTo ApplyTo { get; set; }

    public RequestFilterAsyncAttribute() : this(ApplyTo.All) {}

    /// <summary>
    /// Creates a new <see cref="RequestFilterAttribute"/>
    /// </summary>
    /// <param name="applyTo">Defines when the filter should be executed</param>
    public RequestFilterAsyncAttribute(ApplyTo applyTo) => ApplyTo = applyTo;

    public Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
    {
        var httpMethod = req.HttpMethodAsApplyTo();
        if (ApplyTo.Has(httpMethod))
            return this.ExecuteAsync(req, res, requestDto);

        return TypeConstants.EmptyTask;
    }

    /// <summary>
    /// This method is only executed if the HTTP method matches the <see cref="ApplyTo"/> property.
    /// </summary>
    /// <param name="req">The http request wrapper</param>
    /// <param name="res">The http response wrapper</param>
    /// <param name="requestDto">The request DTO</param>
    public abstract Task ExecuteAsync(IRequest req, IResponse res, object requestDto);

    /// <summary>
    /// Create a ShallowCopy of this instance.
    /// </summary>
    /// <returns></returns>
    public virtual IRequestFilterBase Copy() => (IRequestFilterBase)MemberwiseClone();
}