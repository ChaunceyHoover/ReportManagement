using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers {
	/// <summary>
	/// The controller for handling all information about installers
	/// </summary>
	/// <remarks>All permissions for this controller fall under 'CanAddSite' since that's the only page that uses it</remarks>
	[Route("api/installer"), Produces("application/json")]
	public class InstallerController : Controller {
		/// <summary>Generates a list of all game room installers</summary>
		/// <returns>A list of all game room installers</returns>
		[HttpGet, Authorize]
		public JsonResult GetInstallers() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanAddSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view installers" });

				List<Models.Installer> installers = context.Installers.AsNoTracking().Where(i => i.Id > 0).ToList();
				List<Object> report = new List<object>();
				foreach (var installer in installers)
					report.Add(new { installer.Email, installer.FName, installer.Id, installer.LName, installer.Phone, name = installer.FName + " " + installer.LName });

				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>Gets a specific installer's information</summary>
		/// <param name="installerId">the specified installer's unique ID</param>
		/// <returns>The specified installer's information</returns>
		[HttpGet("{installerId}"), Authorize]
		public JsonResult GetInstaller(int installerId) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanAddSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view installers" });

				Models.Installer installer = context.Installers.AsNoTracking().FirstOrDefault(i => i.Id == installerId);
				if (installer == null)
					return Json(new { status_code = 3, status = "Installer '" + installerId + "' does not exist" });

				return Json(new { status_code = 0, installer });
			}
		}

		/// <summary>Updates a given installer's information</summary>
		/// <param name="installer">The installer's new information, with the `Id` variable set to the specified installer's ID</param>
		/// <returns>status_code = 0 if successful, otherwise see error-code.txt for errors</returns>
		[HttpPost, Authorize]
		public JsonResult UpdateInstaller([FromBody]Models.Installer installer) {

			return Json(new { status_code = -1, status = "Not yet implemented" });
		}

		/// <summary>Creates a new installer from the given information</summary>
		/// <param name="installer">The new installer's information</param>
		/// <returns>status_code = 0 if successful, otherwise see error-code.txt for errors</returns>
		[HttpPut, Authorize]
		public JsonResult CreateInstaller([FromBody]Models.Installer installer) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanAddSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to create installers" });

				if (installer.FName.Length == 0 && installer.LName.Length == 0)
					return Json(new { status_code = 4, status = "Installer must have at least a first or last name" });
				else if (installer.Phone.Length == 0)
					return Json(new { status_code = 4, status = "Installer must have a phone number" });

				context.Installers.Add(installer);
				context.SaveChanges();

				Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateInstaller, authUser.Id, installer.Id,
					String.Format("{0} (id: {1}) created installer '{2}' (id: {3})", authUser.UserName, authUser.Id, installer.FName + " " + installer.LName, installer.Id));

				return Json(new { status_code = 0, installer });
			}
		}
	}
}