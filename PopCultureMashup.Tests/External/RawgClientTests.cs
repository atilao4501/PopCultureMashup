using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External;
using RichardSzalay.MockHttp;
using Xunit;

namespace PopCultureMashup.Tests.External;

public class RawgClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Mock<ILogger<RawgClient>> _mockLogger;
    private const string ApiBaseUrl = "https://api.rawg.io/api";

    public RawgClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _mockLogger = new Mock<ILogger<RawgClient>>();
    }

    /// <summary>
    /// Cria um RawgClient com HttpClient já configurado com BaseAddress.
    /// </summary>
    private RawgClient CreateClient()
    {
        var httpClient = new HttpClient(_mockHttp)
        {
            BaseAddress = new Uri(ApiBaseUrl + "/")
        };

        var options = Options.Create(new RawgOptions
        {
            ApiKey = "fake-api-key",
            BaseUrl = ApiBaseUrl
        });

        return new RawgClient(httpClient, options);
    }

    [Fact]
    public async Task SearchGamesAsync_WithValidResponse_MapsToItems()
    {
        // Arrange
        var gameJson = @"{
            ""results"": [
                {
                    ""id"": 3498,
                    ""slug"": ""the-witcher-3-wild-hunt"",
                    ""name"": ""The Witcher 3: Wild Hunt"",
                    ""released"": ""2015-05-18"",
                    ""genres"": [
                        { ""id"": 4, ""name"": ""Action"" },
                        { ""id"": 5, ""name"": ""RPG"" }
                    ],
                    ""tags"": [
                        { ""id"": 31, ""name"": ""Singleplayer"", ""slug"": ""singleplayer"" },
                        { ""id"": 64, ""name"": ""Fantasy"", ""slug"": ""fantasy"" }
                    ],
                    ""developers"": [
                        { ""id"": 9, ""name"": ""CD Projekt Red"", ""slug"": ""cd-projekt-red"" }
                    ]
                }
            ]
        }";

        _mockHttp
            .When($"{ApiBaseUrl}/games*")
            .Respond("application/json", gameJson);

        var rawgClient = CreateClient();

        // Act
        var games = await rawgClient.SearchGamesAsync("witcher", ct: CancellationToken.None);
        var gamesList = games.ToList();

        // Assert
        Assert.Single(gamesList);

        var game = gamesList[0];
        Assert.Equal("RAWG", game.Source, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("3498", game.ExternalId, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ItemType.Game, game.Type);
        Assert.Equal("The Witcher 3: Wild Hunt", game.Title, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2015, game.Year);
        

        Assert.Equal(2, game.Themes.Count);
        Assert.Contains(game.Themes, t => t.Theme.ToLower() == "singleplayer");
        Assert.Contains(game.Themes, t => t.Theme.ToLower() == "fantasy");
        
        Assert.Single(game.Creators);
        Assert.Contains(game.Creators, c => c.CreatorName == "CD Projekt Red");
    }

    [Fact]
    public async Task SearchGamesAsync_WithMissingFields_HandlesGracefully()
    {
        // Arrange (JSON válido, sem comentários)
        var gameJson = @"{
            ""results"": [
                {
                    ""id"": 1234,
                    ""slug"": ""incomplete-game"",
                    ""name"": ""Incomplete Game""
                }
            ]
        }";

        _mockHttp
            .When($"{ApiBaseUrl}/games*")
            .Respond("application/json", gameJson);

        var rawgClient = CreateClient();

        // Act - Should not throw
        var games = await rawgClient.SearchGamesAsync("incomplete", ct: CancellationToken.None);
        var gamesList = games.ToList();

        // Assert
        Assert.Single(gamesList);

        var game = gamesList[0];
        Assert.Equal("RAWG", game.Source, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("1234", game.ExternalId, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Incomplete Game", game.Title, StringComparer.OrdinalIgnoreCase);
        Assert.Null(game.Year);      // sem released
        Assert.Empty(game.Genres);   // coleções vazias, não nulas
        Assert.Empty(game.Themes);
        Assert.Empty(game.Creators);
    }

    [Fact]
    public async Task SearchGamesAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttp
            .When($"{ApiBaseUrl}/games*")
            .Respond(HttpStatusCode.InternalServerError);

        var rawgClient = CreateClient();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            rawgClient.SearchGamesAsync("error", ct: CancellationToken.None)
        );
    }

    [Fact]
    public async Task SearchGamesAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var emptyJson = @"{ ""results"": [] }";

        _mockHttp
            .When($"{ApiBaseUrl}/games*")
            .Respond("application/json", emptyJson);

        var rawgClient = CreateClient();

        // Act
        var games = await rawgClient.SearchGamesAsync("nonexistent", ct: CancellationToken.None);

        // Assert
        Assert.Empty(games);
    }
}
