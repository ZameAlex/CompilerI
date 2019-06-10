using Shared.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyntaxAnalyzer
{
	public class Node
	{
		public string Data { get; protected set; }
		public int Level { get; protected set; }
		public Lexem Lexem{ get; protected set; }
		public List<Node> Children { get; protected set; }
		public Node Parent{ get; protected set; }
		public Node(string data, int level, Lexem lexem = default(Lexem))
		{
			if (!String.IsNullOrEmpty(data))
				Data = data;
			if (level >= 0)
				Level = level;
			Lexem = lexem;
			Children = new List<Node>();
		}

		public void Add(Node item)
		{
			item.Parent = this;
			Children.Add(item);
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			for (int i = 0; i < Level; i++)
			{
				result.Append("| ");
			}
			result.Append($"{Data}\n");
			foreach (var item in Children)
			{
				result.Append(item.ToString());
			}
			return result.ToString();
		}
	}
}
