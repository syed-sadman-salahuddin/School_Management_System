using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes", t =>
                t.HasCheckConstraint("ck_classes_total_seats", "total_seats > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasMaxLength(32);
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.TotalSeats).HasColumnName("total_seats");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasMaxLength(32);
            e.Property(x => x.ClassId).HasColumnName("class_id").HasMaxLength(32).IsRequired();
            e.Property(x => x.StudentId).HasColumnName("student_id").HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
            e.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at").HasColumnType("timestamptz");

            e.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId);

            // Rule 3 enforced by the database: one active/confirmed hold per student per class
            e.HasIndex(x => new { x.ClassId, x.StudentId })
             .IsUnique()
             .HasFilter("status IN ('HELD','CONFIRMED')")
             .HasDatabaseName("ux_holds_active_student_class");

            e.HasIndex(x => new { x.ClassId, x.Status, x.ExpiresAt });
            e.HasIndex(x => x.StudentId);
        });
    }
}