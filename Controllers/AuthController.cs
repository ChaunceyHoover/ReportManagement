using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace ReportPortal.Controllers {
	[Route("api/auth"), Produces("application/json")]
	public class AuthController : Controller {
		/// <summary>Decodes the authorization token</summary>
		/// <returns>The claims inside of the token, separated as a JSON object</returns>
		/// <remarks>
		/// GET /api/auth
		/// Headers
		///	 Authorization: Bearer {auth_token}
		/// </remarks>
		[HttpGet, Authorize]
		public JsonResult CheckToken() {
			using (var context = new Data.ApplicationDbContext()) {
				Models.User user = context.Users.FirstOrDefault(u => u.Id == Int32.Parse(User.GetClaim(OpenIdConnectConstants.Claims.Subject)));
				if (user == null) {
					return Json(null);
				}

				return Json(new {
					Subject = User.GetClaim(OpenIdConnectConstants.Claims.Subject),
					Name = user.UserName,
					user.Level,
					user.Active
				});
			}
		}

		/// <summary>Authenticates the user and gives an expirable token</summary>
		/// <param name="request">Requires username, password, and grant_type in the header</param>
		/// <returns>A bearer token</returns>
		/// <remarks>
		/// POST /api/auth/token
		/// Headers
		///	 Content-Type: application/x-www-form-urlencoded
		/// Form data for x-www-form-urlencoded parameters
		///	 username=
		///	 password=
		///	 grant_type=password</remarks>
		[HttpPost, Route("token")]
		public IActionResult CreateToken(OpenIdConnectRequest request) {
			if (!request.IsPasswordGrantType()) {
				return BadRequest(new OpenIdConnectResponse {
					Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
					ErrorDescription = "The specified grant type is not supported."
				});
			}

			// Auto-renews token so it won't expire
			if (User != null && User.Identity.IsAuthenticated) {
				return SignIn(User, OpenIdConnectServerDefaults.AuthenticationScheme);
			}

			using (var context = new Data.ApplicationDbContext()) {
				Models.User user = context.Users.FirstOrDefault(u => u.UserName.ToLower() == request.Username.ToLower() && request.Password.Equals(u.Password));
				if (user == null) {
					return BadRequest(new OpenIdConnectResponse {
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "Invalid username or password"
					});
				} else if (user.Active != true) {
					return BadRequest(new OpenIdConnectResponse {
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "User not active"
					});
				}

				var identity = new ClaimsIdentity(OpenIdConnectServerDefaults.AuthenticationScheme,
					OpenIdConnectConstants.Claims.Name,
					OpenIdConnectConstants.Claims.Role);
				identity.AddClaim(OpenIdConnectConstants.Claims.Subject,
					user.Id.ToString(),
					OpenIdConnectConstants.Destinations.AccessToken);
				identity.AddClaim(OpenIdConnectConstants.Claims.Name, user.UserName,
					OpenIdConnectConstants.Destinations.AccessToken);
				identity.AddClaim(OpenIdConnectConstants.Claims.Role, user.Level.HasValue ? user.Level.ToString() : "1",
					OpenIdConnectConstants.Destinations.AccessToken);

				var principal = new ClaimsPrincipal(identity);
				return SignIn(principal, OpenIdConnectServerDefaults.AuthenticationScheme);
			}

			throw new InvalidOperationException("The specified grant type is not supported.");
		}
	}
}
