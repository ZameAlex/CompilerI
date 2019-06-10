using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Lexer
{
	#region Structs
	public struct Lexem
    {
        public int line;
        public int column;
        public int code;
		public string value;
		public LexemType type;
    }

	struct Error
	{
		public int line;
		public int column;
		public string message;
		public Error(int _line,int _column, string _message)
		{
			line = _line;
			column = _column;
			message = _message;
		}
	}
	#endregion Structs

	#region Enums
	public enum LexemType
    {
        Identifier, Keyword, Separtor, Variable, MultiSymbolicSeparator, SpecialLexem
    }

	enum State
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
        MultiSeparator,
		FirstA,
		At,
		SecondA,
		Dot,
		LastA
	}
    #endregion Enums

     class Lexer
    {
        private const int MS = 501;
		private int SL = 2000;
		#region State
		public int CurrentLine { get; protected set; }
        public int CurrentColumn { get; protected set; }
        public List<Lexem> LexemString { get; protected set; }

		public List<Error> ErrorList { get; protected set; }

		private State currentState;

        private StreamReader programText;
		private StringBuilder currentLexem;
		private Lexem nextLexem;
		private int lastId, lastConst;
		#endregion State

		#region Tables
		public Dictionary<string, int> Identifiers { get; protected set; }
        public Dictionary<string, int> Constants { get; protected set; }
        public Dictionary<string, int> Keywords { get; protected set; }
		public Dictionary<string, int> Separators { get; protected set; }
        public Dictionary<string, int> MultiSeparators { get; protected set; }
        #endregion Tables

        public Lexer(string programTextFilePath, string keywordsTableFilePath, string separTableFilePath = "",
            string multiSeparTableFilePath = "")
        {
			CurrentLine = 1;
			lastId = 1001;
			lastConst = 501;
            Constants = new Dictionary<string, int>();
            Keywords = new Dictionary<string, int>();
            Identifiers = new Dictionary<string, int>();
			Separators = new Dictionary<string, int>();
            MultiSeparators = new Dictionary<string, int>();
            programText = new StreamReader(programTextFilePath);
			ErrorList = new List<Error>();
			LexemString = new List<Lexem>();
			currentLexem = new StringBuilder();
            using (StreamReader keywordsTable = new StreamReader(keywordsTableFilePath))
            {
                while(!keywordsTable.EndOfStream)
                {
                    string[] pair = keywordsTable.ReadLine().Trim().Split(new char[1] { ' ' });
                    Keywords.Add(pair[0], Convert.ToInt32(pair[1]));
                }
            }
			using (StreamReader separatorTable = new StreamReader(separTableFilePath))
			{
				while (!separatorTable.EndOfStream)
				{
					string[] pair = separatorTable.ReadLine().Trim().Split(new char[1] { ' ' });
					Separators.Add(pair[0], Convert.ToInt32(pair[1]));
				}
			}
            using (StreamReader multiSepar = new StreamReader(multiSeparTableFilePath))
            {
                int i = MS;
                while (!multiSepar.EndOfStream)
                {
                    string separ = multiSepar.ReadLine();
                    MultiSeparators.Add(separ, i++);
                }
            }
            currentState = State.Start;
        }

        private bool IsMultiSeparator (char symbol)
        {
            foreach (var item in MultiSeparators)
            {
                if (symbol == item.Key[0])
                {
                    return true;
                }
            }
            return false;
        }
		private bool IsWhiteSpace(char symbol)
		{
			int current = (int)symbol;
			switch (current)
			{
				case 32:
				case 13:
				case 10:
				case 9:
				case 11:
				case 12:
					return true;
			}
			return false;
		}
		private bool IsSeparator(char symbol)
		{
			foreach (var item in Separators)
				if (Char.ToString(symbol) == item.Key)
					return true;
			foreach (var item in MultiSeparators)
			{
				if (symbol == item.Key[1])
					return true;
			}
			return false;
		}

		private char? NextChatacter()
        {
			if (currentState == State.Start && programText.EndOfStream)
			{
				ErrorList.Add(new Error(0, 0, "File is empty!"));
				return null;
			}
			else if (programText.EndOfStream)
				return null;
			else
				return (char)programText.Read();
        }

		public void Analyze()
		{
			int? current = '\0';
			while ((current = NextChatacter()) != null)
			{
				CurrentColumn++;
				if (current == '\r')
				{
					DefineState((char?)current);
					continue;
				}
				if(current=='\n')
				{
					CurrentColumn = 0;
					CurrentLine++;
				}
				DefineState((char?)current);
			}
			DefineState((char?)current);
		}
		private void DefineState(char? current)
		{
			if (current == null)
			{
				if (currentState == State.BeginComment || currentState == State.Comment || currentState == State.EndComment)
					DefineError('\0');
				if (currentLexem.Length > 0)
					DefineLexem();
				return;
			}
			char symbol = (char)current;
			switch (currentState)
			{
				case State.BeginComment:
					if (symbol == '*')
					{
						currentState = State.Comment;
						return;
					}
					DefineError(symbol);
					return;
				case State.Comment:
					if (symbol == '*')
						currentState = State.EndComment;
					return;
				case State.EndComment:
					if (symbol == ')')
					{
						currentState = State.Input;
						return;
					}
					currentState = State.Comment;
					return;

				case State.Start:
				case State.Input:
					if(symbol=='a')
					{
						nextLexem.line = CurrentLine;
						nextLexem.column = CurrentColumn;
						currentLexem.Append(symbol);
						currentState = State.FirstA;
						return;
					}
					if (Char.IsUpper(symbol))
					{
						nextLexem.line = CurrentLine;
						nextLexem.column = CurrentColumn;
						currentLexem.Append(symbol);
						currentState = State.Identificator;
						return;
					}
					if (Char.IsDigit(symbol))
					{
						nextLexem.line = CurrentLine;
						nextLexem.column = CurrentColumn;
						currentLexem.Append(symbol);
						currentState = State.Variable;
						return;
					}
					if (symbol == '(')
					{
						currentState = State.BeginComment;
						return;
					}
                    if (IsMultiSeparator(symbol))
                    {
                        currentLexem.Append(symbol);
                        currentState = State.MultiSeparator;
                        return;
                    }
                    //foreach (var item in MultiSeparators)
                    //{
                      //  if (symbol == item.Key[0])
                       // {
                        //    currentLexem.Append(symbol);
                        //    currentState = State.MultiSeparator;
                        //    return;
                       // }
                    //}
					if (IsSeparator(symbol))
					{
						goto case State.Separtor;
					}
					if (IsWhiteSpace(symbol))
						return;
					DefineError(symbol);
					return;

				case State.Identificator:
					if (Char.IsUpper(symbol) || Char.IsDigit(symbol))
					{
						currentLexem.Append(symbol);
						return;
					}
                    if (IsMultiSeparator(symbol))
                    {
                        DefineLexem();
                        currentLexem.Append(symbol);
                        currentState = State.MultiSeparator;
                        return;
                    }
					if (IsSeparator(symbol))
					{
						DefineLexem();
						goto case State.Separtor;
					}
					if (IsWhiteSpace(symbol))
					{
						if (currentLexem.Length != 0)
							DefineLexem();
						return;
					}
					DefineError(symbol);
					return;

				case State.Variable:
					if (Char.IsDigit(symbol))
					{
						currentLexem.Append(symbol);
						return;
					}
                    if (IsMultiSeparator(symbol))
                    {
                        currentLexem.Append(symbol);
                        currentState = State.MultiSeparator;
                        return;
                    }
					if (IsSeparator(symbol))
					{
						DefineLexem();
						goto case State.Separtor;
					}
					if (IsWhiteSpace(symbol))
					{
						if (currentLexem.Length != 0)
							DefineLexem();
						return;
					}
					DefineError(symbol);
					return;
				case State.Separtor:
					currentLexem.Append(symbol);
					currentState = State.Separtor;
					DefineLexem();
					return;
                case State.MultiSeparator:
                    if (Char.IsUpper(symbol))
                    {
                        currentState = State.Separtor;
                        DefineLexem();
                        currentState = State.Identificator;
                        currentLexem.Append(symbol);
                        return;
                    }
                    if(Char.IsDigit(symbol))
                    {
                        currentState = State.Separtor;
                        DefineLexem();
                        currentState = State.Variable;
                        currentLexem.Append(symbol);
                        return;
                    }
                    if (IsSeparator(symbol))
                    {
                        currentLexem.Append(symbol);
                        DefineLexem();
                        return;
                    }
                    if (IsWhiteSpace(symbol))
                    {
                        CurrentColumn--;
                        if (currentLexem.Length != 0)
                            DefineLexem();
                        return;
                    }
                    DefineError(symbol);
                    return;
				case State.FirstA:
					if(Char.IsLetterOrDigit(symbol))
					{
						currentLexem.Append(symbol);
						return;
					}
					if (symbol == '@')
					{
						currentLexem.Append(symbol);
						currentState = State.At;
						return;
					}
					DefineError(symbol);
					return;
				case State.At:
					if (symbol == 'a')
					{
						currentLexem.Append(symbol);
						currentState = State.SecondA;
						return;
					}
					if(symbol=='.')
					{
						currentLexem.Append(symbol);
						currentState = State.Dot;
						return;
					}
					DefineError(symbol);
					return;
				case State.SecondA:
					if (Char.IsLetterOrDigit(symbol))
					{
						currentLexem.Append(symbol);
						return;
					}
					if (symbol == '.')
					{
						currentLexem.Append(symbol);
						currentState = State.Dot;
						return;
					}
					DefineError(symbol);
					return;
				case State.Dot:
					if (symbol == 'a')
					{
						currentLexem.Append(symbol);
						currentState = State.LastA;
						return;
					}
					DefineError(symbol);
					return;
				case State.LastA:
					if (Char.IsLetterOrDigit(symbol))
					{
						currentLexem.Append(symbol);
						return;
					}
					if (IsSeparator(symbol))
					{
						DefineLexem();
						goto case State.Separtor;
					}
					if (IsWhiteSpace(symbol))
					{
						if (currentLexem.Length != 0)
							DefineLexem();
						return;
					}
					DefineError(symbol);
					return;
			}
		}
		
		
		private void DefineLexem()
		{
			bool f = false;
			switch (currentState)
			{
				case State.Identificator:
					f = SearchInTable(Keywords);
                    nextLexem.type = LexemType.Keyword;
                    if (!f)
                    {
                        f = SearchInTable(Identifiers);
                        nextLexem.type = LexemType.Identifier;
                    }
                    if (!f)
					{
						nextLexem.code = lastId++;
						Identifiers.Add(currentLexem.ToString(), nextLexem.code);
                        nextLexem.type = LexemType.Identifier;
                    }
					OutputLexem();
					break;
				case State.Variable:
					f=SearchInTable(Constants);
					if (!f)
					{
						nextLexem.code = lastConst++;
						Constants.Add(currentLexem.ToString(), nextLexem.code);
					}
					nextLexem.type = LexemType.Variable;
					OutputLexem();
					break;
				case State.Separtor:
					SearchInTable(Separators);
					nextLexem.type = LexemType.Separtor;
					nextLexem.line = CurrentLine;
					nextLexem.column = CurrentColumn;
					OutputLexem();
					break;
				case State.WhiteSpace:
					break;
                case State.MultiSeparator:
                        f = SearchInTable(MultiSeparators);
                    if(f)
                    {
                        nextLexem.type = LexemType.MultiSymbolicSeparator;
                        nextLexem.line = CurrentLine - currentLexem.Length;
                        nextLexem.column = CurrentColumn - currentLexem.Length+1;
                        OutputLexem();
                    }
                    else
                    {
                        int i = 1;
                        string separators = currentLexem.ToString();
                        foreach (var item in separators)
                        {
                            currentLexem.Clear();
                            currentLexem.Append(item);
                            nextLexem.line = CurrentLine;
                            nextLexem.column = CurrentColumn - separators.Length + i;
                            i++;
                            if (SearchInTable(Separators))
                            {
                                nextLexem.type = LexemType.Separtor;
                                OutputLexem();
                            }
                            else
                            {
                                currentState = State.Separtor;
                                DefineError(item);
                            }
                        }
                    }
                        break;
				case State.LastA:
					nextLexem.type = LexemType.SpecialLexem;
					nextLexem.code = SL++;
					OutputLexem();
					break;
			}
		}
		private void OutputLexem()
		{
			nextLexem.value = currentLexem.ToString();
			LexemString.Add(nextLexem);
			currentLexem.Clear();
			currentState = State.Input;
			return;
		}

		private void DefineError(char currentSymbol)
		{
			StringBuilder message = new StringBuilder("Unresolved  symbol: " + currentSymbol +
				"\n line:" + CurrentLine + " column:" + CurrentColumn + "\n");
			if (currentState == State.Identificator)
				message.Append(" in identificator starts on [" + nextLexem.line + "," + nextLexem.column + "]");
			if (currentState == State.Variable)
				message.Append(" in unsigned integer starts on [" + nextLexem.line + "," + nextLexem.column + "]");
			if (currentState == State.BeginComment)
			{
				message.Clear();
				message.Append("Error in comment. Asterisk expected!");
				currentState = State.Input;
			}
			if (currentSymbol == '\0')
			{
				message.Clear();
				message.Append("Error! Unexpected end of file. Unclosed comment.");
			}
			ErrorList.Add(new Error(CurrentLine, CurrentColumn, message.ToString()));

		}

		private bool SearchInTable(Dictionary<string, int> dict)
		{
			foreach (var item in dict)
				if (item.Key == currentLexem.ToString())
				{
					nextLexem.code = item.Value;
					return true;
				}
			return false;
		}

		#region Output

		private void WriteTable(Dictionary<string, int> dict)
		{
			foreach (var item in dict)
				Console.WriteLine("{1} : {0}", item.Key, item.Value);
		}
        public void WriteLexemStringToConsole()
        {
			Console.WriteLine("Identifiers");
			WriteTable(Identifiers);
			Console.WriteLine("\nKeywords");
			WriteTable(Keywords);
			Console.WriteLine("\nContants");
			WriteTable(Constants);
			Console.WriteLine("\nSeparators");
			WriteTable(Separators);
			Console.WriteLine("\n\nLexem string");
			foreach (var item in LexemString)
            {
                Console.WriteLine(item.code+"\t[{0},{1}] "+item.value+" \t\t"+item.type,item.line,item.column);
                
            }
			Console.WriteLine();
			foreach(var item in ErrorList)
			{
				Console.WriteLine(item.message);
			}
        }

        public void WriteLexemStringToFile()
        {
            StreamWriter resultFile = new StreamWriter("LexemString.txt");
            foreach (var item in LexemString)
                resultFile.Write(item.code);
        }

		

        public void WriteTablesToFile(string identifierTableFilePath="IdentifierTable.txt",
			string constTableFilePath="ConstTable.txt")
        {
            using (StreamWriter identifierTable = new StreamWriter(identifierTableFilePath))
            {
                foreach (var item in Identifiers)
                    identifierTable.WriteLine("{0} {1}", item.Key, item.Value);
            }

            using (StreamWriter constTable = new StreamWriter(constTableFilePath))
            {
                foreach (var item in Constants)
                    constTable.WriteLine("{0} {1}", item.Key, item.Value);
            }
        }

        #endregion Output

    }
}
