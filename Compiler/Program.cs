using LexicAnalyzer;
using SemanticAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
	class Program
	{
		static Lexer l = new Lexer("F1.txt", "2.txt", "separs.txt", "Multi.txt");
		static Syntaxer s = new Syntaxer(l.LexemString, l.Identifiers, l.Keywords);
		static CodeGenerator g = new CodeGenerator(l.Identifiers, l.Keywords, l.Constants);

		static void Error()
		{
			//s.Tree.Write();
			Console.ReadKey();
			Environment.Exit(0);
		}

		static void Main(string[] args)
		{
			
			var x =Directory.GetCurrentDirectory();
			l.Analyze();
			l.WriteLexemStringToConsole();
			s.OnError += ErrorHandler;
			s.Analyze();
			Console.WriteLine(s.Tree.ToString());
			g.Generate(s.Tree);
			foreach (var item in g.Errors)
			{
				Console.WriteLine(item);
			}
			Console.ReadKey();
		}

		static void ErrorHandler()
		{
			Console.WriteLine(s.Error);
			Console.ReadKey();
			Environment.Exit(1);
		}
	}
}
