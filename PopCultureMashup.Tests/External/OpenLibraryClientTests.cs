using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External;
using RichardSzalay.MockHttp;
using Xunit;

namespace PopCultureMashup.Tests.External;

public class OpenLibraryClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Mock<IOptions<OpenLibraryOptions>> _mockOptions;

    // Mantive apenas o que é usado
    private const string SearchBaseUrl = "https://openlibrary.org/search.json";

    public OpenLibraryClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();

        _mockOptions = new Mock<IOptions<OpenLibraryOptions>>();
        _mockOptions
            .Setup(o => o.Value)
            .Returns(new OpenLibraryOptions { BaseUrl = "https://openlibrary.org" });
    }

    /// <summary>
    /// Cria o client do OpenLibrary com BaseAddress configurado.
    /// </summary>
    private OpenLibraryClient CreateClient()
    {
        var httpClient = new HttpClient(_mockHttp)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };
        return new OpenLibraryClient(httpClient, _mockOptions.Object);
    }

    [Fact]
    public async Task SearchBooksAsync_WithValidResponse_MapsToItems()
    {
        // Arrange
        var searchJson = @"{
            ""docs"": [
                {
                    ""key"": ""/works/OL45883W"",
                    ""title"": ""The Hobbit"",
                    ""first_publish_year"": 1937,
                    ""author_name"": [""J.R.R. Tolkien""],
                    ""subject"": [""Fantasy"", ""Adventure"", ""Middle Earth""],
                    ""subject_facet"": [""Fantasy fiction"", ""Adventure stories""]
                }
            ]
        }";

        var workJson = @"{
            ""key"": ""/works/OL45883W"",
            ""title"": ""The Hobbit"",
            ""description"": ""A fantasy novel about a hobbit who goes on an adventure."",
            ""subjects"": [""Fantasy"", ""Adventure"", ""Dragons""],
            ""subject_places"": [""Middle-earth""],
            ""authors"": [
                {
                    ""author"": {
                        ""key"": ""/authors/OL26320A""
                    }
                }
            ]
        }";

        var authorJson = @"{
            ""key"": ""/authors/OL26320A"",
            ""name"": ""J.R.R. Tolkien"",
            ""personal_name"": ""John Ronald Reuel Tolkien""
        }";

        _mockHttp.When(SearchBaseUrl + "*")
                 .Respond("application/json", searchJson);

        _mockHttp.When("https://openlibrary.org/works/OL45883W.json")
                 .Respond("application/json", workJson);

        _mockHttp.When("https://openlibrary.org/authors/OL26320A.json")
                 .Respond("application/json", authorJson);

        var openLibraryClient = CreateClient();

        // Act
        var books = await openLibraryClient.SearchBooksAsync("hobbit", ct: CancellationToken.None);
        var booksList = books.ToList();

        // Assert
        Assert.Single(booksList);
        
        var book = booksList[0];
        Assert.Equal("OpenLibrary", book.Source, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("OL45883W", book.ExternalId, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ItemType.Book, book.Type);
        Assert.Equal("The Hobbit", book.Title, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1937, book.Year);
        
        Assert.Contains(book.Themes, t => string.Equals(t.Theme, "Fantasy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(book.Themes, t => string.Equals(t.Theme, "Adventure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(book.Themes, t => string.Equals(t.Theme, "Middle Earth", StringComparison.OrdinalIgnoreCase));
        
        Assert.Contains(book.Creators, c => string.Equals(c.CreatorName, "J.R.R. Tolkien", StringComparison.OrdinalIgnoreCase));

    }

    [Fact]
    public async Task SearchBooksAsync_WithMissingFields_HandlesGracefully()
    {
        // Arrange: JSONs válidos, apenas com os campos mínimos
        var searchJson = @"{
        ""docs"": [
            {
                ""key"": ""/works/OL12345W"",
                ""title"": ""Incomplete Book""
            }
        ]
    }";

        var workJson = @"{
        ""key"": ""/works/OL12345W"",
        ""title"": ""Incomplete Book""
    }";

        _mockHttp
            .When(SearchBaseUrl + "*")
            .Respond("application/json", searchJson);

        _mockHttp
            .When("https://openlibrary.org/works/OL12345W.json")
            .Respond("application/json", workJson);

        var openLibraryClient = CreateClient();

        // Act - Should not throw
        var books = await openLibraryClient.SearchBooksAsync("incomplete", ct: CancellationToken.None);
        var booksList = books.ToList();

        // Assert
        Assert.Single(booksList);

        var book = booksList[0];
        Assert.Equal("OpenLibrary", book.Source, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("OL12345W", book.ExternalId, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Incomplete Book", book.Title, StringComparer.OrdinalIgnoreCase);
        Assert.Null(book.Year);   // sem first_publish_year
        Assert.Empty(book.Genres);
        Assert.Empty(book.Themes);
        Assert.Empty(book.Creators);
    }


    // [Fact]
    // public async Task SearchBooksAsync_WithHttpError_ThrowsHttpRequestException()
    // {
    //     // Arrange
    //     _mockHttp.When(SearchBaseUrl + "*")
    //              .Respond(HttpStatusCode.InternalServerError);
    //
    //     var openLibraryClient = CreateClient();
    //
    //     // Act & Assert
    //     await Assert.ThrowsAsync<HttpRequestException>(() => 
    //         openLibraryClient.SearchBooksAsync("error", ct: CancellationToken.None)
    //     );
    // }

    [Fact]
    public async Task SearchBooksAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var emptyJson = @"{""docs"": []}";

        _mockHttp.When(SearchBaseUrl + "*")
                 .Respond("application/json", emptyJson);

        var openLibraryClient = CreateClient();

        // Act
        var books = await openLibraryClient.SearchBooksAsync("nonexistent", ct: CancellationToken.None);

        // Assert
        Assert.Empty(books);
    }

    [Fact]
    public async Task SearchBooksAsync_WithAuthorErrorButValidBooks_ContinuesProcessing()
    {
        // Arrange
        var searchJson = @"{
            ""docs"": [
                {
                    ""key"": ""/works/OL45883W"",
                    ""title"": ""The Hobbit"",
                    ""first_publish_year"": 1937,
                    ""author_name"": [""J.R.R. Tolkien""],
                    ""subject"": [""Fantasy"", ""Adventure""]
                }
            ]
        }";

        var workJson = @"{
            ""key"": ""/works/OL45883W"",
            ""title"": ""The Hobbit"",
            ""authors"": [
                {
                    ""author"": {
                        ""key"": ""/authors/OL26320A""
                    }
                }
            ]
        }";

        _mockHttp.When(SearchBaseUrl + "*")
                 .Respond("application/json", searchJson);

        _mockHttp.When("https://openlibrary.org/works/OL45883W.json")
                 .Respond("application/json", workJson);

        // Author endpoint returns error
        _mockHttp.When("https://openlibrary.org/authors/OL26320A.json")
                 .Respond(HttpStatusCode.NotFound);

        var openLibraryClient = CreateClient();

        // Act - Should not throw, should continue with available data
        var books = await openLibraryClient.SearchBooksAsync("hobbit", ct: CancellationToken.None);
        var booksList = books.ToList();

        // Assert
        Assert.Single(booksList);
        
        var book = booksList[0];
        Assert.Equal("The Hobbit", book.Title);
        // Should have added creator from search result even if author details failed
        Assert.Contains(book.Creators, c => c.CreatorName == "J.R.R. Tolkien");
    }
}
