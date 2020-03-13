using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// All information pertaining to an installer. 
	/// </summary>
	public class Log {
		/// <summary>The type of action being logged</summary>
		public enum ActionType {
			CreateUser = 0,
			ModifyUser = 1,
			DeleteUser = 2,
			CreateSite = 3,
			ModifySite = 4,
			DeleteSite = 5,
			ActivateSite = 6,
			CreateInstaller = 7,
			ModifyInstaller = 8,
			DeleteInstaller = 9,
			CreateManager = 10,
			ModifyManager = 11,
			DeleteManager = 12,
			ApproveSite = 13,
			CreateAdjustment = 14,
			ClaimAdjustment = 15,
			CompleteAdjustment = 16,
			DeleteAdjustment = 17,
			DeleteActivation = 18
		}

		/// <summary>A way to log the diference between two properties when comparing two objects</summary>
		public struct Variance {
			public string Property;
			public object Original, New;
		}

		public Log() { /* This constructor is to make EF core happy */ }

		public Log(int userId, ActionType action, String desc, DateTime time, int modifiedId, String changes) {
			UserId = userId;
			Action = action;
			Description = desc;
			LogTime = time;
			ModifiedId = modifiedId;
			Changes = changes;
		}

		/// <summary>The unique log ID</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("log_id")]
		public int LogId { get; set; }

		/// <summary>The user who performed the action being logged</summary>
		[Column("user_id")]
		public int UserId { get; set; }

		/// <summary>The action being performed</summary>
		/// <remarks>See `Log.Action` enum for reference</remarks>
		[Column("action")]
		public ActionType Action { get; set; }

		/// <summary>A summary of everything changed</summary>
		[Column("description")]
		public String Description { get; set; }

		/// <summary>The time the event was performed</summary>
		[Column("log_time")]
		public DateTime LogTime { get; set; }

		/// <summary>The ID of the object modified. See `Action` to see which table the object is in.</summary>
		/// <remarks>This value is dependent on the `action` value.</remarks>
		[Column("modified_id")]
		public int ModifiedId { get; set; }

		/// <summary>A list of the values that got modified and their value</summary>
		/// <remarks>Format: [(key=value)][(key=value)][(key=value)]...</remarks>
		[Column("changes")]
		public String Changes { get; set; }
	}
}
