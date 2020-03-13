using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// A table responsible for mapping an adjustment type (such as 0, 1, 2, ..., 10) to a name, as well as the query needed for ServerHelper to run said adjustment
	/// </summary>
	public class AdjustmentType {
		/// <summary>
		/// The integer value that represents this adjustment type
		/// </summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("adjustment_type")]
		public int Type { get; set; }

		/// <summary>
		/// The name value of this adjustment type
		/// </summary>
		[Column("adjustment_name")]
		public String Name { get; set; }

		/// <summary>
		/// The query to be executed through Server Helper for this adjustment type
		/// </summary>
		[Column("adjustment_query")]
		public String Query { get; set; }

		/// <summary>
		/// Description of what each adjustment does
		/// </summary>
		[Column("adjustment_description")]
		public String Description { get; set; }
	}
}
