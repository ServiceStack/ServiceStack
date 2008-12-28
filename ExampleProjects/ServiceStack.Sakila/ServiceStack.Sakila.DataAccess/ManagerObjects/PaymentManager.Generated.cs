using System;
using System.Collections.Generic;
using System.Text;

using NHibernate;
using ServiceStack.Sakila.DataAccess.DataModel;
using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.ManagerObjects
{
    public partial interface IPaymentManager : IManagerBase<Payment, ushort>
    {
		// Get Methods
		IList<Payment> GetBycustomer_id(System.UInt16 customerId);
		IList<Payment> GetByrental_id(System.Int32 rentalId);
		IList<Payment> GetBystaff_id(System.Byte staffId);
    }

    partial class PaymentManager : ManagerBase<Payment, ushort>, IPaymentManager
    {
		#region Constructors
		
		public PaymentManager() : base()
        {
        }
        public PaymentManager(INHibernateSession session) : base(session)
        {
        }
		
		#endregion
		
        #region Get Methods

		
		public IList<Payment> GetBycustomer_id(System.UInt16 customerId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Payment));
			
			ICriteria customerCriteria = criteria.CreateCriteria("CustomerMember");
            customerCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", customerId));
			
			return criteria.List<Payment>();
        }
		
		public IList<Payment> GetByrental_id(System.Int32 rentalId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Payment));
			
			ICriteria rentalCriteria = criteria.CreateCriteria("RentalMember");
            rentalCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", rentalId));
			
			return criteria.List<Payment>();
        }
		
		public IList<Payment> GetBystaff_id(System.Byte staffId)
        {
            ICriteria criteria = Session.GetISession().CreateCriteria(typeof(Payment));
			
			ICriteria staffCriteria = criteria.CreateCriteria("StaffMember");
            staffCriteria.Add(NHibernate.Criterion.Expression.Eq("Id", staffId));
			
			return criteria.List<Payment>();
        }
		
		#endregion
    }
}