using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatusServer;

namespace Tests
{
	[TestClass]
	public class ImmutableStackTests
	{
		[TestMethod]
		public void TestEnumerate() {
			IStack<int> x = ImmutableStack<int>.Empty.Push(0).Push(1).Push(2);

			CollectionAssert.AreEqual(new int[] { 2, 1, 0 }, x.ToList());
		}

		[TestMethod]
		public void TestNew() {
			Action<IEnumerable<int>> verify = list =>
				CollectionAssert.AreEqual(list.Reverse().ToList(), ImmutableStack<int>.New(list).ToList());
			
			verify(new List<int>());

			verify(new List<int> { 1 });

			verify(new List<int> { 1, 2 });
		}
	}
}
