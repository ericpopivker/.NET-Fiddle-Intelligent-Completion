using System;
using System.Collections.Generic;

namespace DotNetFiddle.IntelligentCompletion
{
	public class NamespaceToDllMap
	{
		protected internal static readonly Dictionary<string, object> _map;

		static NamespaceToDllMap()
		{
			var namespaces = new List<string>
				{
					"System",
					"System.ComponentModel.DataAnnotations",
					"System.Core",
					"System.Data",
					"System.Data.DataSetExtensions",
					"System.Data.Entity",
					"System.Data.Linq",
					"System.Drawing",
					"System.EnterpriseServices",
					"System.Net",
					"System.Numerics",
					"System.Web",
					"System.Xml",
					"System.Xml.Linq"
				};
			
			_map = new Dictionary<string, object>();
			foreach (string ns in namespaces)
				_map[ns] = ns;

			_map["System.Linq"] = "System.Core";
			_map.Remove("System.Core");
		}


		public static List<string> GetDlls(List<string> namespaces)
		{
			var dlls = new List<string>();
			foreach (string ns in namespaces)
			{
				string dll = GetDllFromNamespace(ns);
				if (dll != null && !dlls.Contains(dll))
					dlls.Add(dll);
			}

			return dlls;
		}

		private static string GetDllFromNamespace(string ns)
		{
			//Keep removing last namespace till there is a match
			while (true)
			{
				if (String.IsNullOrEmpty(ns))
					return null;

				if (_map.ContainsKey(ns))
					return _map[ns] + ".dll";

				int index = ns.LastIndexOf(".");
				if (index == -1)
					return null;

				if (index == 0)
					return null;

				ns = ns.Substring(0, index);
			}
		}
	}
}
