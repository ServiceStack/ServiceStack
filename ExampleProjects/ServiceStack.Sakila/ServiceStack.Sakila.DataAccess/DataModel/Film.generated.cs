using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Film : BusinessBase<ushort>
    {
        #region Declarations

		
		private string _title = String.Empty;
		private string _description = String.Empty;
		private object _releaseYear = null;
		private byte _rentalDuration = default(Byte);
		private decimal _rentalRate = default(Decimal);
		private ushort _length = default(UInt16);
		private decimal _replacementCost = default(Decimal);
		private object _rating = null;
		private object _specialFeature = null;
		private System.DateTime _lastUpdate = new DateTime();
		
		private Language _language = null;
		
		private IList<Inventory> _inventories = new List<Inventory>();
		
        #endregion

        #region Constructors

        public Film() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_title);
			sb.Append(_description);
			sb.Append(_releaseYear);
			sb.Append(_rentalDuration);
			sb.Append(_rentalRate);
			sb.Append(_length);
			sb.Append(_replacementCost);
			sb.Append(_rating);
			sb.Append(_specialFeature);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

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
		
		public virtual string Description
        {
            get { return _description; }
			set
			{
				OnDescriptionChanging();
				_description = value;
				OnDescriptionChanged();
			}
        }
		partial void OnDescriptionChanging();
		partial void OnDescriptionChanged();
		
		public virtual object ReleaseYear
        {
            get { return _releaseYear; }
			set
			{
				OnReleaseYearChanging();
				_releaseYear = value;
				OnReleaseYearChanged();
			}
        }
		partial void OnReleaseYearChanging();
		partial void OnReleaseYearChanged();
		
		public virtual byte RentalDuration
        {
            get { return _rentalDuration; }
			set
			{
				OnRentalDurationChanging();
				_rentalDuration = value;
				OnRentalDurationChanged();
			}
        }
		partial void OnRentalDurationChanging();
		partial void OnRentalDurationChanged();
		
		public virtual decimal RentalRate
        {
            get { return _rentalRate; }
			set
			{
				OnRentalRateChanging();
				_rentalRate = value;
				OnRentalRateChanged();
			}
        }
		partial void OnRentalRateChanging();
		partial void OnRentalRateChanged();
		
		public virtual ushort Length
        {
            get { return _length; }
			set
			{
				OnLengthChanging();
				_length = value;
				OnLengthChanged();
			}
        }
		partial void OnLengthChanging();
		partial void OnLengthChanged();
		
		public virtual decimal ReplacementCost
        {
            get { return _replacementCost; }
			set
			{
				OnReplacementCostChanging();
				_replacementCost = value;
				OnReplacementCostChanged();
			}
        }
		partial void OnReplacementCostChanging();
		partial void OnReplacementCostChanged();
		
		public virtual object Rating
        {
            get { return _rating; }
			set
			{
				OnRatingChanging();
				_rating = value;
				OnRatingChanged();
			}
        }
		partial void OnRatingChanging();
		partial void OnRatingChanged();
		
		public virtual object SpecialFeature
        {
            get { return _specialFeature; }
			set
			{
				OnSpecialFeatureChanging();
				_specialFeature = value;
				OnSpecialFeatureChanged();
			}
        }
		partial void OnSpecialFeatureChanging();
		partial void OnSpecialFeatureChanged();
		
		public virtual System.DateTime LastUpdate
        {
            get { return _lastUpdate; }
			set
			{
				OnLastUpdateChanging();
				_lastUpdate = value;
				OnLastUpdateChanged();
			}
        }
		partial void OnLastUpdateChanging();
		partial void OnLastUpdateChanged();
		
		public virtual Language LanguageMember
        {
            get { return _language; }
			set
			{
				OnLanguageMemberChanging();
				_language = value;
				OnLanguageMemberChanged();
			}
        }
		partial void OnLanguageMemberChanging();
		partial void OnLanguageMemberChanged();
		
		public virtual IList<Inventory> Inventories
        {
            get { return _inventories; }
            set
			{
				OnInventoriesChanging();
				_inventories = value;
				OnInventoriesChanged();
			}
        }
		partial void OnInventoriesChanging();
		partial void OnInventoriesChanged();
		
        #endregion
    }
}
