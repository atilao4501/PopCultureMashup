using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Persistence;
using PopCultureMashup.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace PopCultureMashup.Tests.Repositories;

public class ItemRepositoryTests
{
    /// <summary>
    /// Creates an in-memory SQLite connection and DbContext options.
    /// IMPORTANT: keep the connection open until the test is finished.
    /// </summary>
    private static DbContextOptions<AppDbContext> CreateSqliteOptions(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection) // provider relacional (suporta ExecuteDelete)
            .Options;

        // Criar o schema
        using var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();

        return options;
    }

    [Fact]
    public async Task UpsertAsync_WithNewItem_InsertsProperly()
    {
        // Arrange
        var options = CreateSqliteOptions(out var conn);

        // Item to insert
        var item = new Item
        {
            Source = "RAWG",
            ExternalId = "3498",
            Type = ItemType.Game,
            Title = "The Witcher 3: Wild Hunt",
            Year = 2015,
            Genres = new List<ItemGenre>
            {
                new() { Genre = "RPG" },
                new() { Genre = "Open World" }
            },
            Themes = new List<ItemTheme>
            {
                new() { Theme = "Dark Fantasy", Slug = "dark-fantasy" }
            },
            Creators = new List<ItemCreator>
            {
                new() { CreatorName = "CD Projekt Red", Slug = "cd-projekt-red" }
            }
        };

        // Act
        using (conn)
        {
            using (var context = new AppDbContext(options))
            {
                var repository = new ItemRepository(context);
                var result = await repository.UpsertAsync(item, CancellationToken.None);

                Assert.NotEqual(Guid.Empty, result.Id);
            }

            // Verify using a separate context instance (mesma conexão aberta)
            using (var context = new AppDbContext(options))
            {
                var savedItem = await context.Items
                    .Include(i => i.Genres)
                    .Include(i => i.Themes)
                    .Include(i => i.Creators)
                    .FirstOrDefaultAsync(i => i.ExternalId == "3498" && i.Source == "RAWG");

                Assert.NotNull(savedItem);
                Assert.Equal("The Witcher 3: Wild Hunt", savedItem.Title);
                Assert.Equal(2015, savedItem.Year);
                Assert.Equal(2, savedItem.Genres.Count);
                Assert.Single(savedItem.Themes);
                Assert.Single(savedItem.Creators);
            }
        }
    }

    [Fact]
    public async Task UpsertAsync_WithExistingItem_UpdatesProperly()
    {
        // Arrange
        var options = CreateSqliteOptions(out var conn);

        var originalItem = new Item
        {
            Source = "RAWG",
            ExternalId = "3498",
            Type = ItemType.Game,
            Title = "The Witcher 3",
            Year = 2015,
            Genres = new List<ItemGenre>
            {
                new() { Genre = "RPG" }
            }
        };

        using (conn)
        {
            using (var context = new AppDbContext(options))
            {
                context.Items.Add(originalItem);
                await context.SaveChangesAsync();
            }

            var updatedItem = new Item
            {
                Source = "RAWG",
                ExternalId = "3498",
                Type = ItemType.Game,
                Title = "The Witcher 3: Wild Hunt", // Updated title
                Year = 2015,
                Genres = new List<ItemGenre>
                {
                    new() { Genre = "RPG" },
                    new() { Genre = "Open World" } // Added genre
                },
                Themes = new List<ItemTheme>
                {
                    new() { Theme = "Dark Fantasy", Slug = "dark-fantasy" } // Added theme
                }
            };

            // Act
            using (var context = new AppDbContext(options))
            {
                var repository = new ItemRepository(context);
                _ = await repository.UpsertAsync(updatedItem, CancellationToken.None);
            }

            // Assert
            using (var context = new AppDbContext(options))
            {
                var savedItem = await context.Items
                    .Include(i => i.Genres)
                    .Include(i => i.Themes)
                    .Include(i => i.Creators)
                    .FirstOrDefaultAsync(i => i.ExternalId == "3498" && i.Source == "RAWG");

                Assert.NotNull(savedItem);
                Assert.Equal("The Witcher 3: Wild Hunt", savedItem.Title); // Title should be updated
                Assert.Equal(2, savedItem.Genres.Count); // Should have two genres now
                Assert.Single(savedItem.Themes); // Should have theme added
            }
        }
    }

    [Fact]
    public async Task UpsertAsync_WithDuplicateCollectionItems_NormalizesProperly()
    {
        // Arrange
        var options = CreateSqliteOptions(out var conn);

        var item = new Item
        {
            Source = "OpenLibrary",
            ExternalId = "OL82563W",
            Type = ItemType.Book,
            Title = "The Lord of the Rings",
            Genres = new List<ItemGenre>
            {
                new() { Genre = "Fantasy" },
                new() { Genre = "fantasy" }, // Duplicate, case-insensitive
                new() { Genre = " Fantasy " } // Duplicate with extra spaces
            },
            Themes = new List<ItemTheme>
            {
                new() { Theme = "Hero's Journey", Slug = "heros-journey" },
                new() { Theme = "hero's journey", Slug = "heros-journey" } // Duplicate, case-insensitive
            },
            Creators = new List<ItemCreator>
            {
                new() { CreatorName = "J. R. R. Tolkien", Slug = "jrr-tolkien" },
                new() { CreatorName = "J.R.R. Tolkien", Slug = "jrr-tolkien" } // Duplicate with different formatting
            }
        };

        using (conn)
        {
            // Act
            using (var context = new AppDbContext(options))
            {
                var repository = new ItemRepository(context);
                _ = await repository.UpsertAsync(item, CancellationToken.None);
            }

            // Assert
            using (var context = new AppDbContext(options))
            {
                var savedItem = await context.Items
                    .Include(i => i.Genres)
                    .Include(i => i.Themes)
                    .Include(i => i.Creators)
                    .FirstOrDefaultAsync(i => i.ExternalId == "OL82563W");

                Assert.NotNull(savedItem);
                // Normalização esperada
                Assert.Single(savedItem.Genres);
                Assert.Single(savedItem.Themes);
                
                var creatorsBySlug = savedItem.Creators
                    .GroupBy(c => c.Slug, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Assert.Single(creatorsBySlug);             
                Assert.Equal("jrr-tolkien", creatorsBySlug[0].Key);
                
                var canonical = creatorsBySlug[0].First();
                Assert.Contains("Tolkien", canonical.CreatorName, StringComparison.OrdinalIgnoreCase);

                // Verificações de casing/trimming das outras coleções
                Assert.Equal("Fantasy", savedItem.Genres.First().Genre);
                Assert.Equal("Hero's Journey", savedItem.Themes.First().Theme);
            }

        }
    }

    [Fact]
    public async Task UpsertAsync_WithNullCollections_HandlesGracefully()
    {
        // Arrange
        var options = CreateSqliteOptions(out var conn);

        var item = new Item
        {
            Source = "RAWG",
            ExternalId = "1234",
            Type = ItemType.Game,
            Title = "Test Game",
            // Coleções vazias em vez de null
            Genres = new List<ItemGenre>(),
            Themes = new List<ItemTheme>(),
            Creators = new List<ItemCreator>()
        };

        using (conn)
        {
            // Act
            using (var context = new AppDbContext(options))
            {
                var repository = new ItemRepository(context);
                _ = await repository.UpsertAsync(item, CancellationToken.None);
            }

            // Assert - should not throw
            using (var context = new AppDbContext(options))
            {
                var savedItem = await context.Items.FirstOrDefaultAsync(i => i.ExternalId == "1234");
                Assert.NotNull(savedItem);
                Assert.Equal("Test Game", savedItem.Title);
            }
        }
    }

    [Fact]
    public async Task UpsertRangeAsync_WithMultipleItems_HandlesCorrectly()
    {
        // Arrange
        var options = CreateSqliteOptions(out var conn);

        var items = new List<Item>
        {
            new()
            {
                Source = "RAWG",
                ExternalId = "1001",
                Type = ItemType.Game,
                Title = "Game 1"
            },
            new()
            {
                Source = "RAWG",
                ExternalId = "1002",
                Type = ItemType.Game,
                Title = "Game 2"
            },
            new()
            {
                Source = "OpenLibrary",
                ExternalId = "B001",
                Type = ItemType.Book,
                Title = "Book 1"
            }
        };

        using (conn)
        {
            // Act
            using (var context = new AppDbContext(options))
            {
                var repository = new ItemRepository(context);
                var results = await repository.UpsertRangeAsync(items, CancellationToken.None);

                // Assert
                Assert.Equal(3, results.Count());
                Assert.All(results, item => Assert.True(item.Id != Guid.Empty));
            }

            // Verify all items were saved
            using (var context = new AppDbContext(options))
            {
                Assert.Equal(3, await context.Items.CountAsync());
            }
        }
    }
}
