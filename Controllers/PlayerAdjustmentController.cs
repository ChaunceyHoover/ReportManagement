using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.Controllers {
	[Route("api/pins"), Produces("application/json")]
	public class PlayerAdjustmentController : Controller {
		/// <summary>
		/// Generates a list of all player pin adjustments if user has permission to view them
		/// 
		/// Status codes
		/// -------------
		/// 0 - All unfinished player pin adjustments
		/// 1 - All player pin adjustments
		/// </summary>
		/// <returns>A list of all player pin adjustments</returns>
		[HttpGet, Authorize]
		public JsonResult GetAdjustments([RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "Unknown user '" + authUserId + "'" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

				if (status == 0)
					return Json(new { status_code = 0, report = context.PlayerAdjustments.AsNoTracking().Where(pa => !pa.CompletedTime.HasValue).ToList() });
				else if (status == 1)
					return Json(new { status_code = 0, report = context.PlayerAdjustments.AsNoTracking().ToList() });
				else
					return Json(new { status_code = 5, status = "Unknown status code '" + status + "'" });
			}
		}

		/// <summary>
		/// Gets all info relating to the given adjustment, or all player pin adjustments for a given site, depending on status
		/// </summary>
		/// <param name="id">The ID of the adjustment to get</param>
		/// <param name="status">Determins how ID variable is translated; 0 = specific adjustment, 1 = all player pin adjustments for given site</param>
		/// <returns>The adjustment row from the database, and encoded string to automate the process</returns>
		[HttpGet("{id}"), Authorize]
		public JsonResult GetAdjustment(int id, [RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "Unknown user '" + authUserId + "'" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

				// 0 = individual player pin
				if (status == 0) {
					// Get player pin adjustment for website
					Models.PlayerAdjustment adjustment = context.PlayerAdjustments.AsNoTracking().FirstOrDefault(pa => pa.Id == id);
					if (adjustment == null)
						return Json(new { status_code = 2, status = "Player Adjustment '" + id + "' does not exist" });

					// Encrypt data to process adjustment
					String rawData = adjustment.Amount + "," + adjustment.CardNumber + "," + (adjustment.AdjustmentType ? 0 : 1) + "," + adjustment.SecretKey;

					return Json(new { status_code = 0, adjustment, encoded = Helpers.Crypto.Encrypt(rawData) });
				} else if (status == 1) {
					// 1 = all pins for given website
					List<object> report = new List<object>();

					foreach (Models.PlayerAdjustment adjustment in context.PlayerAdjustments.AsNoTracking().Where(pa => pa.SiteId == id).ToList()) {
						report.Add(new {
							adjustment.Amount, adjustment.CardNumber, adjustment.CompletedTime, adjustment.Id, adjustment.Notes, adjustment.SiteId,
							adjustment.SubmissionDate, adjustment.SubmittedBy, adjustment.SubmittedByName, adjustment.AdjustmentType, TypeName = adjustment.AdjustmentType ? "Cashable" : "Playable",
							adjustment.ClaimedBy, adjustment.ClaimedByName, adjustment.ClaimedDate
						});
					}
					return Json(new { status_code = 0, report });
				} else {
					return Json(new { status_code = 5, status = "Unknown status code '" + status + "'" });
				}
			}
		}

		/// <summary>
		/// Creates a new player pin adjustment if the user has permission
		/// </summary>
		/// <param name="adjustment"></param>
		/// <returns>The newly created adjustment</returns>
		[HttpPost, Authorize]
		public JsonResult CreateAdjustment([FromBody]Models.PlayerAdjustment adjustment) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user trying to do request exists and has permission
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "Unknown user '" + authUserId + "'" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot do adjustments" });

				// Verify request is proper before processing
				if (adjustment.CardNumber.Length == 0)
					return Json(new { status_code = 4, status = "Invalid card number given" });
				if (adjustment.Amount <= 0)
					return Json(new { status_code = 4, status = "Cannot adjust zero or negative amount" });
				if (adjustment.Notes == null || adjustment.Notes.Length == 0)
					return Json(new { status_code = 4, status = "Please enter notes for adjustment" });

				adjustment.SecretKey = Guid.NewGuid().ToString();
				adjustment.SubmissionDate = DateTime.UtcNow;
				adjustment.SubmittedBy = authUser.Id;
				adjustment.SubmittedByName = authUser.UserName;
				adjustment.CompletedTime = null;

				context.PlayerAdjustments.Add(adjustment);
				context.SaveChanges();

				Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateAdjustment, authUser.Id, adjustment.Id,
					String.Format("{0} (id: {1}) created adjustment {2}", authUser.UserName, authUser.Id, adjustment.Id));

				return Json(new { status_code = 0, adjustment });
			}
		}

		/// <summary>
		/// Marks an adjustment as claimed (start of completion process) or completed
		/// 
		/// Status codes
		/// -------------
		/// 0 - Claim adjustment
		/// 1 - Complete adjustment
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpPut("{id}"), Authorize]
		public JsonResult CompleteAdjustment(int id, [FromBody]Models.PlayerAdjustment.VerificationContainer verification, [RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to do adjustments" });

				Models.PlayerAdjustment adjustment = context.PlayerAdjustments.FirstOrDefault(pa => pa.Id == id);
				if (adjustment == null)
					return Json(new { status_code = 2, status = "Player pin adjustment '" + id + "' does not exist" });
				if (adjustment.Notes == null || adjustment.Notes.Length == 0)
					return Json(new { status_code = -1, status = "Cannot claim adjustment with no notes" });

				if (status == 0) {
					// Claim adjustment
					if (adjustment.ClaimedBy.HasValue)
						return Json(new { status_code = 7, status = "Adjustment has already been claimed by " + adjustment.ClaimedByName });

					adjustment.ClaimedBy = authUser.Id;
					adjustment.ClaimedByName = authUser.UserName;
					adjustment.ClaimedDate = DateTime.UtcNow;
					adjustment.CompletedTime = null;
					context.SaveChanges();

					String rawData = adjustment.Amount + "," + adjustment.CardNumber + "," + (adjustment.AdjustmentType ? 0 : 1) + "," + adjustment.SecretKey;

					Helpers.LogHelper.LogAction(Models.Log.ActionType.ClaimAdjustment, authUser.Id, adjustment.Id,
						String.Format("{0} (id: {1}) claimed adjustment {2}", authUser.UserName, authUser.Id, adjustment.Id));

					return Json(new { status_code = 0, status = "Successfully claimed adjustment", encoded = Helpers.Crypto.Encrypt(rawData) });
				} else if (status == 1) {
					// Complete adjustment
					if (!adjustment.ClaimedBy.HasValue)
						return Json(new { status_code = 4, status = "Cannot complete unclaimed adjustment" });
					if (adjustment.ClaimedBy != authUser.Id && authUser.Level != 10)
						return Json(new { status_code = 1, status = "Cannot complete adjustment claimed by another user" });

					String[] decrypted = Helpers.Crypto.Decrypt(verification.Verification).Split(',');
					if (decrypted.Length != 3 || decrypted[2] != adjustment.SecretKey)
						return Json(new { status_code = 4, status = "Invalid verification code given" });

					adjustment.BeforeValue = Int32.Parse(decrypted[0]);
					adjustment.AfterValue = Int32.Parse(decrypted[1]);
					adjustment.CompletedTime = DateTime.UtcNow;

					context.SaveChanges();

					Helpers.LogHelper.LogAction(Models.Log.ActionType.CompleteAdjustment, authUser.Id, adjustment.Id,
						String.Format("{0} (id: {1}) completed adjustment {2}", authUser.UserName, authUser.Id, adjustment.Id));
				} else {
					return Json(new { status_code = 5, status = "Unknown status code" });
				}

				return Json(new { status_code = 0, status = Helpers.Crypto.Decrypt(verification.Verification) });
			}
		}

		[HttpDelete("{id}"), Authorize]
		public JsonResult DeleteAdjustment(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				if (!Helpers.PermissionChecker.CanDoAdjustments(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to do adjustments" });

				// Verify adjustment exists and is either: a) submitted by the user trying to delete b) user is level 10
				Models.PlayerAdjustment adjustment = context.PlayerAdjustments.FirstOrDefault(a => a.Id == id);
				if (adjustment == null)
					return Json(new { status_code = 2, status = "Adjustment '" + id + "' does not exist" });
				if (adjustment.SubmittedBy != authUser.Id && authUser.Level != 10)
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' cannot delete other user's adjustments" });

				// Make sure adjustment isn't completed or user is level 10
				if (adjustment.CompletedTime.HasValue && authUser.Level != 10)
					return Json(new { status_code = 4, status = "Only administrators can delete completed adjustments" });

				context.PlayerAdjustments.Remove(adjustment);
				context.SaveChanges();
				
				Helpers.LogHelper.LogAction(Models.Log.ActionType.DeleteAdjustment, authUser.Id, adjustment.Id,
					String.Format("{0} (id: {1}) deleted adjustment {2}", authUser.UserName, authUser.Id, adjustment.Id));

				return Json(new { status_code = 0 });
			}
		}
	}
}
