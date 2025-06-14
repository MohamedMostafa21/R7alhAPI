using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace R7alaAPI.Data
{
    public class ApplicationDBContext : IdentityDbContext<User, ApplicationRole, int>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TourGuide> TourGuides { get; set; }
        public DbSet<TourGuideApplication> TourGuideApplications { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<PlanPlace> PlanPlaces { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Activity> Activities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Value comparer for List<string> properties
            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                c => c != null ? c.ToList() : new List<string>());

            // User configuration
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(20);
                entity.Property(u => u.LastName).HasMaxLength(20);
                entity.Property(u => u.Gender).IsRequired();
                entity.Property(u => u.DateOfBirth).IsRequired();
                entity.Property(u => u.Country).HasMaxLength(100);
                entity.Property(u => u.City).HasMaxLength(100);
                entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
                entity.Property(u => u.CreatedAt).IsRequired();
            });

            // TourGuide configuration
            builder.Entity<TourGuide>(entity =>
            {
                entity.HasKey(tg => tg.Id);
                entity.Property(tg => tg.UserId).IsRequired();
                entity.Property(tg => tg.Bio).HasMaxLength(1000);
                entity.Property(tg => tg.YearsOfExperience).IsRequired();
                entity.Property(tg => tg.Languages).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
                entity.Property(tg => tg.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(tg => tg.IsAvailable).IsRequired();
                entity.Property(tg => tg.ProfilePictureUrl).HasMaxLength(500);
                entity.HasOne(tg => tg.User)
                    .WithOne(u => u.TourGuide)
                    .HasForeignKey<TourGuide>(tg => tg.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TourGuideApplication configuration
            builder.Entity<TourGuideApplication>(entity =>
            {
                entity.HasKey(tga => tga.Id);
                entity.Property(tga => tga.UserId).IsRequired();
                entity.Property(tga => tga.Bio).HasMaxLength(1000);
                entity.Property(tga => tga.YearsOfExperience).IsRequired();
                entity.Property(tga => tga.Languages).IsRequired().HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
                entity.Property(tga => tga.HourlyRate).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(tga => tga.CVUrl).HasMaxLength(500);
                entity.Property(tga => tga.Status).IsRequired();
                entity.Property(tga => tga.SubmittedAt).IsRequired();
                entity.Property(tga => tga.AdminComment).HasMaxLength(500);
                entity.HasOne(tga => tga.User)
                    .WithMany()
                    .HasForeignKey(tga => tga.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Place configuration
            builder.Entity<Place>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Type).IsRequired().HasMaxLength(100);
                entity.Property(p => p.City).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Country).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Location).HasMaxLength(200);
                entity.Property(p => p.Latitude).IsRequired();
                entity.Property(p => p.Longitude).IsRequired();
                entity.Property(p => p.AveragePrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.CreatedAt).IsRequired();
                entity.Property(p => p.ThumbnailUrl).HasMaxLength(500);
                entity.Property(p => p.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
            });

            // City configuration
            builder.Entity<City>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Country).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(1000);
                entity.Property(c => c.Latitude).IsRequired();
                entity.Property(c => c.Longitude).IsRequired();
                entity.Property(c => c.ThumbnailUrl).HasMaxLength(500);
                entity.Property(c => c.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
            });

            // Favorite configuration
            builder.Entity<Favorite>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.UserId).IsRequired();
                entity.HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.Place)
                    .WithMany()
                    .HasForeignKey(f => f.PlaceId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.TourGuide)
                    .WithMany()
                    .HasForeignKey(f => f.TourGuideId)
                    .OnDelete(DeleteBehavior.NoAction); // Changed to NO ACTION
                entity.HasOne(f => f.Hotel)
                    .WithMany()
                    .HasForeignKey(f => f.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.Restaurant)
                    .WithMany()
                    .HasForeignKey(f => f.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(f => f.Plan)
                    .WithMany()
                    .HasForeignKey(f => f.PlanId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(f => new { f.UserId, f.PlaceId }).IsUnique().HasFilter("[PlaceId] IS NOT NULL");
                entity.HasIndex(f => new { f.UserId, f.TourGuideId }).IsUnique().HasFilter("[TourGuideId] IS NOT NULL");
                entity.HasIndex(f => new { f.UserId, f.HotelId }).IsUnique().HasFilter("[HotelId] IS NOT NULL");
                entity.HasIndex(f => new { f.UserId, f.RestaurantId }).IsUnique().HasFilter("[RestaurantId] IS NOT NULL");
                entity.HasIndex(f => new { f.UserId, f.PlanId }).IsUnique().HasFilter("[PlanId] IS NOT NULL");
            });

            // Review configuration
            builder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.UserId).IsRequired();
                entity.Property(r => r.Rating).IsRequired();
                entity.Property(r => r.Comment).HasMaxLength(1000);
                entity.Property(r => r.CreatedAt).IsRequired();
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Place)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.PlaceId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.TourGuide)
                    .WithMany(tg => tg.Reviews)
                    .HasForeignKey(r => r.TourGuideId)
                    .OnDelete(DeleteBehavior.NoAction); // Changed to NO ACTION
                entity.HasOne(r => r.Hotel)
                    .WithMany(h => h.Reviews)
                    .HasForeignKey(r => r.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Restaurant)
                    .WithMany(res => res.Reviews)
                    .HasForeignKey(r => r.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.Plan)
                    .WithMany()
                    .HasForeignKey(r => r.PlanId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Hotel configuration
            builder.Entity<Hotel>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Description).HasMaxLength(1000);
                entity.Property(h => h.City).IsRequired().HasMaxLength(50);
                entity.Property(h => h.Country).IsRequired().HasMaxLength(50);
                entity.Property(h => h.Location).HasMaxLength(200);
                entity.Property(h => h.Latitude).IsRequired();
                entity.Property(h => h.Longitude).IsRequired();
                entity.Property(h => h.StartingPrice).HasColumnType("decimal(18,2)");
                entity.Property(h => h.Rate).HasColumnType("decimal(18,2)");
                entity.Property(h => h.ThumbnailUrl).HasMaxLength(200);
                entity.Property(h => h.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
            });

            // Room configuration
            builder.Entity<Room>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.HotelId).IsRequired();
                entity.Property(r => r.Type).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Capacity).IsRequired();
                entity.Property(r => r.PricePerNight).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(r => r.Description).HasMaxLength(500);
                entity.Property(r => r.IsAvailable).IsRequired();
                entity.Property(r => r.ThumbnailUrl).HasMaxLength(500);
                entity.Property(r => r.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
                entity.HasOne(r => r.Hotel)
                    .WithMany(h => h.Rooms)
                    .HasForeignKey(r => r.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Restaurant configuration
            builder.Entity<Restaurant>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Description).HasMaxLength(1000);
                entity.Property(r => r.City).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Country).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Location).HasMaxLength(200);
                entity.Property(r => r.Latitude).IsRequired();
                entity.Property(r => r.Longitude).IsRequired();
                entity.Property(r => r.Cuisine).IsRequired().HasMaxLength(50);
                entity.Property(r => r.AveragePrice).HasColumnType("decimal(18,2)");
                entity.Property(r => r.ThumbnailUrl).HasMaxLength(500);
                entity.Property(r => r.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
            });

            // Meal configuration
            builder.Entity<Meal>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.RestaurantId).IsRequired();
                entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(m => m.Description).HasMaxLength(500);
                entity.Property(m => m.ThumbnailUrl).HasMaxLength(500);
                entity.Property(m => m.ImageUrls).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    stringListComparer);
                entity.HasOne(m => m.Restaurant)
                    .WithMany(r => r.Meals)
                    .HasForeignKey(m => m.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Plan configuration
            builder.Entity<Plan>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Days).IsRequired();
                entity.Property(p => p.ThumbnailUrl).HasMaxLength(500);
                entity.Property(p => p.TourGuideId).IsRequired();
                entity.HasOne(p => p.TourGuide)
                    .WithMany(tg => tg.Plans)
                    .HasForeignKey(p => p.TourGuideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PlanPlace configuration
            builder.Entity<PlanPlace>(entity =>
            {
                entity.HasKey(pp => pp.Id);
                entity.Property(pp => pp.PlanId).IsRequired();
                entity.Property(pp => pp.PlaceId).IsRequired();
                entity.Property(pp => pp.Duration).IsRequired();
                entity.Property(pp => pp.AdditionalDescription).HasMaxLength(1000);
                entity.Property(pp => pp.SpecialPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(pp => pp.Plan)
                    .WithMany()
                    .HasForeignKey(pp => pp.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(pp => pp.Place)
                    .WithMany()
                    .HasForeignKey(pp => pp.PlaceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Event configuration
            builder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Latitude).IsRequired();
                entity.Property(e => e.Longitude).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            });

            // Activity configuration
            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Description).HasMaxLength(1000);
                entity.Property(a => a.ThumbnailUrl).HasMaxLength(500);
                entity.Property(a => a.Price).HasColumnType("decimal(18,2)");
                entity.Property(a => a.PlaceId).IsRequired();
                entity.HasOne(a => a.Place)
                    .WithMany(p => p.Activities)
                    .HasForeignKey(a => a.PlaceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}