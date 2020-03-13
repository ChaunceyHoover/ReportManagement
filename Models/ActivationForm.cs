using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportPortal.Models
{
	/// <summary>This is the actual object that gets sent when a user fills out an activation form</summary>
	/// <remarks>Do not confuse with 'Activation' object from MySQL table; this object is used to map objects from POST/PUT request</remarks>
    public class ActivationForm
    {
		/// <summary>The new site to be created</summary>
		/// <remarks>Some information gets overwritten from the other values in this class</remarks>
		public Site NewSite { get; set; }

		/// <summary>The new account being created and owner of the new site</summary>
		public User NewOwner { get; set; }

		/// <summary>The site installer</summary>
		/// <remarks>If `Id` is 0, a new installer will be created</remarks>
		public Installer SiteInstaller { get; set; }

		/// <summary>The site manager</summary>
		/// <remarks>If `Id` is 0, a new manager will be created</remarks>
		public Manager SiteManager { get; set; }

		/// <summary>Information relating to the physical server and new site being created</summary>
		/// <remarks>Almost all values in this class get set from other variables during activation</remarks>
		public Activation ActivationInfo { get; set; }

		/// <summary>A way to check if site has already been approved through the old report portal (url that ended with "/rpt/")</summary>
		/// <remarks>If this value is true, it assumes all info entered is correct, as the site will automatically be approved</remarks>
		public bool PreApproved { get; set; }
    }
}
