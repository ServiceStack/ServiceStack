using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@Order : BusinessBase<uint>
    {
        #region Declarations

		
		private byte[] _userGlobalId = null;
		private System.DateTime _createdDate = new DateTime();
		private string _createdBy = String.Empty;
		private string _cardName = String.Empty;
		private string _cardNo = String.Empty;
		private string _cardCvv = String.Empty;
		private System.DateTime _cardExpiryDate = new DateTime();
		private decimal _total = default(Decimal);
		
		private @ModelName@ _user = null;
		
		private IList<@ModelName@OrderLineItem> _userOrderLineItems = new List<@ModelName@OrderLineItem>();
		private IList<@ModelName@Product> _userProducts = new List<@ModelName@Product>();
		
        #endregion

        #region Constructors

        public @ModelName@Order() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_userGlobalId);
			sb.Append(_createdDate);
			sb.Append(_createdBy);
			sb.Append(_cardName);
			sb.Append(_cardNo);
			sb.Append(_cardCvv);
			sb.Append(_cardExpiryDate);
			sb.Append(_total);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual byte[] @ModelName@GlobalId
        {
            get { return _userGlobalId; }
			set
			{
				On@ModelName@GlobalIdChanging();
				_userGlobalId = value;
				On@ModelName@GlobalIdChanged();
			}
        }
		partial void On@ModelName@GlobalIdChanging();
		partial void On@ModelName@GlobalIdChanged();
		
		public virtual System.DateTime CreatedDate
        {
            get { return _createdDate; }
			set
			{
				OnCreatedDateChanging();
				_createdDate = value;
				OnCreatedDateChanged();
			}
        }
		partial void OnCreatedDateChanging();
		partial void OnCreatedDateChanged();
		
		public virtual string CreatedBy
        {
            get { return _createdBy; }
			set
			{
				OnCreatedByChanging();
				_createdBy = value;
				OnCreatedByChanged();
			}
        }
		partial void OnCreatedByChanging();
		partial void OnCreatedByChanged();
		
		public virtual string CardName
        {
            get { return _cardName; }
			set
			{
				OnCardNameChanging();
				_cardName = value;
				OnCardNameChanged();
			}
        }
		partial void OnCardNameChanging();
		partial void OnCardNameChanged();
		
		public virtual string CardNo
        {
            get { return _cardNo; }
			set
			{
				OnCardNoChanging();
				_cardNo = value;
				OnCardNoChanged();
			}
        }
		partial void OnCardNoChanging();
		partial void OnCardNoChanged();
		
		public virtual string CardCvv
        {
            get { return _cardCvv; }
			set
			{
				OnCardCvvChanging();
				_cardCvv = value;
				OnCardCvvChanged();
			}
        }
		partial void OnCardCvvChanging();
		partial void OnCardCvvChanged();
		
		public virtual System.DateTime CardExpiryDate
        {
            get { return _cardExpiryDate; }
			set
			{
				OnCardExpiryDateChanging();
				_cardExpiryDate = value;
				OnCardExpiryDateChanged();
			}
        }
		partial void OnCardExpiryDateChanging();
		partial void OnCardExpiryDateChanged();
		
		public virtual decimal Total
        {
            get { return _total; }
			set
			{
				OnTotalChanging();
				_total = value;
				OnTotalChanged();
			}
        }
		partial void OnTotalChanging();
		partial void OnTotalChanged();
		
		public virtual @ModelName@ @ModelName@Member
        {
            get { return _user; }
			set
			{
				On@ModelName@MemberChanging();
				_user = value;
				On@ModelName@MemberChanged();
			}
        }
		partial void On@ModelName@MemberChanging();
		partial void On@ModelName@MemberChanged();
		
		public virtual IList<@ModelName@OrderLineItem> @ModelName@OrderLineItems
        {
            get { return _userOrderLineItems; }
            set
			{
				On@ModelName@OrderLineItemsChanging();
				_userOrderLineItems = value;
				On@ModelName@OrderLineItemsChanged();
			}
        }
		partial void On@ModelName@OrderLineItemsChanging();
		partial void On@ModelName@OrderLineItemsChanged();
		
		public virtual IList<@ModelName@Product> @ModelName@Products
        {
            get { return _userProducts; }
            set
			{
				On@ModelName@ProductsChanging();
				_userProducts = value;
				On@ModelName@ProductsChanged();
			}
        }
		partial void On@ModelName@ProductsChanging();
		partial void On@ModelName@ProductsChanged();
		
        #endregion
    }
}
