using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers {
	[Route("api/users"), Produces("application/json")]
	public class UsersController : Controller {
		/// <summary>Returns an object with all user info that isn't sensitive</summary>
		/// <param name="user">The user to de-sensitize</param>
		/// <returns>A user object with no sensitive data</returns>
		private Object GetUserObject(Models.User user) {
			return new {
				user.Active,
				user.Email,
				user.FName,
				user.Id,
				user.IsDistributor,
				user.Level,
				user.LName,
				user.Notes,
				user.Phone,
				user.Percentage,
				user.UserLastLogin,
				user.UserName,
				name = user.FName + " " + user.LName
			};
		}

		/// <summary>
		/// Generates a list of all users who are a distributor and returns it
		/// </summary>
		/// <returns>list of all users who are a distributor</returns>
		private JsonResult GetDistributors() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Object> report = new List<Object>();
				foreach (Models.User user in context.Users.AsNoTracking().Where(u => (u.IsDistributor == true || u.Level == 3) && u.Active == true).ToList())
					report.Add(GetUserObject(user));
				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>
		/// Generates a list of all users and returns it
		/// </summary>
		/// <returns>Json list of all users</returns>
		private JsonResult GetUsers() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Object> users = new List<Object>();
				foreach (Models.User user in context.Users.AsNoTracking().ToList()) {
					users.Add(GetUserObject(user));
				}
				return Json(new { status_code = 0, users });
			}
		}

		/// <summary>
		/// Generates a list of all owners
		/// </summary>
		/// <returns>A list of all users who are owners</returns>
		private JsonResult GetOwners() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Models.User> owners = context.Users.AsNoTracking().Where(u => u.Active == true && u.Level == 2).ToList();
				List<Object> report = new List<object>();
				foreach (Models.User owner in owners) {
					report.Add(GetUserObject(owner));
				}
				return Json(new { stauts_code = 0, report });
			}
		}

		/// <summary>
		/// Returns all info on a given user
		/// </summary>
		/// <remarks>Requires level 2 or 10 permission</remarks>
		/// <param name="userId">ID of user to access</param>
		/// <returns>Json user</returns>
		[HttpGet("{userId}"), Authorize]
		public JsonResult GetUser(int userId) {
			if (userId == 0) return GetRoles();
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				Models.User dbUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
				if (dbUser == null) {
					return Json(new { status_code = 2, status = "User '" + userId + "' does not exist" });
				}

				return Json(new { status_code = 0, user = GetUserObject(dbUser) });
			}
		}

		/// <summary>
		/// Returns a list of all levels that a user can be assigned
		/// </summary>
		/// <returns>a list of all levels that a user can be assigned</returns>
		private JsonResult GetRoles() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				// possible update: Read XML file for all ranks
				//	* Would allow only certain ranks to view/know existance of all ranks
				//		ex: if distributor were changing role of cashier to owner, ranks 4-10 would come back as "RESERVED" instead of actual rank and be disabled from selection menu
				//		ex2: If Tech were to try to change user's role, Tech Trainee would come back as "RESERVED" and be disabled from selection menu
				List<Object> report = new List<Object>();
				report.Add(new { value = 1, label = "Cashier" });
				report.Add(new { value = 2, label = "Owner" });
				report.Add(new { value = 3, label = "Distributor" });
				report.Add(new { value = 4, label = "Tech Trainee" });
				report.Add(new { value = 5, label = "Tech" });
				report.Add(new { value = 6, label = "RESERVED" });
				report.Add(new { value = 7, label = "Billing Administrator" });
				report.Add(new { value = 8, label = "RESERVED" });
				report.Add(new { value = 9, label = "TPS Administrator" });
				report.Add(new { value = 10, label = "Master" });

				return Json(new { status_code = 0, report });
			}
		}
		
		/// <summary>
		/// Gets all information from user table, excluding password, pertaining to authorized user
		/// </summary>
		/// <returns>Authorized user's information</returns>
		private JsonResult GetUserInfo() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				return GetUser(authUser.Id);
			}
		}

		/// <summary>
		/// Generates a list of all users with permission level > 4
		/// </summary>
		/// <returns>A list of users who have permission level > 4</returns>
		private JsonResult GetHighLevelUsers() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Object> users = new List<Object>();
				foreach (Models.User user in context.Users.AsNoTracking().Where(u => u.Level >= 4).ToList()) {
					users.Add(GetUserObject(user));
				}
				return Json(new { status_code = 0, users });
			}
		}

		/// <summary>
		/// Filters HTTP GETs based off the status
		/// 
		/// 0 = Get distributor list (requires distributor auth)
		/// 1 = Get user list (requires level 2 or 10 auth)
		/// 2 = Get authorized user info
		/// 3 = Get owner list
		/// </summary>
		/// <param name="status">The type of information to be generated</param>
		/// <returns>
		/// Information return is dependent on status code given.
		/// 
		/// 0 = distributor list
		/// 1 = user list
		/// 2 = authorized user info
		/// 3 = owner list
		/// </returns>
		[HttpGet, Authorize]
		public JsonResult Get([RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				switch (status) {
					case 0: // distrib list
						return GetDistributors();
					case 1: // user list
						return GetUsers();
					case 2: // authorized user info
						return GetUserInfo();
					case 3: // owner list
						return GetOwners();
					case 4: // users with permission level > 4
						return GetHighLevelUsers();
					default:
						return Json(new { status_code = 6, status = "Unknown status code '" + status + "' for GET /api/users" });
				}
			}
		}

		/// <summary>
		/// Creates or deletes a user, based off given status
		/// </summary>
		/// <remarks>
		/// 0 = Create
		/// 1 = Delete
		/// 
		/// PUT /api/users?status=[0|1]
		/// Headers:
		///		Authorization: Bearer [auth token]
		///	Body:
		///		Models.User
		/// </remarks>
		/// <param name="user">The JSON value that contains all the Model.User info</param>
		/// <returns>The Json status_code of the creation (0 = success, anything else = error code + error msg)</returns>
		[HttpPut, Authorize]
		public JsonResult Create([FromBody]Models.User user, [RequiredFromQuery]int status) {
			if (status == 0) { 
				using (var context = new Data.ApplicationDbContext()) {
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
					if (authUser == null) {
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
					}

					if (!Helpers.PermissionChecker.CanAddUser(authUser)) {
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to create new user" });
					}

					if (context.Users.AsNoTracking().FirstOrDefault(u => user.UserName != null && u.UserName != null && user.UserName.ToLower().Equals(u.UserName.ToLower())) != null) {
						return Json(new { status_code = 3, status = "User '" + user.UserName + "' already exists" });
					}

					if (String.IsNullOrWhiteSpace(user.UserName) || String.IsNullOrWhiteSpace(user.Password))
						return Json(new { status_code = 4, status = "Invalid user creation body" });

					user.Active = true;
					user.UserLastLogin = new DateTime(2000, 1, 1);
					context.Users.Add(user);
					context.SaveChanges();

					Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateUser, authUser.Id, user.Id,
						String.Format("{0} (id: {1}) created user {2} (id: {3})", authUser.UserName, authUser.Id, user.UserName, user.Id));

					return Json(new { status_code = 0, user.Id });
				}
			} else if (status == 1) {
				using (var context = new Data.ApplicationDbContext()) {
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
					if (authUser == null) {
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
					}
					if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser)) {
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to delete user" });
					}

					Models.User deleteUser = context.Users.FirstOrDefault(u => u.Id == user.Id);
					if (deleteUser == null) {
						return Json(new { status_code = 2, status = "User '" + user.Id + "' does not exist" });
					}

					context.Users.Remove(deleteUser);
					context.SaveChanges();

					Helpers.LogHelper.LogAction(Models.Log.ActionType.DeleteUser, authUser.Id, deleteUser.Id,
						String.Format("{0} (id: {1}) deleted user {2} (id: {3})", authUser.UserName, authUser.Id, deleteUser.UserName, deleteUser.Id));
				}
				return Json(new { status_code = 0 });
			} else {
				return Json(new { status_code = 4, status = "Invalid PUT status code '" + status + "'" });
			}
		}

		// Note to self: Possibly in future, make separate HttpPost for updating password specifically
		/// <summary>
		/// Updates the given user's information
		/// </summary>
		/// <param name="newUserInfo">The new information to update</param>
		/// <returns>Json status_code of the update (0 = success, anything else = error code + error msg)</returns>
		[HttpPost]
		public JsonResult Update([FromBody]Models.User data, [RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString() == authUserId);
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				if (!Helpers.PermissionChecker.CanModifyUser(authUser) && authUser.Id != data.Id) {
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to edit user" });
				}

				Models.User dbUser = context.Users.FirstOrDefault(u => u.Id == data.Id);
				if (dbUser == null) {
					return Json(new { status_code = 2, status = "User '" + data.Id + "' does not exist" });
				}

				// Changes made to user object to be logged
				String changes = "";

				// Id cannot be changed and LastLogin is handled elsewhere
				if (status == 0) {
					// Compares given user (data) to row stored in database (dbUser)
					List<Models.Log.Variance> variances = dbUser.Compare(data, false);
					if (variances.Count == 0)
						return Json(new { status_code = 0, status = "No changes made (given object same as database row)" });

					dbUser.Copy(data, false);

					// Logs each change in format: [(KEY=VALUE)][(KEY=VALUE)]
					// note: this format was chosen so values may contain commas or other symbols otherwise used to separate lists
					foreach (Models.Log.Variance var in variances) {
						changes += "[(" + var.Property + "=" + (var.New != null ? var.New.ToString() : "null") + ")]";
					}
				} else if (status == 1) {
					dbUser.Password = data.Password;
					changes = "[(Password)]";
				} else {
					return Json(new { status_code = 4, status = "Unknown status code '" + status + "' when updating user info" });
				}

				context.SaveChanges();

				String description = "";
				if (authUser.Id == dbUser.Id)
					description = String.Format("{0} (id: {1}) changed his/her info", authUser.UserName, authUser.Id);
				else
					description = String.Format("{0} (id: {1}) changed {2}'s (id: {3}) info", authUser.UserName, authUser.Id, dbUser.UserName, dbUser.Id);

				Helpers.LogHelper.LogAction(Models.Log.ActionType.ModifyUser, authUser.Id, dbUser.Id, description, changes);

				return Json(new { status_code = 0 });
			}
		}
	}
}