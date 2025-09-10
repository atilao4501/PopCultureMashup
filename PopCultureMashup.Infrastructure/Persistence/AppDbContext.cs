using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

        // USERS
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

        // ITEMS
        modelBuilder.Entity<Item>(e =>
        {
            e.ToTable("Items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<byte>().IsRequired(); // tinyint
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.Year);                 // int? (NULL)
            e.Property(x => x.Popularity);           // float? (NULL)
            e.Property(x => x.Summary);              // nvarchar(max)? (NULL)
            e.Property(x => x.Source).HasMaxLength(32).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();

            // Índices
            e.HasIndex(x => new { x.Type, x.Title });
            e.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
        });

        // ITEM GENRES (join)  Item 1 ── * ItemGenres
        modelBuilder.Entity<ItemGenre>(e =>
        {
            e.ToTable("ItemGenres");
            e.HasKey(x => new { x.ItemId, x.Genre });
            e.Property(x => x.Genre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Genre);

            e.HasOne<Item>()
             .WithMany(i => i.Genres)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Cascade); // apagar Item remove vínculos
        });

        // ITEM THEMES (join)
        modelBuilder.Entity<ItemTheme>(e =>
        {
            e.ToTable("ItemThemes");
            e.HasKey(x => new { x.ItemId, x.Theme });
            e.Property(x => x.Theme).HasMaxLength(120).IsRequired(); 
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired(); 
            e.HasIndex(x => x.Theme);
            
            e.HasIndex(x => x.Slug);
            e.HasIndex(x => x.Theme);

            e.HasOne<Item>()
             .WithMany(i => i.Themes)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ITEM CREATORS (join)
        modelBuilder.Entity<ItemCreator>(e =>
        {
            e.ToTable("ItemCreators");
            e.HasKey(x => new { x.ItemId, x.CreatorName });
            e.Property(x => x.CreatorName).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.CreatorName);

            e.HasOne<Item>()
             .WithMany(i => i.Creators)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SEEDS
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

            // Índice para consultas recentes por usuário (DESC no CreatedAt)
            e.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);

            // Evita seed duplicado do mesmo item para o mesmo usuário
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();

            // FK: Seed -> Item (Restrict)
            e.HasOne(s => s.Item)        
                .WithMany()              
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

        });

        // RECOMMENDATIONS
        modelBuilder.Entity<Recommendation>(e =>
        {
            e.ToTable("Recommendations");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.Direction).HasConversion<byte>().IsRequired(); // tinyint
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            e.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
        });

        // RECOMMENDATION RESULTS
        modelBuilder.Entity<RecommendationResult>(e =>
        {
            e.ToTable("RecommendationResults");
            e.HasKey(x => x.Id);
            e.Property(x => x.RecommendationId).IsRequired();
            e.Property(x => x.ItemId).IsRequired();
            e.Property(x => x.Rank).IsRequired();

            // Scores com precisão decimal(5,2)
            e.Property(x => x.Score).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.GenresScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.ThemesScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.YearScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.PopularityScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.TextScore).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.FranchiseBonus).HasColumnType("decimal(5,2)").IsRequired();

            // Result -> Recommendation (Cascade) | Result -> Item (Restrict)
            e.HasOne<Recommendation>()
                .WithMany(r => r.Results)
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FEEDBACK
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

            // Feedback -> Recommendation (Restrict), Feedback -> Item (Restrict)
            e.HasOne<Recommendation>()
                .WithMany()
                .HasForeignKey(x => x.RecommendationId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WEIGHTS
        modelBuilder.Entity<Weight>(e =>
        {
            e.ToTable("Weights");
            e.HasKey(x => x.Id);
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

            // Permite 0 ou 1 conjunto por usuário (UserId NULL = global)
            e.HasIndex(x => x.UserId)
             .IsUnique()
             .HasFilter("[UserId] IS NOT NULL");
        });
    }
}
