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

using ReportPortal.Models;

namespace ReportPortal.Controllers {
	/// <summary>
	/// Controller to handle all server-related adjustments (NOT player pin adjustments)
	/// </summary>
	[Route("api/adjust"), Produces("application/json")]
	public class AdjustmentController : Controller {
		/// <summary>
		/// Obtains a list of info relating to adjustments, depending on the status code given
		/// 
		/// 0 = Unfinished adjustments
		/// 1 = All adjustments
		/// 2 = Get all adjustment types
		/// </summary>
		/// <returns>See summary</returns>
		[HttpGet, Authorize]
		public JsonResult GetUnfinishedAdjustments([RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user making request exists & has permission
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view adjustments" });

				if (status == 2) {
					return Json(new { status_code = 0, report = context.AdjustmentTypes.AsNoTracking().ToList() });
				} else {
					List<Object> report = new List<object>();
					List<Adjustment> adjustments;

					// Generate different list depending on status given
					//	0 = Only unfinished
					//	1 = All adjustments
					if (status == 0)
						adjustments = context.Adjustments.AsNoTracking().Where(a => a.Completed == false).ToList();
					else
						adjustments = context.Adjustments.AsNoTracking().ToList();

					// Add all adjustment info to report, as well as additional site info relating to adjustment
					foreach (Adjustment adjust in adjustments) {
						Site site = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == adjust.SiteId);
						report.Add(new {
							adjust.Completed,
							adjust.CompletedDate,
							adjust.Id,
							adjust.Ip,
							adjust.MonthMoneyIn,
							adjust.MonthMoneyOut,
							adjust.Notes,
							adjust.ResetRequest,
							adjust.RestartTime,
							adjust.SecretKey,
							adjust.SiteId,
							adjust.SubmissionDate,
							adjust.SubmittedBy,
							adjust.SubmitterId,
							adjust.Type,
							adjust.WeekMoneyIn,
							adjust.WeekMoneyOut,
							site.SiteName,
							site.SiteNumber
						});
					}
					return Json(new { status_code = 0, report });
				}
			}
		}

		/// <summary>
		/// Gets either all adjustments for a given site, or a code for doing a manual adjustment for a site, depending on status code
		/// </summary>
		/// <param name="id">Either site ID or adjustment ID, depending on status code</param>
		/// <param name="status">0 if getting adjustments for a site, 1 if getting adjustment code from adjustment</param>
		/// <returns></returns>
		[HttpGet("{id}"), Authorize]
		public JsonResult GetAdjustments(int id, [RequiredFromQuery]int status) {
			if (status == 0) {
				using (var context = new Data.ApplicationDbContext()) {
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
					if (authUser == null)
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

					if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

					return Json(new { status_code = 0, report = context.Adjustments.AsNoTracking().Where(a => a.SiteId == id && a.Type < Adjustment.AdjustmentType.Playable).ToList() });
				}
			} else if (status == 1) {
				using (var context = new Data.ApplicationDbContext()) {
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
					if (authUser == null)
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

					if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

					Adjustment adjustment = context.Adjustments.AsNoTracking().FirstOrDefault(a => a.Id == id);
					if (adjustment == null)
						return Json(new { status_code = 3, status = "Adjustment '" + id + "' does not exist" });

					String rawData = "," + adjustment.ResetRequest.ToString() + ",";
					switch (adjustment.Type) {
						case Adjustment.AdjustmentType.SmallIncrease:
							rawData = "+5%" + rawData;
							break;
						case Adjustment.AdjustmentType.MediumIncrease:
							rawData = "+10%" + rawData;
							break;
						case Adjustment.AdjustmentType.LargeIncrease:
							rawData = "+15%" + rawData;
							break;
						case Adjustment.AdjustmentType.SmallDecrease:
							rawData = "-5%" + rawData;
							break;
						case Adjustment.AdjustmentType.MediumDecrease:
							rawData = "-10%" + rawData;
							break;
						case Adjustment.AdjustmentType.LargeDecrease:
							rawData = "-15%" + rawData;
							break;
						default:
							rawData = "0%" + rawData;
							break;
					}

					rawData += adjustment.SecretKey.ToString() + "," + (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

					return Json(new { status_code = 0, code = Helpers.Crypto.Encrypt(rawData), expire = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-dd HH:mm:ss") });
				}
			} else {
				return Json(new { status_code = 5, status = "Unknown status code: " + status });
			}
		}

		/// <summary>
		/// This class is solely used for internal bulk adjustment creation. 
		/// 
		/// This simply acts as a wrapper to wrap Site object (site adjusted), User object (distributor), and System (site's system)
		/// </summary>
		private class AdjustmentWrapperObject {
			public Models.System SiteSystem { get; set; }
			public User Distributor { get; set; }
			public Site AdjustedSite { get; set; }
		}

		/// <summary>
		/// This class is used to map PUT requests to an object for bulk adjustment creation
		/// </summary>
		public class BulkAdjustmentObject {
			public String DelimitedSites { get; set; }
			public Adjustment BulkAdjustment { get; set; }
		}

		[HttpPost, Authorize]
		public JsonResult CreateBulkAdjustment([FromBody]BulkAdjustmentObject bao) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "Unknown user '" + authUserId + "'" });

				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to make adjustmetns" });
				
				String[] sites = bao.DelimitedSites.Split(',');
				List<AdjustmentWrapperObject> siteList = new List<AdjustmentWrapperObject>();

				// Loop through once to verify info given is valid, then again to do actual reports
				foreach (String siteId in sites) {
					Site siteAdjusted = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId.ToString().Equals(siteId));
					if (siteAdjusted == null)
						return Json(new { status_code = 3, status = "Site '" + siteId + "' does not exist" });
					User distributor = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == siteAdjusted.SiteDistributor);
					if (distributor == null)
						return Json(new { status_code = 4, status = "Site '" + siteAdjusted.SiteName + "' has no distributor" });
					Models.System system = context.Systems.AsNoTracking().FirstOrDefault(s => s.Id == siteAdjusted.SystemId);
					if (system == null)
						return Json(new { status_code = 4, status = "Site '" + siteAdjusted.SiteName + "' has no system" });

					AdjustmentWrapperObject awo = new AdjustmentWrapperObject();
					awo.AdjustedSite = siteAdjusted;
					awo.Distributor = distributor;
					awo.SiteSystem = system;
					siteList.Add(awo);
				}

				foreach (var siteAdjusted in siteList) {
					Adjustment newAdjustment = new Adjustment();
					newAdjustment.Copy(bao.BulkAdjustment, false);
					newAdjustment.SiteId = siteAdjusted.AdjustedSite.SiteId;
					newAdjustment.SubmissionDate = DateTime.UtcNow;
					newAdjustment.SubmittedBy = authUser.UserName;
					newAdjustment.SubmitterId = authUser.Id;
					newAdjustment.Completed = false;
					newAdjustment.CompletedDate = new DateTime(1970, 1, 1);
					newAdjustment.Ip = "127.0.0.1";
					newAdjustment.SecretKey = Guid.NewGuid().ToString();

					switch (bao.BulkAdjustment.Type) {
						case Adjustment.AdjustmentType.DropCommunityPrize:
						case Adjustment.AdjustmentType.DropGrandPrize:
							newAdjustment.RestartTime = null;
							break;
					}

#if LOCAL
						using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
					using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
						conn.Open();
						DateTime weekStart = newAdjustment.SubmissionDate.Subtract(new TimeSpan(7, 0, 0, 0)),
							monthStart = newAdjustment.SubmissionDate.Subtract(new TimeSpan(31, 0, 0, 0));
						MySqlCommand cmd = new MySqlCommand(String.Format(
							"SELECT " +
							"	s.site_id,s.site_name,s.site_number,WeekMoneyIN AS 'WeekIn', WeekMoneyOUT AS 'WeekOut',MonthMoneyIN AS 'MonthIn', MonthMoneyOUT AS 'MonthOut'" +
							"FROM permission p " +
							"RIGHT JOIN " +
							"	(SELECT " +
							"	 site_id,site_name,site_active,site_number " +
							"	 FROM sites) s " +
							"	 ON p.site_id = s.site_id " +
							"LEFT JOIN " +
							"	(SELECT billing.site_id, SUM(iAmount) / 100 AS WeekMoneyIN " +
							"	 FROM billing " +
							"	 WHERE " +
							"		strStatus = 'MD' AND dtDate >= '{1}' AND dtDate < '{0}' " +
							"	 GROUP BY site_id) WMI " +
							"	 ON WMI.site_id = s.site_id " +
							"LEFT JOIN " +
							"	(SELECT billing.site_id, SUM(iAmount) / 100 AS WeekMoneyOUT " +
							"	 FROM billing " +
							"	 WHERE " +
							"		strStatus = 'MW' AND dtDate >= '{1}' AND dtDate < '{0}' " +
							"	 GROUP BY site_id) WMO " +
							"	 ON WMO.site_id = s.site_id " +
							"LEFT JOIN " +
							"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MonthMoneyIN " +
							"    FROM billing " +
							"    WHERE " +
							"		strStatus = 'MD' AND dtDate >= '{2}' AND dtDate < '{0}' " +
							"	GROUP BY site_id) MMI " +
							"    ON MMI.site_id = s.site_id " +
							"LEFT JOIN " +
							"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MonthMoneyOUT " +
							"    FROM billing " +
							"    WHERE " +
							"		strStatus = 'MW' AND dtDate >= '{2}' AND dtDate < '{0}' " +
							"	GROUP BY site_id) MMO " +
							"    ON MMO.site_id = s.site_id " +
							"WHERE " +
							"	(s.site_id = {3} AND s.site_active = 1);", bao.BulkAdjustment.SubmissionDate.ToString("yyyy-MM-dd HH:mm:ss"),
							weekStart.ToString("yyyy-MM-dd HH:mm:ss"), monthStart.ToString("yyyy-MM-dd HH:mm:ss"), siteAdjusted.AdjustedSite.SiteId), conn);

						using (var reader = cmd.ExecuteReader()) {
							while (reader.Read()) {
								var money = new {
									WeekIn = DBNull.Value.Equals(reader["WeekIn"]) ? 0 : Convert.ToDecimal(reader["WeekIn"]),
									WeekOut = DBNull.Value.Equals(reader["WeekOut"]) ? 0 : Convert.ToDecimal(reader["WeekOut"]),
									MonthIn = DBNull.Value.Equals(reader["MonthIn"]) ? 0 : Convert.ToDecimal(reader["MonthIn"]),
									MonthOut = DBNull.Value.Equals(reader["MonthOut"]) ? 0 : Convert.ToDecimal(reader["MonthOut"])
								};
								newAdjustment.WeekMoneyIn = money.WeekIn;
								newAdjustment.WeekMoneyOut = money.WeekOut;
								newAdjustment.MonthMoneyIn = money.MonthIn;
								newAdjustment.MonthMoneyOut = money.MonthOut;
							}
						}
						conn.Close();
					}

					context.Adjustments.Add(newAdjustment);
					context.SaveChanges();

					// Log and notify actions
					var json = new {
						newAdjustment.WeekMoneyIn,
						newAdjustment.WeekMoneyOut,
						newAdjustment.MonthMoneyIn,
						newAdjustment.MonthMoneyOut,
						SubmissionDate = newAdjustment.SubmissionDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
						newAdjustment.SubmittedBy,
						Type = newAdjustment.Type.ToString(),
						newAdjustment.ResetRequest,
						newAdjustment.Notes,
						RoomName = siteAdjusted.AdjustedSite.SiteName,
						siteAdjusted.AdjustedSite.SiteNumber,
						Distributor = siteAdjusted.Distributor.FName + " " + siteAdjusted.Distributor.LName,
						System = siteAdjusted.SiteSystem.Name
					};
					Helpers.LogHelper.LogAction(Log.ActionType.CreateAdjustment, authUser.Id, newAdjustment.Id,
						String.Format("{0} (id: {1}) created adjustment (id: {2}) for site {3} (id: {4})",
						authUser.UserName, authUser.Id, newAdjustment.Id, siteAdjusted.AdjustedSite.SiteName, siteAdjusted.AdjustedSite.SiteId));
					Helpers.LogHelper.NotifyAction(Log.ActionType.CreateAdjustment, json);
				}

				return Json(new { status_code = 0 });
			}
		}

		/// <summary>
		/// Creates an adjustment to be claimed and completed at another time
		/// </summary>
		/// <param name="adjustment">The adjustment object (only needs type, reset request, and both money ins and outs)</param>
		/// <returns></returns>
		[HttpPut, Authorize]
		public JsonResult CreateAdjustment([FromBody]Adjustment adjustment) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "Unknown user '" + authUserId + "'" });

				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to make adjustments" });

				Site siteAdjusted = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == adjustment.SiteId);
				if (siteAdjusted == null)
					return Json(new { status_code = 3, status = "Site '" + adjustment.SiteId + "' doesn't exist", adjustment });

				// Info needed for sending to specified callback URL
				User distributor = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == siteAdjusted.SiteDistributor);
				if (distributor == null)
					return Json(new { status_code = 4, status = "Site '" + siteAdjusted.SiteName + "' has no distributor" });
				Models.System system = context.Systems.AsNoTracking().FirstOrDefault(s => s.Id == siteAdjusted.SystemId);
				if (system == null)
					return Json(new { status_code = 4, status = "Site '" + siteAdjusted.SiteName + "' has no system" });

				adjustment.SubmissionDate = DateTime.UtcNow;
				adjustment.SubmittedBy = authUser.UserName;
				adjustment.SubmitterId = authUser.Id;
				adjustment.Completed = false;
				adjustment.CompletedDate = new DateTime(1970, 1, 1);
				adjustment.Ip = "127.0.0.1";
				adjustment.SecretKey = Guid.NewGuid().ToString();

				switch (adjustment.Type) {
					case Adjustment.AdjustmentType.DropCommunityPrize:
					case Adjustment.AdjustmentType.DropGrandPrize:
						adjustment.RestartTime = null;
						break;
				}

#if LOCAL
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
				using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
					conn.Open();
					DateTime weekStart = adjustment.SubmissionDate.Subtract(new TimeSpan(7, 0, 0, 0)),
						monthStart = adjustment.SubmissionDate.Subtract(new TimeSpan(31, 0, 0, 0));
					MySqlCommand cmd = new MySqlCommand(String.Format(
						"SELECT " +
						"	s.site_id,s.site_name,s.site_number,WeekMoneyIN AS 'WeekIn', WeekMoneyOUT AS 'WeekOut',MonthMoneyIN AS 'MonthIn', MonthMoneyOUT AS 'MonthOut'" +
						"FROM permission p " +
						"RIGHT JOIN " +
						"	(SELECT " +
						"	 site_id,site_name,site_active,site_number " +
						"	 FROM sites) s " +
						"	 ON p.site_id = s.site_id " +
						"LEFT JOIN " +
						"	(SELECT billing.site_id, SUM(iAmount) / 100 AS WeekMoneyIN " +
						"	 FROM billing " +
						"	 WHERE " +
						"		strStatus = 'MD' AND dtDate >= '{1}' AND dtDate < '{0}' " +
						"	 GROUP BY site_id) WMI " +
						"	 ON WMI.site_id = s.site_id " +
						"LEFT JOIN " +
						"	(SELECT billing.site_id, SUM(iAmount) / 100 AS WeekMoneyOUT " +
						"	 FROM billing " +
						"	 WHERE " +
						"		strStatus = 'MW' AND dtDate >= '{1}' AND dtDate < '{0}' " +
						"	 GROUP BY site_id) WMO " +
						"	 ON WMO.site_id = s.site_id " +
						"LEFT JOIN " +
						"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MonthMoneyIN " +
						"    FROM billing " +
						"    WHERE " +
						"		strStatus = 'MD' AND dtDate >= '{2}' AND dtDate < '{0}' " +
						"	GROUP BY site_id) MMI " +
						"    ON MMI.site_id = s.site_id " +
						"LEFT JOIN " +
						"	(SELECT billing.site_id, SUM(iAmount) / 100 AS MonthMoneyOUT " +
						"    FROM billing " +
						"    WHERE " +
						"		strStatus = 'MW' AND dtDate >= '{2}' AND dtDate < '{0}' " +
						"	GROUP BY site_id) MMO " +
						"    ON MMO.site_id = s.site_id " +
						"WHERE " +
						"	(s.site_id = {3} AND s.site_active = 1);", adjustment.SubmissionDate.ToString("yyyy-MM-dd HH:mm:ss"),
						weekStart.ToString("yyyy-MM-dd HH:mm:ss"), monthStart.ToString("yyyy-MM-dd HH:mm:ss"), adjustment.SiteId), conn);

					using (var reader = cmd.ExecuteReader()) {
						while (reader.Read()) {
							var money = new {
								WeekIn = DBNull.Value.Equals(reader["WeekIn"]) ? 0 : Convert.ToDecimal(reader["WeekIn"]),
								WeekOut = DBNull.Value.Equals(reader["WeekOut"]) ? 0 : Convert.ToDecimal(reader["WeekOut"]),
								MonthIn = DBNull.Value.Equals(reader["MonthIn"]) ? 0 : Convert.ToDecimal(reader["MonthIn"]),
								MonthOut = DBNull.Value.Equals(reader["MonthOut"]) ? 0 : Convert.ToDecimal(reader["MonthOut"])
							};
							adjustment.WeekMoneyIn = money.WeekIn;
							adjustment.WeekMoneyOut = money.WeekOut;
							adjustment.MonthMoneyIn = money.MonthIn;
							adjustment.MonthMoneyOut = money.MonthOut;
						}
					}
					conn.Close();
				}

				context.Adjustments.Add(adjustment);
				context.SaveChanges();

				// Log and notify actions
				var json = new {
					adjustment.WeekMoneyIn,
					adjustment.WeekMoneyOut,
					adjustment.MonthMoneyIn,
					adjustment.MonthMoneyOut,
					SubmissionDate = adjustment.SubmissionDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
					adjustment.SubmittedBy,
					Type = adjustment.Type.ToString(),
					adjustment.ResetRequest,
					adjustment.Notes,
					RoomName = siteAdjusted.SiteName,
					siteAdjusted.SiteNumber,
					Distributor = distributor.FName + " " + distributor.LName,
					System = system.Name
				};
				Helpers.LogHelper.LogAction(Log.ActionType.CreateAdjustment, authUser.Id, adjustment.Id,
					String.Format("{0} (id: {1}) created adjustment (id: {2}) for site {3} (id: {4})",
					authUser.UserName, authUser.Id, adjustment.Id, siteAdjusted.SiteName, siteAdjusted.SiteId));
				Helpers.LogHelper.NotifyAction(Log.ActionType.CreateAdjustment, json);

				return Json(new { status_code = 0 });
			}
		}

		[HttpDelete("{adjustmentId}"), Authorize]
		public JsonResult DeleteAdjustment(int adjustmentId) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

				Adjustment adjustment = context.Adjustments.FirstOrDefault(a => a.Id == adjustmentId);
				if (adjustment == null)
					return Json(new { status_code = 2, status = "Adjustment '" + adjustmentId + "' does not exist" });

				if (adjustment.Completed && authUser.Level != 10)
					return Json(new { status_code = 1, status = "Only administrators can delete completed adjustments" });

				context.Adjustments.Remove(adjustment);
				context.SaveChanges();

				Helpers.LogHelper.LogAction(Log.ActionType.DeleteAdjustment, authUser.Id, adjustmentId,
					String.Format("{0} (id: {1}) deleted adjustment {2}", authUser.UserName, authUser.Id, adjustmentId));

				return Json(new { status_code = 0 });
			}
		}
	}
}
