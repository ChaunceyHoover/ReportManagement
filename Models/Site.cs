using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReportPortal.Models
{
	public class Site {
		/// <summary>The Site's ID. Only really used in database, not on website</summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("site_id", TypeName = "int")]
		public int SiteId { get; set; }

		/// <summary>The system that the site runs (example: STRYK3R or TYPHOON)</summary>
		[Column("system_id", TypeName = "int")]
		public int? SystemId { get; set; }

		/// <summary>The website's number. This is value displayed on the website</summary>
		[Column("site_number", TypeName = "int")]
		public int? SiteNumber { get; set; }

		/// <summary>A generated, unique ID</summary>
		[Column("site_guid", TypeName = "varchar")]
		public String SiteGuid { get; set; }

		/// <summary>The website's display name</summary>
		[Column("site_name", TypeName = "varchar")]
		public String SiteName { get; set; }

		/// <summary>If the site is active, the value is true. Otherwise, the site is pretty much ignored.</summary>
		/// <remarks>Note: this value is not set from program; it's handled automatically</remarks>
		[Column("site_active", TypeName = "tinyint")]
		public bool? SiteActive { get; set; }

		/// <summary>Enable/disable the site through this.</summary>
		[Column("site_enabled", TypeName = "tinyint")]
		public bool? SiteEnabled { get; set; }

		/// <summary>idk honestly</summary>
		[Column("site_on_report", TypeName = "tinyint")]
		public bool? SiteOnReport { get; set; }

		/// <summar>Site serial number</summary>
		[Column("site_serial", TypeName = "varchar")]
		public String SiteSerial { get; set; }

		/// <summary>The last IPv4 address to ping the site</summary>
		[Column("site_last_ip", TypeName = "varchar")]
		public String SiteLastIp { get; set; }

		/// <summary>The date & time the site was last pinged</summary>
		[Column("site_last_ping", TypeName = "datetime")]
		public DateTime? SiteLastPing { get; set; }

		/// <summary>The ID of the site's distributor</summary>
		[Column("site_distributor", TypeName = "int")]
		public int? SiteDistributor { get; set; }

		/// <summary>The physical location of the site</summary>
		[Column("site_state", TypeName = "varchar")]
		public String SiteState { get; set; }

		/// <summary>The site owner's name</summary>
		[Column("site_owner_name", TypeName = "varchar")]
		public String SiteOwnerName { get; set; }

		/// <summary>The site owner's phone number</summary>
		[Column("site_owner_phone", TypeName = "varchar")]
		public String SiteOwnerPhone { get; set; }

		/// <summary>The site owner's email</summary>
		[Column("site_owner_email", TypeName = "varchar")]
		public String SiteOwnerEmail { get; set; }

		/// <summary>The physical location of the site</summary>
		[Column("site_address", TypeName = "varchar")]
		public String SiteAddress { get; set; }

		/// <summary>The city the site is located</summary>
		[Column("site_city", TypeName = "varchar")]
		public String SiteCity { get; set; }

		/// <summary>The country the site is in</summary>
		[Column("site_country", TypeName = "varchar")]
		public String SiteCountry { get; set; }

		/// <summary>The zip code for the site's physical address</summary>
		[Column("site_zip", TypeName = "varchar")]
		public String SiteZip { get; set; }

		/// <summary>Any notes for the site</summary>
		/// <remarks>Usually set by techs</remarks>
		[Column("notes", TypeName = "text")]
		public String Notes { get; set; }

		/// <summary>The store's phone number</summary>
		[Column("store_phone", TypeName = "varchar")]
		public String StorePhone { get; set; }

		/// <summary>Any comments for the site</summary>
		/// <remarks>Usually set programatically</remarks>
		[Column("comment", TypeName = "text")]
		public String Comment { get; set; }

		/// <summary>The time physical store location opens</summary>
		/// <remarks>If `IsTwentyFourSeven` is true, this value is null</remarks>
		[Column("store_open_time", TypeName = "Time")]
		public TimeSpan? StoreOpenTime { get; set; }

		/// <summary>The time the physical store location closes</summary>
		/// <remarks>If `IsTwentyFourSeven` is true, this value is null</remarks>
		/// <remarks></remarks>
		[Column("store_close_time", TypeName = "Time")]
		public TimeSpan? StoreCloseTime { get; set; }

		/// <summary>Determines if physical store is open 24/7</summary>
		/// <remarks>If this value is true, `StoreOpenTime` and `StoreCloseTime` will be null</remarks>
		[Column("twenty_four_seven", TypeName = "tinyint")]
		public bool? IsTwentyFourSeven { get; set; }

		/// <summary>The current grand prize value on the site's community board</summary>
		[Column("grand_prize", TypeName = "int")]
		public int? GrandPrize { get; set; }

		/// <summary>The current community prize value on the site's community board</summary>
		[Column("community_prize", TypeName = "int")]
		public int? CommunityPrize { get; set; }

		/// <summary>The current lucky winner prize value on the site's community board</summary>
		[Column("lucky_winner_prize", TypeName = "int")]
		public int? LuckyWinnerPrize { get; set; }

		/// <summary>The current winner prize value on the site's community board</summary>
		[Column("winner_prize", TypeName = "int")]
		public int? WinnerPrize { get; set; }

		/// <summary>Used to tell ServerHelper's player report to send data updated after this time</summary>
		[Column("last_player_update", TypeName = "DateTime")]
		public DateTime LastPlayerUpdate { get; set; }

		/// <summary>Used to tell ServerHelper's community report to send data updated after this time</summary>
		[Column("last_community_update", TypeName = "DateTime")]
		public DateTime LastCommunityUpdate { get; set; }

		/// <summary>The last time the community prize dropped</summary>
		[Column("last_community_drop", TypeName = "DateTime")]
		public DateTime LastCommunityDrop { get; set; }

		/// <summary>The last time the grand prize dropped</summary>
		[Column("last_grand_drop", TypeName = "DateTime")]
		public DateTime LastGrandDrop { get; set; }

		/// <summary>Number of games installed on the server for the game room</summary>
		/// <remarks>Only null when server hasn't synced with Server Helper</remarks>
		[Column("games_installed", TypeName = "int")]
		public int? GamesInstalled { get; set; }

		/// <summary>Number of games enabled on the server for the game room</summary>
		/// <remarks>Only null when server hasn't synced with Server Helper</remarks>
		[Column("games_enabled", TypeName = "int")]
		public int? GamesEnabled { get; set; }

		/// <summary>The date the site was installed (not used?)</summary>
		/// <remarks>MUST be updated manually via SQL queries.
		/// EF Core hates zero datetimes, and the database is full of them, so that's the only fix.</remarks>
		//[Column("site_date_installed", TypeName = "datetime")]
		//public DateTime? SiteDateInstalled { get; set; }

		/// <summary>The date the site was installed (use this one)</summary>
		/// <remarks>MUST be updated manually via SQL queries.
		/// EF Core hates zero datetimes, and the database is full of them, so that's the only fix.</remarks>
		[Column("site_install_date", TypeName = "datetime")]
		public DateTime? SiteInstallDate { get; set; }

		/// <summary>Compares this site object to another site object, and returns all properties that are different</summary>
		/// <param name="other">The other site object to compare against</param>
		/// <returns>A list of differences between the two site objects</returns>
		public List<Log.Variance> Compare(Site other) {
			List<Log.Variance> variances = new List<Log.Variance>();
			PropertyInfo[] pi = this.GetType().GetProperties();
			foreach (PropertyInfo p in pi) {
				Log.Variance var;
				var.Property = p.Name;
				var.Original = p.GetValue(this);
				var.New = p.GetValue(other);
				if (!Equals(var.Original, var.New))
					variances.Add(var);
			}
			return variances;
		}

		/// <summary>
		/// Selectively copies values from the given site to this site object (copies other site's values and pastes on this site's values)
		/// 
		/// Basically, the ID of a site should never change, and just in case I add/remove more values in the future, this method will
		/// allow me to still update site objects easier from POST requests
		/// </summary>
		/// <param name="other">The site to copy data from</param>
		/// <param name="exactCopy">If true, copies properties exactly, even if they're null. If false, only copies properties that are not null</param>
		public void Copy(Site other, bool exactCopy) {
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
