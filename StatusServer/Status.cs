using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StatusServer
{
	class StatusData
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
	}

	public abstract class Status
	{
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
		}

		protected Status(string name)
			: this(name, TimeSpan.FromMinutes(5)) {
		}

		protected Status(string name, TimeSpan delay) {
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



			Log(new StatusData("Initializing... (server just started)"));

			new Thread(() => {
				while (true) {
					Thread.Sleep(delay);

					StatusData data;
					try {
						Verify();
						data = new StatusData();
					} catch (Exception e) {
						data = new StatusData(e.Message);
					}

					Log(data);
				}
			}) {
				IsBackground = true
			}.Start();
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
}
