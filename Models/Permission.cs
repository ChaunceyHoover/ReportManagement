using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// A permission system, used to give a user access to a certain site if they are not authorized to see all site reports
	/// </summary>
	public class Permission {
		/// <summary>The site's ID to access</summary>
		[Key]
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>The user's ID who will have permission</summary>
		[Key]
		[Column("user_id")]
		public int UserId { get; set; }

		/// <summary>If 0, the user does not have permission to a site. Otherwise, user has permission</summary>
		/// <remarks>This should just be a BIT value, or removed all together. Will change in future.</remarks>
		[Column("permission")]
		public int? Access { get; set; }
	}
}
