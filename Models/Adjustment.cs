using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models {
	/// <summary>
	/// Representation of an adjustment object
	/// </summary>
	public class Adjustment {
		/// <summary>The type of adjustment being made</summary>
		public enum AdjustmentType {
			SmallIncrease = 0,
			MediumIncrease = 1,
			LargeIncrease = 2,
			SmallDecrease = 3,
			MediumDecrease = 4,
			LargeDecrease = 5,
			JustAReset = 6,
			DropGrandPrize = 7,
			DropCommunityPrize = 8,
			Playable = 9,
			Cashable = 10
		}

		/// <summary>Unique ID for each installer</summary>
		/// <remarks>Primary key</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("adjustment_id")]
		public int Id { get; set; }

		/// <summary>The site that is getting the adjustment</summary>
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>Date adjustment was originally submitted</summary>
		[Column("adjustment_date")]
		public DateTime SubmissionDate { get; set; }

		/// <summary>Snapshot of the user's name who submitted request</summary>
		[Column("submitted_by")]
		public String SubmittedBy { get; set; }

		/// <summary>Snapchat of the user's ID who submitted the request</summary>
		[Column("submitter_id")]
		public int SubmitterId { get; set; }

		/// <summary>Adjustment type [Small/Medium/Large] [Increase/Decrease], or Playable/Cashable</summary>
		/// <remarks>
		/// If type is between 0 and 8, the adjustment is a server restart (small/medium/large increase/decrease, full reset, or drop community/grand prize
		/// Otherwise, it's a player pin adjustment
		/// </remarks>
		[Column("adjustment_type")]
		public AdjustmentType Type { get; set; }

		/// <summary>Card number for player pin adjustment</summary>
		/// <remarks>Only has value if type is 9 or 10 (Playable or Cashable)</remarks>
		[Column("card_number")]
		public String CardNumber { get; set; }

		/// <summary>Money in for the previous week the adjustment was requested</summary>
		/// <remarks>Only gets set if adjustment is a server adjustment, otherwise null</remarks>
		[Column("week_money_in")]
		public Decimal? WeekMoneyIn { get; set; }

		/// <summary>Money out for the previous week the adjustment was requested</summary>
		/// <remarks>Only gets set if adjustment is a server adjustment, otherwise null</remarks>
		[Column("week_money_out")]
		public Decimal? WeekMoneyOut { get; set; }

		/// <summary>Money in for the previous month the adjustment was requested</summary>
		/// <remarks>Only gets set if adjustment is a server adjustment, otherwise null</remarks>
		[Column("month_money_in")]
		public Decimal? MonthMoneyIn { get; set; }

		/// <summary>Money out for the previous month the adjustment was requested</summary>
		/// <remarks>Only gets set if adjustment is a server adjustment, otherwise null</remarks>
		[Column("month_money_out")]
		public Decimal? MonthMoneyOut { get; set; }

		/// <summary>The amount for a player pin adjustment</summary>
		/// <remarks>Only gets set if adjustment is a player pin adjustment, otherwise null</remarks>
		[Column("amount")]
		public Decimal? Amount { get; set; }

		/// <summary>Flag to determine if adjustment should drop grand prize</summary>
		/// <remarks>True or False if adjustment type is between 0 and 8, otherwise null</remarks>
		[Column("drop_grand_prize")]
		public bool? DropGrandPrize { get; set; }

		/// <summary>Flag to determine if adjustment should drop grand prize</summary>
		/// <remarks>True or False if adjustment type is between 0 and 8, otherwise null</remarks>
		[Column("drop_community_prize")]
		public bool? DropCommunityPrize { get; set; }

		/// <summary>Whether or not this adjustment is complete</summary>
		[Column("completed")]
		public bool Completed { get; set; }

		/// <summary>Date the adjustment was completed</summary>
		[Column("completed_date")]
		public DateTime CompletedDate { get; set; }

		/// <summary></summary>
		[Column("adjustment_ip")]
		public String Ip { get; set; }

		/// <summary>The time the server was restarted</summary>
		[Column("restart_time")]
		public DateTime? RestartTime { get; set; }

		/// <summary>Whether or not a full reset is required</summary>
		[Column("reset_request")]
		public bool ResetRequest { get; set; }

		/// <summary>Used as extra security for adjustment process</summary>
		[Column("secret_key")]
		public String SecretKey { get; set; }

		/// <summary>Notes about adjustment</summary>
		[Column("notes")]
		public String Notes { get; set; }

		/// <summary>
		/// Selectively copies values from the given adjustment to this site object (copies other adjustment's values and pastes on this adjustment's values)
		/// 
		/// Basically, the ID of an adjustment should never change, and just in case I add/remove more values in the future, this method will
		/// allow me to still update adjustment objects easier from POST requests
		/// </summary>
		/// <param name="other">The adjustment to copy data from</param>
		/// <param name="exactCopy">If true, copies properties exactly, even if they're null. If false, only copies properties that are not null</param>
		public void Copy(Adjustment other, bool exactCopy) {
			PropertyInfo[] pi = this.GetType().GetProperties();
			foreach (PropertyInfo p in pi) {
				if (!p.Name.Equals("Id")) {
					if (exactCopy)
						p.SetValue(this, p.GetValue(other));
					else if (p.GetValue(other) != null)
						p.SetValue(this, p.GetValue(other));
				}
			}
		}
	}
}
