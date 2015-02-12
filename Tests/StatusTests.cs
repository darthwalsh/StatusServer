using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatusServer;
using System.Threading;
using System.IO;

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

        class ControllableStatus : Status
        {
            readonly EventWaitHandle pauser;
            readonly EventWaitHandle starter;

            public ControllableStatus(EventWaitHandle pauser, EventWaitHandle starter)
                : base(small)
            {
                this.pauser = pauser;
                this.starter = starter;

                this.Pass = true;
            }

            public bool Pass { get; set; }

            protected override void Verify() {
                pauser.WaitOne();
                if (!this.Pass) {
                    throw new Exception("failure!");
                }

                this.starter.Set();
            }
        }

        [TestMethod]
        public void TestOnFailure() {
            using (EventWaitHandle pauser = new AutoResetEvent(false))
            using (EventWaitHandle starter = new AutoResetEvent(false)) {
                const int MAGIC = -55;

                int fails = MAGIC;
                Action<Status, int> onFailure = (s, f) => { 
                    fails = f;
                    starter.Set();
                };

                Status.OnFailure += onFailure;
                
                var dir = Path.Combine(Status.StatusServerPath, DateTime.Now.ToString("yyyy-MM-dd"));

                Directory.Delete(Status.StatusServerPath, recursive: true);
                Directory.CreateDirectory(dir);

                dir = Path.Combine(dir, typeof(ControllableStatus).Name + ".txt");

                File.WriteAllText(dir, "");

                using (var writer = new StreamWriter(dir, append: true)) {
                    writer.WriteLine(new StatusData("failure").Serialize());
                    writer.WriteLine(new StatusData().Serialize());
                }

                var status = new ControllableStatus(pauser, starter);

                Status.Initialize(new[] { status });

                pauser.Set();
                starter.WaitOne();

                Assert.AreEqual(MAGIC, fails);

                status.Pass = false;

                pauser.Set();
                starter.WaitOne();

                Assert.AreEqual(2, fails);
                fails = MAGIC;

                pauser.Set();
                starter.WaitOne();

                Assert.AreEqual(0, fails);
                fails = MAGIC;

                status.Pass = true;

                pauser.Set();
                starter.WaitOne();

                Assert.AreEqual(MAGIC, fails);

                status.Pass = false;

                pauser.Set();
                starter.WaitOne();

                Assert.AreEqual(1, fails);

                Status.OnFailure -= onFailure;

                Status.ShutDown();
            }
        }
	}
}
