using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Enums;
using Shared.Structs;

namespace SyntaxAnalyzer
{
	public class Syntaxer
	{
		private int currentLexem;
		public Node Tree { get; protected set; }
		public SyntaxError Error { get; set; }
		public List<Lexem> LexemString { get; protected set; }
		public Dictionary<string, int> Identifiers { get; protected set; }
		public Dictionary<string, int> Keywords { get; protected set; }
		public event Action OnError;

		public Syntaxer(List<Lexem> lexemString, Dictionary<string, int> identifiers, Dictionary<string, int> keywords)
		{
			LexemString = lexemString;
			Identifiers = identifiers;
			Keywords = keywords;
			currentLexem = 0;
		}

		public void Analyze()
		{
			int currentLevel = 0;
			SignalProgram(currentLevel);
		}

		#region AnalyzeFunctions
		private void SignalProgram(int currentLevel)
		{
			Tree = new Node("<signal-program>", currentLevel);
			Tree.Add(new Node("<program>", currentLevel + 1));
			Tree = Tree.Children[0];
			Program(currentLevel + 1);
		}
		private void Program(int currentLevel)
		{
			RiseErrorOrAddNodeToTree(currentLevel, "PROGRAM");/*якщо дійшли до термінального символа викликаємо цю функцію*/
			Tree.Add(new Node("<procedure-identifier>", currentLevel));/*додаємо дочірній елемент*/
			Tree = Tree.Children[1];/*переходимо на нього*/
			ProcedureIdentifier(currentLevel + 1);/*виконуємо процедуру,який буде розбирати цей елемент (правило що відповідає"<procedure-identifier>") */
			RiseErrorOrAddNodeToTree(currentLevel, ";");
			Tree.Add(new Node("<block>", currentLevel));
			Tree = Tree.Children[3];
			Block(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, ";");
		}
		private void Block(int currentLevel)
		{
			Tree.Add(new Node("<declarations>", currentLevel));
			Tree = Tree.Children[0];
			Declarations(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, "BEGIN");
			Tree.Add(new Node("<statement-list>", currentLevel));
			Tree = Tree.Children[2];
			StatementList(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, "END");
			Tree = Tree.Parent;
		}
		private void StatementList(int currentLevel)
		{
			Empty(currentLevel + 1);

			Tree = Tree.Parent;
		}


		private void Declarations(int currentLevel)
		{
			Tree.Add(new Node("<procedure-declaration>", currentLevel));
			Tree = Tree.Children[0];
			ProcedureDeclarations(currentLevel + 1);
			Tree = Tree.Parent;
		}
		private void ProcedureDeclarations(int currentLevel)
		{
			if (LexemString[currentLexem].value == "BEGIN")
			{
				Empty(currentLevel + 1);
				Tree = Tree.Parent;
				return;
			}
			Tree.Add(new Node("<procedure>", currentLevel));
			Tree = Tree.Children[0];
			Procedure(currentLevel + 1);
			Tree.Add(new Node("<procedure-declaration>", currentLevel));
			Tree = Tree.Children[1];
			ProcedureDeclarations(currentLevel + 1);
			Tree = Tree.Parent;
		}

		private void Procedure(int currentLevel)
		{
			RiseErrorOrAddNodeToTree(currentLevel, "PROCEDURE");
			Tree.Add(new Node("<procedure-identifier>", currentLevel));
			Tree = Tree.Children[1];
			ProcedureIdentifier(currentLevel + 1);
			if (LexemString[currentLexem + 1].value == ";")
				RiseErrorOrAddNodeToTree(currentLevel, ";");
			else
			{
				Tree.Add(new Node("<parameters-list>", currentLevel));
				Tree = Tree.Children[2];
				ParametersList(currentLevel + 1);
				RiseErrorOrAddNodeToTree(currentLevel, ";");
			}
			Tree = Tree.Parent;

		}
		private void ParametersList(int currentLevel)
		{
			if (LexemString[currentLexem].value == ";")
			{
				Empty(currentLevel + 1);
				Tree = Tree.Parent;
				return;
			}
			RiseErrorOrAddNodeToTree(currentLevel, "(");
			Tree.Add(new Node("<declarations-list>", currentLevel));
			Tree = Tree.Children[1];
			DeclarationsList(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, ")");
			Tree = Tree.Parent;
		}
		private void DeclarationsList(int currentLevel)
		{
			if (LexemString[currentLexem].value == ")")
			{
				Empty(currentLevel);
				Tree = Tree.Parent;
				return;
			}
			Tree.Add(new Node("<declaration>", currentLevel));
			Tree = Tree.Children[0];
			Declaration(currentLevel + 1);
			Tree.Add(new Node("<declarations-list>", currentLevel));
			Tree = Tree.Children[1];
			DeclarationsList(currentLevel + 1);
			Tree = Tree.Parent;

		}
		private void Declaration(int currentLevel)
		{
			Tree.Add(new Node("<variable-identifier>", currentLevel));
			Tree = Tree.Children[0];
			VariableIdentifier(currentLevel + 1);
			Tree.Add(new Node("<identifiers-list>", currentLevel));
			Tree = Tree.Children[1];
			IdentifierList(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, ":");
			Tree.Add(new Node("<attribute>", currentLevel));
			Tree = Tree.Children[3];
			Attribute(currentLevel + 1);
			Tree.Add(new Node("<attribute-list>", currentLevel));
			Tree = Tree.Children[4];
			AttributesList(currentLevel + 1);
			RiseErrorOrAddNodeToTree(currentLevel, ";");
			Tree = Tree.Parent;
		}
		private void IdentifierList(int currentLevel)
		{
			if (LexemString[currentLexem].value == ":")
			{
				Empty(currentLevel + 1);
				Tree = Tree.Parent;
				return;
			}

			RiseErrorOrAddNodeToTree(currentLevel, ",");
			Tree.Add(new Node("<variable-identifier>", currentLevel));
			Tree = Tree.Children[1];
			VariableIdentifier(currentLevel + 1);
			Tree.Add(new Node("<identifiers-list>", currentLevel));
			Tree = Tree.Children[2];
			IdentifierList(currentLevel + 1);

			Tree = Tree.Parent;
		}
		private void AttributesList(int currentLevel)
		{
			if (LexemString[currentLexem].value == ";")
			{
				Empty(currentLevel + 1);
				Tree = Tree.Parent;
				return;
			}
			Tree.Add(new Node("<attribute>", currentLevel));
			Tree = Tree.Children[0];
			Attribute(currentLevel + 1);
			Tree.Add(new Node("<attribute-list>", currentLevel));
			Tree = Tree.Children[1];
			AttributesList(currentLevel + 1);
			Tree = Tree.Parent;
		}
		private void Attribute(int currentLevel)
		{
			if (Keywords.ContainsValue(LexemString[currentLexem].code))
				Tree.Add(new Node(LexemString[currentLexem].value, currentLevel, LexemString[currentLexem++]));
			else
				RiseErrorOrAddNodeToTree(currentLevel, "ATTRIBUTE");
			Tree = Tree.Parent;
		}
		private void VariableIdentifier(int currentLevel)
		{
			Tree.Add(new Node("<identifier>", currentLevel));
			Tree = Tree.Children[0];
			Identifier(currentLevel + 1);
			Tree = Tree.Parent;
		}
		private void ProcedureIdentifier(int currentLevel)
		{
			Tree.Add(new Node("<identifier>", currentLevel));
			Tree = Tree.Children[0];
			Identifier(currentLevel + 1);
			Tree = Tree.Parent;
		}
		private void Identifier(int currentLevel)
		{
			if (Identifiers.ContainsValue(LexemString[currentLexem].code))
				Tree.Add(new Node(LexemString[currentLexem].value, currentLevel, LexemString[currentLexem++]));
			else
				RiseErrorOrAddNodeToTree(currentLevel, "IDENTIFIER");
			Tree = Tree.Parent;
		}

		private void Expression(int currentLevel)
		{
			Tree.Add(new Node("<variable-identifier>", currentLevel));
			Tree = Tree.Children[0];
			VariableIdentifier(currentLevel + 1);
			Tree.Add(new Node("<expression-tail>", currentLevel));
			Tree = Tree.Children[1];
			ExpressionTail(currentLevel + 1);
			Tree = Tree.Parent;

		}
		private void ExpressionTail(int currentLevel)
		{
			if(LexemString[currentLexem].value=="+" || LexemString[currentLexem].value == "-")
			{
				RiseErrorOrAddNodeToTree(currentLevel, LexemString[currentLexem].value);
				Tree.Add(new Node("<variable-identifier>", currentLevel));
				Tree = Tree.Children[1];
				VariableIdentifier(currentLevel + 1);
			}
			else
			{
				Empty(currentLevel + 1);
			}
			Tree = Tree.Parent;

		}

		private void Empty(int currentLevel)
		{
			Tree.Add(new Node("<empty>", currentLevel));
		}

		#endregion AnalyzeFunctions


		private void RiseErrorOrAddNodeToTree(int currentLevel, string expectedString)
		{
			if (currentLexem < LexemString.Count && LexemString[currentLexem].value == expectedString) /*перевіряє чи не вийшли рядку лексем і чи відновідає значення поточної лексеми на яку ми очікуємо*/
			{
				Tree.Add(new Node(expectedString, currentLevel, LexemString[currentLexem]));
				currentLexem++;
			}
			else
			{
				SyntaxErrorTypes type = SyntaxErrorTypes.Begin; /*для визначення типу */
				switch (expectedString)
				{
					case "PROGRAM":
						type = SyntaxErrorTypes.Program;
						break;
					case "BEGIN":
						type = SyntaxErrorTypes.Begin;
						break;
					case "END":
						type = SyntaxErrorTypes.End;
						break;
					case "PROCEDURE":
						type = SyntaxErrorTypes.Procedure;
						break;
					case "ATTRIBUTE":
						type = SyntaxErrorTypes.Attribute;
						break;
					case "VARIABLE":
						type = SyntaxErrorTypes.Variable;
						break;
					case "IDENTIFIER":
						type = SyntaxErrorTypes.Identifier;
						break;

					case ";":
						type = SyntaxErrorTypes.Semicolon;
						break;
					case ":":
						type = SyntaxErrorTypes.Colon;
						break;
					case "(":
						type = SyntaxErrorTypes.OpenBracket;
						break;
					case ")":
						type = SyntaxErrorTypes.CloseBracket;
						break;
					default:
						break;
				}
				ErrorHandler(type);
			}
		}

		private void ErrorHandler(SyntaxErrorTypes type)
		{
			if (currentLexem < LexemString.Count)
				Error = new SyntaxError(type, new Error(LexemString[currentLexem].line, LexemString[currentLexem].column));
			else
				Error = new SyntaxError(type, new Error(LexemString[currentLexem - 1].line, LexemString[currentLexem - 1].column + LexemString[currentLexem - 1].value.Length));
			OnError?.Invoke(); /*подія - коли відбулася помилка в средині то воно вибиває на верх */
		}
	}
}
