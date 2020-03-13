using AspNet.Security.OAuth.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReportPortal {
	public class Startup {
		public static IConfigurationRoot Configuration { get; set; }

		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public void ConfigureServices(IServiceCollection services) {
			// Required for OpenIddict authorization
			services.AddDbContext<Data.ApplicationDbContext>(options => {
				options.UseOpenIddict();
			});
			
			// Set up a single route for giving tokens
			services.AddOpenIddict(options => {
				options.AddEntityFrameworkCoreStores<Data.ApplicationDbContext>();
				options.AddMvcBinders();
				options.EnableTokenEndpoint("/api/auth/token/");
				options.AllowPasswordFlow();
				options.DisableHttpsRequirement();
			});

			services.AddMvc(); // Sets up MVC to use the controllers and routes therein.
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
			// Token Authentication
			app.UseOAuthValidation(); // Validates OAuth tokens
			app.UseOpenIddict(); // Handles OAuth and OpenID

			// Angular routing
			app.Use(async (context, next) => {
				context.Response.Headers.Add("Cache-Control", "no-cache"); // disable caching
				await next(); // Default; serves file requested unless a pattern is matched below.

				if (context.Response.StatusCode == 404 && // ONLY route if the requested URL returns a 404 (does not exist).
														  // Do NOT re-route API routes
					!context.Request.Path.Value.StartsWith("/api/") &&
					// Just in case any form of typos occur, do NOT route anything in these folders.
					!context.Request.Path.Value.StartsWith("/css/") && !context.Request.Path.Value.StartsWith("/img/") && !context.Request.Path.Value.StartsWith("/js/") && !context.Request.Path.Value.StartsWith("/shifts/") && !context.Request.Path.Value.StartsWith("/spa/") && !context.Request.Path.Value.StartsWith("/template/") &&
					// Login is handled in the next else-if statement.
					!context.Request.Path.Value.StartsWith("/login") &&
					// Print is handled in last else-if statement.
					!context.Request.Path.Value.StartsWith("/print")) {
					context.Request.Path = "/index.html";
					await next(); // Serves index.html
				} else if (context.Request.Path.Value.StartsWith("/login")) { // Login redirection
					context.Request.Path = "/login.html";
					await next(); // Serves login.html
				} else if (context.Request.Path.Value.StartsWith("/print")) {
					context.Request.Path = "/print.html";
					await next(); // Serves print.html
				}
			});

//#if LOCAL
			app.UseDeveloperExceptionPage();
//#endif
			app.UseStaticFiles(); // Serves resource requests
			app.UseMvc(); // Serves API requests
		}
	}
}
