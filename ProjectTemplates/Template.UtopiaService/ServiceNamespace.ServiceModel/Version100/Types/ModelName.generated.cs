namespace @ServiceModelNamespace@.Version100.Types
{
	using System;
	using System.Collections.Generic;
	
	
	public partial class @ModelName@
	{
		
		public virtual @DomainModelNamespace@.@ModelName@ ToModel()
		{
			return this.UpdateModel(new @DomainModelNamespace@.@ModelName@());
		}
		
		public virtual @DomainModelNamespace@.@ModelName@ UpdateModel(@DomainModelNamespace@.@ModelName@ model)
		{
			return model;
		}
		
		public static @ServiceModelNamespace@.Version100.Types.@ModelName@ Parse(@DomainModelNamespace@.@ModelName@ from)
		{
			@ServiceModelNamespace@.Version100.Types.@ModelName@ to = new @ServiceModelNamespace@.Version100.Types.@ModelName@();
			return to;
		}
		
		public static System.Collections.Generic.List<@ServiceModelNamespace@.Version100.Types.@ModelName@> ParseAll(System.Collections.Generic.IEnumerable<@DomainModelNamespace@.@ModelName@> from)
		{
			System.Collections.Generic.List<@ServiceModelNamespace@.Version100.Types.@ModelName@> to = new System.Collections.Generic.List<@ServiceModelNamespace@.Version100.Types.@ModelName@>();
			for (System.Collections.Generic.IEnumerator<@DomainModelNamespace@.@ModelName@> iter = from.GetEnumerator(); iter.MoveNext(); 
			)
			{
				@DomainModelNamespace@.@ModelName@ item = iter.Current;
				to.Add(@ServiceModelNamespace@.Version100.Types.@ModelName@.Parse(item));
			}
			return to;
		}
	}
}
