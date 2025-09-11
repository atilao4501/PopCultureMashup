using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Application;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.Persistence;
using PopCultureMashup.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ⚡ Primeiro: configuração
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);


// Add services to the container.
builder.Services.AddControllers();

// Custom extension methods for DI
builder.Services.AddApplication();
builder.Services.AddRepositories();
builder.Services.AddExternalClients(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();