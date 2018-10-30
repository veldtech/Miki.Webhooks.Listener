using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Configuration;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Webhooks.Listener
{
	public class WebhookContext : MikiDbContext
	{
		public DbSet<Achievement> Achievements { get; set; }
		public DbSet<DonatorKey> DonatorKey { get; set; }
		public DbSet<User> Users { get; set; }

		public WebhookContext()
		{
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
			optionsBuilder.UseNpgsql(Program.Configurations.DatabaseConnectionString);
		}
	}
}