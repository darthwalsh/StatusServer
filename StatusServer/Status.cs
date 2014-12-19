using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace StatusServer
{
	class StatusData : IEquatable<StatusData>
	{
		public StatusData(string errorMessage = null) {
			this.DateTime = DateTime.Now;
			this.ErrorMessage = errorMessage;
		}

		public DateTime DateTime { get; private set; }
		public string ErrorMessage { get; private set; }

		public string Serialize() {
			string result = "" + this.DateTime.Ticks;
			if (this.ErrorMessage != null) {
				result += " " + this.ErrorMessage.Replace("\\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n");
			}
			return result;
		}

		public static StatusData Deserialize(string serialized) {
			int index = serialized.IndexOf(" ");
			if (index == -1) {
				return new StatusData { DateTime = DateTime.FromBinary(long.Parse(serialized)) };
			}
			return new StatusData { 
				DateTime = DateTime.FromBinary(long.Parse(serialized.Substring(0, index))),
				ErrorMessage = serialized.Substring(index + 1).Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\\", "\\")
			};
		}

		public static StatusData TryDeserialize(string serialized) {
			try {
				return Deserialize(serialized);
			} catch {
				return null;
			}
		}

		public override int GetHashCode() {
			int hash = this.DateTime.GetHashCode();
			if (this.ErrorMessage != null)
				hash ^= this.ErrorMessage.GetHashCode();
			return hash;
		}

		public override bool Equals(object other) {
			return Equals(other as StatusData);
		}

		public bool Equals(StatusData other) {
			return other != null 
				&& this.DateTime == other.DateTime
				&& this.ErrorMessage == other.ErrorMessage;
		}
	}

	public abstract class Status
	{
#if DEBUG
        static readonly TimeSpan defaultWait = TimeSpan.FromSeconds(1);
#else
		static readonly TimeSpan defaultWait = TimeSpan.FromMinutes(5);
#endif

		static Dictionary<string, Status> all;

		internal static Dictionary<string, Status> All {
			get {
				return all;
			}
		}

		public static void Initialize() {
			all = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(Status)))
			.Where(t => !t.IsAbstract)
			.Select(Activator.CreateInstance)
			.Cast<Status>()
            .ToDictionary(s => s.Name);

            foreach (var s in all.Values)
                s.Start();
        }

        public static void ShutDown() {
            foreach (var s in all.Values) {
                lock (s.padLock) {
                    s.stop = true;
                }
                s.threadWait.Set();
            }

            foreach (var s in all.Values) {
                s.thread.Join();
                s.threadWait.Dispose();
            }

            all.Clear();
        }

        public static void WaitAll() {
            using (var e = new CountdownEvent(all.Count)) {
                foreach (var s in all.Values) {
                    lock (s.padLock) {
                        s.finishCallbacks.Enqueue(() => e.Signal());
                    }
                }

                foreach (var s in all.Values) {
                    s.threadWait.Set();
                }

                e.Wait();
            }
        }

        readonly Thread thread;
        readonly object padLock = new object();
        readonly Queue<Action> finishCallbacks = new Queue<Action>();
        readonly EventWaitHandle threadWait = new AutoResetEvent(false);
        
        bool stop = false;

		protected Status()
			: this(defaultWait) {
		}

		protected Status(TimeSpan delay) {
			string name = GetType().Name;
			
			if (!name.All(char.IsLetterOrDigit) || name.Length == 0)
				throw new ArgumentException("name", "Not a valid file name!");

			this.Name = name;

			this.History = ImmutableStack<StatusData>.Empty;
			var saved = new List<StatusData>();
			if (Directory.Exists(StatusServerPath))
				this.History = ImmutableStack<StatusData>.New(
					Directory.EnumerateFiles(StatusServerPath, this.Name + ".txt", SearchOption.AllDirectories)
						.SelectMany(File.ReadAllLines)
						.Select(StatusData.TryDeserialize)
						.Where(data => data != null)
						.OrderBy(data => data.DateTime));

			this.thread = new Thread(() => {
				while (true) {
                    threadWait.WaitOne(delay);

                    lock (this.padLock) {
                        if (this.stop)
                            break;
                    }

					StatusData data;
					try {
						Verify();
						data = new StatusData();
					} catch (Exception e) {
						data = new StatusData(e.ToString());
					}

					Log(data);

                    lock (this.padLock) {
                        while (this.finishCallbacks.Any())
                            this.finishCallbacks.Dequeue()();
                    }
				}
			}) {
				IsBackground = true
			};
		}

        void Start() {
            this.thread.Start();
        }

		void Log(StatusData data) {
			this.History = this.History.Push(data);
			using (var writer = new StreamWriter(this.SavePath, append: true)) {
				writer.WriteLine(data.Serialize());
			}
		}

		static string StatusServerPath {
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"StatusServer");
			}
		}

		string SavePath {
			get {
				var dir = Path.Combine(StatusServerPath,
					DateTime.Now.ToString("yyyy-MM-dd"));
				Directory.CreateDirectory(dir);
				return Path.Combine(dir, this.Name + ".txt");
			}
		}
		
		internal IStack<StatusData> History { get; private set; }

		public string Name { get; private set; }

		protected abstract void Verify();
	}

	static class MinimizeExtensions
	{
		// Removes duplicates, e.g. 0, 0, 0, 1, 1, 1 becomes 0, 0, 1, 1
		internal static IEnumerable<T> Minimize<T>(this IEnumerable<T> data, IEqualityComparer<T> comparer) {
			T past = default(T), present = default(T), future = default(T);
			using (var it = data.GetEnumerator()) {

				if (!it.MoveNext())
					yield break;
				future = it.Current;

				if (!it.MoveNext()) {
					yield return future;
					yield break;
				}
				present = future;
				future = it.Current;
				yield return present;

				while (it.MoveNext()) {
					past = present;
					present = future;
					future = it.Current;

					if (!comparer.Equals(future, present) || !comparer.Equals(present, past))
						yield return present;
				}
			}

			yield return future;
		}
			 
		internal static IEnumerable<T> Minimize<T>(this IEnumerable<T> data) {
			return data.Minimize(EqualityComparer<T>.Default);
		}
	}
}
