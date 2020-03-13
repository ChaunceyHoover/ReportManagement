using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	public class CommunityTable {
		/// <summary>
		/// Card ID
		/// </summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("card_id")]
		public int CardId { get; set; }

		/// <summary>
		/// Site ID
		/// </summary>
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>
		/// Event time
		/// </summary>
		[Column("event_time")]
		public DateTime EventTime { get; set; }

		/// <summary>
		/// Money won in cents
		/// </summary>
		[Column("cents_won")]
		public int CentsWon { get; set; }
	}
}
