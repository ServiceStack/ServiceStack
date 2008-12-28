using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Sakila.DataAccess.Base
{
    interface IBusinessBase<T>
    {
        // Methods
        int GetHashCode();
        bool Equals(object obj);

        // Properties
        T Id { get; }
    }

    /// <summary>
    /// Base for all business objects.
    /// 
    /// For an explanation of why Equals and GetHashCode are overriden, read the following...
    /// http://devlicio.us/blogs/billy_mccafferty/archive/2007/04/25/using-equals-gethashcode-effectively.aspx
    /// </summary>
    /// <typeparam name="T">DataType of the primary key.</typeparam>
    public abstract class BusinessBase<T> : IBusinessBase<T>
    {
        #region Declarations

        private T _id = default(T);

        #endregion

        #region Methods

        public abstract override int GetHashCode();
        public override bool Equals(object obj)
        {
            return (obj != null)                                                    // 1) Object is not null.
                && (obj.GetType() == this.GetType())                                // 2) Object is of same Type.
                && (MatchingIds((BusinessBase<T>)obj) || MatchingHashCodes(obj));   // 3) Ids or Hashcodes match.
        }
        private bool MatchingIds(BusinessBase<T> obj)
        {
            return (this.Id != null && !this.Id.Equals(default(T)))                 // 1) this.Id is not null/default.
                && (obj.Id != null && !obj.Id.Equals(default(T)))                   // 1.5) obj.Id is not null/default.
                && (this.Id.Equals(obj.Id));                                        // 2) Ids match.
        }
        private bool MatchingHashCodes(object obj)
        {
            return this.GetHashCode().Equals(obj.GetHashCode());                    // 1) Hashcodes match.
        }

        #endregion

        #region Properties

        public virtual T Id
        {
            get { return _id; }
            set { _id = value; }
        }

        #endregion
    }
}
