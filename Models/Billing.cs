using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models
{
	public class Billing
	{
		/// <summary>PK of table</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("site_id", TypeName = "int")]
		public int SiteId { get; set; }

		/// <summary>Cashier terminal event code</summary>
		[Column("iSequence", TypeName = "int")]
		public int Sequence { get; set; }

		/// <summary>A unique ID from ServerHelper to determine which 'loop' the row was created in</summary>
		[Column("iSessionID", TypeName = "int")]
		public int? SessionId { get; set; }

		/// <summary>DateTime billing row applies to</summary>
		[Column("dtDate", TypeName = "datetime")]
		public DateTime? BillDate { get; set; }

		/// <summary>Type of transaction occuring (money deposited, money withdrawn, etc)</summary>
		[Column("strStatus", TypeName = "varchar")]
		public String Status { get; set; }

		/// <summary>Amount of money involved (in pennies)</summary>
		[Column("iAmount", TypeName = "int")]
		public int? Amount { get; set; }

		/// <summary>idk, it's null in every row so pretty sure it's useless</summary>
		[Column("dateFixed", TypeName = "tinyint")]
		public bool? DateFixed { get; set; }
	}
}
