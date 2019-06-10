using System;

namespace Shared.Enums
{
	public enum LexemType
	{
		Identifier, Keyword, Separtor, Variable, MultiSymbolicSeparator
	}

	public enum State
	{
		Start,
		Input,
		Identificator,
		Variable,
		Separtor,
		WhiteSpace,
		BeginComment,
		Comment,
		EndComment,
		MultiSeparator
	}

	public enum SyntaxErrorTypes
	{
		Program,
		Semicolon,
		Begin,
		End,
		OpenBracket,
		CloseBracket,
		Procedure,
		Colon,
		Attribute,
		Variable,
		Identifier
	}
}
