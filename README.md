# PopCultureMashup

A recommendation system API that delivers personalized game and book recommendations based on user preferences. Built with .NET 8 and Clean Architecture. Integrates with RAWG (games) and OpenLibrary (books) to provide intelligent cross‑media recommendations.

## Features

### Core
- Personalized recommendations from user “seed” items (liked games/books)
- Cross‑media intelligence (games and books in one model)
- Multi‑factor ranking (similarity, popularity, recency, novelty)
- Diversification with MMR (Maximal Marginal Relevance)

### Authentication & Security
- JWT authentication with refresh tokens
- User registration, login, and profile management
- Protected endpoints
- ASP.NET Core Identity with custom user entities

### External Integrations
- RAWG API (video games)
- OpenLibrary API (books)
- Resilient error handling for external calls

## Architecture

Clean Architecture with clear separation of concerns:

```
PopCultureMashup/
├── PopCultureMashup.Api/            # Web API
├── PopCultureMashup.Application/    # Use cases / business logic
├── PopCultureMashup.Domain/         # Domain entities
├── PopCultureMashup.Infrastructure/ # Data access, external services
└── PopCultureMashup.Tests/          # Tests
```

### Technology Stack
- .NET 8
- SQL Server + Entity Framework Core
- JWT + ASP.NET Core Identity
- Swagger / OpenAPI
- Docker (Compose)
- xUnit test suite

## Recommendation Algorithm (Overview)

- **Similarity** (themes, genres, creators) with domain‑specific weights  
  - Books emphasize themes and authors  
  - Games balance themes, genres, creators
- **Temporal scoring** with exponential recency decay  
  - Games use a shorter half‑life; books a longer one
- **Final ranking** combines: Similarity, Novelty, Popularity, Recency  
- **Diversification** with MMR to reduce redundancy

## API Endpoints (Selected)

### Auth
- `POST /auth/register` — register
- `POST /auth/login` — authenticate
- `POST /auth/refresh` — refresh token
- `GET  /auth/me` — current user

### Recommendations
- `GET /recommendations/generate?numberOfRecommendations=10` — generate
- `GET /recommendations` — list stored recommendations

### Data
- `POST /seed` — add seed items (preferences)
- `GET  /seed` — list user seeds
- `GET  /search?query=...&type=...` — search games/books

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (or SQL Server Express)
- Docker (optional)

### Configuration

```bash
git clone https://github.com/atilao4501/PopCultureMashup.git
cd PopCultureMashup
```

Set secrets (API project):

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

Create/update database (dont worry, if you do not run these commands the app runs by default. Just make sure to have a DB running in the appropriate port estabilished in the secrets):

```bash
dotnet ef database update --project ../PopCultureMashup.Infrastructure --startup-project .
```

Run locally:

```bash
dotnet run --project .
```

## Running with Docker

Bring up API and SQL Server with Docker Compose (root of repo).

1) Configure environment values in `docker-compose.yml`:
- `MSSQL_SA_PASSWORD` — strong SA password (min 8 chars, complexity)
- `ConnectionStrings__DefaultConnection` — uses the same password
- `External__Rawg__ApiKey` — your RAWG key
- `Jwt__Key` — secret (≥ 32 chars)

2) Start:

```bash
docker compose up --build
```

3) Access:
- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- SQL Server: `localhost,1433` (container `popmashup-sql`)

Stop and remove:

```bash
docker compose down
# or reset data as well:
docker compose down -v
```

## API Documentation

- Swagger UI (Docker): `http://localhost:8080/swagger`  
- Swagger UI (local): `http://localhost:5000/swagger`

## Testing

```bash
dotnet test
```

Covers:
- Unit tests for business logic
- Integration tests for endpoints
- Repository and service tests

## Usage Examples

### Register and Login

```bash
# Register
curl -X POST "http://localhost:5000/auth/register"   -H "Content-Type: application/json"   -d '{"email":"user@example.com","password":"SecurePass123"}'

# Login
curl -X POST "http://localhost:5000/auth/login"   -H "Content-Type: application/json"   -d '{"email":"user@example.com","password":"SecurePass123"}'
```

### Search Items

```bash
curl -X GET "http://localhost:5000/search?query=tolkien"   -H "Authorization: Bearer YOUR_JWT_TOKEN"
```
It will respond with the externalId for the item that you want, and then you can create your own seeds for personal recommendations.

### Add Seeds (Preferences)

```bash
curl -X POST "http://localhost:5000/seed"   -H "Authorization: Bearer YOUR_JWT_TOKEN"   -H "Content-Type: application/json"   -d '[{"type":"game","externalId":"3498"},{"type":"book","externalId":"OL7353617M"}]'
```

### Retrieve Seeds

```bash
curl -X GET "http://localhost:5000/seed"   -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Generate Recommendations

```bash
curl -X GET "http://localhost:5000/recommendations/generate?numberOfRecommendations=10"   -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Configuration (Samples)

### Recommendation Tuning

```json
{
  "RecommendationSettings": {
    "SimilarityWeight": 0.65,
    "PopularityWeight": 0.10,
    "RecencyWeight": 0.05,
    "NoveltyWeight": 0.20,
    "UseDiversification": true,
    "DiversificationK": 50,
    "ThemeWeightDefault": 0.50,
    "GenreWeightDefault": 0.30,
    "CreatorWeightDefault": 0.20,
    "ThemeWeightBooks": 0.60,
    "GenreWeightBooks": 0.15,
    "CreatorWeightBooks": 0.25,
    "HalfLifeGames": 4.0,
    "HalfLifeBooks": 15.0
  }
}
```

### External APIs

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

## Performance (Targets)

- Minimize external API calls and response time through efficient batching and filtering
- EF Core queries optimized with appropriate tracking and projections

## Roadmap

Planned enhancements to increase performance, observability, and adaptability:

- **Caching (Redis):** Cache search results and intermediate recommendation data to reduce latency and external API load; add cache-aware invalidation strategies.
- **Structured Logging (Serilog):** Centralized, structured logs with enrichers and per-request correlation; environment-specific sinks.
- **User Feedback Service:** Collect explicit feedback (like/skip/better matches) and dynamically adjust similarity weights/novelty thresholds to personalize ranking.
- **Analytics & Insights:** Aggregate usage metrics and recommendation outcomes; dashboards for conversion/engagement; privacy-aware telemetry with opt-in.

## Contributing

1. Fork the repository  
2. Create a feature branch: `git checkout -b feature/YourFeature`  
3. Commit: `git commit -m "feat: add YourFeature"`  
4. Push: `git push origin feature/YourFeature`  
5. Open a Pull Request

## License

MIT — see [LICENSE](LICENSE).

## Acknowledgments

- RAWG API  
- OpenLibrary  
- ASP.NET Core and Entity Framework teams
