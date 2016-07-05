﻿using System;

using NUnit.Framework;

namespace Rant.Tests.Compiler
{
	[TestFixture]
	public class Valid
	{
		[Test]
		public void Plaintext()
		{
			RantPattern.FromString(@"just some text");
		}

		[TestCase(@"{}")]
		[TestCase(@"{|}")]
		[TestCase(@"{||}")]
		[TestCase(@"{Item 1}")]
		[TestCase(@"{Item 1|Item 2}")]
		[TestCase(@"{Item 1|Item 2|Item 3}")]
		public void Blocks(string pattern)
		{
			RantPattern.FromString(pattern);
		}

		[Test]
		public void SubroutineNoParams()
		{
			RantPattern.FromString(@"[$[test]:{A|B|C|D}]");
		}

		[TestCase("arg1")]
		[TestCase("@arg1")]
		[TestCase("arg1;arg2")]
		[TestCase("@arg1;arg2")]
		[TestCase("arg1;arg2;arg3")]
		[TestCase("@arg1;@arg2;@arg3")]
		public void SubroutineParams(string args)
		{
			RantPattern.FromString($"[$[test:{args}]:{{A|B|C|D}}]");
		}

		[TestCase("<noun>")]
		[TestCase("<noun.plural>")]
		[TestCase("<noun-class>")]
		[TestCase("<noun - class>")]
		[TestCase("<noun-class1-class2>")]
		[TestCase("<noun-class1-!class2>")]
		[TestCase("<noun-!class-!class2>")]
		[TestCase("<noun-class1|class2>")]
		[TestCase("<noun-class1 | class2>")]
		[TestCase("<noun-!class1|!class2>")]
		[TestCase("<noun-class1|class2-class3|class4>")]
		[TestCase("<noun-!class1|!class2-!class3|!class4>")]
		[TestCase("<noun$-class.plural>")]
		[TestCase("<noun$-class .plural>")]
		[TestCase("<noun$-class>")]
		[TestCase("<noun$>")]
		[TestCase("<noun ? `regex`>")]
		[TestCase("<noun ?! `regex`>")]
		[TestCase("<noun ? `regex` ?! `regex`>")]
		[TestCase("<noun::=a>")]
		[TestCase("<noun :: =a>")]
		[TestCase("<noun :: =a @b>")]
		public void Queries(string query)
		{
			RantPattern.FromString(query);
		}
	}
}