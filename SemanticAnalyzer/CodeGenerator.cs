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
		public List<string> Errors { get; protected set; }

		private List<Lexem> globalVariables;
		private List<Lexem> localVariables;
		private Dictionary<Lexem, DataType> extVariables;
		private StreamWriter writer;
		private Lexem programIdentifier;
		private Lexem procedureIdentifier;
		private Lexem currentIdentifier;
		private List<string> mainDataTypes;
		private List<string> additionalDataTypes;
		private bool isError;
		private DataType type;

		public CodeGenerator(Dictionary<string, int> identifiers, Dictionary<string, int> keywords, Dictionary<string, int> constants)
		{
			extVariables = new Dictionary<Lexem, DataType>();
			type = DataType.None;
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
					type = DataType.None;
					break;
				case "<identifiers-list>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count > 1)
						Analyze(tree.Children[1]);
					break;
				case "<attribute-list>":
					Analyze(tree.Children.First());
					if (tree.Children != null && tree.Children.Count > 1)
						Analyze(tree.Children[1]);
					if (CheckType(DataType.Ext))
					{
						CheckExtVariable();
					}
					if(!(CheckBaseTypes() || CheckType(DataType.Signal)))
						Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], should have base or signal type!");
					break;
				case "<attribute>":
					var data = tree.Children.First().Data;
					if (mainDataTypes.Contains(data))
					{
						if (CheckBaseTypes())
						{
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], {currentIdentifier.value} already have data type!");
						}
						else
						{
							type |= AssignType(data);
						}
					}
					else if (additionalDataTypes.Contains(data))
					{
						var additionalType = AssignType(data);
						if (CheckType(additionalType))
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], {currentIdentifier.value} already have such additional type!");
						else if (additionalType == DataType.Signal)
							type |= AssignType(data);
						else if (CheckBaseTypes())
						{
							Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], {currentIdentifier.value} have base datatype! Additional datatype should be the first one!");
						}
						else
						{
							switch (additionalType)
							{

								case DataType.Complex:
									AnalyzeComplex();
									break;
								case DataType.Ext:
									AnalyzeExt();
									break;
								case DataType.Signal:
									AnalyzeSignal();
									break;
								default:
									break;
							}
						}
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

		private bool CheckType(DataType typeToCheck)
		{
			return (type & typeToCheck) == typeToCheck;
		}

		private bool CheckBaseTypes()
		{
			return CheckType(DataType.Integer) || CheckType(DataType.Float) || CheckType(DataType.BlockFloat);
		}

		private DataType AssignType(string type)
		{
			switch (type)
			{
				case "FLOAT":
					return DataType.Float;
				case "INTEGER":
					return DataType.Integer;
				case "BLOCKFLOAT":
					return DataType.BlockFloat;
				case "COMPLEX":
					return DataType.Complex;
				case "EXT":
					return DataType.Ext;
				case "SIGNAL":
					return DataType.Signal;
				default:
					return DataType.None;
			}
		}

		private void AnalyzeComplex()
		{
				type |= DataType.Complex;
		}
		private void AnalyzeExt()
		{
			if (CheckType(DataType.None) || CheckType(DataType.Signal))
				type |= DataType.Ext;
			else
				Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], variable should have Ext modifier as first (except signal)!");//Not sure

		}
		private void AnalyzeSignal()
		{
			type |= DataType.Signal;
		}

		private void CheckExtVariable()
		{
			if (extVariables.Keys.Contains(currentIdentifier))
			{
				if (extVariables[currentIdentifier] != type)
					Errors.Add($"Error at [{currentIdentifier.line};{currentIdentifier.column + currentIdentifier.value.Length + 1}], types should be the same for this variables!");
			}
			else
				extVariables.Add(currentIdentifier, type);
		}

	}
}
