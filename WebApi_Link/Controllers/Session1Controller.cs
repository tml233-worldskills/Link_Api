using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace WebApi_Link.Controllers {
	public class Session1Controller : ApiController {
		[HttpGet]
		public dynamic GetGrades() {
			try {
				var data = Utils.ExecuteQuery("SELECT Id,Grade FROM Grades");
				return new { result = "Successful", data = data };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}

		[HttpGet]
		public dynamic GetLinks([FromUri] string name = null) {
			if (string.IsNullOrWhiteSpace(name)) {
				name = null;
			}

			try {
				const string sql = "SELECT Id FROM Links";
				DataTable dt;
				if (name == null) {
					dt = Utils.ExecuteQuery(sql);
				} else {
					dt = Utils.ExecuteQuery(sql + " WHERE SiteName LIKE @0", string.Format("%{0}%", name));
				}
				List<int> ids = new List<int>();
				foreach (DataRow row in dt.Rows) {
					ids.Add((int)row["Id"]);
				}
				return new { result = "Successful", data = ids };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}

		[HttpGet]
		public dynamic GetLink(int id, [FromUri(Name = "limited-photo")] bool limitedPhoto = false) {
			try {
				var dt = Utils.ExecuteQuery("SELECT Links.Id as Id,SiteName,Url,GradeId,Grade as GradeName,Beizhu,Time FROM Links,Grades WHERE GradeId=Grades.Id AND Links.Id=@0", id);
				if (dt.Rows.Count == 0) {
					return new { result = "Error", reason = "No such link." };
				}
				var row = dt.Rows[0];

				var photos = new JArray();
				var data = new JObject {
					{"id",(int)row["Id"] },
					{"siteName",row["SiteName"] as string },
					{"url",row["Url"] as string },
					{"gradeId",(int)row["GradeId"]},
					{"gradeName",row["GradeName"] as string },
					{"beizhu",row["Beizhu"] as string },
					{"time",((DateTime)row["Time"]).ToString("yyyy-MM-dd") },
					{"photos",photos }
				};

				foreach (var r in Utils.ExecuteReader("SELECT LinkPhoto FROM LinkPhotos WHERE LinkId=@0", id)) {
					photos.Add(Convert.ToBase64String(r["LinkPhoto"] as byte[]));
					if (limitedPhoto) {
						break;
					}
				}

				return new { result = "Successful", data = data };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}

		[HttpPost]
		public dynamic AddLink() {
			try {
				string body = Request.Content.ReadAsStringAsync().Result;

				JObject bodyObj = JObject.Parse(body);
				Utils.ExecuteUpdate(
					"INSERT INTO Links(SiteName,Url,GradeId,Beizhu,Time) VALUES(@0,@1,@2,@3,@4)",
					bodyObj["siteName"].ToObject<string>(),
					bodyObj["url"].ToObject<string>(),
					bodyObj["gradeId"].ToObject<int>(),
					bodyObj["beizhu"].ToObject<string>(),
					bodyObj["exp"].ToObject<DateTime>()
				);

				int maxId = (int)Utils.ExecuteScalar("SELECT MAX(Id) as MaxId FROM Links");

				var photos = bodyObj["photos"].ToObject<JArray>();
				foreach (string item in photos) {
					var photo = Convert.FromBase64String(item);
					Utils.ExecuteUpdate("INSERT INTO LinkPhotos(LinkId,LinkPhoto) VALUES(@0,@1)", maxId, photo);
				}
				return new { result = "Successful" };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}

		[HttpPost]
		public dynamic EditLink(int id) {
			try {
				string body = Request.Content.ReadAsStringAsync().Result;
				JObject bodyObj = JObject.Parse(body);
				string siteName = bodyObj["siteName"].ToObject<string>();
				string url = bodyObj["url"].ToObject<string>();
				int gradeId = bodyObj["gradeId"].ToObject<int>();
				string beizhu = bodyObj["beizhu"].ToObject<string>();
				DateTime exp = bodyObj["exp"].ToObject<DateTime>();
				var photos = bodyObj["photos"].ToObject<JArray>();

				bool exists = (int)Utils.ExecuteScalar("SELECT COUNT(Id) FROM Links WHERE Id=@0", id) > 0;
				if (!exists) {
					return new { result = "Error", reason = "No such link." };
				}

				Utils.ExecuteUpdate("DELETE FROM LinkPhotos WHERE LinkId=@0", id);

				Utils.ExecuteUpdate("UPDATE Links SET SiteName=@1, Url=@2, GradeId=@3, Beizhu=@4, Time=@5 WHERE Id=@0",
					id, siteName, url, gradeId, beizhu, exp);

				foreach (string item in photos) {
					var photo = Convert.FromBase64String(item);
					Utils.ExecuteUpdate("INSERT INTO LinkPhotos(LinkId,LinkPhoto) VALUES(@0,@1)", id, photo);
				}
				return new { result = "Successful" };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}
	}
}