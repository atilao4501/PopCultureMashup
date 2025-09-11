using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PopCultureMashup.Application.Abstractions;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Recommend;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using Xunit;

namespace PopCultureMashup.Tests.UseCases
{
    public class GenerateRecommendationsHandlerTests
    {
        private readonly Mock<ISeedRepository> _seedRepo = new();
        private readonly Mock<IItemRepository> _itemRepo = new();
        private readonly Mock<IRecommendationRepository> _recRepo = new();
        private readonly Mock<IRawgClient> _rawg = new();
        private readonly Mock<IOpenLibraryClient> _openLib = new();
        private readonly Mock<IRecommendationRanker> _ranker = new();
        private readonly Mock<ILogger<GenerateRecommendationsHandler>> _logger = new();

        private GenerateRecommendationsHandler CreateHandler()
            => new(
                _seedRepo.Object,
                _itemRepo.Object,
                _recRepo.Object,
                _rawg.Object,
                _openLib.Object,
                _logger.Object,
                _ranker.Object
            );

        private static Seed MakeSeedGameWithTheme(string theme, string? genre = null, string? creator = null)
        {
            return new Seed
            {
                Item = new Item
                {
                    Type = ItemType.Game,
                    Themes = new List<ItemTheme>
                        { new() { Theme = theme, Slug = theme?.ToLowerInvariant().Replace(' ', '-') } },
                    Genres = string.IsNullOrWhiteSpace(genre)
                        ? new List<ItemGenre>()
                        : new List<ItemGenre> { new() { Genre = genre } },
                    Creators = string.IsNullOrWhiteSpace(creator)
                        ? new List<ItemCreator>()
                        : new List<ItemCreator>
                            { new() { CreatorName = creator, Slug = creator!.ToLowerInvariant().Replace(' ', '-') } }
                }
            };
        }

        [Fact]
        public async Task HandleAsync_WhenNoSeeds_ThrowsInvalidOperationException()
        {
            // Arrange
            _seedRepo
                .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Seed>());

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(
                    new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(Guid.NewGuid(), 10),
                    CancellationToken.None));
        }

        [Fact]
public async Task HandleAsync_ContinuesWhenOneClientFails()
{
    // Arrange
    var userId = Guid.NewGuid();
    var seeds = new List<Seed> { MakeSeedGameWithTheme("Action") };

    _seedRepo
        .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(seeds);

    // RAWG OK: DiscoverGamesAsync(subjects, themes, pageSize, ct)
    var rawgItem = new Item { Source = "RAWG", ExternalId = "g-1", Type = ItemType.Game, Title = "Game A" };
    _rawg
        .Setup(c => c.DiscoverGamesAsync(
            It.IsAny<IEnumerable<string>>(),   // subjects/genres
            It.IsAny<IEnumerable<string>>(),   // themes
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Item> { rawgItem });

    // OpenLibrary FALHA: DiscoverBooksAsync(themes, creators, limit, ct)  <-- 4 parâmetros
    _openLib
        .Setup(c => c.DiscoverBooksAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new HttpRequestException("API Error"));

    // Rank: retorna lista vazia
    _ranker
        .Setup(r => r.Rank(
            It.IsAny<IEnumerable<Item>>(),
            It.IsAny<IEnumerable<Seed>>(),
            It.IsAny<RankingDTOs.RankingOptions?>(),
            It.IsAny<CancellationToken>()))
        .Returns(new List<ScoredItemDTOs.ScoredItem>());

    // UpsertRangeAsync: retorne uma LISTA vazia de verdade
    _itemRepo
        .Setup(r => r.UpsertRangeAsync(It.IsAny<IEnumerable<Item>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Item>());

    // SaveAsync deve retornar Task<Recommendation>
    _recRepo
        .Setup(r => r.SaveAsync(It.IsAny<Recommendation>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Recommendation rec, CancellationToken _) => rec);

    var handler = CreateHandler();

    // Act (não deve lançar)
    await handler.HandleAsync(
        new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(userId, 10),
        CancellationToken.None);

    // Assert
    _rawg.Verify(c => c.DiscoverGamesAsync(
        It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(),
        It.IsAny<CancellationToken>()), Times.Once);

    _openLib.Verify(c => c.DiscoverBooksAsync(
        It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(),It.IsAny<int>() ,
        It.IsAny<CancellationToken>()), Times.Once);

    _ranker.Verify(r => r.Rank(
        It.Is<IEnumerable<Item>>(xs => xs.Any(i => i.Title == "Game A")),
        It.Is<IEnumerable<Seed>>(s => s.SequenceEqual(seeds)),
        It.IsAny<RankingDTOs.RankingOptions?>(),
        It.IsAny<CancellationToken>()), Times.Once);
}


        [Fact]
        public async Task HandleAsync_WhenBothClientsFail_ThrowsInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var seeds = new List<Seed> { MakeSeedGameWithTheme("Action") };

            _seedRepo
                .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(seeds);

            // Ambos falham -> tasks não RanToCompletion -> failures == tasks.Count -> InvalidOperationException
            _rawg
                .Setup(c => c.DiscoverGamesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("RAWG down"));

            _openLib
                .Setup(c => c.DiscoverBooksAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("OpenLibrary down"));

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(
                    new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(userId, 10),
                    CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_PropagatesCancellationToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var seeds = new List<Seed> { MakeSeedGameWithTheme("Action") };
            var cts = new CancellationTokenSource();
            var ct = cts.Token;

            _seedRepo
                .Setup(r => r.GetByUserIdAsync(userId, ct))
                .ReturnsAsync(seeds);

            _rawg
                .Setup(c => c.DiscoverGamesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    ct))
                .ReturnsAsync(new List<Item>
                    { new() { Source = "RAWG", ExternalId = "g-1", Type = ItemType.Game, Title = "Game A" } });

            _openLib
                .Setup(c => c.DiscoverBooksAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    ct))
                .ReturnsAsync(new List<Item>
                    { new() { Source = "OpenLibrary", ExternalId = "b-1", Type = ItemType.Book, Title = "Book A" } });

            _ranker
                .Setup(r => r.Rank(
                    It.IsAny<IEnumerable<Item>>(),
                    It.IsAny<IEnumerable<Seed>>(),
                    It.IsAny<RankingDTOs.RankingOptions?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(new List<ScoredItemDTOs.ScoredItem>());

            _itemRepo
                .Setup(r => r.UpsertRangeAsync(It.IsAny<IEnumerable<Item>>(), ct))
                .ReturnsAsync(new List<Item>());

            _recRepo
                .Setup(r => r.SaveAsync(It.IsAny<Recommendation>(), ct))
                .ReturnsAsync((Recommendation rec, CancellationToken _) => rec);

            var handler = CreateHandler();

            // Act
            await handler.HandleAsync(
                new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(userId, 10),
                ct);

            // Assert: todos chamados com o mesmo token
            _seedRepo.Verify(r => r.GetByUserIdAsync(userId, ct), Times.Once);
            _rawg.Verify(
                c => c.DiscoverGamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(), ct), Times.Once);
            _openLib.Verify(
                c => c.DiscoverBooksAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(), It.IsAny<int>(), ct), Times.Once);
            _recRepo.Verify(r => r.SaveAsync(It.IsAny<Recommendation>(), ct), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_CreatesAndPersistsRecommendation_WithResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var seeds = new List<Seed>
            {
                // inclui theme/genre/creator para alimentar os Discover*
                MakeSeedGameWithTheme("Action", genre: "RPG", creator: "FromSoft")
            };

            _seedRepo
                .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(seeds);

            var rawgCandidate = new Item
            {
                Source = "RAWG", ExternalId = "3498", Type = ItemType.Game, Title = "Elden Ring", Year = 2022,
                Themes = new List<ItemTheme> { new() { Theme = "Action", Slug = "action" } }
            };

            var openLibCandidate = new Item
            {
                Source = "OpenLibrary", ExternalId = "OL-DFT", Type = ItemType.Book, Title = "Dark Fantasy Tales",
                Year = 2019,
                Themes = new List<ItemTheme> { new() { Theme = "Dark Fantasy", Slug = "dark-fantasy" } }
            };

            _rawg
                .Setup(c => c.DiscoverGamesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Item> { rawgCandidate });

            _openLib
                .Setup(c => c.DiscoverBooksAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Item> { openLibCandidate });

            // Rank devolve ambos, com scores
            var scored = new List<ScoredItemDTOs.ScoredItem>
            {
                new(rawgCandidate, 0.92),
                new(openLibCandidate, 0.71)
            };
            _ranker
                .Setup(r => r.Rank(
                    It.Is<IEnumerable<Item>>(xs => xs.Count() == 2),
                    It.Is<IEnumerable<Seed>>(s => s.SequenceEqual(seeds)),
                    It.IsAny<RankingDTOs.RankingOptions?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(scored);

            // UpsertRangeAsync deve devolver itens com Ids (mesmos Source/ExternalId) para montar o idMap
            _itemRepo
                .Setup(r => r.UpsertRangeAsync(It.IsAny<IEnumerable<Item>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Item> items, CancellationToken _) =>
                    items.Select(i => new Item
                    {
                        Id = Guid.NewGuid(),
                        Source = i.Source,
                        ExternalId = i.ExternalId,
                        Type = i.Type,
                        Title = i.Title,
                        Year = i.Year
                    }).ToList());

            Recommendation? captured = null;
            _recRepo
                .Setup(r => r.SaveAsync(It.IsAny<Recommendation>(), It.IsAny<CancellationToken>()))
                .Callback<Recommendation, CancellationToken>((rec, _) => captured = rec)
                .ReturnsAsync((Recommendation rec, CancellationToken _) => rec);

            var handler = CreateHandler();

            // Act
            await handler.HandleAsync(
                new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(userId, 10),
                CancellationToken.None);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(userId, captured!.UserId);
            Assert.True(captured.TotalCandidates >= 2);
            Assert.True(captured.TotalReturned >= 2);
            Assert.NotEmpty(captured.Results);
            // os itens persistidos foram mapeados por Source||ExternalId
            Assert.All(captured.Results, r => Assert.NotEqual(Guid.Empty, r.ItemId));
        }
    }
}