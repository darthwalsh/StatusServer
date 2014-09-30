using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusServer
{
	public abstract class HttpStatus : Status
	{
		protected abstract Uri Uri { get; }

		protected sealed override void Verify() {
			var request = WebRequest.Create(this.Uri);
			using (request.GetResponse()) { }
		}
	}
}
