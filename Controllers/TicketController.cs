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
	[Route("api/ticket"), Produces("application/json")]
	public class TicketController : Controller {
		/// <summary>
		/// Creates an anonymous object with all info relating to a ticket, instead of just the essential information.
		/// 
		/// Normally, tickets only have ID values and not string values because they are used internally and can be
		/// converted on the server as needed, but the client side doesn't have direct access to all the internal data
		/// the server does (unless accessed via multiple routes, which would waste time). 
		/// 
		/// tl;dr This method just gives info the client would need about a ticket.
		/// </summary>
		/// <remarks>Assumes ticket is valid</remarks>
		/// <param name="ticket">The ticket to pull all information from</param>
		/// <returns>An anonymous object with extra information about a given ticket</returns>
		private object FullTicketInfo(Models.Ticket ticket) {
			using (var context = new Data.ApplicationDbContext()) {
				Models.Site siteTicket = context.Sites.AsNoTracking().FirstOrDefault(s => s.SiteId == ticket.SiteId);
				Models.User ticketCreator = context.Users.AsNoTracking().FirstOrDefault(c => c.Id == ticket.CreatedBy);
				Models.User ticketAssignee = null;
				if (ticket.AssignedTo.HasValue)
					ticketAssignee = context.Users.AsNoTracking().FirstOrDefault(a => a.Id == ticket.AssignedTo);

				return new {
					Id = ticket.Id,
					SiteId = ticket.SiteId,
					Subject = ticket.Subject,
					Comments = ticket.Comments,
					Priority = (int)ticket.Priority,
					PriorityName = ticket.Priority.ToString().Replace('_', ' '),
					Status = (int)ticket.Status,
					StatusName = ticket.Status.ToString().Replace('_', ' '),
					Category = (int)ticket.Category,
					CategoryName = ticket.Category.ToString().Replace('_', ' '),
					SiteName = siteTicket != null ? siteTicket.SiteName : "",
					AssignedTo = ticket.AssignedTo,
					CreatedBy = ticket.CreatedBy,
					CreatedDate = ticket.CreatedDate,
					Creator = ticketCreator != null ?
						new {
							UserName = ticketCreator.UserName,
							FName = ticketCreator.FName,
							LName = ticketCreator.LName
						} : null,
					Assignee = ticketAssignee != null ?
						new {
							UserName = ticketAssignee.UserName,
							FName = ticketAssignee.FName,
							LName = ticketAssignee.LName
						} : null,
					DueDate = ticket.DueDate,
					LastUpdated = ticket.LastUpdated
				};
			}
		}

		/// <summary>
		/// Gets a list of tickets. The list generated depends on the status given.
		/// 
		/// 0 = All tickets
		/// 1 = All open tickets
		/// 2 = All tickets for a given site
		/// </summary>
		/// <param name="status">The status code to determine which list of tickets are returned</param>
		/// <param name="id">(Only needed for status = 2)The site to get all tickets from</param>
		/// <returns>A list of tickets</returns>
		private JsonResult GetAllTickets(int status, int id, Models.User authUser) {
			using (var context = new Data.ApplicationDbContext()) {
				if (status == 0) {
					// Get ALL tickets
					// Get list of sites user has permission to view, then return list of tickets from those sites
					List<object> report = new List<object>();
#if LOCAL
					using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("LocalDatabase"))) {
#else
					using (MySqlConnection conn = new MySqlConnection(Startup.Configuration.GetConnectionString("Database"))) {
#endif
						conn.Open();
						string command = (authUser.Level >= 4 ?
											"SELECT * FROM `tickets` as t " +
											"JOIN(SELECT user_id as u_user_id, user_name as u_user_name, contact_fname as u_contact_fname, contact_lname as u_contact_lname FROM `user`) as u " +
											"ON u_user_id = t.created_by " +
											"LEFT JOIN(SELECT user_id as a_user_id, user_name as a_user_name, contact_fname as a_contact_fname, contact_lname as a_contact_lname FROM `user`) as a " +
											"ON a_user_id = t.assigned_to " +
											"JOIN(SELECT site_name, site_id FROM `sites`) as s " +
											"ON t.site_id = s.site_id;"
										:
											String.Format(
												"SELECT * FROM `tickets` as t " +
												"JOIN " +
												"	(SELECT " +
												"		user_id as u_user_id, user_name as u_user_name, contact_fname as u_contact_fname, contact_lname as u_contact_lname " +
												"	 FROM `user`) as u " +
												"ON u_user_id = t.created_by " +
												"LEFT JOIN " +
												"	(SELECT " +
												"		user_id as a_user_id, user_name as a_user_name, contact_fname as a_contact_fname, contact_lname as a_contact_lname " +
												"	 FROM `user`) as a " +
												"ON a_user_id = t.assigned_to " +
												"JOIN " +
												"	(SELECT " +
												"		s.site_id, s.site_name " +
												"	 FROM `sites` as s " +
												"	 JOIN " +
												"		(SELECT * FROM `permission` " +
												"		 WHERE `user_id` = {0}) as p " +
												"	 ON s.site_id = p.site_id) as sp " +
												"ON sp.site_id = t.site_id;", authUser.Id));
						MySqlCommand ticketCmd = new MySqlCommand(command, conn);
						using (var reader = ticketCmd.ExecuteReader())
							while (reader.Read()) {
								Models.Ticket.PriortiyLevel priority;
								Models.Ticket.TicketStatus tStatus;
								Models.Ticket.TicketCategory tCategory;
								int ticketId, ticketSiteId;
								string ticketSubject, ticketComments;

								ticketId = DBNull.Value.Equals(reader["id"]) ? -1 : Convert.ToInt32(reader["id"]);
								ticketSiteId = DBNull.Value.Equals(reader["site_id"]) ? -1 : Convert.ToInt32(reader["site_id"]);
								ticketSubject = DBNull.Value.Equals(reader["subject"]) ? "" : Convert.ToString(reader["subject"]);
								ticketComments = DBNull.Value.Equals(reader["comments"]) ? "" : Convert.ToString(reader["comments"]);

								// Something is wrong with this ticket, so ignore it & don't add it to list
								if (ticketId == -1 || ticketSiteId == -1 || ticketSubject.Length == 0 || ticketComments.Length == 0)
									continue;

								if (!DBNull.Value.Equals(reader["priority"]) && Convert.ToInt32(reader["priority"]) >= 0 && Convert.ToInt32(reader["priority"]) <= 3)
									priority = (Models.Ticket.PriortiyLevel)Convert.ToInt32(reader["priority"]);
								else
									priority = Models.Ticket.PriortiyLevel.LOW;

								if (!DBNull.Value.Equals(reader["status"]) && Convert.ToInt32(reader["status"]) >= 0 && Convert.ToInt32(reader["status"]) <= 4)
									tStatus = (Models.Ticket.TicketStatus)Convert.ToInt32(reader["status"]);
								else
									tStatus = Models.Ticket.TicketStatus.NEW;

								if (!DBNull.Value.Equals(reader["category"]) && Convert.ToInt32(reader["category"]) >= 0 && Convert.ToInt32(reader["category"]) <= 6)
									tCategory = (Models.Ticket.TicketCategory)Convert.ToInt32(reader["category"]);
								else
									tCategory = Models.Ticket.TicketCategory.SOFTWARE;

								var ticket = new {
									Id = ticketId,
									SiteId = ticketSiteId,
									Subject = ticketSubject,
									Comments = ticketComments,
									Priority = (int)priority,
									PriorityName = priority.ToString(),
									Status = (int)tStatus,
									StatusName = tStatus.ToString(),
									Category = (int)tCategory,
									CategoryName = tCategory.ToString().Replace('_', ' '),
									SiteName = DBNull.Value.Equals(reader["site_name"]) ? "" : Convert.ToString(reader["site_name"]),
									AssignedTo = DBNull.Value.Equals(reader["assigned_to"]) ? -1 : Convert.ToInt32(reader["assigned_to"]),
									CreatedBy = DBNull.Value.Equals(reader["created_by"]) ? -1 : Convert.ToInt32(reader["created_by"]),
									CreatedDate = DBNull.Value.Equals(reader["created_date"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["created_date"]),
									Creator = new {
										UserName = DBNull.Value.Equals(reader["u_user_name"]) ? "" : Convert.ToString(reader["u_user_name"]),
										FName = DBNull.Value.Equals(reader["u_contact_fname"]) ? "" : Convert.ToString(reader["u_contact_fname"]),
										LName = DBNull.Value.Equals(reader["u_contact_lname"]) ? "" : Convert.ToString(reader["u_contact_lname"])
									},
									Assignee = new {
										UserName = DBNull.Value.Equals(reader["a_user_name"]) ? "" : Convert.ToString(reader["a_user_name"]),
										FName = DBNull.Value.Equals(reader["a_contact_fname"]) ? "" : Convert.ToString(reader["a_contact_fname"]),
										LName = DBNull.Value.Equals(reader["a_contact_lname"]) ? "" : Convert.ToString(reader["a_contact_lname"])
									},
									DueDate = DBNull.Value.Equals(reader["due_date"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["due_date"]),
									LastUpdated = DBNull.Value.Equals(reader["last_updated"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["last_updated"])
								};

								report.Add(ticket);
							}
						return Json(new { status_code = 0, report });
					}
				} else if (status == 1) {
					// Get all open tickets
					if (authUser.Level >= 4)
						return Json(new { status_code = 0, report = context.Tickets.AsNoTracking().Where(t => t.Status != Models.Ticket.TicketStatus.CLOSED).ToList() });
					else {
						// Get list of sites user has permission to view, then return list of open tickets from those sites
						List<object> report = new List<object>();
						MySqlCommand ticketCmd = new MySqlCommand(
							String.Format("SELECT * " +
											"FROM " +
											"	`tickets` as t " +
											"	JOIN " +
											"		(SELECT " +
											"				s.site_id, s.site_name " +
											"			FROM " +
											"				`sites` as s " +
											"				JOIN " +
											"					(SELECT " +
											"							* " +
											"						FROM " +
											"							`permission`  " +
											"						WHERE " +
											"							`user_id` = {0}) " +
											"					as p " +
											"					ON s.site_id = p.site_id) " +
											"		as sp " +
											"		ON sp.site_id = t.site_id AND t.status != 4; ", authUser.Id));
						using (var reader = ticketCmd.ExecuteReader())
							while (reader.Read()) {
								Models.Ticket.PriortiyLevel priority;
								Models.Ticket.TicketStatus tStatus;
								Models.Ticket.TicketCategory tCategory;
								int ticketId, ticketSiteId;
								string ticketSubject, ticketComments;

								ticketId = DBNull.Value.Equals(reader["id"]) ? -1 : Convert.ToInt32(reader["id"]);
								ticketSiteId = DBNull.Value.Equals(reader["site_id"]) ? -1 : Convert.ToInt32(reader["site_id"]);
								ticketSubject = DBNull.Value.Equals(reader["subject"]) ? "" : Convert.ToString(reader["subject"]);
								ticketComments = DBNull.Value.Equals(reader["comments"]) ? "" : Convert.ToString(reader["comments"]);

								// Something is wrong with this ticket, so ignore it & don't add it to list
								if (ticketId == -1 || ticketSiteId == -1 || ticketSubject.Length == 0 || ticketComments.Length == 0)
									continue;

								if (!DBNull.Value.Equals(reader["priority"]) && Convert.ToInt32(reader["priority"]) >= 0 && Convert.ToInt32(reader["priority"]) <= 3)
									priority = (Models.Ticket.PriortiyLevel)Convert.ToInt32(reader["priority"]);
								else
									priority = Models.Ticket.PriortiyLevel.LOW;

								if (!DBNull.Value.Equals(reader["status"]) && Convert.ToInt32(reader["status"]) >= 0 && Convert.ToInt32(reader["status"]) <= 4)
									tStatus = (Models.Ticket.TicketStatus)Convert.ToInt32(reader["status"]);
								else
									tStatus = Models.Ticket.TicketStatus.NEW;

								if (!DBNull.Value.Equals(reader["category"]) && Convert.ToInt32(reader["category"]) >= 0 && Convert.ToInt32(reader["category"]) <= 6)
									tCategory = (Models.Ticket.TicketCategory)Convert.ToInt32(reader["category"]);
								else
									tCategory = Models.Ticket.TicketCategory.SOFTWARE;

								var ticket = new {
									Id = ticketId,
									SiteId = ticketSiteId,
									Subject = ticketSubject,
									Comments = ticketComments,
									Priority = (int)priority,
									PriorityName = priority.ToString(),
									Category = (int)tCategory,
									CategoryName = tCategory.ToString().Replace('_', ' '),
									Status = (int)tStatus,
									StatusName = tStatus.ToString(),
									SiteName = DBNull.Value.Equals(reader["site_name"]) ? "" : Convert.ToString(reader["site_name"]),
									AssignedTo = DBNull.Value.Equals(reader["assigned_to"]) ? -1 : Convert.ToInt32(reader["assigned_to"]),
									CreatedBy = DBNull.Value.Equals(reader["created_by"]) ? -1 : Convert.ToInt32(reader["created_by"]),
									CreatedDate = DBNull.Value.Equals(reader["created_date"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["created_date"]),
									DueDate = DBNull.Value.Equals(reader["due_date"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["due_date"]),
									LastUpdated = DBNull.Value.Equals(reader["last_updated"]) ? new DateTime(2000, 1, 1) : Convert.ToDateTime(reader["last_updated"])
								};

								report.Add(ticket);
							}

						return Json(new { status_code = 0, report });
					}
				} else if (status == 2) {
					if (authUser.Level >= 4)
						return Json(new { status_code = 0, report = context.Tickets.AsNoTracking().Where(t => t.SiteId == id).ToList() });
					else {
						Models.Permission permission = context.Permissions.AsNoTracking().FirstOrDefault(p => p.UserId == authUser.Id && p.SiteId == id);
						if (permission == null || permission.Access == 0)
							return Json(new { status_code = 1, status = "User does not have permission to view this ticket" });
						return Json(new { status_code = 0, report = context.Tickets.AsNoTracking().Where(t => t.SiteId == id).ToList() });
					}
				}
			}

			// If this return statement is reached, request has invalid status type
			return Json(new { status_code = 5, status = "Unknown status code: " + status });
		}

		/// <summary>
		/// Gets an individual ticket with the given internal ID
		/// </summary>
		/// <param name="id">Internal ID of ticket</param>
		/// <returns>The ticket (if exists) with given internal ID</returns>
		[HttpGet("{id}"), Authorize]
		public JsonResult GetTicket(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				Models.Ticket ticket = context.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == id);
				if (ticket == null)
					return Json(new { status_code = 2, status = "Ticket '" + id + "' does not exist" });
				if (authUser.Level >= 4)
					return Json(new { status_code = 0, ticket });
				else {
					Models.Permission permission = context.Permissions.AsNoTracking().FirstOrDefault(p => p.UserId == authUser.Id && p.SiteId == id);
					if (permission == null || permission.Access == 0)
						return Json(new { status_code = 1, status = "User does not have permission to view this ticket" });
					return Json(new { status_code = 0, ticket });
				}
			}
		}

		/// <summary>
		/// Because there are so many different status codes for GET requests, this method simply routes requests to another method.
		/// 
		/// See each method for more information about what each does.
		/// </summary>
		/// <param name="status">Status code to dictate what request is asking for</param>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet, Authorize]
		public JsonResult Get([RequiredFromQuery]int status, [FromQuery]int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				switch (status) {
					case 0:
					case 1:
					case 2:
						return GetAllTickets(status, id, authUser);
					default:
						return Json(new { status_code = 5, status = "Unknown status code '" + status + "'" });
				}
			}
		}

		/// <summary>
		/// Creates a new ticket with the given information
		/// </summary>
		/// <param name="ticket">The ticket to create</param>
		/// <returns>Status code based off request being ran</returns>
		[HttpPut, Authorize]
		public JsonResult CreateTicket([FromBody]Models.Ticket ticket) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (ticket.Comments.Length == 0)
					return Json(new { status_code = 4, status = "User must enter ticket comments" });
				else if (ticket.Priority < 0 || (int)ticket.Priority > 3)
					ticket.Priority = Models.Ticket.PriortiyLevel.LOW;
				else if (ticket.Status < 0 || (int)ticket.Status > 4)
					ticket.Status = Models.Ticket.TicketStatus.NEW;
				else if (ticket.SiteId <= 0)
					return Json(new { status_code = 4, status = "Ticket must be assigned to a site" });

				ticket.CreatedBy = authUser.Id;
				ticket.CreatedDate = DateTime.UtcNow;

				if (ticket.AssignedTo.HasValue)
					ticket.Status = Models.Ticket.TicketStatus.OPEN;

				context.Tickets.Add(ticket);
				context.SaveChanges();
			}
			return Json(new { status_code = 0, ticket = FullTicketInfo(ticket) });
		}

		/// <summary>
		/// Updates a given ticket
		/// </summary>
		/// <param name="ticket">The ticket to update</param>
		/// <remarks>Ticket Id MUST be the Id of the ticket you want to update</remarks>
		/// <returns>Status code of request being ran</returns>
		[HttpPost, Authorize]
		public JsonResult UpdateTicket([FromBody]Models.Ticket ticket) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				Models.Ticket dbTicket = context.Tickets.FirstOrDefault(t => t.Id == ticket.Id);

				if (dbTicket == null)
					return Json(new { status_code = 2, status = "Ticket '" + ticket.Id + "' does not exist" });
				else if (ticket.Comments.Length == 0)
					return Json(new { status_code = 4, status = "User must enter ticket comments" });
				else if (ticket.Priority < 0 || (int)ticket.Priority > 3)
					return Json(new { status_code = 4, status = "Invalid ticket priority" });
				else if (ticket.Status < 0 || (int)ticket.Status > 4)
					return Json(new { status_code = 4, status = "Invalid ticket status" });

				if (ticket.Status == Models.Ticket.TicketStatus.NEW && ticket.AssignedTo.HasValue)
					ticket.Status = Models.Ticket.TicketStatus.OPEN;

				dbTicket.LastUpdated = DateTime.UtcNow;
				dbTicket.Copy(ticket);
				context.SaveChanges();
			}

			return Json(new { status_code = 0, ticket = FullTicketInfo(ticket) });
		}

		/// <summary>
		/// Deletes a ticket with the given internal Id
		/// </summary>
		/// <remarks>You can only delete a ticket if you are: the ticket creator, the ticket assignee, or level 5+ user</remarks>
		/// <param name="id">Internal ticket Id</param>
		/// <returns>Status code of deletion</returns>
		[HttpDelete("{id}"), Authorize]
		public JsonResult DeleteTicket(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				Models.Ticket ticket = context.Tickets.FirstOrDefault(t => t.Id == id);
				if (ticket == null)
					return Json(new { status_code = 2, status = "Ticket with Id '" + id + "' does not exist" });

				context.Tickets.Remove(ticket);
				context.SaveChanges();

				return Json(new { status_code = 0 });
			}
		}
	}
}
