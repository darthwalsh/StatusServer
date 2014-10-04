StatusServer
============

Display status history in a web site simple .NET web server.

Simple add some status you care about:


```
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
		static void Main(string[] args) {
			Status.Initialize();

			using (NancyHost host = new NancyHost(
				new HostConfiguration { RewriteLocalhost = false },
				new Uri("http://localhost:8080"))) {
				host.Start();

				Console.WriteLine("Press [Enter] to close");
				Console.ReadLine();
			}
		}
	}
```

and you're running a web server!

<table border="1">
  <tr>
    <td><span style="color:red">MyStatus</span></td>
    <td>Could not find file &#39;c:\Example\bin\Debug\This file isn&#39;t here&#39;.</td>
  </tr>
  <tr>
    <td><span style="color:green">GoogleStatus</span></td>
    <td></td>
  </tr>
</table>


