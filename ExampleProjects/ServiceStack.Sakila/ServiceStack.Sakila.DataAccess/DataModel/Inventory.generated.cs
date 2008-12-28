using System;
using System.Collections;
using System.Collections.Generic;

using ServiceStack.Sakila.DataAccess.Base;

namespace ServiceStack.Sakila.DataAccess.DataModel
{
    public partial class Inventory : BusinessBase<uint>
    {
        #region Declarations

		
		private System.DateTime _lastUpdate = new DateTime();
		
		private Film _film = null;
		private Store _store = null;
		
		private IList<Rental> _rentals = new List<Rental>();
		
        #endregion

        #region Constructors

        public Inventory() { }

        #endregion

        #region Methods

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(this.GetType().FullName);
			sb.Append(_lastUpdate);

            return sb.ToString().GetHashCode();
        }

        #endregion

        #region Properties

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
		
		public virtual Film FilmMember
        {
            get { return _film; }
			set
			{
				OnFilmMemberChanging();
				_film = value;
				OnFilmMemberChanged();
			}
        }
		partial void OnFilmMemberChanging();
		partial void OnFilmMemberChanged();
		
		public virtual Store StoreMember
        {
            get { return _store; }
			set
			{
				OnStoreMemberChanging();
				_store = value;
				OnStoreMemberChanged();
			}
        }
		partial void OnStoreMemberChanging();
		partial void OnStoreMemberChanged();
		
		public virtual IList<Rental> Rentals
        {
            get { return _rentals; }
            set
			{
				OnRentalsChanging();
				_rentals = value;
				OnRentalsChanged();
			}
        }
		partial void OnRentalsChanging();
		partial void OnRentalsChanged();
		
        #endregion
    }
}
