using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// Model that represents a system, such as STRYKER or SCARLETT
	/// </summary>
	public class System {
		/// <summary>The system's unique ID</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("system_id")]
		public int Id { get; set; }

		/// <summary>The name of the system</summary>
		[Column("system_name")]
		public String Name { get; set; }

		/// <summary>The base URL for each system</summary>
		[Column("url")]
		public String Url { get; set; }

		/// <summary>The prefix to add to each site number</summary>
		/// <remarks>Only set in database - does not change from this application</remarks>
		[Column("system_prefix")]
		public String Prefix { get; set; }
	}
}
