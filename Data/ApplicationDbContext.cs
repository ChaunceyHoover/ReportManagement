using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ReportPortal.Data {
	public class ApplicationDbContext : DbContext {
		public IConfigurationRoot config;

		public ApplicationDbContext(DbContextOptions options) : base(options) { }
		public ApplicationDbContext() { }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
#if LOCAL
			var builder = optionsBuilder.UseMySQL(Startup.Configuration.GetConnectionString("LocalDatabase"));
#else
			var builder = optionsBuilder.UseMySQL(Startup.Configuration.GetConnectionString("Database"));
#endif
#if DEBUG
			builder.EnableSensitiveDataLogging();
#endif
		}

		public DbSet<Models.Activation> Activations { get; set; }
		public DbSet<Models.Adjustment> Adjustments { get; set; }
		public DbSet<Models.AdjustmentType> AdjustmentTypes { get; set; }
		public DbSet<Models.Billing> Billing { get; set; }
		public DbSet<Models.Installer> Installers { get; set; }
		public DbSet<Models.Log> Logs { get; set; }
		public DbSet<Models.Manager> Managers { get; set; }
		public DbSet<Models.Permission> Permissions { get; set; }
		public DbSet<Models.PlayerAdjustment> PlayerAdjustments { get; set; }
		public DbSet<Models.Player> Players { get; set; }
		public DbSet<Models.Site> Sites { get; set; }
		public DbSet<Models.System> Systems { get; set; }
		public DbSet<Models.Ticket> Tickets { get; set; }
		public DbSet<Models.TicketComment> TicketComments { get; set; }
		public DbSet<Models.User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<Models.Activation>().ToTable("activations");
			modelBuilder.Entity<Models.Adjustment>().ToTable("adjustments");
			modelBuilder.Entity<Models.AdjustmentType>().ToTable("adjustment_types");
			modelBuilder.Entity<Models.Billing>().ToTable("billing");
			modelBuilder.Entity<Models.Installer>().ToTable("installers");
			modelBuilder.Entity<Models.Log>().ToTable("logs");
			modelBuilder.Entity<Models.Manager>().ToTable("managers");
			modelBuilder.Entity<Models.Permission>().ToTable("permission");
			modelBuilder.Entity<Models.PlayerAdjustment>().ToTable("player_adjustments");
			modelBuilder.Entity<Models.Player>().ToTable("players");
			modelBuilder.Entity<Models.Site>().ToTable("sites");
			modelBuilder.Entity<Models.Ticket>().ToTable("tickets");
			modelBuilder.Entity<Models.TicketComment>().ToTable("ticket_comments");
			modelBuilder.Entity<Models.System>().ToTable("systems");
			modelBuilder.Entity<Models.User>().ToTable("user");

			modelBuilder.Entity<Models.Billing>().HasKey(b => new { b.SiteId, b.Sequence });
			modelBuilder.Entity<Models.Permission>().HasKey(p => new { p.SiteId, p.UserId });
			modelBuilder.Entity<Models.Player>().HasKey(pl => new { pl.SiteId, pl.CardId });
			modelBuilder.Entity<Models.PlayerAdjustment>().HasKey(pa => new { pa.Id, pa.SiteId });
		}
	}
}