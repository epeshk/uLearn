﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using uLearn.Model.Blocks;

namespace uLearn.CSharp
{
	[TestFixture]
	public class CsMembersRemover_should
	{
		private readonly DirectoryInfo dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).GetSubdir("tests");

		private string LoadCode(string file = "OverloadedMethods.cs")
		{
			return dir.GetContent(file).LineEndingsToUnixStyle();
		}

		private string RemoveRegion(string region, bool onlyBody = false)
		{
			var code = LoadCode();
			IEnumerable<Label> notRemoved;
			code = new CsMembersRemover().Remove(code, new[] { new Label { Name = region, OnlyBody = onlyBody } }, out notRemoved);
			Assert.IsEmpty(notRemoved);
			return code;
		}

		private string RemoveRegions(params Label[] regions)
		{
			var code = LoadCode();
			IEnumerable<Label> notRemoved;
			code = new CsMembersRemover().Remove(code, regions, out notRemoved);
			Assert.IsEmpty(notRemoved);
			return code;
		}

		[Test]
		public void SingleMethod()
		{
			Approvals.Verify(RemoveRegion("Main"));
		}

		[Test]
		public void SingleMethodBody()
		{
			Approvals.Verify(RemoveRegion("Main", true));
		}

		[Test]
		public void OverloadMethod()
		{
			Approvals.Verify(RemoveRegion("Method"));
		}

		[Test]
		public void OverloadMethodBody()
		{
			Approvals.Verify(RemoveRegion("Method", true));
		}

		[Test]
		public void SingleClass()
		{
			Approvals.Verify(RemoveRegion("OverloadedMethods"));
		}

		[Test]
		public void SingleClassBody()
		{
			Approvals.Verify(RemoveRegion("OverloadedMethods", true));
		}

		[Test]
		public void SingleMethodSolution()
		{
			var code = LoadCode();
			int index;
			code = new CsMembersRemover().RemoveSolution(code, new Label { Name = "Main" }, out index);
			Approvals.Verify(String.Format("solution index: {0}\r\nCode:\r\n{1}", index, code));
		}

		[Test]
		public void SingleMethodBodySolution()
		{
			var code = LoadCode();
			int index;
			code = new CsMembersRemover().RemoveSolution(code, new Label { Name = "Main", OnlyBody = true }, out index);
			Approvals.Verify(String.Format("solution index: {0}\r\nCode:\r\n{1}", index, code));
		}

		[Test]
		public void RemoveUsings()
		{
			var code = LoadCode();
			Approvals.Verify(CsMembersRemover.RemoveUsings(code));
		}

		[Test]
		public void RemoveManyMethods()
		{
			Approvals.Verify(RemoveRegions(new Label { Name = "Method" }, new Label { Name = "Main" }));
		}

		[Test]
		public void RemoveManyMethodsSameBody()
		{
			Approvals.Verify(RemoveRegions(new Label { Name = "Method", OnlyBody = true }, new Label { Name = "Main" }));
		}

		[Test]
		public void RemoveManyMethodsAllBody()
		{
			Approvals.Verify(RemoveRegions(new Label { Name = "Method", OnlyBody = true }, new Label { Name = "Main", OnlyBody = true }));
		}
	}
}