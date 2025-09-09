using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemGenre> ItemGenres => Set<ItemGenre>();
    public DbSet<ItemTheme> ItemThemes => Set<ItemTheme>();
    public DbSet<ItemCreator> ItemCreators => Set<ItemCreator>();
    public DbSet<Seed> Seeds => Set<Seed>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationResult> RecommendationResults => Set<RecommendationResult>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Weight> Weights => Set<Weight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.ToTable("Items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type)
                .HasConversion<byte>()
                .IsRequired(); // tinyint
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.Year); // int null
            e.Property(x => x.Popularity); // float null
            e.Property(x => x.Summary); // nvarchar(max) null
            e.Property(x => x.Source).HasMaxLength(32).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Type, x.Title });
            e.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<ItemGenre>(e =>
        {
            e.ToTable("ItemGenres");
            e.HasKey(x => new { x.ItemId, x.Genre });
            e.Property(x => x.Genre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Genre);
        });

        modelBuilder.Entity<ItemTheme>(e =>
        {
            e.ToTable("ItemThemes");
            e.HasKey(x => new { x.ItemId, x.Theme });
            e.Property(x => x.Theme).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Theme);
        });

        modelBuilder.Entity<ItemCreator>(e =>
        {
            e.ToTable("ItemCreators");
            e.HasKey(x => new { x.ItemId, x.CreatorName });
            e.Property(x => x.CreatorName).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.CreatorName);
        });

        modelBuilder.Entity<Seed>(e =>
        {
            e.ToTable("Seeds");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.ItemId).IsRequired();
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
            e.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);

            // FK: Seed -> Item (Restrict)
            e.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recommendation>(e =>
        {
            e.ToTable("Recommendations");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.Direction)
                .HasConversion<byte>()
                .IsRequired(); // tinyint
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
            e.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
        });

        modelBuilder.Entity<RecommendationResult>(e =>
        {
            e.ToTable("RecommendationResults");
            e.HasKey(x => x.Id);
            e.Property(x => x.RecommendationId).IsRequired();
            e.Property(x => x.ItemId).IsRequired();
            e.Property(x => x.Rank).IsRequired();
            e.Property(x => x.Score).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.GenresScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.ThemesScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.YearScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.PopularityScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.TextScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.FranchiseBonus).HasColumnType("decimal(5,2)").IsRequired();

            // FKs: Result -> Recommendation (Cascade), Result -> Item (Restrict)
            e.HasOne<Recommendation>()
                .WithMany(r => r.Results)
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Feedback>(e =>
        {
            e.ToTable("Feedback");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.RecommendationId).IsRequired();
            e.Property(x => x.ItemId).IsRequired();
            e.Property(x => x.Value).HasColumnType("smallint").IsRequired();
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            // FKs: Feedback -> Recommendation (Restrict), Feedback -> Item (Restrict)
            e.HasOne<Recommendation>()
                .WithMany()
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Weight>(e =>
        {
            e.ToTable("Weights");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId);
            e.Property(x => x.Genres).IsRequired();
            e.Property(x => x.Themes).IsRequired();
            e.Property(x => x.Year).IsRequired();
            e.Property(x => x.Popularity).IsRequired();
            e.Property(x => x.Text).IsRequired();
            e.Property(x => x.Franchise).IsRequired();
            e.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
        });
    }
}
