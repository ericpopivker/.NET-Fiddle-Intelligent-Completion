using System;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace DotNetFiddle.IntelligentCompletion
{
	/// <summary>
	/// We need DocumenationProvider to be able parse XML documenation for existing assemblies
	/// </summary>
	public class DocumentationProviderFactory
	{
		private static readonly Func<string, DocumentationProvider> _creator;

		static DocumentationProviderFactory()
		{
			try
			{
				// It's a bit hackery, as this class is internal and we can't access it, so we use Reflaction and Expression tree to get access
				var type = Type.GetType("Microsoft.CodeAnalysis.XmlDocumentationProvider, Microsoft.CodeAnalysis.Workspaces");

				var method = type.GetMethod(
					"Create",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new Type[1] { typeof(string) },
					new ParameterModifier[0]);

				var pathParameter = Expression.Parameter(typeof(string));
				var callExpression = Expression.Call(method, pathParameter);

				var lambdaExpr = Expression.Lambda<Func<string, DocumentationProvider>>(callExpression, new[] { pathParameter });

				_creator = lambdaExpr.Compile();
			}
			catch
			{
				// it might can't find it if assembly changed, so in that case we just won't return anything
			}
		}

		public static DocumentationProvider Create(string xmlPath)
		{
			return _creator != null ? _creator(xmlPath) : null;
		}
	}
}