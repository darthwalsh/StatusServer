using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace StatusServer
{
	public abstract class PingStatus : Status
	{
		protected abstract string ServerPath { get; }

		protected sealed override void Verify() {
			using (var ping = new Ping()) {
				var reply = ping.Send(this.ServerPath, 1000);
				if (reply.Status != IPStatus.Success)
					throw new Exception(string.Format("Pinging {0} failed with status: {1}.", reply.Address, reply.Status));
			}
		}
	}
}
