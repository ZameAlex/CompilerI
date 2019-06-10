using Shared.Structs;
using Shared.Enums;
using LexicAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemanticAnalyzer
{
	public class CodeGenerator
	{
		//public List<string> DataTypes { get; protected set; }
		public Dictionary<string, int> Identifiers { get; protected set; }
		public Dictionary<string, int> Keywords { get; protected set; }
		public Dictionary<string, int> Constants { get; protected set; }
		public List<string> Errors{ get; protected set; }

		private List<Lexem> globalVariables;
		private List<Lexem> localVariables;
		private StreamWriter writer;
		private Lexem programIdentifier; 
		private Lexem procedureIdentifier; 
		private Lexem currentIdentifier;
		private List<string> mainDataTypes;
		private List<string> additionalDataTypes;
		private bool isError;
		private string dataType;
		private string additionalType;

		public CodeGenerator(Dictionary<string,int> identifiers, Dictionary<string, int> keywords, Dictionary<string, int> constants) 
		{
			isError = false;
			Errors = new List<string>();
			globalVariables = new List<Lexem>();
			localVariables = new List<Lexem>();
			mainDataTypes = new List<string> { "INTEGER", "FLOAT", "BLOCKFLOAT" };
			additionalDataTypes = new List<string> { "SIGNAL", "EXT", "COMPLEX" };
			writer = new StreamWriter("code.asm");
			Identifiers = identifiers;
			Keywords = keywords;
			Constants = constants;
			
		}

		public void Generate(Node tree)
		{
			Analyze(tree);
			writer.Dispose(); 
		}

		private void Analyze(Node tree)
		{
			switch (tree.Data)
			{
				case "<signal-program>":
					Analyze(tree.Children.First());
					break;
				case "<program>":
					Analyze(tree.Children[1]);
					programIdentifier = currentIdentifier;
					writer.WriteLine("CODE SEGMENT:");
					writer.WriteLine();
					Analyze(tree.Children[3]);
					writer.WriteLine($"{programIdentifier.value}:");
					writer.WriteLine("NOP");
					writer.WriteLine("CODE ENDS");
					writer.WriteLine($"END {programIdentifier.value}");
					break;
				case "<block>":
					Analyze(tree.Children.First());
					Analyze(tree.Children[2]);
					break;
				case "<statement-list>":
					break;
				case "<declarations>":
					Analyze(tree.Children.First());
					break;
				case "<procedure-declaration>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count > 1)
						Analyze(tree.Children[1]);
					break;
				case "<procedure>":
					Analyze(tree.Children[1]);
					if (!isError)
					{
						procedureIdentifier = currentIdentifier;
						writer.WriteLine($"{procedureIdentifier.value} PROC");
						writer.WriteLine("PUSH EBP");
						writer.WriteLine("MOV EBP, ESP");
						writer.WriteLine($"RET {procedureIdentifier.value}");
						writer.WriteLine("ENDP");
					}
					else
						isError = false;
					Analyze(tree.Children[2]);

					break;
				case "<parameters-list>":
					if (tree.Children != null && tree.Children.Count > 1)
					{
						Analyze(tree.Children[1]);
						localVariables.Clear();
					}
					break;
				case "<declarations-list>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count > 1)
						Analyze(tree.Children[1]);
					break;
				case "<declaration>":
					Analyze(tree.Children.First());
					Analyze(tree.Children[1]);
					Analyze(tree.Children[3]);
					Analyze(tree.Children[4]);
					break;
				case "<identifiers-list>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count>1)
						Analyze(tree.Children[1]);
					break;
				case "<attribute-list>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count > 1)
						Analyze(tree.Children[1]);
					dataType = null;
					additionalType = null;
					break;
				case "<attribute>":
					var data = tree.Children.First().Data;
					if (mainDataTypes.Contains(data))
					{
						if (!String.IsNullOrEmpty(dataType))
						{
							dataType = null;
							additionalType = null;
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], {currentIdentifier.value} already have data type!");
						}
						else
						{
							dataType = data;
						}
					}
					else if(additionalDataTypes.Contains(data))
					{
						if (!String.IsNullOrEmpty(dataType))
						{
							dataType = null;
							additionalType = null;
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], additional type should be first!");
						}
						else if (!String.IsNullOrEmpty(additionalType))
						{
							dataType = null;
							additionalType = null;
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], additional type should be onle one!");
						}
						else
							additionalType = data;
					}
					break;
				case "<procedure-identifier>":
				case "<variable-identifier>":
					isError = true;
					Analyze(tree.Children.First());
					if (currentIdentifier.value == programIdentifier.value)
					{
						Errors.Add($"Error ocurred at [{currentIdentifier.line};{currentIdentifier.column}], identifier is already used as program identifier");
						Errors.Add($"Program identifier defined at [{programIdentifier.line};{programIdentifier.column}]");
					}
					else if (globalVariables.Contains(currentIdentifier) || localVariables.Contains(currentIdentifier))
					{
						Errors.Add($"This identifier '{currentIdentifier.value}' is already used at [{currentIdentifier.line};{currentIdentifier.column}]");
					}
					else
					{
						isError = false;
						if (tree.Data == "<variable-identifier>")
							localVariables.Add(currentIdentifier);
						else
							globalVariables.Add(currentIdentifier);
					}
					break;
				case "<identifier>":
					currentIdentifier = tree.Children.First().Lexem;
					break;
				default:
					break;
			}

		}
	}
}
