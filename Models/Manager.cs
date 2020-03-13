using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// All information pertaining to an installer. Essentially, it's just a user with less fields
	/// </summary>
	public class Manager {
		/// <summary>Unique ID for each installer</summary>
		/// <remarks>Primary key</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("manager_id")]
		public int Id { get; set; }

		/// <summary>Manager's first name</summary>
		[Column("manager_fname")]
		public String FName { get; set; }

		/// <summary>Manager's last name</summary>
		[Column("manager_lname")]
		public String LName { get; set; }

		/// <summary>Manager's email</summary>
		[Column("manager_email")]
		public String Email { get; set; }

		/// <summary>Manager's phone</summary>
		[Column("manager_phone")]
		public String Phone { get; set; }
	}
}
