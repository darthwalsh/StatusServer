using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace StatusServer
{
    public interface IData
    {
        string ErrorMessage { get; }
        bool HadError { get; }
    }

	class StatusData : IEquatable<StatusData>, IData
	{
		public StatusData(string errorMessage = null) {
			this.DateTime = DateTime.Now;
			this.ErrorMessage = errorMessage;
		}

		public DateTime DateTime { get; private set; }
		public string ErrorMessage { get; private set; }
        public bool HadError { get { return this.ErrorMessage != null; } }

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

        static Dictionary<string, Status> all = new Dictionary<string,Status>();

		internal static Dictionary<string, Status> All {
			get {
				return all;
			}
		}

        public static void Initialize(IEnumerable<Status> stati) {
            if (all.Any()) {
                throw new Exception("All ready Intialized!");
            }
            all = stati.ToDictionary(s => s.Name);

            foreach (var s in all.Values)
                s.Start();
        }

		public static void Initialize() {
            Initialize(AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsSubclassOf(typeof(Status)))
			.Where(t => !t.IsAbstract)
			.Select(Activator.CreateInstance)
			.Cast<Status>());
        }

        public static void ShutDown() {
            List<Status> toDispose = all.Values.ToList();
            all = new Dictionary<string,Status>();

            foreach (var s in toDispose)
                s.Stop();

            foreach (var s in toDispose)
                s.Join();
        }

        public static event Action<Status> OnFailure = delegate { };

        public static void WaitAll() {
            using (var e = new CountdownEvent(all.Count)) {
                foreach (var s in all.Values) {
                    lock (s.padLock) {
                        s.finishCallbacks.Enqueue(() => e.Signal());
                    }
                }

                foreach (var s in all.Values) {
                    s.verifyWait.Set();
                }

                e.Wait();
            }
        }

        readonly object padLock = new object();
        
        readonly Thread verifyThread;
        readonly EventWaitHandle verifyWait = new AutoResetEvent(true);

        readonly Thread hungThread;
        readonly EventWaitHandle hungWait = new AutoResetEvent(true);

        readonly Queue<Action> finishCallbacks = new Queue<Action>();

        bool stop = false;
        readonly TimeSpan delay;

		protected Status()
			: this(defaultWait) {
		}

		protected Status(TimeSpan d) {
            this.delay = d;
			string name = GetType().Name;
			
			if (!name.All(char.IsLetterOrDigit) || name.Length == 0)
				throw new ArgumentException("name", "Not a valid file name!");

			this.Name = name;

			this.History = ImmutableStack<StatusData>.Empty;
            if (Directory.Exists(StatusServerPath)) {
                this.History = ImmutableStack<StatusData>.New(
                    Directory.EnumerateFiles(StatusServerPath, this.Name + ".txt", SearchOption.AllDirectories)
                        .SelectMany(File.ReadAllLines)
                        .Select(StatusData.TryDeserialize)
                        .Where(data => data != null)
                        .OrderBy(data => data.DateTime));
            }

            this.verifyThread = new Thread(() => {
				while (true) {
                    verifyWait.WaitOne(this.delay);

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
                    if (data.HadError) {
                        OnFailure(this);
                    }

                    lock (this.padLock) {
                        while (this.finishCallbacks.Any())
                            this.finishCallbacks.Dequeue()();
                    }
				}
			}) {
				IsBackground = true
            };

            TimeSpan hungDelay = this.delay + this.delay + TimeSpan.FromSeconds(2);
            DateTime hangIgnored = DateTime.Now + hungDelay;
            this.hungThread = new Thread(() => {
                while (true) {
                    hungWait.WaitOne(hungDelay);

                    lock (this.padLock) {
                        if (this.stop)
                            break;
                    }

                    StatusData first = this.History.FirstOrDefault();
                    if (first == null) {
                        continue;
                    }

                    DateTime now = DateTime.Now;

                    TimeSpan lastResult = now - first.DateTime;

                    if (lastResult > hungDelay && now > hangIgnored) {
                        Log(new StatusData("Evaluation didn't finish within exected time."));
                        OnFailure(this);
                    }
                }
            }) {
                IsBackground = true
            };
		}

        void Start() {
            this.verifyThread.Start();
            this.hungThread.Start();
        }

        void Stop() {
            lock (this.padLock) {
                this.stop = true;
            }
            this.verifyWait.Set();
            //TODO missing? this.hungWait.Set();
        }

        void Join() {
            TimeSpan printDelay = new TimeSpan(100);
            if (!this.verifyThread.Join(printDelay)) {
                Console.WriteLine("Waiting for {0} verify", this.Name);
                this.verifyThread.Join();
            }
            if (!this.hungThread.Join(printDelay)) {
                Console.WriteLine("Waiting for {0} hung", this.Name);
                this.hungThread.Join();
            }
        }

		void Log(StatusData data) {
            lock (this.padLock) {
                this.History = this.History.Push(data);
                using (var writer = new StreamWriter(this.SavePath, append: true)) {
                    writer.WriteLine(data.Serialize());
                } 
            }
		}

		internal static string StatusServerPath {
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"StatusServer");
			}
		}

		internal string SavePath {
			get {
				var dir = Path.Combine(StatusServerPath,
					DateTime.Now.ToString("yyyy-MM-dd"));
				Directory.CreateDirectory(dir);
				return Path.Combine(dir, this.Name + ".txt");
			}
		}
		
		internal IStack<StatusData> History { get; private set; }

        public IEnumerable<IData> ErrorHistory { get { return this.History; } }

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
