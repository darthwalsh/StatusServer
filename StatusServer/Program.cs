using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;

namespace StatusServer
{
	public class YesStatus : Status
	{
		public override string Name {
			get { return "Yes"; }
		}

		protected override void Verify() {
			// Don't mess this up!
		}
	}

	public class NoStatus : Status
	{
		public override string Name {
			get { return "No"; }
		}

		protected override void Verify() {
			throw new Exception("MyMessage");
		}
	}

	public class FileStatus : Status
	{
		public override string Name {
			get { return "File"; }
		}

		protected override void Verify() {
			if (!File.Exists(@"C:\Users\Carl\Documents\GitHub\temp\out.txt"))
				throw new Exception("out.txt is gone!");
		}
	}

	class Program
	{
		//TODO move to new project
		static void Main(string[] args) {

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
