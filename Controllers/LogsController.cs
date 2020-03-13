using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers
{
	[Route("api/logs"), Produces("application/json")]
	public class LogsController : Controller
    {
		[HttpGet, Authorize]
		public JsonResult Get([RequiredFromQuery]DateTime start, [RequiredFromQuery]DateTime end) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));

				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				// A list of unique users who are in the logs (ex: 'charles' has 5 logs and 'elliot' has 2 logs, this list will consist of 1 index of 'Charles' and 1 index of 'Elliot' only)
				List<Object> users = new List<Object>();

				// A list of all the actions that can be performed
				List<Object> actions = new List<Object>();

				// All logs (filtering done client side)
				List<Models.Log> logs = context.Logs.AsNoTracking().Where(l => l.LogId > 0 && l.LogTime >= start && l.LogTime <= end).OrderByDescending(l => l.LogId).ToList();

				// List all actions and their Enum value
				foreach (Models.Log.ActionType value in Enum.GetValues(typeof(Models.Log.ActionType))) {
					foreach (var log in logs) {
						if (log.Action == value) {
							actions.Add(new {
								Id = (int)value,
								Label = value.ToString()
							});
							break;
						}
					}
				}

#if LOCAL
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
					conn.Open();

					// Select all unique users from logs (ex: if there's 10 logs all be same user, only returns 1 user instead of 10 users)
					MySqlCommand cmd = new MySqlCommand("SELECT DISTINCT u.user_name, " +
														"				u.user_id " +
														"FROM `user` AS u " +
														"	RIGHT JOIN `logs` AS l " +
														"		ON u.user_id = l.user_id; ", conn);
					using (var reader = cmd.ExecuteReader()) {
						while (reader.Read()) {
							var user = new {
								UserName = DBNull.Value.Equals(reader["user_name"]) ? "" : Convert.ToString(reader["user_name"]),
								Id = DBNull.Value.Equals(reader["user_id"]) ? -1 : Convert.ToInt32(reader["user_id"])
							};

							// If log is stored incorrectly, log is ignored (probably manually made log [for some stupid reason])
							if (user.Id > 0 && user.UserName.Length > 0)
								users.Add(user);
						}
					}
				}

				return Json(new { status_code = 0, users, logs, actions });
			}
		}
    }

}