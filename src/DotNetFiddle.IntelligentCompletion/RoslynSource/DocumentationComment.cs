// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
	internal sealed class DocumentationComment
	{
		private readonly Dictionary<string, string> parameterTexts = new Dictionary<string, string>();
		private readonly Dictionary<string, string> typeParameterTexts = new Dictionary<string, string>();
		private readonly Dictionary<string, ImmutableArray<string>> exceptionTexts = new Dictionary<string, ImmutableArray<string>>();
		public static readonly DocumentationComment Empty = new DocumentationComment();

		public bool HadXmlParseError { get; private set; }

		public string FullXmlFragment { get; private set; }

		public string ExampleText { get; private set; }

		public string SummaryText { get; private set; }

		public string ReturnsText { get; private set; }

		public string RemarksText { get; private set; }

		public ImmutableArray<string> ParameterNames { get; private set; }

		public ImmutableArray<string> TypeParameterNames { get; private set; }

		public ImmutableArray<string> ExceptionTypes { get; private set; }

		static DocumentationComment()
		{
		}

		private DocumentationComment()
		{
			this.ParameterNames = ImmutableArray.Create<string>();
			this.TypeParameterNames = ImmutableArray.Create<string>();
			this.ExceptionTypes = ImmutableArray.Create<string>();
		}

		public static DocumentationComment FromXmlFragment(string xml)
		{
			try
			{
				StringReader stringReader = new StringReader(xml);
				XmlReaderSettings settings = new XmlReaderSettings();
				int num = 1;
				settings.ConformanceLevel = (ConformanceLevel)num;
				XmlReader xmlReader = XmlReader.Create((TextReader)stringReader, settings);
				DocumentationComment documentationComment = new DocumentationComment();
				documentationComment.FullXmlFragment = xml;
				List<string> list1 = new List<string>();
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				try
				{
					Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
					while (!xmlReader.EOF)
					{
						if (xmlReader.IsStartElement())
						{
							string localName = xmlReader.LocalName;
							if (DocumentationCommentXmlNames.ElementEquals(localName, "example", false) && documentationComment.ExampleText == null)
								documentationComment.ExampleText = xmlReader.ReadInnerXml().Trim();
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "summary", false) && documentationComment.SummaryText == null)
								documentationComment.SummaryText = xmlReader.ReadInnerXml().Trim();
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "returns", false) && documentationComment.ReturnsText == null)
								documentationComment.ReturnsText = xmlReader.ReadInnerXml().Trim();
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "remarks", false) && documentationComment.RemarksText == null)
								documentationComment.RemarksText = xmlReader.ReadInnerXml().Trim();
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "param", false))
							{
								string attribute = xmlReader.GetAttribute("name");
								string str = xmlReader.ReadInnerXml();
								if (!string.IsNullOrWhiteSpace(attribute) && !documentationComment.parameterTexts.ContainsKey(attribute))
								{
									list1.Add(attribute);
									documentationComment.parameterTexts.Add(attribute, str.Trim());
								}
							}
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "typeparam", false))
							{
								string attribute = xmlReader.GetAttribute("name");
								string str = xmlReader.ReadInnerXml();
								if (!string.IsNullOrWhiteSpace(attribute) && !documentationComment.typeParameterTexts.ContainsKey(attribute))
								{
									list2.Add(attribute);
									documentationComment.typeParameterTexts.Add(attribute, str.Trim());
								}
							}
							else if (DocumentationCommentXmlNames.ElementEquals(localName, "exception", false))
							{
								string attribute = xmlReader.GetAttribute("cref");
								string str = xmlReader.ReadInnerXml();
								if (!string.IsNullOrWhiteSpace(attribute))
								{
									if (!dictionary.ContainsKey(attribute))
									{
										list3.Add(attribute);
										dictionary.Add(attribute, new List<string>());
									}
									dictionary[attribute].Add(str);
								}
							}
							else
								xmlReader.Read();
						}
						else
							xmlReader.Read();
					}
					foreach (KeyValuePair<string, List<string>> keyValuePair in dictionary)
						documentationComment.exceptionTexts.Add(keyValuePair.Key, ImmutableArrayExtensions.AsImmutable<string>((IEnumerable<string>)keyValuePair.Value));
				}
				finally
				{
					documentationComment.ParameterNames = ImmutableArrayExtensions.AsImmutable<string>((IEnumerable<string>)list1);
					documentationComment.TypeParameterNames = ImmutableArrayExtensions.AsImmutable<string>((IEnumerable<string>)list2);
					documentationComment.ExceptionTypes = ImmutableArrayExtensions.AsImmutable<string>((IEnumerable<string>)list3);
				}
				return documentationComment;
			}
			catch //(Exception ex)
			{
				DocumentationComment documentationComment = new DocumentationComment();
				string str = xml;
				documentationComment.FullXmlFragment = str;
				int num = 1;
				documentationComment.HadXmlParseError = num != 0;
				return documentationComment;
			}
		}

		public string GetParameterText(string parameterName)
		{
			string str;
			this.parameterTexts.TryGetValue(parameterName, out str);
			return str;
		}

		public string GetTypeParameterText(string typeParameterName)
		{
			string str;
			this.typeParameterTexts.TryGetValue(typeParameterName, out str);
			return str;
		}

		public ImmutableArray<string> GetExceptionTexts(string exceptionName)
		{
			ImmutableArray<string> immutableArray;
			this.exceptionTexts.TryGetValue(exceptionName, out immutableArray);
			if (immutableArray.IsDefault)
				immutableArray = ImmutableArray.Create<string>();
			return immutableArray;
		}
	}
}