
namespace ServiceStack
{
	internal interface IResolver<T>
	{
		T Current { get; }
	}
}
