
namespace Funq
{
#if CF20
	/// <summary>
	/// Encapsulates a method that has no parameters and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate TResult Func<TResult>();
	/// <summary>
	/// Encapsulates a method that has one parameter and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate TResult Func<T, TResult>(T arg);
	/// <summary>
	/// Encapsulates a method that has two parameters and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
	/// <summary>
	/// Encapsulates a method that has three parameters and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
	/// <summary>
	/// Encapsulates a method that has four parameters and returns a value of the 
	///  type specified by the <typeparamref name="TResult"/> parameter.
	/// </summary>
	public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
#endif
    /// <summary>
    /// Encapsulates a method that has five parameters and returns a value of the 
    ///  type specified by the <typeparamref name="TResult"/> parameter.
    /// </summary>
    public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    /// <summary>
    /// Encapsulates a method that has six parameters and returns a value of the 
    ///  type specified by the <typeparamref name="TResult"/> parameter.
    /// </summary>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    /// <summary>
    /// Encapsulates a method that has seven parameters and returns a value of the 
    ///  type specified by the <typeparamref name="TResult"/> parameter.
    /// </summary>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
}
