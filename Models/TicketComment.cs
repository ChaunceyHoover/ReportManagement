using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models {
	/// <summary>
	/// <see cref="TicketComment"/> table; mapped in <see cref="Data.ApplicationDbContext"/> from the SQL server.
	/// </summary>
	public class TicketComment {

		/// <summary>Primary key of the <see cref="TicketComment"/> table.</summary>
		/// <remarks>Generated in database and cannot be set or changed pragmatically.</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("id")]
		public int Id { get; set; }

		/// <summary>
		/// The ticket that the comment is linked to
		/// </summary>
		[Column("ticket_id")]
		public int TicketId { get; set; }

		/// <summary>
		/// The Id of the user who posted the comment
		/// </summary>
		[Column("poster")]
		public int Poster { get; set; }

		/// <summary>
		/// The comment made by the poster
		/// </summary>
		[Column("comment")]
		public string Comment { get; set; }

		/// <summary>
		/// The time the comment was submitted
		/// </summary>
		[Column("time")]
		public DateTime Time { get; set; }
	}
}
