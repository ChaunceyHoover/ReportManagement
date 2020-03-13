using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models {
	/// <summary>
	/// <see cref="Ticket"/> table; mapped in <see cref="Data.ApplicationDbContext"/> from the SQL server.
	/// </summary>
	public class Ticket {

		/// <summary>Enum constants for all possible ticket priorty levels</summary>
		public enum PriortiyLevel {
			LOW = 0, MEDIUM = 1,
			HIGH = 2, VERY_HIGH = 3
		}

		/// <summary>All possible status types for a ticket</summary>
		public enum TicketStatus {
			NEW = 0, OPEN = 1, AWAITING = 2,
			IN_PROGRESS = 3, CLOSED = 4
		}

		/// <summary>All possible categories for a ticket to be listed as</summary>
		public enum TicketCategory {
			HARDWARE = 0,
			SOFTWARE = 1,
			NETWORK = 2,
			REVERSAL = 3,
			MANUAL_ADJUSTMENT = 4,
			REPORT_PORTAL = 5,
			ACTIVATION = 6
		}

		/// <summary>Primary key of the <see cref="Ticket"/> table.</summary>
		/// <remarks>Generated in database and cannot be set or changed pragmatically.</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("id")]
		public int Id { get; set; }

		/// <summary>The internal ID of the site that the ticket is assigned to</summary>
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>The subject/title of the created ticket</summary>
		[Column("subject")]
		public String Subject { get; set; }

		/// <summary>Additional info about the given ticket</summary>
		[Column("comments")]
		public String Comments { get; set; }

		/// <summary>The internal ID of the user the ticket is assigned to</summary>
		/// <remarks>Null/0 until assigned to a user</remarks>
		[Column("assigned_to")]
		public int? AssignedTo { get; set; }

		/// <summary>The internal ID of the user who created the ticket</summary>
		[Column("created_by")]
		public int CreatedBy { get; set; }

		[Column("created_date")]
		public DateTime CreatedDate { get; set; }

		/// <summary>The priority level of the ticket.</summary>
		/// <remarks>See <see cref="PriortiyLevel"/> for possible levels.</remarks>
		[Column("priority")]
		public PriortiyLevel Priority { get; set; }

		/// <summary>The category that the ticket relates to</summary>
		[Column("status")]
		public TicketStatus Status { get; set; }

		/// <summary>The general-purpose category the ticket relates to</summary>
		[Column("category")]
		public TicketCategory Category { get; set; }

		/// <summary>(Optional)The date the ticket must be completed</summary>
		[Column("due_date")]
		public DateTime? DueDate { get; set; }

		/// <summary>The last time the ticket was updated</summary>
		/// <remarks>Note: Set programatically</remarks>
		[Column("last_updated")]
		public DateTime? LastUpdated { get; set; }

		/// <summary>
		/// Copies the given ticket's data to this ticket object
		/// </summary>
		/// <param name="other">The other ticket object to copy data from</param>
		public void Copy(Ticket other) {
			PropertyInfo[] pi = this.GetType().GetProperties();
			foreach (PropertyInfo p in pi) {
				if (!p.Name.Equals("Id")) {
					p.SetValue(this, p.GetValue(other));
				}
			}
		}
	}
}
