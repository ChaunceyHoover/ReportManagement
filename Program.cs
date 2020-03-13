using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ReportPortal {
	public class Program {
		//public static String sqlServerString = "server=mydbinstance.czpgu1lidhwq.us-east-1.rds.amazonaws.com;userid=stkUser;password=CMZokUlt4ovAhJiVQPAr;database=STK;UseAffectedRows=false;";

		public static void Main(String[] args) =>
			// Runs an ASP.NET Core Application
			new WebHostBuilder()
				.UseKestrel() // Specifies Kestrel will be used as the server
				.UseContentRoot(Directory.GetCurrentDirectory()) // Use IIS directory as the current directory
				.UseSetting("detailedErrors", "true")
				.UseIISIntegration() // Use IIS as a reverse proxy
				.UseStartup<Startup>() // Configures application using the functions in Startup.cs
				.CaptureStartupErrors(true)
				.Build().Run(); // Build & Run application
	}
}
