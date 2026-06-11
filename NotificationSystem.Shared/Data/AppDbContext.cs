using Microsoft.EntityFrameworkCore;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();
    public DbSet<ArchivedNotification> ArchivedNotifications => Set<ArchivedNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Индексы для быстрого поиска
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.Status);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.CreatedAt);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.Recipient);

        modelBuilder.Entity<NotificationHistory>()
            .HasIndex(h => h.NotificationId);

        modelBuilder.Entity<NotificationHistory>()
            .HasIndex(h => h.AttemptedAt);
    }
}