using NexusSales.Core.Models; // Add this for NotificationItem
using System.Collections.Generic;
using System.Data.Entity; // Keep this for EF6
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;

namespace NexusSales.Data
{
    public class NexusSalesDbContext : DbContext
    {
        public NexusSalesDbContext()
            : base("name=NexusSalesConnection")
        {
            // Constructor logic if needed
        }

        public DbSet<NotificationItem> Notifications { get; set; }
        // Other DbSet properties for your entities

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure your entity mappings here
            base.OnModelCreating(modelBuilder);
        }
    }
}