using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

	public class HistoryModel
	{
		public string DateTime { get; set; }
		public string Message { get; set; }
		public string Color { get; set; }
	}

	public class HistoriesModel
	{
		public string Name { get; set; }
		public List<HistoryModel> Histories { get; set; }
	}

	public class StatusModule : NancyModule
	{
		public StatusModule() {
			Get["/"] = parameters => {
				var model = new StatiModel {
					Stati = Status.All.Values.Select(s => {
						var data = s.History.First();
						return new StatusModel {
							Name = s.Name,
							Message = WebUtility.HtmlEncode(data.ErrorMessage ?? ""),
							Color = data.ErrorMessage == null ? "green" : "red",
						};
					}).ToList()
				};

				return View["index.html", model];
			};

			Get["/details/"] = parameters => {
				string name = Request.Query.name;

				var status = Status.All[name];

				var model = new HistoriesModel {
					Name = status.Name,
					Histories = status.History.Select(data => new HistoryModel {
						DateTime = data.DateTime.ToString(),
						Message = WebUtility.HtmlEncode(data.ErrorMessage ?? ""),
						Color = data.ErrorMessage == null ? "green" : "red",
					}).ToList()
				};

				return View["details.html", model];
			};
		}
	}
}
