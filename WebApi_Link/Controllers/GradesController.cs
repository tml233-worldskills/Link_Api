using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebApi_Link.Controllers {
	public class GradesController : ApiController {
		// GET api/<controller>
		public dynamic Get() {
			try {
				var data = Utils.ExecuteQuery("SELECT Id,Grade FROM Grades");
				return new { result = "Successful", data = data };
			} catch (Exception e) {
				return new { result = "Error", reason = e.Message };
			}
		}
	}
}