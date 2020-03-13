using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers {
	[Route("api/permission"), Produces("application/json")]
	public class PermissionsController : Controller {
		private String GetSupportNumber(String host) {
			host = host.ToLower();
			if (host.Equals("report.strykersweeps.com"))
				return "8669443989";
			else if (host.Equals("report.typhoonsweeps.com"))
				return "8558642630";
			else if (host.Equals("report.titansweepstakes.com"))
				return "8446284826";
			else if (host.Equals("report.phantomnow.com"))
				return "8552207511";
			else if (host.Equals("report.cardinalsweeps.com"))
				return "8886001982";
			else if (host.Equals("report.scarlett.la"))
				return "8779691882";
			else if (host.Equals("report.classicsweeps.com"))
				return "8554102730";
			return "";
		}

		/// <summary>Sets a user's permission to view a site</summary>
		/// <param name="userId">The user to give/take permission from</param>
		/// <param name="siteId">The site to give/take permission from</param>
		/// <param name="hasAccess">Sets whether user has access to site or not</param>
		/// <returns>Json object with <code>status_code</code> of event (0 = success)</returns>
		private JsonResult SetPermission(int userId, int siteId, int? access) {
			using (var context = new Data.ApplicationDbContext()) {
				Models.User user = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
				if (user == null)
					return Json(new { status_code = 2, status = "Cannot create permission for user '" + userId + "'; user doesn't exist" });
				
				Models.Permission permission = context.Permissions.FirstOrDefault(p => p.UserId == userId && p.SiteId == siteId);
				if (permission != null && (access.HasValue && access.Value == 0)) {
					context.Permissions.Remove(permission);
				} else if (permission == null && (access.HasValue && access.Value == 1)) {
					permission = new Models.Permission {
						UserId = userId,
						SiteId = siteId,
						Access = 1,
					};

					context.Permissions.Add(permission);
				}
				
				context.SaveChanges();
				return Json(new { status_code = 0, permission });
			}
		}

		/// <summary>Generates a list of site ids that user has permission to view</summary>
		/// <param name="userId">User to generate list based off of</param>
		/// <returns>List of site ids that user has permission to view</returns>
		private JsonResult GetAccessibleSites(int userId) {
			using (var context = new Data.ApplicationDbContext()) {
				List<int> report = new List<int>();
				foreach (Models.Permission permission in context.Permissions.AsNoTracking().Where(p => p.UserId == userId).ToList()) {
					if (permission.Access != null && (int)permission.Access == 1)
						report.Add(permission.SiteId);
				}

				return Json(new { status_code = 0, report });
			}
		}

		// Map HTTP GET to functions. Any permission checks (ex: IsDistributor or User.Level) would be checked here
		[HttpGet("{userId}"), Authorize]
		public JsonResult Get(int userId) => GetAccessibleSites(userId);

		/* Notes about permission:
		 * Lvl 2 & 10: Can set any permission
		 * IsDistributor: Can only set permissions relating to their site
		 */
		[HttpPost, Authorize]
		public JsonResult Update([RequiredFromQuery]int userId, [RequiredFromQuery]int siteId, [FromQuery]int access) => SetPermission(userId, siteId, access);

		/// <summary>
		/// Reads all permissions and links from appsettings.json file and returns them in JSON format
		/// </summary>
		/// <returns>All information needed from appsettings.json</returns>
		[HttpGet("links"), Authorize]
		public JsonResult GetLinks() {
			var settingsList = Startup.Configuration.GetSection("Links");
			var addUserPermission = Startup.Configuration.GetSection("Permissions:AddUser");
			var modifyPermission = Startup.Configuration.GetSection("Permissions:ModifyUser");
			var addSitePermission = Startup.Configuration.GetSection("Permissions:AddSite");
			var modifySitePermission = Startup.Configuration.GetSection("Permissions:ModifySite");
			var deleteSitePermission = Startup.Configuration.GetSection("Permissions:DeleteSite");
			var adjustmentPermission = Startup.Configuration.GetSection("Permissions:Adjustments");

			List<Object> links = new List<Object>();
			foreach (var section in settingsList.GetChildren()) {
				var levels = section.GetSection("Level");
				links.Add(new { label = section.GetValue<String>("Label"), level = levels.Get<int[]>(), url = section.GetValue<String>("URL") });
			}
			return Json(new { status_code = 0, links, addUser = addUserPermission.Get<int[]>(), modifyUser = modifyPermission.Get<int[]>(),
				addSite = addSitePermission.Get<int[]>(), modifySite = modifySitePermission.Get<int[]>(), deleteSite = deleteSitePermission.Get<int[]>(),
				adjustments = adjustmentPermission.Get<int[]>(), supportNumber = GetSupportNumber(HttpContext.Request.Host.Host) });
		}

		/// <summary>
		/// Reads theme from appsettings.json and returns color
		/// </summary>
		/// <returns>Color theme for current report tool</returns>
		[HttpGet("theme")]
		public JsonResult GetTheme() {
			var theme = Startup.Configuration.GetSection("Theme");

			return Json(new { theme = theme["Color"] });
		}
	}
}