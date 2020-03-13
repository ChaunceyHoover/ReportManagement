using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	public class PlayerAdjustment {
		/// <summary>
		/// Because .NET Core doesn't allow POST/PUT body's to be just a string, a class is needed to contain the string
		/// 
		/// This class is simply a wrapper for a string variable, in this case the verification code
		/// </summary>
		public class VerificationContainer {
			public String Verification { get; set; }
		}

		/// <summary>Primary Key - Unique identifier for each player adjustment</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("id")]
		public int Id { get; set; }

		/// <summary>Primary Key - Site that player pin adjustment is tied to</summary>
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>Amount to be adjusted in cents</summary>
		[Column("amount")]
		public int Amount { get; set; }

		/// <summary>The player's account to adjust</summary>
		[Column("player_card")]
		public String CardNumber { get; set; }

		/// <summary>The side the adjustment is going toward</summary>
		/// <remarks>0/False = Playable, 1/True = Cashable</remarks>
		[Column("side")]
		public bool AdjustmentType { get; set; }

		/// <summary>Randomly generated Guid</summary>
		[Column("secret_key")]
		public String SecretKey { get; set; }

		/// <summary>Snapshot user's ID who submitted adjustment</summary>
		/// <remarks>Set by server</remarks>
		[Column("submitted_by")]
		public int SubmittedBy { get; set; }

		/// <summary>Snapshot of user's name who submitted adjustment</summary>
		[Column("submitted_by_name")]
		public String SubmittedByName { get; set; }

		/// <summary>Date adjustment was submitted</summary>
		[Column("submission_date")]
		public DateTime SubmissionDate { get; set; }

		/// <summary>A snapshot of the user's ID who claimed the adjustment</summary>
		[Column("claimed_by")]
		public int? ClaimedBy { get; set; }

		/// <summary>A snapshot of the user's name who claimed the adjustment</summary>
		[Column("claimed_by_name")]
		public String ClaimedByName { get; set; }

		/// <summary>The date the adjustment was claimed</summary>
		[Column("claimed_date")]
		public DateTime? ClaimedDate { get; set; }

		[Column("before_value")]
		public int? BeforeValue { get; set; }

		[Column("after_value")]
		public int? AfterValue { get; set; }

		/// <summary>Notes submitted with this adjustment</summary>
		[Column("notes")]
		public String Notes { get; set; }

		/// <summary>The time the adjustment was completed</summary>
		/// <remarks>If null, adjustment is not yet completed</remarks>
		[Column("completed_time")]
		public DateTime? CompletedTime { get; set; }
	}
}