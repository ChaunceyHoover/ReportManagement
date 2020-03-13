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
	/// Controller responsible for handling all data pertaining to activations
	/// </summary>
	[Route("api/activate"), Produces("application/json")]
	public class ActivationController : Controller {
		/// <summary>
		/// Gets all activation forms submitted for a given site
		/// </summary>
		/// <param name="siteId">The site to look up the acitvation forms for</param>
		/// <returns>The activation(s), if it exists</returns>
		private JsonResult GetActivations(int siteId) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user exists & has permission
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser) && !Helpers.PermissionChecker.IsBillingDepartment(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view activations" });

				// Generate list of reports, ordered from newest to oldest
				List<Object> report = new List<object>();
				List<Models.Activation> activations = context.Activations.AsNoTracking().Where(a => a.SiteId == siteId).OrderByDescending(a => a.SubmissionDate).ToList();
				if (activations.Count == 0)
					return Json(new { status_code = 2, status = "Site '" + siteId + "' does not have any activation forms from RP2.0+" });

				// Add activation, system, and extra site info to each activation object, then add all that info to report
				foreach (Models.Activation activation in activations) {
					Models.System system = context.Systems.AsNoTracking().FirstOrDefault(s => s.Id == activation.SystemId);
					Object approver = null, siteQuickInfo = null;
					if (activation.ApprovedBy.HasValue) {
						Models.User approveUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == (int)activation.ApprovedBy);
						Models.Site site = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == activation.SiteId);
						if (activation.ApprovedBy != -1)
							approver = new { approveUser.FName, approveUser.LName, name = approveUser.FName + " " + approveUser.LName, approveUser.Id };
						else
							approver = new { FName = "PRE-APPROVED", LName = "AUTOMATICALLY", name = "PRE-APPROVED AUTOMATICALLY", Id = -1 };
						siteQuickInfo = new { site.SiteLastPing, site.SiteLastIp };
					}
					
					report.Add(new {
						activation,
						system,
						approver,
						siteQuickInfo
					});
				}

				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>Gets a specific activation</summary>
		/// <param name="formId"></param>
		/// <returns></returns>
		private JsonResult GetActivation(int formId) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user exists & has permission
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser) && !Helpers.PermissionChecker.IsBillingDepartment(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view activations" });

				// Verify activation form exists, and if so, return form
				Models.Activation activation = context.Activations.AsNoTracking().FirstOrDefault(a => a.ActivationId == formId);
				if (activation == null)
					return Json(new { status_code = 2, status = "Activation '" + formId + "' does not exist" });

				return Json(new { status_code = 0, activation });
			}
		}

		/// <summary>
		/// Used to filter GET requests based off given status from query
		/// </summary>
		/// <param name="id">If status == 0 then SiteId, otherwise activation form ID</param>
		/// <param name="status">Used to filter which type of info to retreive from GET request</param>
		/// <returns>if status == 0 then all activations for the given site ID,
		/// if status == 1 then an the activation form with the given ID,
		/// otherwise status_code = 6 (invalid status code)</returns>
		[HttpGet("{id}"), Authorize]
		public JsonResult Get(int id, [RequiredFromQuery]int status) {
			if (status == 0)
				return GetActivations(id);
			else if (status == 1)
				return GetActivation(id);
			else
				return Json(new { status_code = 5, status = "Unknown status code '" + status + "'" });
		}

		/// <summary>
		/// Returns a list of all unapproved sites
		/// </summary>
		/// <returns>A list of all unapproved sites</returns>
		[HttpGet, Authorize]
		public JsonResult GetUnapproved() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.IsTechOrAdmin(authUser) && !Helpers.PermissionChecker.IsBillingDepartment(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to view activations" });

				List<Models.Activation> report = context.Activations.AsNoTracking().Where(a => a.ApprovedBy.HasValue == false).ToList();
				
				return Json(new { status_code = 0, report });
			}
		}

		/// <summary>
		/// Approves a given site's activation, if it has an unapproved activation form
		/// </summary>
		/// <param name="siteId">Site ID to approve</param>
		/// <returns>Status code of request, along with status to give further details</returns>
		/// <remarks>If status code == 0, then POST ran successfully</remarks>
		[HttpPost, Authorize]
		public JsonResult ApproveSite([FromBody]Models.Activation form) {
			using (var context = new Data.ApplicationDbContext()) {
				// Verify user exists & has permission to approve site
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));

				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });
				if (!Helpers.PermissionChecker.IsBillingDepartment(authUser) && authUser.Level != 10)
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to approve site" });

				// Verify site exists & has an activation form
				Models.Site site = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == form.SiteId);
				if (site == null)
					return Json(new { status_code = 2, status = "Site for activation form '" + form.ActivationId + "' does not exist (did someone delete it?)" });

				Models.Activation activation = context.Activations.FirstOrDefault(a => a.SiteId == form.SiteId && a.ActivationId == form.ActivationId);
				if (activation == null)
					return Json(new { status_code = 2, status = "Activation form '" + form.ActivationId + "' for site '" + form.SiteId + "' does not exist" });

				// Verify site has not already been approved by someone else (this shouldn't normally happen, but never hurts to check)
				Models.User approver = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == (int)activation.ApprovedBy);
				if (activation.ApprovedBy.HasValue) {
					// note: may error if a user approves a site then later gets deleted and someone tries to approve the same site again
					return Json(new {
						status_code = 11,
						status = "Site already approved by '" + (approver != null ? approver.UserName : activation.ApprovedBy.ToString()) + "'",
						approver = new {
							approver.FName,
							approver.LName,
							name = approver.FName + " " + approver.LName,
							approver.Id
						}
					});
				}

				// Set site to be approved, as well as additional info
				approver = authUser;
				activation.ApprovedBy = authUser.Id;
				activation.ApprovalNotes = form.ApprovalNotes;
				site.SiteGuid = form.Key;
				context.SaveChanges();

				// Info to send to Zapier
				var json = new {
					Approver = new { approver.Id, approver.UserName, approver.FName, approver.LName, approver.Phone, approver.Email },
					Activation = activation
				};

				// Log site being approved and send json object to Zapier
				Helpers.LogHelper.LogAction(Models.Log.ActionType.ApproveSite, authUser.Id, form.SiteId,
					String.Format("{0} (id: {1}) approved site {2}'s (site number: {3}) activation", authUser.UserName, authUser.Id, site.SiteName, site.SiteNumber));
				Helpers.LogHelper.NotifyAction(Models.Log.ActionType.ApproveSite, json);

				return Json(new { status_code = 0 });
			}
		}

		/// <summary>
		/// Creates an activation form for a site, and if site doesn't exist yet, creates new site, depending on `type` variable.
		/// 
		/// Process:
		/// 1) Verify installer (create if installer doesn't exist)
		/// 2) Verify manager (create if manager doesn't exist)
		/// 3) Verify new owner info and create if info is acceptable
		/// 4) Create new site
		/// 5) Create new row in activation table with all the newly created info
		/// 6) Assign new owner to new site
		/// 7) Finalize any information & save to database
		/// 8) Log events and send email
		/// </summary>
		/// <param name="form">The activation form containing all 5 needed objects (see `ActivationForm`)</param>
		/// <remarks>If any information is incorrect, nothing gets created</remarks>
		/// <returns>status_code = 0 if success, otherwise status_code from `error_code.txt` and appropriate description</returns>
		[HttpPut, Authorize]
		public JsonResult Create([FromBody]Models.ActivationForm form, [RequiredFromQuery]int type) {
			if (type == 0) {
				// Create new site & activation
				using (var context = new Data.ApplicationDbContext()) {
					// Verify user exists & has permission
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
					if (authUser == null)
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

					if (!Helpers.PermissionChecker.CanAddSite(authUser))
						return Json(new { status_code = 3, status = "User '" + authUser.UserName + "' does not have permission to activate sites" });

					// Used to check if installer object and/or manager object need to be added to database
					bool InstallerCreated = false, ManagerCreated = false, OwnerCreated = false;

					// #1 - Check installer //
					// if Id is 0, create new installer. Otherwise, verify installer exists
					if (form.SiteInstaller.Id != 0) {
						Models.Installer installer = context.Installers.AsNoTracking().FirstOrDefault(i => i.Id == form.SiteInstaller.Id);
						if (installer == null)
							return Json(new { status_code = 5, status = "Installer '" + form.SiteInstaller.Id + "' does not exist" });
					} else {
						if (form.SiteInstaller.FName.Length == 0 && form.SiteInstaller.LName.Length == 0)
							return Json(new { status_code = 5, status = "Installer must have at least a first or last name" });
						else if (form.SiteInstaller.Phone.Length == 0)
							return Json(new { status_code = 5, status = "Installer must have a phone number" });
						context.Installers.Add(form.SiteInstaller);
						InstallerCreated = true;
					}

					// #2 - Check manager //
					// Same logic as installer (0 = new, otherwise use existing)
					if (form.SiteManager.Id != 0) {
						Models.Manager manager = context.Managers.AsNoTracking().FirstOrDefault(m => m.Id == form.SiteManager.Id);
						if (manager == null)
							return Json(new { status_code = 5, status = "Manager '" + form.SiteManager.Id + "' does not exist" });
					} else {
						if (form.SiteManager.FName.Length == 0 && form.SiteManager.LName.Length == 0)
							return Json(new { status_code = 5, status = "Manager must have at least a first or last name" });
						else if (form.SiteManager.Phone.Length == 0)
							return Json(new { status_code = 5, status = "Manager must have a phone number" });
						context.Managers.Add(form.SiteManager);
						ManagerCreated = true;
					}

					// #3 - Verify new owner info //
					if (form.NewOwner.Id == 0) {
						if (context.Users.AsNoTracking().FirstOrDefault(u => form.NewOwner.UserName != null && u.UserName != null && form.NewOwner.UserName.ToLower().Equals(u.UserName.ToLower())) != null) {
							return Json(new { status_code = 3, status = "User '" + form.NewOwner.UserName + "' already exists" });
						}

						if (String.IsNullOrWhiteSpace(form.NewOwner.UserName) || String.IsNullOrWhiteSpace(form.NewOwner.Password))
							return Json(new { status_code = 4, status = "Invalid user creation body" });

						form.NewOwner.Active = true;
						form.NewOwner.Level = 2; // force user to be owner
						form.NewOwner.UserLastLogin = new DateTime(2000, 1, 1);
						OwnerCreated = true;
					} else {
						form.NewOwner = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == form.NewOwner.Id);
						if (form.NewOwner == null)
							return Json(new { status_code = 2, status = "Owner '" + form.NewOwner.Id + "' does not exist" });
					}

					if (!form.PreApproved && (String.IsNullOrWhiteSpace(form.NewOwner.UserName) || String.IsNullOrWhiteSpace(form.NewOwner.Password)))
						return Json(new { status_code = 5, status = "Invalid user creation body" });

					// #4 - Create new site //
					// note: if site is pre-approved, new site will not be created

					// Verify site info
					Models.System system = context.Systems.AsNoTracking().FirstOrDefault(s => s.Id == form.NewSite.SystemId);
					if (system == null)
						return Json(new { status_code = 5, status = "Invalid system given", id = form.NewSite.SystemId });

					Models.User distrib = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == form.NewSite.SiteDistributor);
					if (distrib == null)
						return Json(new { status_code = 5, status = "Invalid distributor given" });

					if (form.NewSite.SiteName.Length == 0)
						return Json(new { status_code = 5, status = "No site name given" });
					if (!form.NewSite.SiteInstallDate.HasValue)
						return Json(new { status_code = 5, status = "Invalid install date" });
					if (form.NewSite.SiteAddress.Length == 0 || form.NewSite.SiteCity.Length == 0 || form.NewSite.SiteState.Length == 0
						|| form.NewSite.SiteCountry.Length == 0 || form.NewSite.SiteZip.Length < 5 || form.NewSite.SiteZip.Length > 10)
						return Json(new { status_code = 5, status = "Invalid site address given" });
					if (form.NewSite.SiteOwnerName.Length == 0)
						return Json(new { status_code = 5, status = "No owner name given" });
					if (form.NewSite.SiteOwnerEmail.Length == 0 && form.NewSite.SiteOwnerPhone.Length == 0)
						return Json(new { status_code = 5, status = "Need at least one way to contact owner (none given)" });

					// Generate site info if not pre-approved
					if (!form.PreApproved) {
						List<Models.Site> sites = context.Sites.AsNoTracking().OrderByDescending(s => s.SiteNumber).ToList();
						int maxSiteNum = -1;
						foreach (Models.Site s in sites)
							if (s.SiteNumber.HasValue) {
								maxSiteNum = (int)s.SiteNumber;
								break;
							}

						form.NewSite.SiteNumber = ++maxSiteNum;
						form.NewSite.SiteActive = true;
						form.NewSite.SiteEnabled = true;
						form.NewSite.SiteLastPing = new DateTime(2000, 1, 1);
						form.NewSite.SiteLastIp = "127.0.0.1";
					}

					// #5 - New activation row //
					// Need to save these to database to generate any IDs (in case new installer/manager is used, and for new site/owner) if site isn't pre-approved
					if (!form.PreApproved) {
						if (form.NewOwner.Id == 0)
							context.Users.Add(form.NewOwner);
						context.Sites.Add(form.NewSite);
						context.SaveChanges();
					}

					// Verify that distributor has access to site
					// (because users can create distributors when activating site)
					Models.Permission permission = context.Permissions.FirstOrDefault(p => form.NewSite.SiteDistributor.HasValue 
							&& p.UserId == form.NewSite.SiteDistributor && p.SiteId == form.NewSite.SiteId);
					if (permission == null) {
						permission = new Models.Permission {
							UserId = (int)form.NewSite.SiteDistributor,
							SiteId = form.NewSite.SiteId,
							Access = 1,
						};

						context.Permissions.Add(permission);
					} else if (permission.Access.HasValue && (int)permission.Access != 1)
						permission.Access = 1;

					// Set some default values for newly created sites
					form.NewSite.LastCommunityDrop = new DateTime(1970, 1, 1, 0, 0, 0);
					form.NewSite.LastGrandDrop = new DateTime(1970, 1, 1, 0, 0, 0);

					// Activation form needs to save what was submitted, not reflect current information
					// therefore, activation table essentially acts as a merged version of multiple tables
					form.ActivationInfo.SiteId = form.NewSite.SiteId;
					form.ActivationInfo.RoomName = form.NewSite.SiteName;
					form.ActivationInfo.SystemId = system.Id;
					form.ActivationInfo.StorePhone = form.NewSite.StorePhone;
					form.ActivationInfo.InstallerId = form.SiteInstaller.Id;
					form.ActivationInfo.InstallerFName = form.SiteInstaller.FName;
					form.ActivationInfo.InstallerLName = form.SiteInstaller.LName;
					form.ActivationInfo.InstallerEmail = form.SiteInstaller.Email;
					form.ActivationInfo.InstallerPhone = form.SiteInstaller.Phone;
					form.ActivationInfo.ManagerId = form.SiteManager.Id;
					form.ActivationInfo.ManagerFName = form.SiteManager.FName;
					form.ActivationInfo.ManagerLName = form.SiteManager.LName;
					form.ActivationInfo.ManagerEmail = form.SiteManager.Email;
					form.ActivationInfo.ManagerPhone = form.SiteManager.Phone;
					form.ActivationInfo.OwnerId = form.NewOwner.Id;
					form.ActivationInfo.OwnerFName = form.NewOwner.FName;
					form.ActivationInfo.OwnerLName = form.NewOwner.LName;
					form.ActivationInfo.OwnerEmail = form.NewOwner.Email;
					form.ActivationInfo.OwnerPhone = form.NewOwner.Phone;
					form.ActivationInfo.OwnerUserName = form.NewOwner.UserName;
					form.ActivationInfo.DistributorId = distrib.Id;
					form.ActivationInfo.DistributorFName = distrib.FName;
					form.ActivationInfo.DistributorLName = distrib.LName;
					form.ActivationInfo.SiteAddress = form.NewSite.SiteAddress;
					form.ActivationInfo.SiteCity = form.NewSite.SiteCity;
					form.ActivationInfo.SiteState = form.NewSite.SiteState;
					form.ActivationInfo.SiteCountry = form.NewSite.SiteCountry;
					form.ActivationInfo.SiteZip = form.NewSite.SiteZip;
					form.ActivationInfo.SubmissionDate = DateTime.UtcNow;

					// #6 - Create permission for new site
					if (!form.PreApproved) {
						Models.Permission newSitePermission = new Models.Permission {
							Access = 1,
							SiteId = form.NewSite.SiteId,
							UserId = form.NewOwner.Id
						};

						context.Permissions.Add(newSitePermission);
					}

					// Since these objects aren't being created through their respective controllers, need to manually log them
					if (InstallerCreated)
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateInstaller, authUser.Id, form.SiteInstaller.Id,
							String.Format("{0} (id: {1}) created installer '{2}' (id: {3})", authUser.UserName, authUser.Id,
								form.SiteInstaller.FName + " " + form.SiteInstaller.LName, form.SiteInstaller.Id));
					if (ManagerCreated)
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateManager, authUser.Id, form.SiteManager.Id,
							String.Format("{0} (id: {1}) created manager '{2}' (id: {3})", authUser.UserName, authUser.Id,
								form.SiteManager.FName + " " + form.SiteManager.LName, form.SiteManager.Id));
					if (!form.PreApproved) {
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateUser, authUser.Id, form.NewOwner.Id,
							String.Format("{0} (id: {1}) created user {2} (id: {3})", authUser.UserName, authUser.Id,
								form.NewOwner.UserName, form.NewOwner.Id));
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateSite, authUser.Id, form.NewSite.SiteId,
							String.Format("{0} (id: {1}) created site '{2}' (site number: {3})", authUser.UserName, authUser.Id,
								form.NewSite.SiteName, form.NewSite.SiteNumber));
					}

					// #7 - Save all final info to database (all information has been verified by this point) //
					context.Activations.Add(form.ActivationInfo);
					context.SaveChanges();

					// #8 - Logging and alerting

					// log
					Helpers.LogHelper.LogAction(Models.Log.ActionType.ActivateSite, authUser.Id, form.NewSite.SiteId,
						String.Format("{0} (id: {1}) activated site '{2}' (site number: {3})", authUser.UserName, authUser.Id, form.NewSite.SiteName, form.NewSite.SiteNumber));

					// send to Zapier to handle proper notification
					var json = new {
						SiteId = form.NewSite.SiteNumber,
						form.NewSite.SiteName,
						SystemName = system.Name,
						form.ActivationInfo.SystemId,
						form.NewSite.StorePhone,
						form.SiteInstaller,
						form.SiteManager,
						NewOwner = new { form.NewOwner.Active, form.NewOwner.Email, form.NewOwner.FName, form.NewOwner.LName, form.NewOwner.Phone, form.NewOwner.UserName },
						SiteDistributor = new { distrib.Active, distrib.Email, distrib.FName, distrib.LName, distrib.Phone, distrib.UserName },
						form.NewSite.SiteAddress,
						form.NewSite.SiteCity,
						form.NewSite.SiteState,
						form.NewSite.SiteCountry,
						form.NewSite.SiteZip,
						SubmissionDate = form.ActivationInfo.SubmissionDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
						form.ActivationInfo.ActivationNotes
					};
					Helpers.LogHelper.NotifyAction(Models.Log.ActionType.ActivateSite, json);
				}
				return Json(new { status_code = 0, form.NewSite.SiteId });
			} else if (type == 1) {
				// Just create new activation. Site was already approved through old report portal.
				using (var context = new Data.ApplicationDbContext()) {
					// Verify user exists & has permission
					String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
					Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
					if (authUser == null)
						return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

					if (!Helpers.PermissionChecker.CanAddSite(authUser))
						return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to activate sites" });

					bool InstallerCreated = false, ManagerCreated = false, OwnerCreated = false;

					// #1 - Check installer //
					// if Id is 0, create new installer. Otherwise, verify installer exists
					if (form.SiteInstaller.Id != 0) {
						Models.Installer installer = context.Installers.AsNoTracking().FirstOrDefault(i => i.Id == form.SiteInstaller.Id);
						if (installer == null)
							return Json(new { status_code = 2, status = "Installer '" + form.SiteInstaller.Id + "' does not exist" });
					} else {
						if (form.SiteInstaller.FName.Length == 0 && form.SiteInstaller.LName.Length == 0)
							return Json(new { status_code = 4, status = "Installer must have at least a first or last name" });
						else if (form.SiteInstaller.Phone.Length == 0)
							return Json(new { status_code = 4, status = "Installer must have a phone number" });
						context.Installers.Add(form.SiteInstaller);
						InstallerCreated = true;
					}

					// #2 - Check manager //
					// Same logic as installer (0 = new, otherwise use existing)
					if (form.SiteManager.Id != 0) {
						Models.Manager manager = context.Managers.AsNoTracking().FirstOrDefault(m => m.Id == form.SiteManager.Id);
						if (manager == null)
							return Json(new { status_code = 2, status = "Manager '" + form.SiteManager.Id + "' does not exist" });
					} else {
						if (form.SiteManager.FName.Length == 0 && form.SiteManager.LName.Length == 0)
							return Json(new { status_code = 4, status = "Manager must have at least a first or last name" });
						else if (form.SiteManager.Phone.Length == 0)
							return Json(new { status_code = 4, status = "Manager must have a phone number" });
						context.Managers.Add(form.SiteManager);
						ManagerCreated = true;
					}

					// #3 - Verify new owner info //
					if (form.NewOwner.Id == 0) {
						if (context.Users.AsNoTracking().FirstOrDefault(u => form.NewOwner.UserName != null && u.UserName != null && form.NewOwner.UserName.ToLower().Equals(u.UserName.ToLower())) != null) {
							return Json(new { status_code = 3, status = "User '" + form.NewOwner.UserName + "' already exists" });
						}

						if (String.IsNullOrWhiteSpace(form.NewOwner.UserName) || String.IsNullOrWhiteSpace(form.NewOwner.Password))
							return Json(new { status_code = 4, status = "Invalid user creation body" });

						form.NewOwner.Active = true;
						form.NewOwner.Level = 2; // force user to be owner
						form.NewOwner.UserLastLogin = new DateTime(2000, 1, 1);
						OwnerCreated = true;
					} else {
						form.NewOwner = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == form.NewOwner.Id);
						if (form.NewOwner == null)
							return Json(new { status_code = 2, status = "Owner '" + form.NewOwner.Id + "' does not exist" });
					}

					// #4 - Create new site //

					// Verify site info
					Models.System system = context.Systems.AsNoTracking().FirstOrDefault(s => s.Id == form.NewSite.SystemId);
					if (system == null)
						return Json(new { status_code = 4, status = "Invalid system given", id = form.NewSite.SystemId });

					Models.User distrib = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == form.NewSite.SiteDistributor);
					if (distrib == null)
						return Json(new { status_code = 4, status = "Invalid distributor given" });

					if (form.NewSite.SiteName.Length == 0)
						return Json(new { status_code = 4, status = "No site name given" });
					if (!form.NewSite.SiteInstallDate.HasValue || form.NewSite.SiteInstallDate > DateTime.UtcNow)
						return Json(new { status_code = 4, status = "Invalid install date" });
					if (form.NewSite.SiteAddress.Length == 0 || form.NewSite.SiteCity.Length == 0 || form.NewSite.SiteState.Length == 0
						|| form.NewSite.SiteCountry.Length == 0 || form.NewSite.SiteZip.Length < 5 || form.NewSite.SiteZip.Length > 10)
						return Json(new { status_code = 4, status = "Invalid site address given" });
					if (form.NewSite.SiteOwnerName.Length == 0)
						return Json(new { status_code = 4, status = "No owner name given" });
					if (form.NewSite.SiteOwnerEmail.Length == 0 && form.NewSite.SiteOwnerPhone.Length == 0)
						return Json(new { status_code = 4, status = "Need at least one way to contact owner (none given)" });
					
					// Copy over any modified info if site is pre-approved
					if (form.PreApproved) {
						Models.Site currentSite = context.Sites.FirstOrDefault(s => s.SiteId == form.NewSite.SiteId);
						currentSite.Copy(form.NewSite, false);
						context.SaveChanges();
					}

					// #5 - New activation row //
					// Need to save these to database to generate any IDs (in case new installer/manager is used, and for new site/owner)
					if (form.NewOwner.Id == 0) {
						context.Users.Add(form.NewOwner);
						context.SaveChanges();
					}

					// Activation form needs to save what was submitted, not reflect current information
					// therefore, activation table essentially acts as a merged version of multiple tables
					form.ActivationInfo.SiteId = form.NewSite.SiteId;
					form.ActivationInfo.RoomName = form.NewSite.SiteName;
					form.ActivationInfo.SystemId = system.Id;
					form.ActivationInfo.StorePhone = form.NewSite.StorePhone;
					form.ActivationInfo.InstallerId = form.SiteInstaller.Id;
					form.ActivationInfo.InstallerFName = form.SiteInstaller.FName;
					form.ActivationInfo.InstallerLName = form.SiteInstaller.LName;
					form.ActivationInfo.InstallerEmail = form.SiteInstaller.Email;
					form.ActivationInfo.InstallerPhone = form.SiteInstaller.Phone;
					form.ActivationInfo.ManagerId = form.SiteManager.Id;
					form.ActivationInfo.ManagerFName = form.SiteManager.FName;
					form.ActivationInfo.ManagerLName = form.SiteManager.LName;
					form.ActivationInfo.ManagerEmail = form.SiteManager.Email;
					form.ActivationInfo.ManagerPhone = form.SiteManager.Phone;
					form.ActivationInfo.OwnerId = form.NewOwner.Id;
					form.ActivationInfo.OwnerFName = form.NewOwner.FName;
					form.ActivationInfo.OwnerLName = form.NewOwner.LName;
					form.ActivationInfo.OwnerEmail = form.NewOwner.Email;
					form.ActivationInfo.OwnerPhone = form.NewOwner.Phone;
					form.ActivationInfo.OwnerUserName = form.NewOwner.UserName;
					form.ActivationInfo.DistributorId = distrib.Id;
					form.ActivationInfo.DistributorFName = distrib.FName;
					form.ActivationInfo.DistributorLName = distrib.LName;
					form.ActivationInfo.SiteAddress = form.NewSite.SiteAddress;
					form.ActivationInfo.SiteCity = form.NewSite.SiteCity;
					form.ActivationInfo.SiteState = form.NewSite.SiteState;
					form.ActivationInfo.SiteCountry = form.NewSite.SiteCountry;
					form.ActivationInfo.SiteZip = form.NewSite.SiteZip;
					form.ActivationInfo.SubmissionDate = DateTime.UtcNow;

					// #6 - Create permission for new site
					Models.Permission sitePermission = context.Permissions.AsNoTracking().FirstOrDefault(p => p.UserId == form.NewOwner.Id && p.SiteId == form.NewSite.SiteId);
					if (sitePermission == null) {
						sitePermission = new Models.Permission {
							Access = 1,
							SiteId = form.NewSite.SiteId,
							UserId = form.NewOwner.Id
						};

						context.Permissions.Add(sitePermission);
					}

					// Since these objects aren't being created through their respective controllers, need to manually log them
					if (InstallerCreated)
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateInstaller, authUser.Id, form.SiteInstaller.Id,
							String.Format("{0} (id: {1}) created installer '{2}' (id: {3})", authUser.UserName, authUser.Id,
								form.SiteInstaller.FName + " " + form.SiteInstaller.LName, form.SiteInstaller.Id));
					if (ManagerCreated)
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateManager, authUser.Id, form.SiteManager.Id,
							String.Format("{0} (id: {1}) created manager '{2}' (id: {3})", authUser.UserName, authUser.Id,
								form.SiteManager.FName + " " + form.SiteManager.LName, form.SiteManager.Id));
					if (OwnerCreated)
						Helpers.LogHelper.LogAction(Models.Log.ActionType.CreateUser, authUser.Id, form.NewOwner.Id,
							String.Format("{0} (id: {1}) created user {2} (id: {3})", authUser.UserName, authUser.Id,
								form.NewOwner.UserName, form.NewOwner.Id));

					// #7 - Save all final info to database (all information has been verified by this point) //

					// check if form is pre-approved thru old RP
					if (form.PreApproved) {
						form.ActivationInfo.ApprovedBy = -1;
						form.ActivationInfo.ApprovalNotes = "This site was pre-approved through old report portal.";
					}

					context.Activations.Add(form.ActivationInfo);
					context.SaveChanges();

					// #8 - Logging and alerting

					// log
					Helpers.LogHelper.LogAction(Models.Log.ActionType.ActivateSite, authUser.Id, form.NewSite.SiteId,
						String.Format("{0} (id: {1}) reactivated site '{2}' (site number: {3})", authUser.UserName, authUser.Id, form.NewSite.SiteName, form.NewSite.SiteNumber));

					// send to Zapier to handle proper notification
					var json = new {
						SiteId = form.NewSite.SiteNumber,
						form.NewSite.SiteName,
						SystemName = system.Name,
						form.ActivationInfo.SystemId,
						form.NewSite.StorePhone,
						form.SiteInstaller,
						form.SiteManager,
						NewOwner = new { form.NewOwner.Active, form.NewOwner.Email, form.NewOwner.FName, form.NewOwner.LName, form.NewOwner.Phone, form.NewOwner.UserName },
						SiteDistributor = new { distrib.Active, distrib.Email, distrib.FName, distrib.LName, distrib.Phone, distrib.UserName },
						form.NewSite.SiteAddress,
						form.NewSite.SiteCity,
						form.NewSite.SiteState,
						form.NewSite.SiteCountry,
						form.NewSite.SiteZip,
						SubmissionDate = form.ActivationInfo.SubmissionDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
						form.ActivationInfo.ActivationNotes
					};
					Helpers.LogHelper.NotifyAction(Models.Log.ActionType.ActivateSite, json);
				}
				return Json(new { status_code = 0 });
			} else {
				return Json(new { status_code = 5, status = "Unknown status code '" + type + "'" });
			}
		}

		[HttpDelete("{formId}"), Authorize]
		public JsonResult Delete(int formId) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (!Helpers.PermissionChecker.CanDeleteSite(authUser))
					return Json(new { status_code = 1, status = "User '" + authUser.UserName + "' does not have permission to delete site(s)" });

				Models.Activation activation = context.Activations.FirstOrDefault(a => a.ActivationId == formId);
				if (activation == null)
					return Json(new { status_code = 2, status = "Activation with ID '" + formId + "' does not exist" });

				int siteId = activation.SiteId;

				context.Activations.Remove(activation);

				bool deletedSite = false;
				string siteName = "";
				List<Models.Activation> activations = context.Activations.AsNoTracking().Where(a => a.SiteId == siteId).ToList();
				if (activations.Count == 0) {
					Models.Site site = context.Sites.FirstOrDefault(s => s.SiteId == siteId);
					siteName = site.SiteName;
					context.Sites.Remove(site);
					deletedSite = true;
				}

				context.SaveChanges();

				Helpers.LogHelper.LogAction(Models.Log.ActionType.DeleteActivation, authUser.Id, formId,
					String.Format("{0} (id: {1}) deleted activation '{2}'", authUser.UserName, authUser.Id, formId));
				if (deletedSite)
					Helpers.LogHelper.LogAction(Models.Log.ActionType.DeleteSite, authUser.Id, formId,
						String.Format("{0} (id: {1}) deleted site '{2}'", authUser.UserName, authUser.Id, siteName));
			}

			return Json(new { status_code = 0 });
		}
	}
}