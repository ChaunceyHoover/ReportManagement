using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// All information pertaining to an installer. 
	/// </summary>
	public class Installer {
		/// <summary>Unique ID for each installer</summary>
		/// <remarks>Primary key</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("installer_id")]
		public int Id { get; set; }

		/// <summary>Installer's first name</summary>
		[Column("installer_fname")]
		public String FName { get; set; }

		/// <summary>Installer's last name</summary>
		[Column("installer_lname")]
		public String LName { get; set; }

		/// <summary>Installer's email</summary>
		[Column("installer_email")]
		public String Email { get; set; }

		/// <summary>Installer's phone number</summary>
		[Column("installer_phone")]
		public String Phone { get; set; }
	}
}
