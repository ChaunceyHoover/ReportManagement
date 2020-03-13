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
	/// Controller that handles all actions relating to a player
	/// </summary>
	/// <remarks>A `user` is a user on report portal, while a `player` is a user in one of the various game rooms</remarks>
	[Route("api/players"), Produces("application/json")]
	public class PlayersController : Controller {
		/// <summary>
		/// Generates a list of all players for the current report portal (note: players != users)
		/// </summary>
		/// <returns>A list of all players</returns>
		[HttpGet, Authorize]
		public JsonResult ListAllPlayers() {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				return Json(new { status_code = 0, report = context.Players.AsNoTracking().ToList() });
			}
		}

		/// <summary>
		/// Returns either specific player info, or all players for a given site
		/// 
		/// Status codes
		/// -------------
		/// 0 = Individual player
		/// 1 = All players for a given site
		/// </summary>
		/// <param name="id">The ID of the player</param>
		/// <returns>The row for the given player</returns>
		[HttpGet("{id}")]
		public JsonResult GetPlayer(int id, [RequiredFromQuery]int status) {
			using (var context = new Data.ApplicationDbContext()) {
				String authUserId = User.GetClaim(OpenIdConnectConstants.Claims.Subject);
				Models.User authUser = context.Users.AsNoTracking().FirstOrDefault(u => u.Id.ToString().Equals(authUserId));
				if (authUser == null)
					return Json(new { status_code = 2, status = "User '" + authUserId + "' does not exist" });

				if (status == 0) {
					Models.Player player = context.Players.AsNoTracking().FirstOrDefault(p => p.CardId == id);
					if (player == null)
						return Json(new { status_code = 2, status = "Player '" + id + "' does not exist" });
					return Json(new { status_code = 0, player });
				} else if (status == 1) {
					return Json(new { status_code = 0, report = context.Players.AsNoTracking().Where(p => p.SiteId == id && !String.IsNullOrWhiteSpace(p.PersonalId)).ToList() });
				} else {
					return Json(new { status_code = -1 });
				}
			}
		}
	}
}
