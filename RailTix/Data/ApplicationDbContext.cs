using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RailTix.Models.Domain;
using System;

namespace RailTix.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<CmsPage> CmsPages { get; set; } = null!;
        public DbSet<CmsPageComponent> CmsPageComponents { get; set; } = null!;
        public DbSet<CmsReservedRoute> CmsReservedRoutes { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
            });
            builder.Entity<CmsPage>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
                entity.Property(p => p.Slug).HasMaxLength(200).IsRequired();
                entity.Property(p => p.Path).HasMaxLength(512).IsRequired();
                entity.Property(p => p.CustomUrl).HasMaxLength(512);
                entity.HasIndex(p => p.Path).IsUnique();
                entity.HasIndex(p => new { p.ParentId, p.Slug }).IsUnique();
                entity.HasIndex(p => p.IsHomepage).IsUnique().HasFilter("[IsHomepage] = 1");
                entity.HasOne(p => p.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(p => p.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CmsPageComponent>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Type).HasMaxLength(100).IsRequired();
                entity.Property(c => c.SettingsJson).IsRequired();
                entity.HasIndex(c => new { c.PageId, c.Position });
                entity.HasOne(c => c.Page)
                    .WithMany(p => p.Components)
                    .HasForeignKey(c => c.PageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CmsReservedRoute>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Segment).HasMaxLength(200).IsRequired();
                entity.HasIndex(r => r.Segment).IsUnique();
            });

            builder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
                entity.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
                entity.Property(e => e.OrganizerName).HasMaxLength(200);
                entity.Property(e => e.VenueName).HasMaxLength(200);
                entity.Property(e => e.AddressLine1).HasMaxLength(200);
                entity.Property(e => e.AddressLine2).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Region).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.CreatedByUserId);
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            var seedCreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc);
            var homepageId = new Guid("7ae3c9b2-43e9-4d72-88f0-9efb6a0a1f10");
            var reservedEventsId = new Guid("2c4f37b4-3b91-4e21-82a1-5b6d5f60d7c5");
            var reservedAccountId = new Guid("9bb81f34-5b12-4bc0-a2a5-66e9b5d2c1ff");

            builder.Entity<CmsPage>().HasData(new CmsPage
            {
                Id = homepageId,
                Title = "Home",
                Slug = "home",
                Path = "/",
                ParentId = null,
                Position = 0,
                IsHomepage = true,
                IsPublished = true,
                CustomUrl = null,
                CreatedAt = seedCreatedAt,
                UpdatedAt = seedCreatedAt
            });

            builder.Entity<CmsReservedRoute>().HasData(
                new CmsReservedRoute
                {
                    Id = reservedEventsId,
                    Segment = "events",
                    IsActive = true,
                    CreatedAt = seedCreatedAt,
                    UpdatedAt = seedCreatedAt
                },
                new CmsReservedRoute
                {
                    Id = reservedAccountId,
                    Segment = "account",
                    IsActive = true,
                    CreatedAt = seedCreatedAt,
                    UpdatedAt = seedCreatedAt
                }
            );
        }
    }
}


