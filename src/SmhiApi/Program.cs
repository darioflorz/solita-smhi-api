using Scalar.AspNetCore;
using SmhiApi.Common.Middleware;
using SmhiApi.Features.Stations;
using SmhiApi.Infrastructure.Clients.Smhi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSmhiClient(builder.Configuration);
builder.Services.AddScoped<IStationsService, StationsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// API key authentication
app.UseApiKeyAuth();

// Map endpoints
app.MapStationsEndpoints();

app.Run();

// Required for integration tests with WebApplicationFactory
public partial class Program { }
