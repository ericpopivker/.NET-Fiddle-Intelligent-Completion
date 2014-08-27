using System;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace DotNetFiddle.IntelligentCompletion
{
	public class DocumentationProviderFactory
	{
		private static readonly Func<string, DocumentationProvider> _creator;

		static DocumentationProviderFactory()
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

		public static DocumentationProvider Create(string xmlPath)
		{
			return _creator(xmlPath);
		}
	}
}