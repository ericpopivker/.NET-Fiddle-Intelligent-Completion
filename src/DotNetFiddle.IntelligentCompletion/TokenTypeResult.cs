using System;

namespace DotNetFiddle.IntelligentCompletion
{
	[Serializable]
	public class TokenTypeResult
	{
		public string Type { get; set; }

		public bool IsInsideArgumentList { get; set; }

		public int? ParentLine { get; set; }

		public int? ParentChar { get; set; }

		public string[] PreviousArgumentListTokenTypes { get; set; }

		public string RawArgumentsList { get; set; }
	}
}