using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models {
	/// <summary>
	/// <see cref="User"/> table; mapped in <see cref="Data.ApplicationDbContext"/> from the SQL server.
	/// </summary>
	/// 
	/// <example>
	/// Creating a <see cref="User"/>
	/// <code>
	/// using (var context = new Data.ApplicationDbContext()) {
	///	 context.Users.Add(new Models.User {
	///		 UserName = "username",
	///		 Password = "password",
	///		 Level = 1,
	///		 Name = "User 1",
	///		 Active = true
	///	 });
	///	 context.SaveChanges();
	/// }
	/// </code>
	/// </example>
	public class User {
		/// <summary>Primary key of the <see cref="User"/> table.</summary>
		/// <remarks>Generated in database and cannot be set or changed pragmatically.</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("user_id")]
		public int Id { get; set; }

		/// <summary>Login username.</summary>
		[Column("user_name")]
		public String UserName { get; set; }

		/// <summary>Login password.</summary>
		/// <remarks>Stored as a bcrypted string.</remarks>
		[Column("user_password")]
		public String Password { get; set; }

		/// <summary>User permission level.</summary>
		/// <remarks>Value range is between 1 and 10</remarks>
		[Column("user_level")]
		public int? Level { get; set; }

		/// <summary>Defines whether the user can log in.</summary>
		[Column("user_enabled")]
		public Boolean? Active { get; set; }

		/// <summary>Display first name.</summary>
		[Column("contact_fname")]
		public String FName { get; set; }

		/// <summary>Display last name.</summary>
		[Column("contact_lname")]
		public String LName { get; set; }

		/// <summary>Phone number to contact user</summary>
		[Column("contact_phone")]
		public String Phone { get; set; }

		/// <summary>Email to contact user</summary>
		[Column("contact_email")]
		public String Email { get; set; }

		/// <summary>Anything needed to be noted about user</summary>
		[Column("user_notes")]
		public String Notes { get; set; }

		/// <summary>True is user is a distributor</summary>
		[Column("is_distributor")]
		public bool? IsDistributor { get; set; }

		/// <summary>DateTime value of last login</summary>
		[Column("user_last_login", TypeName = "datetime")]
		public DateTime? UserLastLogin { get; set; }

		/// <summary>A nullable value that is only applied to distributors</summary>
		[Column("percentage")]
		public int? Percentage { get; set; }

		/// <summary>
		/// Compares this user object to another given user object
		/// </summary>
		/// <param name="other">The other user to be compared against</param>
		/// <param name="fullCompare">If true, checks all information. Otherwise, ignores password and last login</param>
		/// <returns>A list of any differences between the users</returns>
		public List<Log.Variance> Compare(User other, bool fullCompare) {
			List<Log.Variance> variances = new List<Log.Variance>();
			PropertyInfo[] pi = this.GetType().GetProperties();
			foreach (PropertyInfo p in pi) {
				Log.Variance var;
				var.Property = p.Name;
				var.Original = p.GetValue(this);
				var.New = p.GetValue(other);
				if (!Equals(var.Original, var.New)) {
					if (fullCompare)
						variances.Add(var);
					else if (!p.Name.Equals("Password") && !p.Name.Equals("UserLastLogin"))
						variances.Add(var);
				}
			}
			return variances;
		}

		/// <summary>
		/// Copies the given user's data to this user object
		/// </summary>
		/// <param name="other">The other user object to copy data from</param>
		public void Copy(User other, bool fullCopy) {
			PropertyInfo[] pi = this.GetType().GetProperties();
			foreach (PropertyInfo p in pi) {
				if (!p.Name.Equals("Id")) {
					if (fullCopy)
						p.SetValue(this, p.GetValue(other));
					else if (!p.Name.Equals("Password") && !p.Name.Equals("UserLastLogin"))
						p.SetValue(this, p.GetValue(other));
				}
			}
		}
	}
}
