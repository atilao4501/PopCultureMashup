using Microsoft.Extensions.Options;
using PopCultureMashup.Application.Services;
using PopCultureMashup.Application.Settings;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Tests.Services;

public class RecommendationRankerTests
{
    private readonly IOptions<RecommendationSettings> _options;
    
    public RecommendationRankerTests()
    {
        var settings = new RecommendationSettings
        {
            SimilarityWeight = 0.65,
            PopularityWeight = 0.1,
            RecencyWeight = 0.05,
            NoveltyWeight = 0.2,
            UseDiversification = true,
            DiversificationK = 50,
            ThemeWeightDefault = 0.5,
            GenreWeightDefault = 0.3,
            CreatorWeightDefault = 0.2,
            ThemeWeightBooks = 0.6,
            GenreWeightBooks = 0.15,
            CreatorWeightBooks = 0.25,
            HalfLifeGames = 4,
            HalfLifeBooks = 15
        };
    
        _options = Options.Create(settings);
    }
    
    [Fact]
    public void Rank_WhenThemeMatchesExist_ShouldRankHigherThanOnlyGenreOrCreator()
    {
        
   
        
        // Arrange
        var ranker = new RecommendationRanker(_options);
        
        var seed = new Seed 
        { 
            Item = new Item 
            {
                Type = ItemType.Game,
                Themes = new List<ItemTheme> { new() { Theme = "Soulslike" } },
                Genres = new List<ItemGenre> { new() { Genre = "Action RPG" } },
                Creators = new List<ItemCreator> { new() { CreatorName = "FromSoftware" } }
            }
        };
        
        var themeMatchItem = new Item 
        { 
            Type = ItemType.Game,
            Year = 2022,
            Themes = new List<ItemTheme> { new() { Theme = "Soulslike" } }
        };
        
        var genreMatchItem = new Item 
        { 
            Type = ItemType.Game,
            Year = 2022,
            Genres = new List<ItemGenre> { new() { Genre = "Action RPG" } }
        };
        
        var creatorMatchItem = new Item 
        { 
            Type = ItemType.Game,
            Year = 2022,
            Creators = new List<ItemCreator> { new() { CreatorName = "FromSoftware" } }
        };

        // Act
        var ranked = ranker.Rank(
            new List<Item> { themeMatchItem, genreMatchItem, creatorMatchItem },
            new List<Seed> { seed },
            null
        );

        // Assert
        var orderedResults = ranked.ToList();
        Assert.Equal(themeMatchItem, orderedResults[0].Item); // Theme match should be first (weight 0.50)
        Assert.Equal(genreMatchItem, orderedResults[1].Item); // Genre match should be second (weight 0.30)
        Assert.Equal(creatorMatchItem, orderedResults[2].Item); // Creator match should be third (weight 0.20)
    }

    [Fact]
    public void Rank_PrefersRecentAndThemeMatches()
    {
        // Arrange
        var ranker = new RecommendationRanker(_options);
        
        var seed = new Seed 
        { 
            Item = new Item 
            {
                Type = ItemType.Game,
                Themes = new List<ItemTheme> { new() { Theme = "Soulslike" } },
                Genres = new List<ItemGenre> { new() { Genre = "Action RPG" } },
                Creators = new List<ItemCreator> { new() { CreatorName = "FromSoftware" } }
            }
        };
        
        var older = new Item 
        { 
            Type = ItemType.Game, 
            Year = 2011,
            Themes = new List<ItemTheme> { new() { Theme = "Soulslike" } } 
        };
        
        var newer = new Item 
        { 
            Type = ItemType.Game, 
            Year = 2022,
            Themes = new List<ItemTheme> { new() { Theme = "Soulslike" } } 
        };

        // Act
        var ranked = ranker.Rank(new List<Item> { older, newer }, new List<Seed> { seed }, null);

        // Assert
        var orderedResults = ranked.ToList();
        Assert.Equal(newer, orderedResults[0].Item);
        Assert.Equal(older, orderedResults[1].Item);
    }

    [Fact]
    public void Rank_AppliesDifferentHalfLifeBasedOnItemType()
    {
        // Arrange
        var ranker = new RecommendationRanker(_options);
        var currentYear = DateTime.Now.Year;
        
        var seed = new Seed 
        { 
            Item = new Item 
            {
                Type = ItemType.Game,
                Themes = new List<ItemTheme> { new() { Theme = "Fantasy" } }
            }
        };
        
        // Game with age of half-life (4 years old)
        var game = new Item 
        { 
            Type = ItemType.Game, 
            Year = currentYear - 4,
            Themes = new List<ItemTheme> { new() { Theme = "Fantasy" } } 
        };
        
        // Book with age of half-life (15 years old)
        var book = new Item 
        { 
            Type = ItemType.Book, 
            Year = currentYear - 15,
            Themes = new List<ItemTheme> { new() { Theme = "Fantasy" } } 
        };
        
        // Recent book (1 year old)
        var recentBook = new Item 
        { 
            Type = ItemType.Book, 
            Year = currentYear - 1,
            Themes = new List<ItemTheme> { new() { Theme = "Fantasy" } } 
        };

        // Act
        var ranked = ranker.Rank(new List<Item> { game, book, recentBook }, new List<Seed> { seed }, null);

        // Assert
        var orderedResults = ranked.ToList();
        // Recent book should rank first, despite different type than seed
        Assert.Equal(recentBook, orderedResults[0].Item);
        // Game at half-life should rank higher than book at half-life
        Assert.Equal(game, orderedResults[1].Item);
        Assert.Equal(book, orderedResults[2].Item);
    }
}
