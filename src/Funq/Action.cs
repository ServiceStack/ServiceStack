
namespace Funq
{
#if CF20
	/// <summary>
	/// Encapsulates a method that has one parameter and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate void Action<T>(T arg);
	/// <summary>
	/// Encapsulates a method that has two parameters.
	/// </summary>
	public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
	/// <summary>
	/// Encapsulates a method that has three parameters.
	/// </summary>
	public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
	/// <summary>
	/// Encapsulates a method that has four parameters.
	/// </summary>
	public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
#endif
}
