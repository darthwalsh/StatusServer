using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatusServer;

namespace Tests
{
	[TestClass]
	public class StatusDataTests
	{
		[TestMethod]
		public void TestRoundTrip() {
			VerifyRoundTrip(new StatusData());
			VerifyRoundTrip(new StatusData("Failures!"));
			VerifyRoundTrip(new StatusData(""));
			VerifyRoundTrip(new StatusData(" \r\n  fails \r\n "));
		}

		static void VerifyRoundTrip(StatusData original) {
			StatusData serialized = StatusData.TryDeserialize(original.Serialize());
			
			Assert.IsNotNull(serialized);
			Assert.AreEqual(original.DateTime, serialized.DateTime, ".DateTime");
			Assert.AreEqual(original.ErrorMessage, serialized.ErrorMessage, ".ErrorMessage");
		}

		[TestMethod]
		public void TestMinimize() {
			VerifyMinimize(new int[0]);
			
			VerifyMinimize(new[] { 0 }, 0);
			VerifyMinimize(new[] { 1 }, 1);
			
			VerifyMinimize(new[] { 0, 0 }, 0, 0);
			VerifyMinimize(new[] { 1, 0 }, 1, 0);
			VerifyMinimize(new[] { 0, 1 }, 0, 1);
			VerifyMinimize(new[] { 1, 1 }, 1, 1);

			VerifyMinimize(new[] { 0, 0, 0 }, 0, 0);
			VerifyMinimize(new[] { 0, 0, 1 }, 0, 0, 1);
			VerifyMinimize(new[] { 0, 1, 0 }, 0, 1, 0);
			VerifyMinimize(new[] { 0, 1, 1 }, 0, 1, 1);
			VerifyMinimize(new[] { 1, 0, 0 }, 1, 0, 0);
			VerifyMinimize(new[] { 1, 0, 1 }, 1, 0, 1);
			VerifyMinimize(new[] { 1, 1, 0 }, 1, 1, 0);
			VerifyMinimize(new[] { 1, 1, 1 }, 1, 1);

			VerifyMinimize(new[] { 0, 2, 0 }, 0, 2, 0);

			VerifyMinimize(new[] { 0, 0, 0, 1, 2, 2, 2 },
				0, 0, 1, 2, 2);
		}

		static void VerifyMinimize(int[] stream, params int[] expected) {
			CollectionAssert.AreEqual(expected, stream.Minimize().ToArray());
		}
	}
}
