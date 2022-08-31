using CarPark.Api.Data.Entities;

namespace CarPark.Api.Data
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;

    public class CarParkContext : DbContext
    {
        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Inventory> Inventory { get; set; }

        public DbSet<Rate> Rates { get; set; }

        public CarParkContext(DbContextOptions<CarParkContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var inventory = new List<Inventory>();
            var rates = new List<Rate>();

            var date = DateTime.UtcNow.Date;
            var maxLimit = date.AddYears(2);

            // Seed the Inventory and Rate tables for up to 2 years of bookings. TODO Add a CRON job to add a new row to both these tables every day to ensure bookings can always be made up to 2 years in advance.
            while(date <= maxLimit)
            {
                inventory.Add(new Inventory { Date = date, ReservedSpaces = 0, TotalSpaces = 10 });
                rates.Add(new Rate { Date = date , Price = 15.0M });
                date = date.AddDays(1);
            }

            modelBuilder.Entity<Inventory>().HasData(inventory);
            modelBuilder.Entity<Rate>().HasData(rates);
        }
    }
}
