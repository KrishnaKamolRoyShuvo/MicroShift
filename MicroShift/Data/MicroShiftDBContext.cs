using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicroShift.Models;

namespace MicroShift.Data
{
    public class MicroShiftDBContext : IdentityDbContext<ApplicationUser>
    {
        public MicroShiftDBContext(DbContextOptions<MicroShiftDBContext> options)
            : base(options)
        {
        }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Transaction> Transactions { get; set; }



        // 1. Register the new table
        public DbSet<JobApplication> JobApplications { get; set; }

        // 2. Prevent the SQL Server "Cascade Delete" crash
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<JobApplication>()
                .HasOne(a => a.Worker)
                .WithMany()
                .HasForeignKey(a => a.WorkerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents the database crash


            builder.Entity<Message>()
        .HasOne(m => m.Sender)
        .WithMany()
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
    .HasOne(r => r.Reviewer)
    .WithMany()
    .HasForeignKey(r => r.ReviewerId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);


        }
    }
}