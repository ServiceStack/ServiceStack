
namespace Funq
{
    public class ServiceEntry : IRegistration
    {
        protected ServiceEntry() { }

        /// <summary>
        /// Ownership setting for the service.
        /// </summary>
        public Owner Owner;
        /// <summary>
        /// Reuse scope setting for the service.
        /// </summary>
        public ReuseScope Reuse;
        /// <summary>
        /// The container where the entry was registered.
        /// </summary>
        public Container Container;

        /// <summary>
        /// Specifies the owner for instances, which determines how 
        /// they will be disposed.
        /// </summary>
        public void OwnedBy(Owner owner)
        {
            this.Owner = owner;
        }

        /// <summary>
        /// Specifies the scope for instances, which determines 
        /// visibility of instances across containers and hierarchies.
        /// </summary>
        public IOwned ReusedWithin(ReuseScope scope)
        {
            this.Reuse = scope;
            return this;
        }

        public virtual object GetInstance()
        {
            return null;
        }
    }
}
