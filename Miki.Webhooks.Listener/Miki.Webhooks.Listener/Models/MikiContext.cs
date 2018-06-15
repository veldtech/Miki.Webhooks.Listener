using Microsoft.EntityFrameworkCore;
using Miki.Configuration;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Webhooks.Listener.Models
{
    public class MikiContext : DbContext
	{
		[Configurable]
		public string ConnectionString { get; set; }

		public DbSet<Achievement> Achievements { get; set; }
		public DbSet<IsDonator> IsDonator { get; set; }
		public DbSet<User> Users { get; set; }

		public MikiContext()
		: base()
		{ }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql(ConnectionString);
			base.OnConfiguring(optionsBuilder);
		}
	}
}
