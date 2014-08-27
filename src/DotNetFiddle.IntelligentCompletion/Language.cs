using System;
using System.ComponentModel;

namespace DotNetFiddle.IntelligentCompletion
{
	[Serializable]
	public enum Language
	{
		[Description("C#")]
		CSharp = 1,
		[Description("VB.NET")]
		VbNet
	}
}