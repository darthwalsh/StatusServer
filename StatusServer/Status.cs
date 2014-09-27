using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StatusServer
{
	public abstract class Status
	{
		static List<Status> all;
		static ExceptionDispatchInfo exception;

		internal static List<Status> All {
			get {
				if (exception != null)
					exception.Throw();
				return all;
			}
		}

		static Status() {
			try {
				all = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(Status)))
			.Select(Activator.CreateInstance)
			.Cast<Status>()
			.ToList();
			} catch (Exception e) {
				exception = ExceptionDispatchInfo.Capture(e);
			}
		}

		protected Status(string name)
			: this(name, TimeSpan.FromSeconds(5)) {
		}

		protected Status(string name, TimeSpan delay) {
			if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				throw new ArgumentException("name", "Not a valid file name!");

			this.Name = name;
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

		public string Name { get; private set; }
		
		protected abstract void Verify();
	}
}
