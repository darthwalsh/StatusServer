﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using StatusServer;

namespace Example
{
	public class MyStatus : Status
	{
		protected override void Verify() {
			File.ReadAllText("This file isn't here...");
		}
	}

	public class GoogleStatus : HttpStatus
	{
		protected override Uri Uri {
			get { return new Uri("http://google.com"); }
		}
	}

	class Program
	{
        private const string url = "http://localhost:8080";

        static void Main(string[] args) {
            Status.Cleanup(TimeSpan.FromDays(100));

			Status.Initialize();

			using (NancyHost host = new NancyHost(
				new HostConfiguration { RewriteLocalhost = false },
				new Uri(url))) {
				host.Start();

				Console.WriteLine("Press [Enter] to close");

                Process.Start(url);
				Console.ReadLine();

                Console.WriteLine("Shutting down gracefully...");
                Status.ShutDown();
			}
		}
	}
}
