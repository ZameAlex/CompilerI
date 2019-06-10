using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Lexer
{

    public struct SyntaxError
    {
        public string expected;
    }
    public class Node
    {
        public string Data { get; set; }
        public int Level { get; set; }
        public List<Node> Children { get; set; }
        public Node(int level, string data)
        {
            Level = level;
            Data = data;
            Children = new List<Node>();
        }
        public void Write()
        {
            for (int i = 0; i < Level; i++)
            {
                Console.Write("| ");
            }
            Console.WriteLine(Data);
            foreach (var item in Children)
            {
                item.Write();
            }
        }

    }
    public class Syntaxer
    {
        int h;
        Dictionary<string, Dictionary<string, int>> Tables;
        public List<Lexem> String { get; set; }
        public Node Tree;

        public event Action Error;

        SyntaxError err = new SyntaxError();
        public Syntaxer(Dictionary<string, int> identifiers, Dictionary<string, int> constatnts,
            Dictionary<string, int> delimiters,Dictionary<string,int> multiSepar, Dictionary<string, int> keywords, List<Lexem> lexemString, Action ErrorHandler)
        {
            Error += ErrorHandler;
            Tree = new Node(0, "Start");
            String = lexemString;
            Tables = new Dictionary<string, Dictionary<string, int>>();
            Tables.Add("Identifiers", identifiers);
            Tables.Add("Keywords", keywords);
            Tables.Add("Separators", delimiters);
			Tables.Add("MultiSeparators", multiSepar);
            Tables.Add("Constants", constatnts);
            h = 0;

        }

        public void Analyze()
        {
            SignalProgram(Tree);
        }
        

        private void Errors(SyntaxError err)
        {
            Error?.Invoke();
        }

        private void EndOfFileCheck(Node node)
        {
            if (h >= String.Count)
            {
                err.expected = "EOF";
                node.Children.Add(new Node(node.Level + 1, err.expected));
                Errors(err);
            }
        }

        private bool SignalProgram(Node node)// Rule 1
        {
            node.Children.Add(new Node(node.Level + 1, "<signal-program>"));
            return Program(node.Children[0]);
        }

        private bool Program(Node node)// Rule 2
        {
            EndOfFileCheck(node);
            if (String[h].code == Tables["Keywords"]["PROGRAM"])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                h++;
                var procIdentifier = new Node(node.Level + 1, "<procedure-identifier>");
                if (ProcedureIdentifier(procIdentifier))
                {
					node.Children.Add(procIdentifier);
                    EndOfFileCheck(node);
                    if (String[h].code == Tables["Separators"][";"])
                    {
                        node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                        h++;
                        var block = new Node(node.Level + 1, "<block>");
						node.Children.Add(block);
						if (Block(block))
						{
							EndOfFileCheck(node);
							if (String[h].code == Tables["Separators"]["."])
							{
								node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
								h++;
								return true;
							}
							err.expected = ".";
							node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
							Errors(err);
						}
						else
						{
							return false;
						}
                    }
                    err.expected = ";";
                    node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                    Errors(err);
                }
                err.expected = "Procedure identifier";
                node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                Errors(err);
            }
            err.expected = "Keyword PROGRAM";
            node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
            Errors(err);
            return false;
        }

        private bool Block(Node node)// Rule 3
        {
			var varDeclaration = new Node(node.Level + 1, "<variable-declarations>");
			node.Children.Add(varDeclaration);
			if (VariableDeclarations(varDeclaration))
			{
				EndOfFileCheck(node);
				if (String[h].code == Tables["Keywords"]["BEGIN"])
				{
					node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
					h++;
					var statementList = new Node(node.Level + 1, "<statement-list>");
					node.Children.Add(statementList);
					if (StatementsList(statementList))
					{
						EndOfFileCheck(node);
						if (String[h].code == Tables["Keywords"]["END"])
						{
							node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
							h++;
							return true;
						}
						err.expected = "Keyword END";
						node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
						return false;
					}
				}
				err.expected = "Keyword BEGIN";
				node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
				return false;
			}
            return false;
        }

        private bool VariableDeclarations(Node node)// Rule 4
        {
            EndOfFileCheck(node);
            if (String[h].code == Tables["Keywords"]["VAR"])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                h++;
                node.Children.Add(new Node(node.Level + 1, "<declaration-list>"));
                if (DeclarationsList(node.Children[1]))
                    return true;
                return false;
            }
            else
            {
                node.Children.Add(new Node(node.Level + 1, "<empty>"));
                return Empty();
            }
        }

        private bool DeclarationsList(Node node)// Rule 5
        {
			var declaration = new Node(node.Level + 1, "<declaration>");
			node.Children.Add(declaration);
			if (Declaration(declaration))
            {
                if (DeclarationsList(node))
                    return true;
                return false;
            }
            else
            {
                node.Children.Add(new Node(node.Level + 1, "<empty>"));
                return Empty();
            }
        }

        private bool Declaration(Node node)// Rule 6
        {
			var varIdentifier = new Node(node.Level + 1, "<variable-identifier>");
			node.Children.Add(varIdentifier);
			if (VariableIdentifier(varIdentifier))
            {
                EndOfFileCheck(node);
                if (String[h].code == Tables["Separators"][":"])
                {
                    node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                    h++;
					var attrib = new Node(node.Level + 1, "<attribute>");
					if (Atrribute(attrib))
                    {
                        node.Children.Add(attrib);
						var attribList = new Node(node.Level + 1, "<attributes-list>");
						node.Children.Add(attribList);
						if (AttributesList(attribList))
                        {
                            EndOfFileCheck(node);
                            if (String[h].code == Tables["Separators"][";"])
                            {
                                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                                h++;
                                return true;
                            }
                            err.expected = ";";
                            node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                            Errors(err);
                            return false;
                        }
                        return false;
                    }
					{
						err.expected = "Attribute";
						node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
						Errors(err);
						return false;
					}

                }
                err.expected = ":";
                node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                return false;
            }
            return false;
        }

        private bool AttributesList(Node node)// Rule 7
        {
			var attrib = new Node(node.Level + 1, "<attribute>");
			node.Children.Add(attrib);
			if (Atrribute(attrib))
            {
                if (AttributesList(node))
                    return true;
                return false;
            }
            node.Children.Add(new Node(node.Level + 1, "<empty>"));
            return Empty();
        }

        private bool Atrribute(Node node)// Rule 8
        {
            EndOfFileCheck(node);
            if (String[h].code == Tables["Keywords"]["INTEGER"])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                h++;
                return true;
            }
            if (String[h].code == Tables["Keywords"]["FLOAT"])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                h++;
                return true;
            }
            if (String[h].code == Tables["Separators"]["["])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                h++;
				var range = new Node(node.Level + 1, "<range>");

				if (Range(range))
                {
                    node.Children.Add(range);
                    EndOfFileCheck(node);
                    if (String[h].code == Tables["Separators"]["]"])
                    {
                        node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                        h++;
                        return true;
                    }
                    err.expected = "]";
                    node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                    Errors(err);
                }
                return false;
            }
            return false;
        }

        private bool Range(Node node)// Rule 9
        {
			var unsignedInteger1 = new Node(node.Level + 1, "<unsigned-integer>");
			node.Children.Add(unsignedInteger1);
			if (UnsignedInt(unsignedInteger1))
            {
                EndOfFileCheck(node);
                if (String[h].code == Tables["Separators"]["."])
                {
                    node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                    h++;
                    EndOfFileCheck(node);
                    if (String[h].code == Tables["Separators"]["."])
                    {
                        node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                        h++;
						var unsignedInteger2 = new Node(node.Level + 1, "<unsigned-integer>");
						node.Children.Add(unsignedInteger2);
						if (UnsignedInt(unsignedInteger2))
                        {
                            return true;
                        }
                        return false;
                    }
                    err.expected = ".";
                    node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                    Errors(err);
                }
                err.expected = ".";
                node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
                Errors(err);
            }
            return false;
        }

        private bool StatementsList(Node node)// Rule 10
        {
			var statement = new Node(node.Level + 1, "<statement>");
			node.Children.Add(statement);
			if (Statement(statement))
            {
                if (StatementsList(node))
                    return true;
                return false;
            }
            node.Children.Add(new Node(node.Level + 1, "<empty>"));
            return Empty();
        }

        private bool Statement(Node node)// Rule 11
        {
            EndOfFileCheck(node);
			if (String[h].code == Tables["Keywords"]["LOOP"])
			{
				node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
				h++;
				var statementList = new Node(node.Level + 1, "<statement-list>");
				node.Children.Add(statementList);
				if (StatementsList(statementList))
				{
					EndOfFileCheck(node);
					if (String[h].code == Tables["Keywords"]["ENDLOOP"])
					{
						node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
						h++;
						EndOfFileCheck(node);
						if (String[h].code == Tables["Separators"][";"])
						{
							node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
							h++;
							return true;
						}
						err.expected = ";";
						node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
						Errors(err);
					}
					err.expected = "ENDLOOP";
					node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
					Errors(err);
				}
				return false;
			}
			else
			{
				var variable = new Node(node.Level + 1, "<variable>");
				node.Children.Add(variable);
				if (Variable(variable))
				{
					EndOfFileCheck(node);
					if (String[h].code == Tables["MultiSeparators"][":="])
					{
						node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
						h++;
						var expression = new Node(node.Level + 1, "<expression>");
						node.Children.Add(expression);
						if (Expression(expression))
						{
							EndOfFileCheck(node);
							if (String[h].code == Tables["Separators"][";"])
							{
								node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
								h++;
								return true;
							}
							err.expected = ";";
							node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
							Errors(err);
						}
						return false;
					}
					err.expected = ":=";
					node.Children.Add(new Node(node.Level + 1, "Error ocurred, " + err.expected + " expected"));
					Errors(err);
				}
			}
            return false;
        }

        private bool Expression(Node node)// Rule 12
        {
			var variable = new Node(node.Level + 1, "<variable>");
			node.Children.Add(variable);
			if (Variable(variable))
			{
				return true;
			}
			else
			{
				var unsignedInt = new Node(node.Level + 1, "<unsigned-int>");
				node.Children.Add(unsignedInt);
				if (UnsignedInt(unsignedInt))
				{
					return true;
				}
			}
            return false;
        }

        private bool Variable(Node node)// Rule 13
        {
			var variable = new Node(node.Level + 1, "<variable-identifier>");
			node.Children.Add(variable);
			if (VariableIdentifier(variable))
            {
				var dimension = new Node(node.Level + 1, "<dimension>");
				node.Children.Add(dimension);
				if (Dimension(dimension))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool Dimension(Node node)// Rule 14
        {
            EndOfFileCheck(node);
            if (String[h].code == Tables["Separators"]["["])
            {
                node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
				h++;
                if (Expression(node))
                {
                    if (String[h].code == Tables["Separators"]["]"])
                    {
                        node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
						h++;
                        return true;
                    }
                    err.expected = "]";
                    node.Children.Add(new Node(node.Level, "Error ocurred, " + err.expected + " expected"));
                    Errors(err);
                }
                return false;

            }
            else
            {
                node.Children.Add(new Node(node.Level + 1, "<empty>"));
                return Empty();
            }
        }

        private bool VariableIdentifier(Node node)// Rule 15
        {
            node.Children.Add(new Node(node.Level + 1, "<identifier>"));
            return Identifier(node.Children[0]);
        }

        private bool ProcedureIdentifier(Node node)// Rule 16
        {
            node.Children.Add(new Node(node.Level + 1, "<identifier>"));
            return Identifier(node.Children[0]);
        }

        private bool Identifier(Node node)// Rule 17
        {
            foreach (var item in Tables["Identifiers"])
            {
                if (item.Value == String[h].code)
                {
                    node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                    h++;
                    return true;
                }
            }
            return false;
        }

        private bool UnsignedInt(Node node)// Rule 19
        {
            foreach (var item in Tables["Constants"])
            {
                if (item.Value == String[h].code)
                {
                    node.Children.Add(new Node(node.Level + 1, String[h].value + "  " + Convert.ToString(String[h].code)));
                    h++;
                    return true;
                }
            }
            return false;
        }

        private bool Constant(Node node)
        {
            node.Children.Add(new Node(node.Level + 1, "<unsigned-int>"));
            return UnsignedInt(node.Children[0]);
        }

        private bool Empty()
        {
            return true;
        }

        

    }


}
