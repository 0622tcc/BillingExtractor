using BillingExtractor.Api.Data;
using BillingExtractor.Api.Exceptions;
using BillingExtractor.Api.Mappers;
using BillingExtractor.Api.Options;
using BillingExtractor.Api.Services;
using BillingExtractor.Api.Services.Interfaces;
using BillingExtractor.Api.Services.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Override Gemini API key from environment variable
var geminiApiKey = builder.Configuration["GEMINI_API_KEY"]
    ?? builder.Configuration["Gemini:ApiKey"]
    ?? string.Empty;
builder.Configuration["Gemini:ApiKey"] = geminiApiKey;

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=invoices.db"));

// Options
builder.Services.Configure<GeminiOptions>(opts =>
{
    builder.Configuration.GetSection("Gemini").Bind(opts);
    opts.ApiKey = geminiApiKey;
});

// HttpClient
builder.Services.AddHttpClient("gemini");

// -------------------------------------------------------------------------
// Repository — register the concrete type once, forward all three interfaces
// to the same scoped instance (avoids duplicate DbContext operations).
// -------------------------------------------------------------------------
builder.Services.AddScoped<InvoiceRepository>();
builder.Services.AddScoped<IInvoiceRepository>(sp  => sp.GetRequiredService<InvoiceRepository>());
builder.Services.AddScoped<IInvoiceReadRepository>(sp  => sp.GetRequiredService<InvoiceRepository>());
builder.Services.AddScoped<IInvoiceWriteRepository>(sp => sp.GetRequiredService<InvoiceRepository>());

// Mapper (SRP: mapping responsibility isolated from repository)
builder.Services.AddScoped<IInvoiceMapper, InvoiceMapper>();

// Validation rules (OCP: add new rules here without touching ValidationService)
builder.Services.AddScoped<IInvoiceValidationRule, DuplicateValidationRule>();
builder.Services.AddScoped<IInvoiceValidationRule, AmountMismatchValidationRule>();

// Services
builder.Services.AddScoped<ILlmService, GeminiService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// Polly resilience pipeline — retries on LlmTransientException (replaces GeminiRetryableException)
builder.Services.AddResiliencePipeline("gemini-retry", pipelineBuilder =>
{
    pipelineBuilder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder()
            .Handle<LlmTransientException>()
            .Handle<TaskCanceledException>(),
        OnRetry = static args =>
        {
            Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s delay.");
            return default;
        }
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Billing Extractor API",
        Version = "v1",
        Description = "AI-powered invoice data extraction using Google Gemini"
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Apply EF migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing Extractor API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.MapControllers();

// Fallback: serve index.html for non-API routes
app.MapFallbackToFile("index.html");

app.Run();
