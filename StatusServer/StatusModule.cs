using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
		public string CurrentTime { get; set; }
		public string RefeshSeconds { get; set; }
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
#if DEBUG
		static readonly TimeSpan refreshEvery = TimeSpan.FromSeconds(3);
#else
		static readonly TimeSpan refreshEvery = TimeSpan.FromMinutes(5);
#endif

		public StatusModule() {
			Get["/"] = parameters => {
				var model = new StatiModel {
					CurrentTime = DateTime.Now.ToString(),
					RefeshSeconds = Math.Round(refreshEvery.TotalSeconds).ToString(),
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

				bool minimized = Request.Query.min;

				var status = Status.All[name];

				var model = new HistoriesModel {
					Name = status.Name,
					Histories = (minimized 
						? status.History.Minimize(MessageComparer.Instance)
						: status.History)
						.Select(data => new HistoryModel {
							DateTime = data.DateTime.ToString(),
							Message = WebUtility.HtmlEncode(data.ErrorMessage ?? ""),
							Color = data.ErrorMessage == null ? "green" : "red",
					}).ToList()
				};

				return View["details.html", model];
			};
		}
	}

	class MessageComparer : IEqualityComparer<StatusData>
	{
		public static readonly MessageComparer Instance = new MessageComparer();
		
		MessageComparer() { }

		public bool Equals(StatusData x, StatusData y) {
			return x.ErrorMessage == y.ErrorMessage;
		}

		public int GetHashCode(StatusData obj) {
			return obj.ErrorMessage != null ? obj.ErrorMessage.GetHashCode() : 0;
		}
	}

	public class StatusServerBootstrapper : DefaultNancyBootstrapper
	{
		private byte[] favicon;

		protected override byte[] FavIcon {
			get { return this.favicon ?? (this.favicon = LoadFavIcon()); }
		}

		byte[] LoadFavIcon() {
			using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StatusServer.img.favicon.ico")) {
				var tempFavicon = new byte[resourceStream.Length];
				resourceStream.Read(tempFavicon, 0, (int)resourceStream.Length);
				return tempFavicon;
			}
		}
	}
}
