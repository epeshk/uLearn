﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace uLearn.CSharp
{
	public class NotEmptyCodeValidator : BaseStyleValidator
	{
		protected override IEnumerable<string> ReportAllErrors(SyntaxTree userSolution)
		{
			var hasCode = userSolution.GetRoot().DescendantNodes().Any(n => n is StatementSyntax || n is MemberDeclarationSyntax);
			if (!hasCode)
				yield return Report(userSolution.GetRoot(), "Пустое решение?!");
		}
	}
}