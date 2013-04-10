
namespace ServiceStack.Html
{
	internal interface IResolver<T>
	{
		T Current { get; }
	}
}
