using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class CreditCardInfo : BusinessBase<uint>
    {
        #region Declarations

		
		private byte _isActive = default(Byte);
		private string _cardHolderName = String.Empty;
		private string _cardNumber = String.Empty;
		private string _cardCvv = String.Empty;
		private System.DateTime _cardExpiryDate = new DateTime();
		private string _billingAddressLine1 = String.Empty;
		private string _billingAddressLine2 = String.Empty;
		private string _billingAddressTown = String.Empty;
		private string _billingAddressCounty = String.Empty;
		private string _billingAddressPostCode = String.Empty;
		private string _cardType = String.Empty;
		
		private @ModelName@ _user = null;
		
		
        #endregion

        #region Constructors

        public CreditCardInfo() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_isActive);
			sb.Append(_cardHolderName);
			sb.Append(_cardNumber);
			sb.Append(_cardCvv);
			sb.Append(_cardExpiryDate);
			sb.Append(_billingAddressLine1);
			sb.Append(_billingAddressLine2);
			sb.Append(_billingAddressTown);
			sb.Append(_billingAddressCounty);
			sb.Append(_billingAddressPostCode);
			sb.Append(_cardType);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual byte IsActive
        {
            get { return _isActive; }
			set
			{
				OnIsActiveChanging();
				_isActive = value;
				OnIsActiveChanged();
			}
        }
		partial void OnIsActiveChanging();
		partial void OnIsActiveChanged();
		
		public virtual string CardHolderName
        {
            get { return _cardHolderName; }
			set
			{
				OnCardHolderNameChanging();
				_cardHolderName = value;
				OnCardHolderNameChanged();
			}
        }
		partial void OnCardHolderNameChanging();
		partial void OnCardHolderNameChanged();
		
		public virtual string CardNumber
        {
            get { return _cardNumber; }
			set
			{
				OnCardNumberChanging();
				_cardNumber = value;
				OnCardNumberChanged();
			}
        }
		partial void OnCardNumberChanging();
		partial void OnCardNumberChanged();
		
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
		
		public virtual string BillingAddressLine1
        {
            get { return _billingAddressLine1; }
			set
			{
				OnBillingAddressLine1Changing();
				_billingAddressLine1 = value;
				OnBillingAddressLine1Changed();
			}
        }
		partial void OnBillingAddressLine1Changing();
		partial void OnBillingAddressLine1Changed();
		
		public virtual string BillingAddressLine2
        {
            get { return _billingAddressLine2; }
			set
			{
				OnBillingAddressLine2Changing();
				_billingAddressLine2 = value;
				OnBillingAddressLine2Changed();
			}
        }
		partial void OnBillingAddressLine2Changing();
		partial void OnBillingAddressLine2Changed();
		
		public virtual string BillingAddressTown
        {
            get { return _billingAddressTown; }
			set
			{
				OnBillingAddressTownChanging();
				_billingAddressTown = value;
				OnBillingAddressTownChanged();
			}
        }
		partial void OnBillingAddressTownChanging();
		partial void OnBillingAddressTownChanged();
		
		public virtual string BillingAddressCounty
        {
            get { return _billingAddressCounty; }
			set
			{
				OnBillingAddressCountyChanging();
				_billingAddressCounty = value;
				OnBillingAddressCountyChanged();
			}
        }
		partial void OnBillingAddressCountyChanging();
		partial void OnBillingAddressCountyChanged();
		
		public virtual string BillingAddressPostCode
        {
            get { return _billingAddressPostCode; }
			set
			{
				OnBillingAddressPostCodeChanging();
				_billingAddressPostCode = value;
				OnBillingAddressPostCodeChanged();
			}
        }
		partial void OnBillingAddressPostCodeChanging();
		partial void OnBillingAddressPostCodeChanged();
		
		public virtual string CardType
        {
            get { return _cardType; }
			set
			{
				OnCardTypeChanging();
				_cardType = value;
				OnCardTypeChanged();
			}
        }
		partial void OnCardTypeChanging();
		partial void OnCardTypeChanged();
		
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
		
        #endregion
    }
}
