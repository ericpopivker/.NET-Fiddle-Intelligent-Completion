using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace DotNetFiddle.IntelligentCompletion
{
	public class AssemblyHelper
	{
		private static readonly ConcurrentDictionary<string, string> _assemblyLocations =
			new ConcurrentDictionary<string, string>();
	
		//From http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
		public static string GetAssemblyLocation(Assembly assembly)
		{
			if (assembly.IsDynamic)
			{
				return null;
			}

			string codeBase = assembly.CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);

			string path = Uri.UnescapeDataString(uri.Path);
			path = Path.GetDirectoryName(path);
			path = Path.Combine(path, Path.GetFileName(assembly.Location));

			return path;
		}

		public static string GetAssemblyDirectory(Assembly assembly)
		{
			return Path.GetDirectoryName(GetAssemblyLocation(assembly));
		}

		public static string GetGACAssemblyLocation(string assemblyDisplayName)
		{
			return _assemblyLocations.GetOrAdd(
				assemblyDisplayName,
				(name) =>
				{
					string path;
					GlobalAssemblyCache.ResolvePartialName(assemblyDisplayName, out path);
					return path;
				});
		}

		public static string GetAssemblyLocation(string assemblyDisplayName)
		{
			// search in .Net framework folder
			var systemPath = GetAssemblyDirectory(typeof(object).Assembly);
			string assemblyFullPath = Path.Combine(systemPath, assemblyDisplayName + ".dll");

			if (!File.Exists(assemblyFullPath))
			{
			    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			    {
			        // due to changes in Roslyn, ResolveAssemblyName won't search in GAC, because it's platform specific logic
			        // http://roslyn.codeplex.com/discussions/541557
			        assemblyFullPath = GetGACAssemblyLocation(assemblyDisplayName);
			    }
			    // so the last step would be check application BIN folder for it
				if (string.IsNullOrWhiteSpace(assemblyFullPath))
				{
					assemblyFullPath = AssemblyHelper.GetAssemblyLocation(typeof(LanguageService).Assembly);
					assemblyFullPath = Path.GetDirectoryName(assemblyFullPath);
					AssemblyName assemblyName = new AssemblyName(assemblyDisplayName);
					assemblyFullPath = Path.Combine(assemblyFullPath, assemblyName.Name + ".dll");
				}
			}
			return assemblyFullPath;
		}
	}
}