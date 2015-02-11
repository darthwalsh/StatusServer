using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatusServer;
using System.Threading;

namespace Tests
{
	[TestClass]
	public class StatusTests
	{
        static readonly TimeSpan small = TimeSpan.FromMilliseconds(50);
        static readonly TimeSpan medium = small + small;
        static readonly TimeSpan large = medium + medium;

        class DummyStatus : Status
        {
            readonly Action signal;
            public DummyStatus(Action signal)
                : base(medium) {
                this.signal = signal;
            }

            protected override void Verify() {
                this.signal();
            }
        }

		[TestMethod]
		public void TestImmediateStart() {
			object padlock = new object();
            bool signaled = false;
            Action signal = () => {
                lock (padlock) {
                    signaled = true;
                }
            };

            var status = new DummyStatus(signal);

            Thread.Sleep(large);

            Assert.IsFalse(signaled, "signaled before");

            Status.Initialize(new[] { status });

            Thread.Sleep(small);

            Assert.IsTrue(signaled, "signaled after");

            Status.ShutDown();
		}
	}
}
