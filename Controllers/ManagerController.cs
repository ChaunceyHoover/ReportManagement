using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers {
	[Route("api/manager"), Produces("application/json")]
	public class ManagerController : Controller {
		/// <summary>
		/// Generates a list of managers
		/// </summary>
		/// <returns></returns>
		[HttpGet, Authorize]
		public JsonResult GetManagers() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanAddSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view installers" });

				List<Models.Manager> managers = context.Managers.AsNoTracking().Where(m => m.Id > 0).ToList();
				List<Object> report = new List<object>();

				foreach (var manager in managers)
					report.Add(new { manager.Email, manager.FName, manager.Id, manager.LName, manager.Phone, name = manager.FName + " " + manager.LName });

				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>
		/// NOT YET IMPLEMENTED
		/// </summary>
		/// <param name="managerId"></param>
		/// <returns></returns>
		[HttpGet("{managerId}"), Authorize]
		public JsonResult GetManager(int managerId) {

			return Json(new { status_code = -1, status = "NOT YET IMPLEMENTED" });
		}

		/// <summary>
		/// NOT YET IMPLEMENTED
		/// </summary>
		/// <param name="manager"></param>
		/// <returns></returns>
		[HttpPost, Authorize]
		public JsonResult UpdateManager([FromBody]Models.Manager manager) {

			return Json(new { status_code = -1, status = "NOT YET IMPLEMENTED" });
		}

		/// <summary>
		/// Creates a new manager with the given information
		/// </summary>
		/// <param name="manager">The manager object to create</param>
		/// <returns></returns>
		[HttpPut, Authorize]
		public JsonResult CreateManager([FromBody]Models.Manager manager) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanAddSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to create managers" });

				if (manager.FName.Length == 0 && manager.LName.Length == 0)
					return Json(new { status_code = 4, status = "Manager must have at least a first or last name" });
				else if (manager.Phone.Length == 0)
					return Json(new { status_code = 4, status = "Manager must have a phone number" });

				context.Managers.Add(manager);
				context.SaveChanges();

				Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateManager, authUser.Id, manager.Id,
					String.Format("{0} (id: {1}) created manager '{2}' (id: {3})", authUser.UserName, authUser.Id, manager.FName + " " + manager.LName, manager.Id));

				return Json(new { status_code = 0, manager });
			}
		}
	}
}