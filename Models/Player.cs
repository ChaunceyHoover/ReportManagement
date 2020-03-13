using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models {
	public class Player {
		/// <summary>The Site's ID. Only really used in database, not on website</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>The player's card ID</summary>
		[Column("card_id")]
		public int CardId { get; set; }

		/// <summary>The player's 8 digit card number</summary>
		[Column("card_number")]
		public String CardNumber { get; set; }

		/// <summary>The player's first name</summary>
		[Column("first_name")]
		public String FirstName { get; set; }

		/// <summary>The player's last name</summary>
		[Column("last_name")]
		public String LastName { get; set; }

		/// <summary>The city the player lives in</summary>
		[Column("city")]
		public String City { get; set; }

		/// <summary>The state the player lives in</summary>
		[Column("state")]
		public String State { get; set; }

		/// <summary>The zip code the player lives in</summary>
		[Column("zip")]
		public String Zip { get; set; }

		/// <summary>The player's phone number</summary>
		[Column("phone")]
		public String Phone { get; set; }

		/// <summary>The player's birthday</summary>
		[Column("birthdate")]
		public DateTime Birthday { get; set; }

		/// <summary>The player's gender</summary>
		/// <remarks>0/False = Female, 1/True = Male</remarks>
		[Column("gender")]
		public bool? Gender { get; set; }

		/// <summary>The player's card number</summary>
		[Column("personal_id")]
		public String PersonalId { get; set; }

		/// <summary>????</summary>
		[Column("data_enter_type_id")]
		public int? DateEnterTypeId { get; set; }

		/// <summary>Optional notes on the player</summary>
		[Column("notes")]
		public String Notes { get; set; }

		/// <summary>The player's email address</summary>
		[Column("email_address")]
		public String Email { get; set; }

		/// <summary>The player's cashable balance</summary>
		[Column("cashable_balance")]
		public int? CashableBalance { get; set; }

		/// <summary>The player's playable balance</summary>
		[Column("playable_balance")]
		public int? PlayableBalance { get; set; }

		/// <summary>The time last the player's row was updated</summary>
		[Column("last_update")]
		public DateTime LastUpdate { get; set; }
	}
}
