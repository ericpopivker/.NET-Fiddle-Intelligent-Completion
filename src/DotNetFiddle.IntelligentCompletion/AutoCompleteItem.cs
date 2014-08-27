using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DotNetFiddle.IntelligentCompletion
{

	public enum AutoCompleteItemType
	{
		Variable = 0,
		Property = 1,
		Method = 2,
		Class = 3,
		Namespace = 4
	}

	[Serializable]
	[DebuggerDisplay("{ItemType}: {Name}. Returns {Type}")]
	public class AutoCompleteItem : ICloneable
	{
		public string Name { get; set; }

		public string Type { get; set; }

		public bool IsStatic { get; set; }

		public bool IsGeneric { get; set; }

		public bool IsExtension { get; set; }

		public string Description { get; set; }

		public AutoCompleteItemType ItemType { get; set; }

		//Args - method parameters, constructor parameters for classes
		public AutoCompleteItemParameter[] Params { get; set; }

		public object Clone()
		{
			var newItem = (AutoCompleteItem)this.MemberwiseClone();
			newItem.Params = null;
			return newItem;
		}
	}


	public class AutoCompleteItemEqualityComparer : IEqualityComparer<AutoCompleteItem>
	{
		public bool Equals(AutoCompleteItem x, AutoCompleteItem y)
		{
			if (x.ItemType != y.ItemType) return false;
			if (x.Name != y.Name) return false;
			if (x.Type != y.Type) return false;
			if (x.Params != null && y.Params != null && x.Params.Count() != y.Params.Count()) return false;
			if (x.Params != null && y.Params != null &&
				!x.Params.Except(y.Params, new AutoCompleteItemParameterEqualityComparer()).Any() &&
				!y.Params.Except(x.Params, new AutoCompleteItemParameterEqualityComparer()).Any()) return false;
			return true;
		}

		public int GetHashCode(AutoCompleteItem obj)
		{
			return
				String.Format("{0}-{1}-{2}-{3}", obj.Type, obj.ItemType, obj.Name,
							  obj.Params != null ? String.Join("-", obj.Params.Select(p => p.Name + p.Type)) : null)
					  .GetHashCode();
		}
	}


	[Serializable]
	public class AutoCompleteItemParameter
	{
		public string Name { get; set; }

		public string Type { get; set; }

		public string Description { get; set; }

		public bool IsParams { get; set; }

		public bool IsOptional { get; set; }
	}


	public class AutoCompleteItemParameterEqualityComparer : IEqualityComparer<AutoCompleteItemParameter>
	{
		public bool Equals(AutoCompleteItemParameter x, AutoCompleteItemParameter y)
		{
			if (x.Name != y.Name) return false;
			if (x.Type != y.Type) return false;
			return true;
		}

		public int GetHashCode(AutoCompleteItemParameter obj)
		{
			return String.Format("{0}-{1}", obj.Type, obj.Name).GetHashCode();
		}
	}

}