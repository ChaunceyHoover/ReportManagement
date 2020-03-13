using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportPortal.Controllers.Helpers
{
	/// <summary>
	/// A quick helper to check if a given user (whether it be user's level or the user model) has permission to do a given action.
	/// 
	/// Because permissions are array based now instead of integer based (ie levels [1, 5, 9] have permission instead of anyone with >5 level),
	/// this helper will reduce code redundancies for permission checks.
	/// </summary>
    public class PermissionChecker
    {
		/// <summary>
		/// Checks if the given user has permission to create other users
		/// </summary>
		/// <param name="userLevel">The given level of permission to check against</param>
		/// <returns>`true` if user can create users, otherwise `false`</returns>
		public static bool CanAddUser(int userLevel) {
			var addUserPermission = Startup.Configuration.GetSection("Permissions:AddUser");
			if (addUserPermission == null || userLevel == 10)
				return true;
			foreach (int level in addUserPermission.Get<int[]>())
				if (level == userLevel)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permission to create other users
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if user can create users, otherwise `false`</returns>
		public static bool CanAddUser(Models.User user) {
			return CanAddUser(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user has permission to modify other users' information
		/// </summary>
		/// <param name="userLevel">The given level of permission to check against</param>
		/// <returns>`true` if user can modify other users, otherwise `false`</returns>
		public static bool CanModifyUser(int userLevel) {
			var modifyPermission = Startup.Configuration.GetSection("Permissions:ModifyUser");
			if (modifyPermission == null || userLevel == 10)
				return true;
			foreach (int level in modifyPermission.Get<int[]>())
				if (userLevel == level)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permission to modify other users' information
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if user can modify other users, otherwise `false`</returns>
		public static bool CanModifyUser(Models.User user) {
			return CanModifyUser(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user has permission to create/activate sites
		/// </summary>
		/// <param name="userLevel">The given level of permission to check against</param>
		/// <returns>`true` if user can create/activate sites, otherwise `false`</returns>
		public static bool CanAddSite(int userLevel) {
			var addSitePermission = Startup.Configuration.GetSection("Permissions:AddSite");
			if (addSitePermission == null || userLevel == 10)
				return true;
			foreach (int level in addSitePermission.Get<int[]>())
				if (level == userLevel)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permission to create/activate sites
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if user can create/activate sites, otherwise `false`</returns>
		public static bool CanAddSite(Models.User user) {
			return CanAddSite(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user has permission to modify sites' information
		/// </summary>
		/// <param name="userLevel">The given level of permission to check against</param>
		/// <returns>`true` if user can modify sites' info, otherwise `false`</returns>
		public static bool CanModifySite(int userLevel) {
			var modifySitePermission = Startup.Configuration.GetSection("Permissions:ModifySite");
			if (modifySitePermission == null || userLevel == 10)
				return true;
			foreach (int level in modifySitePermission.Get<int[]>())
				if (level == userLevel)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permission to modify sites' information
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if user can modify sites' info, otherwise `false`</returns>
		public static bool CanModifySite(Models.User user) {
			return CanModifySite(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user has permission to delete a site
		/// </summary>
		/// <param name="userLevel">The given level of permission to check against</param>
		/// <returns>`true` if the user has proper level to delete sites, otherwise `false`</returns>
		public static bool CanDeleteSite(int userLevel) {
			var deleteSitePermission = Startup.Configuration.GetSection("Permissions:DeleteSite");
			if (deleteSitePermission == null || userLevel == 10)
				return true;
			foreach (int level in deleteSitePermission.Get<int[]>())
				if (level == userLevel)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permisison to delete a site
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if the given user can delete sites, otherwise `false`</returns>
		public static bool CanDeleteSite(Models.User user) {
			return CanDeleteSite(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user level has permission to do adjustments
		/// </summary>
		/// <param name="userLevel">The user level to check permission for</param>
		/// <returns>`true` if user level can do any adjustment related task, otherwise `false`</returns>
		public static bool CanDoAdjustments(int userLevel) {
			var adjustmentPermissions = Startup.Configuration.GetSection("Permissions:Adjustments");
			if (adjustmentPermissions == null || userLevel == 10)
				return true;
			foreach (int level in adjustmentPermissions.Get<int[]>())
				if (level == userLevel)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if the given user has permission to do adjustments
		/// </summary>
		/// <param name="user">The user to check permission for</param>
		/// <returns>`true` if user can do any adjustment related task, otherwise `false`</returns>
		public static bool CanDoAdjustments(Models.User user) {
			return CanDoAdjustments(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the given user level is either a technician or an administrator
		/// </summary>
		/// <param name="userLevel">The user level to check</param>
		/// <returns>`true` if user is a technician or administrator, otherwise `false`</returns>
		public static bool IsTechOrAdmin(int userLevel) {
			return userLevel == 4 || userLevel == 5 || userLevel == 9 || userLevel == 10;
		}

		/// <summary>
		/// Checks if the given user level is either a technician or an administrator
		/// </summary>
		/// <param name="user">The user to check</param>
		/// <returns>`true` if user is a technician or administrator, otherwise `false`</returns>
		public static bool IsTechOrAdmin(Models.User user) {
			return IsTechOrAdmin(user.Level.HasValue ? (int)user.Level : -1);
		}

		/// <summary>
		/// Checks if the user level is for billing department
		/// </summary>
		/// <param name="userLevel">The user level to check</param>
		/// <returns>`true` if user is a billing administrator, otherwise `false`</returns>
		public static bool IsBillingDepartment(int userLevel) {
			return userLevel == 7 || userLevel == 10;
		}

		/// <summary>
		/// Checks if the user is a billing administrator
		/// </summary>
		/// <param name="userLevel">The user level to check</param>
		/// <returns>`true` if user is a billing administrator, otherwise `false`</returns>
		public static bool IsBillingDepartment(Models.User user) {
			return IsBillingDepartment(user.Level.HasValue ? (int)user.Level : -1);
		}
    }
}
