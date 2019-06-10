using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Shared.Enums;

namespace Shared.Structs
{
	#region Structs
	public struct Lexem:IEquatable<Lexem>
	{
		public int line;
		public int column;
		public int code;
		public string value;
		public LexemType type;

		public bool Equals(Lexem x, Lexem y)
		{
			if (x.code == y.code)
				return true;
			return false;
		}

		public bool Equals(Lexem other)
		{
			if (this.code == other.code)
				return true;
			return false;
		}

		public int GetHashCode(Lexem obj)
		{
			return obj.GetHashCode();
		}

		public override string ToString()
		{
			return $"[{line},{column}] value: {value} code: {code} type: {type}";
		}
	}

	public struct Error
	{
		public int line;
		public int column;
		public string message;
		public Error(int _line, int _column, string _message="")
		{
			line = _line;
			column = _column;
			message = _message;
		}
	}

	public struct SyntaxError
	{
		private SyntaxErrorTypes errorType;
		private Error errorDefinition;
		public SyntaxError(SyntaxErrorTypes type, Error error)
		{
			errorType = type;
			errorDefinition = error;
			var message = new StringBuilder();
			switch (type)
			{
				case SyntaxErrorTypes.Semicolon:
					message.Append(@""";""");
					break;
				case SyntaxErrorTypes.OpenBracket:
					message.Append(@"""(""");
					break;
				case SyntaxErrorTypes.CloseBracket:
					message.Append(@""")""");
					break;
				case SyntaxErrorTypes.Colon:
					message.Append(@""":""");
					break;
				default:
					message.Append(Enum.GetName(typeof(SyntaxErrorTypes), type).ToUpper());
					break;
			}
			message.Append(" expected!\n");
			errorDefinition.message = message.ToString();
		}

		public override string ToString()
		{
			return $"Error in [{errorDefinition.line},{errorDefinition.column}]. {errorDefinition.message}";
		}

	}

	#endregion Structs
}
