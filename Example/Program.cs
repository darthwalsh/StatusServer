using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using StatusServer;

namespace Example
{
	//TODO clean up
	public class YesStatus : Status
	{
		public YesStatus()
			: base(TimeSpan.FromSeconds(10)) {
		}

		protected override void Verify() {
			// Don't mess this up!
		}
	}

	public class NoStatus : Status
	{
		public NoStatus()
			: base() {
		}

		protected override void Verify() {
			throw new ArgumentOutOfRangeException("<b>MyMessage<\\b>");
		}
	}

	public abstract class FileStatus : Status
	{
		public FileStatus()
			: base(TimeSpan.FromSeconds(1)) {
		}

		protected abstract string FilePath { get; }

		protected override void Verify() {
			if (!File.Exists(this.FilePath))
				throw new ArgumentOutOfRangeException("the file is gone!");
		}
	}

	public class OutFile : FileStatus
	{
		protected override string FilePath { get { return @"C:\Users\Carl\Documents\GitHub\temp\out.txt"; } }
	}

	public class Whatever
	{
		public class Inner : FileStatus
		{
			protected override string FilePath { get { return @"C:\Users\Carl\Documents\GitHub\StatusServer\.gitignore"; } }
		}
	}

	public class Google : HttpStatus
	{
		protected override Uri Uri {
			get { return new Uri("http://google.com"); }
		}
	}

	public class FakeSite : HttpStatus
	{
		protected override Uri Uri {
			get { return new Uri("http://fakesitefdsafdsafdsf.com"); }
		}
	}

	public class LocalPing : PingStatus
	{
		protected override string ServerPath {
			get { return "localhost"; }
		}
	}

	public class FakePing : PingStatus
	{
		protected override string ServerPath {
			get { return "192.168.23.12"; }
		}
	}

	class Program
	{
		static void Main(string[] args) {

			Status.Initialize();

			NancyHost host;
			if (args.Length == 0) {
				host = new NancyHost(new HostConfiguration { RewriteLocalhost = false }, new Uri("http://localhost:8080"));
				Console.WriteLine("Debugging: just listening to localhost on 8080.");
			} else if (args.Length == 1) {
				host = new NancyHost(new UriBuilder("http://localhost") { Port = Convert.ToInt32(args[0]) }.Uri);
				Console.WriteLine("Server is listening to all Url's on port {0}.", args[0]);
			} else {
				Console.WriteLine("Usage: {0} [port]", Environment.GetCommandLineArgs()[0]);
				Environment.Exit(1);
				throw null;
			}

			using (host) {
				host.Start();

				Console.WriteLine("Press [Enter] to close");
				Console.ReadLine();
			}
		}
	}
}
