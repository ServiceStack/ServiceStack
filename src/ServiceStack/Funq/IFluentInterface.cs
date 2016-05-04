using System;
using System.ComponentModel;

namespace Funq
{
    /// <summary>
    /// Helper interface used to hide the base <see cref="Object"/> 
    /// members from the fluent API to make for much cleaner 
    /// Visual Studio intellisense experience.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentInterface
    {
        /// <summary/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        /// <summary/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        /// <summary/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();

        /// <summary/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);
    }
}
