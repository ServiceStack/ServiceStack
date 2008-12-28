using System;
using System.Collections;
using System.Collections.Generic;

using @ServiceNamespace@.DataAccess.Base;

namespace @ServiceNamespace@.DataAccess.DataModel
{
    public partial class @ModelName@OrderLineItem : BusinessBase<uint>
    {
        #region Declarations

		
		private string _name = String.Empty;
		private decimal _unitPrice = default(Decimal);
		private int _quantity = default(Int32);
		private decimal _subTotal = default(Decimal);
		private decimal _vat = default(Decimal);
		private decimal _total = default(Decimal);
		
		private @ModelName@Order _userOrder = null;
		
		
        #endregion

        #region Constructors

        public @ModelName@OrderLineItem() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_name);
			sb.Append(_unitPrice);
			sb.Append(_quantity);
			sb.Append(_subTotal);
			sb.Append(_vat);
			sb.Append(_total);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

		public virtual string Name
        {
            get { return _name; }
			set
			{
				OnNameChanging();
				_name = value;
				OnNameChanged();
			}
        }
		partial void OnNameChanging();
		partial void OnNameChanged();
		
		public virtual decimal UnitPrice
        {
            get { return _unitPrice; }
			set
			{
				OnUnitPriceChanging();
				_unitPrice = value;
				OnUnitPriceChanged();
			}
        }
		partial void OnUnitPriceChanging();
		partial void OnUnitPriceChanged();
		
		public virtual int Quantity
        {
            get { return _quantity; }
			set
			{
				OnQuantityChanging();
				_quantity = value;
				OnQuantityChanged();
			}
        }
		partial void OnQuantityChanging();
		partial void OnQuantityChanged();
		
		public virtual decimal SubTotal
        {
            get { return _subTotal; }
			set
			{
				OnSubTotalChanging();
				_subTotal = value;
				OnSubTotalChanged();
			}
        }
		partial void OnSubTotalChanging();
		partial void OnSubTotalChanged();
		
		public virtual decimal Vat
        {
            get { return _vat; }
			set
			{
				OnVatChanging();
				_vat = value;
				OnVatChanged();
			}
        }
		partial void OnVatChanging();
		partial void OnVatChanged();
		
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
		
		public virtual @ModelName@Order @ModelName@OrderMember
        {
            get { return _userOrder; }
			set
			{
				On@ModelName@OrderMemberChanging();
				_userOrder = value;
				On@ModelName@OrderMemberChanged();
			}
        }
		partial void On@ModelName@OrderMemberChanging();
		partial void On@ModelName@OrderMemberChanged();
		
        #endregion
    }
}
