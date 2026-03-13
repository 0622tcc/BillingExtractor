# Billing Extractor

An AI-powered invoice data extraction application built with ASP.NET Core 10 and Google Gemini. Upload invoice images (PNG/JPG) and automatically extract structured data including vendor details, line items, and totals — with built-in duplicate detection and amount validation.

## Features

- **AI-Powered Extraction** — Uses Google Gemini via OpenAI-compatible API to extract invoice data from images
- **Duplicate Detection** — Prevents double-processing of invoices with the same invoice number
- **Amount Mismatch Validation** — Compares line item sums against the stated total, flags discrepancies > $0.01
- **Persistent Storage** — Stores all extracted invoices in SQLite via Entity Framework Core
- **Retry Logic** — Polly v8 resilience pipeline with exponential backoff (2s, 4s, 8s) for transient API errors
- **REST API** — Documented with Swagger/OpenAPI at `/swagger`
- **Web UI** — Clean, responsive drag-and-drop interface in vanilla HTML/CSS/JS
- **Containerized** — Docker and docker-compose support for easy deployment
- **Unit Tested** — xUnit + Moq test suite covering controllers, services, validation rules, and repository (35 tests)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)
- A [Google Gemini API key](https://aistudio.google.com/app/apikey) (free tier available)

## Quick Start (without Docker)

```bash
git clone https://github.com/0622tcc/BillingExtractor.git
cd BillingExtractor/src/BillingExtractor.Api

# Set your Gemini API key (choose one method — see Configuration section)
export GEMINI_API_KEY=your_api_key_here   # macOS/Linux
set GEMINI_API_KEY=your_api_key_here      # Windows CMD

dotnet run
```

Open [http://localhost:5000](http://localhost:5000) in your browser.

## Quick Start (with Docker)

```bash
cp .env.example .env
# Edit .env and fill in your GEMINI_API_KEY

docker compose up --build
```

Open [http://localhost:8080](http://localhost:8080) in your browser.

## Configuration

The application reads Gemini credentials from `appsettings.json` under the `Gemini` key:

```json
{
  "Gemini": {
    "ApiKey": "",
    "Model": "gemini-3-flash-preview",
    "MaxRetries": 3
  }
}
```

**Never commit your API key.** Use one of these approaches instead:

### Option 1 — Environment variable (recommended)

```bash
# macOS / Linux
export GEMINI_API_KEY=your_api_key_here

# Windows CMD
set GEMINI_API_KEY=your_api_key_here

# Windows PowerShell
$env:GEMINI_API_KEY="your_api_key_here"
```

### Option 2 — User secrets (local development only)

```bash
cd src/BillingExtractor.Api
dotnet user-secrets set "Gemini:ApiKey" "your_api_key_here"
```

### Option 3 — appsettings.Development.json

Create this file (it is git-ignored) and add:

```json
{
  "Gemini": {
    "ApiKey": "your_api_key_here"
  }
}
```

### Getting a Free Gemini API Key

1. Visit [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Sign in with your Google account
3. Click **Create API key**
4. Copy the key and use any option above to configure it

The free tier includes generous usage limits suitable for development and testing.

## Building

```bash
dotnet build BillingExtractor.sln
```

## Running

```bash
cd src/BillingExtractor.Api
dotnet run
# Runs on http://localhost:5000 (http profile)

# To use HTTPS profile (ports 7241 / 5231):
dotnet run --launch-profile https
```

The SQLite database file (`invoices.db`) is created automatically on first run via EF Core migrations.

## Running Tests

```bash
cd tests/BillingExtractor.Tests
dotnet test
```

Expected output: **35 tests, 0 failures**.

## API Documentation

Swagger UI is available at [http://localhost:5000/swagger](http://localhost:5000/swagger) when running locally.

### Endpoints

| Method   | Path                    | Description                                                      |
|----------|-------------------------|------------------------------------------------------------------|
| `POST`   | `/api/invoices/extract` | Extract invoice data from an uploaded image (PNG/JPG, max 10 MB) |
| `GET`    | `/api/invoices`         | Retrieve all previously processed invoices                       |
| `DELETE` | `/api/invoices/{id}`    | Delete an invoice record by ID (useful for demo reset)           |
| `GET`    | `/health`               | Health check                                                     |

## Sample Invoices

Three test images are provided in `sample-invoices/`:

| File | Invoice # | Scenario |
|------|-----------|----------|
| `invoice-valid.png`     | INV-2024-001 | Valid invoice — saves successfully |
| `invoice-duplicate.png` | INV-2024-001 | Same number as above — triggers duplicate warning |
| `invoice-mismatch.png`  | INV-2024-002 | Line items ($76) != stated total ($95) — triggers mismatch warning |

To regenerate the PNG files:

```bash
cd sample-invoices
dotnet run
```

## Architecture

The application follows a **Layered Architecture** with **Repository Pattern**, **MVC**, and **SOLID** principles.

```
HTTP Request
    |
    v
InvoicesController          (thin HTTP adapter -- validates file, maps result to DTO)
    |
    v
IInvoiceService             (InvoiceService -- orchestrates the full pipeline)
    |
    +---> ILlmService        (GeminiService -- calls Gemini API with Polly retry)
    |
    +---> IValidationService (ValidationService -- runs all registered rules)
    |       +-- DuplicateValidationRule     -> IInvoiceReadRepository
    |       +-- AmountMismatchValidationRule (uses computed properties on ExtractedInvoice)
    |
    +---> IInvoiceWriteRepository  (saves when not duplicate)
             |
             v
         InvoiceRepository  (EF Core -> SQLite)
             +-- IInvoiceMapper  (maps domain model -> persistence entity)
```

### Key Design Decisions

| Principle | How it's applied |
|-----------|-----------------|
| **SRP** | Controller handles HTTP only; `InvoiceService` orchestrates; each rule class has one validation concern |
| **OCP** | Add a new validation rule by implementing `IInvoiceValidationRule` and registering it in DI — no existing code changes needed |
| **ISP** | `IInvoiceReadRepository` / `IInvoiceWriteRepository` split so consumers only depend on what they use |
| **DIP** | All layers depend on interfaces, not concrete classes |
| **Encapsulation** | `ExtractedInvoice.CalculatedTotal` and `HasAmountMismatch` are computed properties, not externally set |

### Storage

- **Database**: SQLite (`invoices.db`)
- **ORM**: Entity Framework Core (code-first, migrations in `src/BillingExtractor.Api/Migrations/`)
- **Line items**: stored as a JSON string in `LineItemsJson` column, deserialized on read by `InvoiceRecordDto`

### Project Structure

```
BillingExtractor/
├── src/
│   └── BillingExtractor.Api/
│       ├── Controllers/        # HTTP layer
│       ├── Data/
│       │   └── Entities/       # EF Core persistence entity (InvoiceRecord)
│       ├── DTOs/               # HTTP request/response models
│       ├── Exceptions/         # Domain exception hierarchy
│       ├── Mappers/            # Domain model -> persistence entity
│       ├── Migrations/         # EF Core migrations
│       ├── Models/             # Domain models (ExtractedInvoice, ValidationResult, ...)
│       └── Services/
│           ├── Interfaces/     # Service and repository contracts
│           └── Rules/          # IInvoiceValidationRule implementations
├── tests/
│   └── BillingExtractor.Tests/
│       ├── Controllers/
│       ├── Rules/
│       └── Services/
└── sample-invoices/            # Test PNG generator (SkiaSharp)
```
