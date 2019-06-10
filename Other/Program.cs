using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
	class Program
	{
		static Lexer l = new Lexer("1.txt", "2.txt", "separs.txt", "Multi.txt");
		//static Syntaxer s = new Syntaxer(l.Identifiers, l.Constants, l.Separators,l.MultiSeparators, l.Keywords, l.LexemString, Error);

		static void Error()
		{
			//s.Tree.Write();
			Console.ReadKey();
			Environment.Exit(0);
		}

		static void Main(string[] args)
		{
			l.Analyze();
			l.WriteLexemStringToConsole();
			//s.Analyze();
			//s.Tree.Write();
			Console.ReadKey();

		}
	}
}
