﻿using NUnit.Framework;

namespace Rant.Tests
{
	[TestFixture]
	public class Subroutines
	{
		[Test]
		public void NoParamsOutput()
		{
			Assert.AreEqual("1 2 3 4 5 6 7 8 9 10", 
				new RantEngine().Do(@"[$[test]:[r:10][s:\s]{[rn]}][$test]").Main);
		}

		[Test]
		[ExpectedException(typeof(RantRuntimeException))]
		public void OutOfScope()
		{
			new RantEngine().Do(@"{[$[test]:\16,c]}[$test]");
		}

		[Test]
		public void InnerScope()
		{
			new RantEngine().Do(@"[$[test]:\16,c]{[$test]}");
		}

		[Test]
		public void ArgumentsConstant()
		{
			Assert.AreEqual("ABC",
				new RantEngine().Do(@"[$[test: arg1; arg2; arg3]:[arg:arg1][arg:arg2][arg:arg3]][$test:A;B;C]").Main);
		}

		[Test]
		public void ArgumentsDynamic()
		{
			Assert.AreEqual("{|}",
				new RantEngine().Do(@"[$[test: arg1; arg2; arg3]:[arg:arg1][arg:arg2][arg:arg3]][$test:\{;\|;\}]").Main);
		}

		[Test]
		public void ArgumentsLazy()
		{
			Assert.AreEqual("123",
				new RantEngine().Do(@"[$[test: @a]:[r:3]{[arg:a]}][$test:[rn]]").Main);
		}

		[Test]
		public void EmptySubroutine()
		{
			new RantEngine().Do(@"[$[test]:]");
		}

		[Test]
		[ExpectedException(typeof(RantRuntimeException))]
		public void MissingSubroutine()
		{
			new RantEngine().Do(@"[$missing]");
		}

		[Test]
		[ExpectedException(typeof(RantRuntimeException))]
		public void StackOverflow()
		{
			new RantEngine().Do(@"[$[_]:[$_]][$_]");
		}
	}
}