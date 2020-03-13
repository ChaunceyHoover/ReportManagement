using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportPortal.Models {
	/// <summary>
	/// An object that holds all information relating to a site activation
	/// </summary>
	/// <remarks>This is the information that was SUBMITTED, not the CURRENT information</remarks>
	public class Activation {
		/// <remarks>Primary key</remarks>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Column("activation_id")]
		public int ActivationId { get; set; }

		/// <summary>The site's ID that was activated</summary>
		[Column("site_id")]
		public int SiteId { get; set; }

		/// <summary>The room name of the site</summary>
		[Column("room_name")]
		public String RoomName { get; set; }

		/// <summary>The system that the site uses</summary>
		[Column("system_id")]
		public int SystemId { get; set; }

		/// <summary>The label printed on the physical server</summary>
		[Column("hp_id")]
		public String HpId { get; set; }

		/// <summary>The GUID for the server</summary>
		[Column("server_number")]
		public String Key { get; set; }

		/// <summary>The date the activation was submitted</summary>
		[Column("submission_date")]
		public DateTime SubmissionDate { get; set; }

		/// <summary>The date the installation was submitted</summary>
		[Column("install_date")]
		public DateTime InstallDate { get; set; }

		/// <summary>The billing email for the site</summary>
		[Column("billing_email")]
		public String BillingEmail { get; set; }

		/// <summary>The store's phone number</summary>
		[Column("store_phone")]
		public String StorePhone { get; set; }

		/// <summary>The installer's user ID who installed the site</summary>
		[Column("installer_id")]
		public int InstallerId { get; set; }

		/// <summary>The installer's first name</summary>
		[Column("installer_fname")]
		public String InstallerFName { get; set; }

		/// <summary>The installer's last name</summary>
		[Column("installer_lname")]
		public String InstallerLName { get; set; }

		/// <summary>The installer's email</summary>
		[Column("installer_email")]
		public String InstallerEmail { get; set; }

		/// <summary>The installer's phone number</summary>
		[Column("installer_phone")]
		public String InstallerPhone { get; set; }

		/// <summary>The manager's user ID who installed the site</summary>
		[Column("manager_id")]
		public int ManagerId { get; set; }

		/// <summary>The manager's first name</summary>
		[Column("manager_fname")]
		public String ManagerFName { get; set; }

		/// <summary>The manager's last name</summary>
		[Column("manager_lname")]
		public String ManagerLName { get; set; }

		/// <summary>The manager's email</summary>
		[Column("manager_email")]
		public String ManagerEmail { get; set; }

		/// <summary>The manager's phone number</summary>
		[Column("manager_phone")]
		public String ManagerPhone { get; set; }

		/// <summary>The distributor's user ID who installed the site</summary>
		[Column("distributor_id")]
		public int DistributorId { get; set; }

		/// <summary>The distributor's first name</summary>
		[Column("distributor_fname")]
		public String DistributorFName { get; set; }

		/// <summary>The distributor's last name</summary>
		[Column("distributor_lname")]
		public String DistributorLName { get; set; }

		/// <summary>The owner's user ID who installed the site</summary>
		[Column("owner_id")]
		public int OwnerId { get; set; }

		/// <summary>The owner's first name</summary>
		[Column("owner_fname")]
		public String OwnerFName { get; set; }

		/// <summary>The owner's last name</summary>
		[Column("owner_lname")]
		public String OwnerLName { get; set; }

		/// <summary>The owner's email</summary>
		[Column("owner_email")]
		public String OwnerEmail { get; set; }

		/// <summary>The owner's phone</summary>
		[Column("owner_phone")]
		public String OwnerPhone { get; set; }

		[Column("owner_username")]
		public String OwnerUserName { get; set; }

		/// <summary>The site's physical address</summary>
		[Column("site_address")]
		public String SiteAddress { get; set; }

		/// <summary>The city which the site is located</summary>
		[Column("site_city")]
		public String SiteCity { get; set; }

		/// <summary>The state which the site is located</summary>
		[Column("site_state")]
		public String SiteState { get; set; }

		/// <summary>The country that the site is located</summary>
		[Column("site_country")]
		public String SiteCountry { get; set; }

		/// <summary>The zip code that the site is located</summary>
		[Column("site_zip")]
		public String SiteZip { get; set; }

		/// <summary>Billing department user who approved activation</summary>
		[Column("approved_by")]
		public int? ApprovedBy { get; set; }

		/// <summary>(Optional) Activation notes from activation</summary>
		[Column("activation_notes")]
		public String ActivationNotes { get; set; }

		/// <summary>(Optional Approval notes from re-activation</summary>
		[Column("approval_notes")]
		public String ApprovalNotes { get; set; }
	}
}
