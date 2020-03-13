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

namespace ReportPortal.Controllers {
	[Route("api/systems"), Produces("application/json")]
	public class SystemsController : Controller {
		/// <summary>
		/// Lists all systems for the current reporting tool
		/// </summary>
		/// <returns>A list of all systems</returns>
		[HttpGet, Authorize]
		public JsonResult ListSystems() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				// Filter systems based off user permission
				if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser)) {
#if LOCAL
					using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
					using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
						conn.Open();
						List<object> report = new List<object>();
						MySqlCommand cmd = new MySqlCommand(String.Format(
							"SELECT DISTINCT sys.system_id, sys.system_name, sys.system_prefix FROM `permission` as p " +
							"JOIN " +
							"	(SELECT site_id, system_id, site_active FROM `sites`) as s " +
							"JOIN " +
							"	(SELECT system_id, system_name, system_prefix FROM `systems`) as sys " +
							"ON " +
							"	p.site_id = s.site_id AND p.user_id = {0} AND s.system_id = sys.system_id AND s.site_active = 1;", authUser.Id), conn);
						using (var reader = cmd.ExecuteReader()) {
							while (reader.Read()) {
								var systemId = DBNull.Value.Equals(reader["system_id"]) ? -1 : Convert.ToInt32(reader["system_id"]);
								var systemName = DBNull.Value.Equals(reader["system_name"]) ? "" : Convert.ToString(reader["system_name"]);
								var systemPrefix = DBNull.Value.Equals(reader["system_prefix"]) ? "" : Convert.ToString(reader["system_prefix"]);
								if (systemId != -1 && systemName.Length > 0)
									report.Add(new {
										Id = systemId,
										Name = systemName,
										Prefix = systemPrefix
									});
							}
						}
						return Json(new { status_code = 0, report });
					}
				}
				return Json(new { status_code = 0, report = context.Systems.AsNoTracking().ToList() });
			}
		}

		[HttpGet("{id}"), Authorize]
		public JsonResult GetSystem(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				
				Models.System system = context.Systems.AsNoTracking().FirstOrDefault(sys => sys.Id == id);
				if (system == null)
					return Json(new { status_code = 2, status = "System with ID '" + id + "' does not exist" });

				return Json(new { status_code = 0, system });
			}
		}
	}
}
