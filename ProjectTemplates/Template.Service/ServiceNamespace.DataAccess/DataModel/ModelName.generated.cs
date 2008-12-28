using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@ : BusinessBase<uint>
    {
        #region Declarations

		
		private byte[] _globalId = null;
		private System.DateTime _createdDate = new DateTime();
		private string _createdBy = String.Empty;
		private System.DateTime _lastModifiedDate = new DateTime();
		private string _lastModifiedBy = String.Empty;
		private string _userName = String.Empty;
		private string _title = String.Empty;
		private string _gender = String.Empty;
		private string _firstName = String.Empty;
		private string _lastName = String.Empty;
		private string _saltPassword = String.Empty;
		private decimal _balance = default(Decimal);
		private string _email = String.Empty;
		private string _country = String.Empty;
		private string _languageCode = String.Empty;
		private byte _canNotifyEmail = default(Byte);
		private byte _storeCreditCard = default(Byte);
		private byte _singleClickBuyEnabled = default(Byte);
		
		private Discussion _discussion = null;
		
		private IList<CreditCardInfo> _creditCardInfos = new List<CreditCardInfo>();
		private IList<@ModelName@Order> _userOrders = new List<@ModelName@Order>();
		private IList<@ModelName@Product> _userProducts = new List<@ModelName@Product>();
		private IList<@ModelName@Set> _userSets = new List<@ModelName@Set>();
		
        #endregion

        #region Constructors

        public @ModelName@() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_globalId);
			sb.Append(_createdDate);
			sb.Append(_createdBy);
			sb.Append(_lastModifiedDate);
			sb.Append(_lastModifiedBy);
			sb.Append(_userName);
			sb.Append(_title);
			sb.Append(_gender);
			sb.Append(_firstName);
			sb.Append(_lastName);
			sb.Append(_saltPassword);
			sb.Append(_balance);
			sb.Append(_email);
			sb.Append(_country);
			sb.Append(_languageCode);
			sb.Append(_canNotifyEmail);
			sb.Append(_storeCreditCard);
			sb.Append(_singleClickBuyEnabled);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual byte[] GlobalId
        {
            get { return _globalId; }
			set
			{
				OnGlobalIdChanging();
				_globalId = value;
				OnGlobalIdChanged();
			}
        }
		partial void OnGlobalIdChanging();
		partial void OnGlobalIdChanged();
		
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
		
		public virtual System.DateTime LastModifiedDate
        {
            get { return _lastModifiedDate; }
			set
			{
				OnLastModifiedDateChanging();
				_lastModifiedDate = value;
				OnLastModifiedDateChanged();
			}
        }
		partial void OnLastModifiedDateChanging();
		partial void OnLastModifiedDateChanged();
		
		public virtual string LastModifiedBy
        {
            get { return _lastModifiedBy; }
			set
			{
				OnLastModifiedByChanging();
				_lastModifiedBy = value;
				OnLastModifiedByChanged();
			}
        }
		partial void OnLastModifiedByChanging();
		partial void OnLastModifiedByChanged();
		
		public virtual string @ModelName@Name
        {
            get { return _userName; }
			set
			{
				On@ModelName@NameChanging();
				_userName = value;
				On@ModelName@NameChanged();
			}
        }
		partial void On@ModelName@NameChanging();
		partial void On@ModelName@NameChanged();
		
		public virtual string Title
        {
            get { return _title; }
			set
			{
				OnTitleChanging();
				_title = value;
				OnTitleChanged();
			}
        }
		partial void OnTitleChanging();
		partial void OnTitleChanged();
		
		public virtual string Gender
        {
            get { return _gender; }
			set
			{
				OnGenderChanging();
				_gender = value;
				OnGenderChanged();
			}
        }
		partial void OnGenderChanging();
		partial void OnGenderChanged();
		
		public virtual string FirstName
        {
            get { return _firstName; }
			set
			{
				OnFirstNameChanging();
				_firstName = value;
				OnFirstNameChanged();
			}
        }
		partial void OnFirstNameChanging();
		partial void OnFirstNameChanged();
		
		public virtual string LastName
        {
            get { return _lastName; }
			set
			{
				OnLastNameChanging();
				_lastName = value;
				OnLastNameChanged();
			}
        }
		partial void OnLastNameChanging();
		partial void OnLastNameChanged();
		
		public virtual string SaltPassword
        {
            get { return _saltPassword; }
			set
			{
				OnSaltPasswordChanging();
				_saltPassword = value;
				OnSaltPasswordChanged();
			}
        }
		partial void OnSaltPasswordChanging();
		partial void OnSaltPasswordChanged();
		
		public virtual decimal Balance
        {
            get { return _balance; }
			set
			{
				OnBalanceChanging();
				_balance = value;
				OnBalanceChanged();
			}
        }
		partial void OnBalanceChanging();
		partial void OnBalanceChanged();
		
		public virtual string Email
        {
            get { return _email; }
			set
			{
				OnEmailChanging();
				_email = value;
				OnEmailChanged();
			}
        }
		partial void OnEmailChanging();
		partial void OnEmailChanged();
		
		public virtual string Country
        {
            get { return _country; }
			set
			{
				OnCountryChanging();
				_country = value;
				OnCountryChanged();
			}
        }
		partial void OnCountryChanging();
		partial void OnCountryChanged();
		
		public virtual string LanguageCode
        {
            get { return _languageCode; }
			set
			{
				OnLanguageCodeChanging();
				_languageCode = value;
				OnLanguageCodeChanged();
			}
        }
		partial void OnLanguageCodeChanging();
		partial void OnLanguageCodeChanged();
		
		public virtual byte CanNotifyEmail
        {
            get { return _canNotifyEmail; }
			set
			{
				OnCanNotifyEmailChanging();
				_canNotifyEmail = value;
				OnCanNotifyEmailChanged();
			}
        }
		partial void OnCanNotifyEmailChanging();
		partial void OnCanNotifyEmailChanged();
		
		public virtual byte StoreCreditCard
        {
            get { return _storeCreditCard; }
			set
			{
				OnStoreCreditCardChanging();
				_storeCreditCard = value;
				OnStoreCreditCardChanged();
			}
        }
		partial void OnStoreCreditCardChanging();
		partial void OnStoreCreditCardChanged();
		
		public virtual byte SingleClickBuyEnabled
        {
            get { return _singleClickBuyEnabled; }
			set
			{
				OnSingleClickBuyEnabledChanging();
				_singleClickBuyEnabled = value;
				OnSingleClickBuyEnabledChanged();
			}
        }
		partial void OnSingleClickBuyEnabledChanging();
		partial void OnSingleClickBuyEnabledChanged();
		
		public virtual Discussion DiscussionMember
        {
            get { return _discussion; }
			set
			{
				OnDiscussionMemberChanging();
				_discussion = value;
				OnDiscussionMemberChanged();
			}
        }
		partial void OnDiscussionMemberChanging();
		partial void OnDiscussionMemberChanged();
		
		public virtual IList<CreditCardInfo> CreditCardInfos
        {
            get { return _creditCardInfos; }
            set
			{
				OnCreditCardInfosChanging();
				_creditCardInfos = value;
				OnCreditCardInfosChanged();
			}
        }
		partial void OnCreditCardInfosChanging();
		partial void OnCreditCardInfosChanged();
		
		public virtual IList<@ModelName@Order> @ModelName@Orders
        {
            get { return _userOrders; }
            set
			{
				On@ModelName@OrdersChanging();
				_userOrders = value;
				On@ModelName@OrdersChanged();
			}
        }
		partial void On@ModelName@OrdersChanging();
		partial void On@ModelName@OrdersChanged();
		
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
		
		public virtual IList<@ModelName@Set> @ModelName@Sets
        {
            get { return _userSets; }
            set
			{
				On@ModelName@SetsChanging();
				_userSets = value;
				On@ModelName@SetsChanged();
			}
        }
		partial void On@ModelName@SetsChanging();
		partial void On@ModelName@SetsChanged();
		
        #endregion
    }
}
