using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StatusServer
{
	public abstract class Status
	{
		internal static List<Status> All = new List<Status>();

		static Status() {
			All = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t => t.IsSubclassOf(typeof(Status)))
				.Select(Activator.CreateInstance)
				.Cast<Status>()
				.ToList();
		}

		protected Status()
			: this(TimeSpan.FromSeconds(5)) {
		}

		protected Status(TimeSpan delay) {
			this.Fault = "Not running yet!";

			new Thread(() => {
				while (true) {
					Thread.Sleep(delay);
					try {
						Verify();
						this.Fault = null;
					} catch (Exception e) {
						this.Fault = e.Message;
					}
				}
			}) {
				IsBackground = true
			}.Start();
		}

		// not very thread-safe, but probably nothing will go wrong
		public string Fault { get; private set; }

		public abstract string Name { get; }
		
		protected abstract void Verify();
	}
}
