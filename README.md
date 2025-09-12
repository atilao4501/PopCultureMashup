# PopCultureMashup

A sophisticated recommendation system API that delivers personalized game and book recommendations based on user preferences. Built with .NET 8 and Clean Architecture principles, the system integrates with external APIs (RAWG for games, OpenLibrary for books) to provide intelligent cross-media recommendations.

## 🚀 Features

### Core Functionality

- **Personalized Recommendations**: Advanced algorithm generates tailored recommendations based on user preferences
- **Cross-Media Intelligence**: Recommends both games and books using unified preference analysis
- **Smart Ranking System**: Multi-factor scoring algorithm considering similarity, popularity, recency, and novelty
- **Seed-Based Learning**: Uses user-provided "seed" items to understand preferences and generate recommendations
- **Content Diversification**: MMR (Maximal Marginal Relevance) algorithm ensures variety in recommendations

### Authentication & Security

- **JWT Authentication**: Secure token-based authentication with refresh token rotation
- **User Management**: Complete registration, login, and profile management
- **Protected Endpoints**: Recommendation endpoints require authentication
- **Identity Integration**: Built on ASP.NET Core Identity with custom user entities

### External Integrations

- **RAWG API**: Video game data and discovery
- **OpenLibrary API**: Book data and search capabilities
- **Robust Error Handling**: Graceful handling of external API failures

### Advanced Recommendation Features

- **Multi-Factor Scoring**: Combines theme similarity (50%), genre matching (30%), and creator affinity (20%)
- **Domain-Specific Weights**: Different scoring weights for books vs games
- **Recency Decay**: Time-aware scoring with different half-life values (4 years for games, 15 years for books)
- **Popularity Balancing**: Considers item popularity while avoiding mainstream bias
- **Novelty Detection**: Promotes discovery of lesser-known but relevant items

## 🏗️ Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

```
PopCultureMashup/
├── PopCultureMashup.Api/           # Web API Layer
├── PopCultureMashup.Application/   # Use Cases & Business Logic
├── PopCultureMashup.Domain/        # Core Domain Entities
├── PopCultureMashup.Infrastructure/# External Concerns & Data Access
└── PopCultureMashup.Tests/         # Comprehensive Test Suite
```

### Technology Stack

- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer tokens with ASP.NET Core Identity
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker support
- **Testing**: xUnit with comprehensive unit tests

## 📊 Recommendation Algorithm

### Similarity Calculation

The system uses a sophisticated multi-component similarity algorithm:

1. **Theme Matching (50% weight)**: Weighted Jaccard index for thematic similarity
2. **Genre Matching (30% weight)**: Jaccard index for genre overlap
3. **Creator Affinity (20% weight)**: Jaccard index for creator/author preferences

### Domain-Specific Scoring

- **Books**: Higher emphasis on themes (60%) and authors (25%), lower on genres (15%)
- **Games**: Balanced approach with themes (50%), genres (30%), creators (20%)

### Temporal Scoring

- **Recency Decay**: Exponential decay based on item age
- **Games**: 4-year half-life (faster technology evolution)
- **Books**: 15-year half-life (longer cultural relevance)

### Final Ranking

Combined score using configurable weights:

- **Similarity**: 65% (core preference matching)
- **Novelty**: 20% (discovery encouragement)
- **Popularity**: 10% (quality indicator)
- **Recency**: 5% (temporal relevance)

## 🔗 API Endpoints

### Authentication

- `POST /auth/register` - User registration
- `POST /auth/login` - User authentication
- `POST /auth/refresh` - Token refresh
- `GET /auth/me` - Current user information

### Recommendations

- `GET /recommendations/generate?numberOfRecommendations=10` - Generate new recommendations
- `GET /recommendations` - Retrieve stored recommendations

### Data Management

- `POST /seed` - Add preference seeds (liked items)
- `GET /seed` - Retrieve user seeds
- `GET /search?query=...&type=...` - Search games/books

### System

- `POST /seed/populate` - Populate database with sample data

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- Docker (optional)

### Configuration

1. **Clone the repository**:

```bash
git clone https://github.com/atilao4501/PopCultureMashup.git
cd PopCultureMashup
```

2. **Configure User Secrets**:

```bash
cd PopCultureMashup.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Your_Connection_String"
dotnet user-secrets set "Jwt:Key" "Your_32_Character_Plus_Secret_Key_Here"
dotnet user-secrets set "Jwt:Issuer" "PopMashup"
dotnet user-secrets set "Jwt:Audience" "PopMashup.Api"
dotnet user-secrets set "Jwt:AccessTokenMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenDays" "7"
dotnet user-secrets set "External:Rawg:ApiKey" "Your_RAWG_API_Key"
dotnet user-secrets set "External:Rawg:BaseUrl" "https://api.rawg.io/api/"
dotnet user-secrets set "External:OpenLibrary:BaseUrl" "https://openlibrary.org/"
```

3. **Database Setup**:

```bash
dotnet ef database update --project PopCultureMashup.Infrastructure --startup-project PopCultureMashup.Api
```

4. **Run the Application**:

```bash
dotnet run --project PopCultureMashup.Api
```

### Docker Deployment

```bash
docker build -t popculturemashup .
docker run -p 8080:8080 popculturemashup
```

## 📚 API Documentation

Once running, access the interactive API documentation at:

- **Swagger UI**: `http://localhost:5000/swagger`
- **ReDoc**: `http://localhost:5000/redoc`

## 🧪 Testing

Run the comprehensive test suite:

```bash
dotnet test
```

The test suite includes:

- **Unit Tests**: Core business logic validation
- **Integration Tests**: API endpoint testing
- **Repository Tests**: Data access layer validation
- **Service Tests**: External service integration testing

## 📈 Usage Examples

### Authentication Flow

```bash
# Register new user
curl -X POST "http://localhost:5000/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"SecurePass123"}'

# Login
curl -X POST "http://localhost:5000/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"SecurePass123"}'
```

### Adding Seeds (Preferences)

curl -X POST "http://localhost:5000/seed/create" \
curl -X POST "http://localhost:5000/seed" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '[
    {"type":"game","externalId":"3498"},
    {"type":"book","externalId":"OL7353617M"}
]'
```

### Retrieving Seeds

```bash
curl -X GET "http://localhost:5000/seed" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Searching Items

```bash
curl -X GET "http://localhost:5000/search?query=tolkien" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
  -d '{"externalId":"3498","itemType":"Game"}'
```

### Generate Recommendations

```bash
curl -X GET "http://localhost:5000/recommendations/generate?numberOfRecommendations=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🏆 Key Features Deep Dive

### Intelligent Recommendation Engine

- **Hybrid Approach**: Combines collaborative filtering concepts with content-based analysis
- **Real-time Generation**: Recommendations generated on-demand using current preferences
- **Adaptive Scoring**: Algorithm adapts to user's media type preferences (games vs books)

### Data Persistence

- **Recommendation History**: All generated recommendations are stored for analysis
- **Detailed Scoring**: Individual component scores (genre, theme, popularity, etc.) are tracked
- **User Seeds**: Long-term storage of user preferences for consistent recommendations

### Scalability Features

- **Async Processing**: All external API calls are asynchronous
- **Error Resilience**: Graceful handling of external service failures
- **Efficient Caching**: Strategic use of Entity Framework change tracking

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **RAWG API** - Comprehensive video game database
- **OpenLibrary** - Open book data and search capabilities
- **ASP.NET Core Team** - Excellent framework and documentation
- **Entity Framework** - Powerful ORM for .NET

## 🔧 Configuration Options

### Recommendation Algorithm Tuning

```json
{
  "RecommendationOptions": {
    "SimilarityWeight": 0.65,
    "PopularityWeight": 0.1,
    "RecencyWeight": 0.05,
    "NoveltyWeight": 0.2,
    "UseDiversification": true,
    "DiversificationK": 50
  }
}
```

### External API Configuration

```json
{
  "Rawg": {
    "BaseUrl": "https://api.rawg.io/api/",
    "ApiKey": "your-api-key",
    "RequestTimeoutSeconds": 30
  },
  "OpenLibrary": {
    "BaseUrl": "https://openlibrary.org/",
    "RequestTimeoutSeconds": 30
  }
}
```

## 📊 Performance Metrics

- **Response Time**: < 15 seconds for recommendation generation
- **External API Resilience**: Continues operating if one service fails
- **Database Efficiency**: Optimized queries with proper indexing
- **Memory Usage**: Efficient object lifecycle management

---

**PopCultureMashup** - Bridging the gap between your favorite games and books through intelligent recommendations.
