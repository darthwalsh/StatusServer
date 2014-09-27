using System.Collections.Generic;
using System.Linq;
using Nancy;

namespace StatusServer
{
	public class StatusModel
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public string Color { get; set; }
	}

	public class StatiModel
	{
		public List<StatusModel> Stati { get; set; }
	}

	public class StatusModule : NancyModule
	{
		public StatusModule() {
			Get["/"] = parameters => {

				var model = new StatiModel {
					Stati = Status.All.Select(s => new StatusModel { 
						Name = s.Name,
						Message = s.Fault ?? "works",
						Color = s.Fault == null ? "green" : "red",
					}).ToList()
				};

				return View["index.html", model];
			};
		}
	}
}
