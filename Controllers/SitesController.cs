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
	[Route("api/sites"), Produces("application/json")]
	public class SitesController : Controller {
		/// <summary>
		/// Generates a report of a specific site's information
		/// </summary>
		/// <param name="siteId">The site to generate a report of</param>
		/// <returns>Json value containing all information pertaining to specific site</returns>
		private JsonResult SiteInfo(int siteId) {
			using (var context = new Data.ApplicationDbContext()) {
				Models.Site dbSite = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == siteId);
				if (dbSite == null)
					return Json(new { status_code = 1, status = "Site '" + siteId + "' does not exist." });

//#if LOCAL
//				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
//#else
//				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
//#endif
//					conn.Open();
//					MySqlCommand cmd = new MySqlCommand("");
//					using (var reader = cmd.ExecuteReader()) {
//						while (reader.Read()) {
//						}
//					}
//				}

				Models.Activation activation = context.Activations.AsNoTracking().FirstOrDefault(a => a.SiteId == siteId); ;
				Models.System system = context.Systems.AsNoTracking().FirstOrDefault(sys => sys.Id == dbSite.SystemId);

				return Json(new { status_code = 0, site = dbSite, activation, system });
			}
		}

		/// <summary>
		/// Generates a billing report of all sites within a given date range
		/// </summary>
		/// <param name="start">Start period of report</param>
		/// <param name="end">End period of report</param>
		/// <returns>List of all sites with their money in and money out within specified date range</returns>
		private JsonResult GenerateListReport(DateTime start, DateTime end) {
			using (var context = new Data.ApplicationDbContext()) {
				// Authorization check
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				// Generate report
				List<Object> report = new List<Object>();

#if LOCAL
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
					// Calculate the money in and money out for every site, and translate ID values to names
					conn.Open();
					String startDate = start.ToString("yyyy-MM-dd HH:mm:ss");
					String endDate = end.ToString("yyyy-MM-dd HH:mm:ss");
					decimal moneyIn = 0, moneyOut = 0;
					MySqlCommand moneyCmd;
					if (Helpers.PermissionChecker.IsTechOrAdmin(authUser)) {
						moneyCmd = new MySqlCommand(
							String.Format("SELECT " +
								"	s.site_id,s.system_id,s.site_number,s.site_guid,s.site_name,s.site_active,s.site_enabled,s.site_on_report,s.site_serial,s.site_last_ip, " +
								"	s.site_last_ping,s.site_distributor,s.site_state,s.site_owner_name,s.site_owner_phone,s.site_owner_email,s.site_address,s.site_city,s.site_country, " +
								"	s.site_zip,s.notes,s.store_phone,s.site_date_installed,s.site_install_date, MoneyIN AS 'In', MoneyOUT AS 'Out', " +
								"	(COALESCE(MoneyIN, 0) - COALESCE(MoneyOUT, 0))/COALESCE(MoneyIN, 0) * 100 AS Percent,sys.system_name,distrib.contact_lname AS 'distrib_name' " +
								"FROM permission p " +
								"RIGHT JOIN " +
								"	(SELECT " +
								"	 site_id,system_id,site_number,site_guid,site_name,site_active,site_enabled,site_on_report,site_serial,site_last_ip, " +
								"	site_last_ping,site_distributor,site_state,site_owner_name,site_owner_phone,site_owner_email,site_address,site_city,site_country, " +
								"	site_zip,notes,store_phone,site_date_installed,site_install_date " +
								"	 FROM sites) s " +
								"	 ON p.site_id = s.site_id " +
								"INNER JOIN " +
								"	(SELECT contact_lname, user_id, user_name FROM `user`) u " +
								"	 ON u.user_id = s.site_distributor " +
								"LEFT JOIN " +
								"	(SELECT " +
								"	 user_id, user_name, contact_lname " +
								"	 FROM `user`) distrib " +
								"	 ON distrib.user_id = u.user_id " +
								"LEFT JOIN " +
								"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MoneyIN " +
								"	 FROM billing " +
								"	 WHERE " +
								"		strStatus = 'MD' AND dtDate >= '{0}' AND dtDate < '{1}' " +
								"	 GROUP BY site_id) MI " +
								"	 ON MI.site_id = s.site_id " +
								"LEFT JOIN " +
								"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MoneyOUT " +
								"	 FROM billing " +
								"	 WHERE " +
								"		strStatus = 'MW' AND dtDate >= '{0}' AND dtDate < '{1}' " +
								"	 GROUP BY site_id) MO " +
								"	 ON MO.site_id = s.site_id " +
								"INNER JOIN (SELECT system_id, system_name FROM systems) sys " +
								"ON s.system_id = sys.system_id " +
								"WHERE " +
								"	(s.site_number > 0 AND s.site_active = 1) " +
								"GROUP BY s.site_number;", startDate, endDate), conn);
					} else {
						moneyCmd = new MySqlCommand(
							String.Format("SELECT " +
								"	   sys.system_id, " +
								"	   sys.system_name, " +
								"	   s.site_id,s.system_id,s.site_number,s.site_guid,s.site_name,s.site_active,s.site_enabled,s.site_on_report,s.site_serial,s.site_last_ip, " +
								"	s.site_last_ping,s.site_distributor,s.site_state,s.site_owner_name,s.site_owner_phone,s.site_owner_email,s.site_address,s.site_city,s.site_country, " +
								"	s.site_zip,s.notes,s.store_phone,s.site_date_installed,s.site_install_date,distrib.contact_lname AS 'distrib_name', " +
								"	   Coalesce(moneyin, 0)  AS 'In', " +
								"	   Coalesce(moneyout, 0) AS 'Out' " +
								"FROM   permission p " +
								"	   LEFT JOIN (SELECT site_id,system_id,site_number,site_guid,site_name,site_active,site_enabled,site_on_report,site_serial,site_last_ip, " +
								"	site_last_ping,site_distributor,site_state,site_owner_name,site_owner_phone,site_owner_email,site_address,site_city,site_country, " +
								"	site_zip,notes,store_phone,site_date_installed,site_install_date " +
								"				  FROM   sites) s " +
								"			  ON p.site_id = s.site_id " +
								"	   INNER JOIN systems sys " +
								"			   ON s.system_id = sys.system_id " +
								"	   INNER JOIN (SELECT contact_lname, " +
								"						  user_id " +
								"				   FROM   `user`) u " +
								"			   ON u.user_id = s.site_distributor " +
								"	   LEFT JOIN " +
								"			(SELECT " +
								"			 user_id, user_name, contact_lname " +
								"			 FROM `user`) distrib " +
								"			 ON distrib.user_id = u.user_id " +
								"	   LEFT JOIN(SELECT billing.site_id, " +
								"						Sum(iamount) / 100 AS MoneyIN " +
								"				 FROM   billing " +
								"				 WHERE  strstatus = 'MD' " +
								"						AND dtdate >= '{0}' " +
								"						AND dtdate < '{1}' " +
								"				 GROUP  BY site_id) MI " +
								"			  ON MI.site_id = s.site_id " +
								"	   LEFT JOIN(SELECT billing.site_id, " +
								"						Sum(iamount) / 100 AS MoneyOUT " +
								"				 FROM   billing " +
								"				 WHERE  strstatus = 'MW' " +
								"						AND dtdate >= '{0}' " +
								"						AND dtdate < '{1}' " +
								"				 GROUP  BY site_id) MO " +
								"			  ON MO.site_id = s.site_id " +
								"WHERE  ( ( p.user_id = {2} " +
								"			OR s.site_distributor = {2} ) " +
								"		 AND s.site_number > 0 " +
								"		 AND s.site_active = 1 ) " +
								"GROUP  BY s.site_id", startDate, endDate, authUser.Id), conn);
					}
					using (var reader = moneyCmd.ExecuteReader())
						while (reader.Read()) {
							moneyIn = DBNull.Value.Equals(reader["In"]) ? 0 : Convert.ToDecimal(reader["In"]);
							moneyOut = DBNull.Value.Equals(reader["Out"]) ? 0 : Convert.ToDecimal(reader["Out"]);
							Models.Site dbSite = new Models.Site();
							dbSite.SiteId = DBNull.Value.Equals(reader["site_id"]) ? -1 : Convert.ToInt32(reader["site_id"]);
							dbSite.SystemId = DBNull.Value.Equals(reader["system_id"]) ? -1 : Convert.ToInt32(reader["system_id"]);
							dbSite.SiteNumber = DBNull.Value.Equals(reader["site_number"]) ? -1 : Convert.ToInt32(reader["site_number"]);
							dbSite.SiteGuid = DBNull.Value.Equals(reader["site_guid"]) ? "" : Convert.ToString(reader["site_guid"]);
							dbSite.SiteName = DBNull.Value.Equals(reader["site_name"]) ? "" : Convert.ToString(reader["site_name"]);
							dbSite.SiteActive = DBNull.Value.Equals(reader["site_active"]) ? false : Convert.ToBoolean(reader["site_active"]);
							dbSite.SiteEnabled = DBNull.Value.Equals(reader["site_enabled"]) ? false : Convert.ToBoolean(reader["site_enabled"]);
							dbSite.SiteOnReport = DBNull.Value.Equals(reader["site_on_report"]) ? false : Convert.ToBoolean(reader["site_on_report"]);
							dbSite.SiteSerial = DBNull.Value.Equals(reader["site_serial"]) ? "" : Convert.ToString(reader["site_serial"]);
							dbSite.SiteLastIp = DBNull.Value.Equals(reader["site_last_ip"]) ? "" : Convert.ToString(reader["site_last_ip"]);
							dbSite.SiteLastPing = DBNull.Value.Equals(reader["site_last_ping"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["site_last_ping"]);
							dbSite.SiteDistributor = DBNull.Value.Equals(reader["site_distributor"]) ? -1 : Convert.ToInt32(reader["site_distributor"]);
							dbSite.SiteState = DBNull.Value.Equals(reader["site_state"]) ? "" : Convert.ToString(reader["site_state"]);
							dbSite.SiteOwnerName = DBNull.Value.Equals(reader["site_owner_name"]) ? "" : Convert.ToString(reader["site_owner_name"]);
							dbSite.SiteOwnerPhone = DBNull.Value.Equals(reader["site_owner_phone"]) ? "" : Convert.ToString(reader["site_owner_phone"]);
							dbSite.SiteOwnerEmail = DBNull.Value.Equals(reader["site_owner_email"]) ? "" : Convert.ToString(reader["site_owner_email"]);
							dbSite.SiteAddress = DBNull.Value.Equals(reader["site_address"]) ? "" : Convert.ToString(reader["site_address"]);
							dbSite.SiteCity = DBNull.Value.Equals(reader["site_city"]) ? "" : Convert.ToString(reader["site_city"]);
							dbSite.SiteCountry = DBNull.Value.Equals(reader["site_country"]) ? "" : Convert.ToString(reader["site_country"]);
							dbSite.SiteZip = DBNull.Value.Equals(reader["site_zip"]) ? "" : Convert.ToString(reader["site_zip"]);
							dbSite.Notes = DBNull.Value.Equals(reader["notes"]) ? "" : Convert.ToString(reader["notes"]);
							dbSite.StorePhone = DBNull.Value.Equals(reader["store_phone"]) ? "" : Convert.ToString(reader["store_phone"]);
							dbSite.SiteInstallDate = DBNull.Value.Equals(reader["site_install_date"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["site_install_date"]);
							report.Add(new { // need to make an anonymous object because "Site" doesn't have a "moneyIn" and "moneyOut" field, as well as the distributor name and system name
								moneyIn, moneyOut, dbSite.SiteId, dbSite.SystemId, dbSite.SiteNumber, dbSite.SiteGuid, dbSite.SiteName, dbSite.SiteActive, dbSite.SiteEnabled,
								dbSite.SiteOnReport, dbSite.SiteSerial, dbSite.SiteLastIp, dbSite.SiteLastPing, dbSite.SiteDistributor, dbSite.SiteState, dbSite.SiteOwnerName,
								dbSite.SiteOwnerPhone, dbSite.SiteOwnerEmail, dbSite.SiteAddress, dbSite.SiteCity, dbSite.SiteCountry, dbSite.SiteZip, dbSite.Notes, dbSite.StorePhone,
								dbSite.SiteInstallDate,
								systemName = DBNull.Value.Equals(reader["system_name"]) ? "" : Convert.ToString(reader["system_name"]),
								distributor = DBNull.Value.Equals(reader["distrib_name"]) ? "UNKNOWN" : Convert.ToString(reader["distrib_name"])
							});
						}

				};

				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>Genereates a simple list of all sites with just their ID and Name</summary>
		/// <returns>Json list of all sites</returns>
		private JsonResult ListAllSites() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Models.Site> sites = context.Sites.Where(s => s.SiteActive == true).AsNoTracking().OrderBy(s => s.SiteNumber).ToList();
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId)); // assumed to exist b/c this method is only called if it is
				List<Models.Permission> permissions = context.Permissions.Where(p => p.UserId == authUser.Id).AsNoTracking().ToList();
				
				var queriedSites = (from s in sites
									join u in context.Users.AsNoTracking() on s.SiteDistributor equals u.Id
									join sys in context.Systems.AsNoTracking() on s.SystemId equals sys.Id
									join adj in context.Adjustments.AsNoTracking() on s.SiteId equals adj.SiteId orderby adj.SubmissionDate descending
									select new {
										Id = s.SiteId,
										s.SiteNumber,
										s.SiteName,
										SiteLastPing = s.SiteLastPing.HasValue ? s.SiteLastPing : new DateTime(2000, 1, 1),
										SiteLastIp = s.SiteLastIp,
										Distributor = u.LName,
										SystemName = sys.Name,
										SystemPrefix = sys.Prefix,
										s.StoreOpenTime,
										s.StoreCloseTime,
										AdjustmentSubmissionDate = adj.SubmissionDate,
										AdjustmentType = adj.Type.ToString()
									}).ToArray();
				if (Helpers.PermissionChecker.IsTechOrAdmin(authUser))
					return Json(new { status_code = 0, report = queriedSites });
				else {
					var report = new List<Object>();
					foreach (var site in queriedSites) {
						foreach (Models.Permission permission in permissions) {
							if (permission.SiteId == site.Id)
								report.Add(site);
						}
					}
					return Json(new { status_code = 0, report });
				}
			}
		}

		/// <summary>
		/// Generates a list of all de-activated sites.
		/// </summary>
		/// <returns>A list of all sites with `site_active` set to 0</returns>
		private JsonResult ListDisabledSites() {
			using (var context = new Data.ApplicationDbContext()) {
				List<Models.Site> sites = context.Sites.AsNoTracking().Where(s => s.SiteActive == false).ToList();
				var report = (from s in sites
									join u in context.Users.AsNoTracking() on s.SiteDistributor equals u.Id
									join sys in context.Systems.AsNoTracking() on s.SystemId equals sys.Id
									select new {
										Id = s.SiteId,
										s.SiteNumber,
										s.SiteName,
										SiteLastPing = s.SiteLastPing.HasValue ? s.SiteLastPing : new DateTime(2000, 1, 1),
										SiteLastIp = s.SiteLastIp,
										Distributor = u.LName,
										SystemName = sys.Name,
										SystemPrefix = sys.Prefix
									}).ToArray();
				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>
		/// Generates a list of all sites to those who are authorized to view it
		/// </summary>
		/// <param name="start">Start period of report</param>
		/// <param name="end">End period of report</param>
		/// <returns>A list of all sites' billing report within a given date range</returns>
		[HttpGet, Authorize]
		public JsonResult GetSites([FromQuery]DateTime start, [FromQuery]DateTime end, [RequiredFromQuery]int status) {
			switch (status) {
				case 0:
					// normally, the permission check would be done here, but the report generated is based off the user's level, so it's done
					// in that method instead
					if (start == null || end == null)
						return Json(new { status_code = 4, status = "Invalid parameters to GET /api/sites; must set both start & end to valid date format" });
					return GenerateListReport(start, end);
				case 1:
					// again, normally permission checks would be in this method, but as with case 0, the list generated is dependent on the authorized user's level,
					// so the only check needed here is to make sure the user exists
					using (var context = new Data.ApplicationDbContext()) {
						String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
						Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
						if (authUser == null) {
							return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
						}
					}
					return ListAllSites();
				case 2:
					using (var context = new Data.ApplicationDbContext()) {
						String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
						Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
						if (authUser == null) {
							return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
						}

						if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser))
							return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view disabled sites" });
					}
					return ListDisabledSites();
				default:
					return Json(new { status_code = 5, status = "Unknown status code with GET /api/sites" });
			}
		}

		/// <summary>
		/// Generates a report for a specific website to those authorized to view it
		/// </summary>
		/// <param name="siteId">The site to generate a report of</param>
		/// <returns>Json value containing a site's information</returns>
		[HttpGet("{siteId}"), Authorize]
		public JsonResult GetSite(int siteId) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				if (Helpers.PermissionChecker.IsTechOrAdmin(authUser) || Helpers.PermissionChecker.IsBillingDepartment(authUser))
					return SiteInfo(siteId);
				else {
					Models.Permission permission = context.Permissions.AsNoTracking().FirstOrDefault(p => p.UserId == authUser.Id && p.SiteId == siteId);
					if (permission != null)
						return SiteInfo(siteId);
					return Json(new { status_code = 1, status = "User does not have permission to view site '" + siteId + "'" });
				}
			}
		}

		/// <summary>
		/// Updates a given site with all the information supplied
		/// note: SiteId cannot be changed pragmatically, therefore whatever SiteId is specified will be the site updated
		/// </summary>
		/// <param name="site">The site to update</param>
		/// <returns>Json status_code of the update (0 = succes, anything else = error code + error msg)</returns>
		[HttpPost, Authorize]
		public JsonResult Update([FromBody]Models.Site site) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null) {
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				}

				if (!Helpers.PermissionChecker.CanModifySite(authUser) && authUser.Id != site.SiteDistributor)
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to edit site" });

				Models.Permission permission = context.Permissions.AsNoTracking().FirstOrDefault(p => p.UserId == authUser.Id && p.SiteId == site.SiteId);
				if (permission != null || Helpers.PermissionChecker.CanModifySite(authUser)) {
					Models.Site dbSite = context.Sites.FirstOrDefault(s => s.SiteId == site.SiteId);
					if (dbSite == null)
						return Json(new { status_code = 2, status = "Site '" + site.SiteId + "' does not exist" });

					List<Models.Log.Variance> variances = dbSite.Compare(site);
					if (variances.Count == 0)
						return Json(new { status_code = 0, status = "No changes made (given object same as database row)" });
					
					dbSite.Copy(site, true);

					String changes = "";
					foreach (Models.Log.Variance var in variances) {
						changes += "[(" + var.Property + "=" + (var.New != null ? var.New.ToString() : "null") + ")]";
						if (var.Property.Equals("SiteEnabled"))
							dbSite.Comment = authUser.UserName + " " + ((bool)var.New ? "enabled" : "disabled") + " site last at " + DateTime.UtcNow.ToString("M/dd h:mm tt");
					}

					context.SaveChanges();

					Helpers.LogHelper.LogAction(Models.Log.ActionType.ModifySite, authUser.Id, dbSite.SiteId,
						String.Format("{0} (id: {1}) modified site '{2}' (site number: {3})", authUser.UserName, authUser.Id, dbSite.SiteName, dbSite.SiteNumber), changes);

					return Json(new { status_code = 0, site = dbSite });
				}
				return Json(new { status_code = 1, status = "User does not have permission to update site '" + site.SiteId + "'" });
			}
		}

		/// <summary>
		/// Used exclusively for PUT requests to bulk update site info
		/// 
		/// For now, it only has to update distributor. Class will be modified to be more generic when needed
		/// </summary>
		public class BulkDistributorUpdateObject {
			public String[] SiteList { get; set; }
			public Models.User Distributor { get; set; }
		}

		/// <summary>
		/// Applies update(s) to multiple sites with one request
		/// </summary>
		/// <param name="bduo"></param>
		/// <returns></returns>
		[HttpPut, Authorize]
		public JsonResult BulkUpdate([FromBody]BulkDistributorUpdateObject bduo) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user exists & has permission
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				// Verify given distributor exists & is distributor
				Models.User distrib = context.Users.FirstOrDefault(u => u.Id == bduo.Distributor.Id);
				if (distrib == null)
					return Json(new { status_code = 2, status = "User '" + distrib.UserName + "' does not exist" });
				if (distrib.Level < 3) // 1 = Cashier, 2 = Owner; everything else is more important than distrib, so don't want to lower their level
					distrib.Level = 3;

				// Set each site's distributor to given distributor
				foreach (String siteId in bduo.SiteList) {
					Models.Site site = context.Sites.FirstOrDefault(s => s.SiteId.ToString().Equals(siteId));
					if (site == null)
						return Json(new { status_code = 2, status = "Site '" + siteId + "' does not exist" });
					if (!Helpers.PermissionChecker.CanModifySite(authUser) && authUser.Id != site.SiteDistributor)
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to edit site" });

					site.SiteDistributor = distrib.Id;
				}

				// Save all changes
				context.SaveChanges();
			}
			return Json(new { status_code = 0 });
		}
	}
}
