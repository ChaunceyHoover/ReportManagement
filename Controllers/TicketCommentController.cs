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
	[Route("api/comments"), Produces("application/json")]
	public class TicketCommentController : Controller {
		private object FormatTicketComment(Models.TicketComment comment) {
			using (var context = new Data.ApplicationDbContext()) {
				Models.User poster = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == comment.Poster);
				return new {
					comment.Comment, comment.Id, PosterId = comment.Poster, comment.TicketId, comment.Time,
					Poster = new {
						poster.UserName, poster.FName, poster.LName, Name = poster.FName + " " + poster.LName
					}
				};
			}
		}

		/// <summary>
		/// Gets all comments for a ticket
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}")]
		public JsonResult GetComments(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				List<object> report = new List<object>();
				foreach (Models.TicketComment comment in context.TicketComments.AsNoTracking().Where(t => t.TicketId == id).ToList()) {
					report.Add(FormatTicketComment(comment));
				}

				return Json(new { status_code = 0, report });
			}
		}

		[HttpPost]
		public JsonResult CreateComment([FromBody]Models.TicketComment ticketComment) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				// Validate ticket info
				if (ticketComment.Comment.Length == 0)
					return Json(new { status_code = 4, status = "No comment(s) given" });

				Models.Ticket ticket = context.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticketComment.TicketId);
				if (ticket == null)
					return Json(new { status_code = 2, status = "Ticket '" + ticketComment.TicketId + "' does not exist" });

				Models.User poster = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == ticketComment.Poster);
				if (poster == null)
					return Json(new { status_code = 2, status = "User '" + ticketComment.Poster + "' does not exist" });

				ticketComment.Time = DateTime.UtcNow;

				context.TicketComments.Add(ticketComment);
				context.SaveChanges();

				return Json(new { status_code = 0, ticketComment = FormatTicketComment(ticketComment) });
			}
		}

		[HttpDelete("{id}")]
		public JsonResult DeleteComment(int id) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				Models.TicketComment comment = context.TicketComments.FirstOrDefault(c => c.Id == id);
				if (comment == null)
					return Json(new { status_code = 2, status = "Ticket '" + id + "' does not exist" });

				context.TicketComments.Remove(comment);
				context.SaveChanges();

				return Json(new { status_code = 0 });
			}
		}
	}
}
