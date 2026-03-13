// Run from this folder: dotnet run
// Generates 3 sample invoice PNG images for testing the BillingExtractor API.

using SkiaSharp;

var outputDir = Directory.GetCurrentDirectory();

DrawAndSave(Path.Combine(outputDir, "invoice-valid.png"), new InvoiceData
{
    VendorName = "TechSupplies Co.",
    InvoiceNumber = "INV-2024-001",
    Date = "2024-01-15",
    LineItems =
    [
        ("Laptop Stand", 1, 45.00m, 45.00m),
        ("USB-C Hub",    2, 35.00m, 70.00m),
        ("Keyboard",     1, 85.00m, 85.00m),
    ],
    Total = 200.00m   // matches sum — saves successfully
});

DrawAndSave(Path.Combine(outputDir, "invoice-duplicate.png"), new InvoiceData
{
    VendorName = "TechSupplies Co.",
    InvoiceNumber = "INV-2024-001",  // same number → triggers DuplicateValidationRule
    Date = "2024-01-15",
    LineItems =
    [
        ("Laptop Stand", 1, 45.00m, 45.00m),
        ("USB-C Hub",    2, 35.00m, 70.00m),
        ("Keyboard",     1, 85.00m, 85.00m),
    ],
    Total = 200.00m
});

DrawAndSave(Path.Combine(outputDir, "invoice-mismatch.png"), new InvoiceData
{
    VendorName = "Office Depot",
    InvoiceNumber = "INV-2024-002",
    Date = "2024-01-20",
    LineItems =
    [
        ("Paper Ream", 5, 12.00m, 60.00m),
        ("Pens Box",   2,  8.00m, 16.00m),
    ],
    Total = 95.00m   // intentional mismatch: line items sum to $76.00
});

Console.WriteLine("Done! 3 invoice images created in the sample-invoices folder:");
Console.WriteLine("  invoice-valid.png     → valid invoice, saves successfully");
Console.WriteLine("  invoice-duplicate.png → same invoice #, triggers duplicate warning");
Console.WriteLine("  invoice-mismatch.png  → stated total ≠ line items sum, triggers mismatch warning");

// ---------------------------------------------------------------------------

static void DrawAndSave(string filename, InvoiceData data)
{
    const int W = 800, H = 600;

    using var bitmap = new SKBitmap(W, H);
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.White);

    // — Fonts —
    var bold   = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
    var normal = SKTypeface.FromFamilyName("Arial");

    using var titleFont  = new SKFont(bold,   26);
    using var headerFont = new SKFont(bold,   13);
    using var bodyFont   = new SKFont(normal, 11);

    // — Paints —
    using var whitePaint    = new SKPaint { Color = SKColors.White,                IsAntialias = true };
    using var blackPaint    = new SKPaint { Color = SKColors.Black,                IsAntialias = true };
    using var bluePaint     = new SKPaint { Color = new SKColor(26, 86, 219),      IsAntialias = true };
    using var lightGrayFill = new SKPaint { Color = new SKColor(240, 240, 240) };
    using var dividerPaint  = new SKPaint { Color = new SKColor(200, 200, 200),    StrokeWidth = 1, IsAntialias = true };
    using var totalLinePaint= new SKPaint { Color = SKColors.Black,               StrokeWidth = 2 };

    // Blue header bar
    canvas.DrawRect(SKRect.Create(0, 0, W, 70), bluePaint);
    canvas.DrawText("INVOICE", 30, 48, titleFont, whitePaint);

    // Vendor / invoice meta
    canvas.DrawText(data.VendorName,              30, 100, headerFont, blackPaint);
    canvas.DrawText($"Invoice #: {data.InvoiceNumber}", 30, 125, bodyFont,   blackPaint);
    canvas.DrawText($"Date: {data.Date}",         30, 148, bodyFont,   blackPaint);

    // Table header row
    float y = 195;
    canvas.DrawRect(SKRect.Create(30, y, W - 60, 30), lightGrayFill);
    canvas.DrawText("Description", 40,  y + 21, headerFont, blackPaint);
    canvas.DrawText("Qty",        430,  y + 21, headerFont, blackPaint);
    canvas.DrawText("Unit Price", 510,  y + 21, headerFont, blackPaint);
    canvas.DrawText("Total",      660,  y + 21, headerFont, blackPaint);
    y += 35;

    // Line items
    foreach (var (desc, qty, unitPrice, lineTotal) in data.LineItems)
    {
        canvas.DrawText(desc,               40,  y + 17, bodyFont, blackPaint);
        canvas.DrawText(qty.ToString(),     440,  y + 17, bodyFont, blackPaint);
        canvas.DrawText($"${unitPrice:F2}", 510,  y + 17, bodyFont, blackPaint);
        canvas.DrawText($"${lineTotal:F2}", 660,  y + 17, bodyFont, blackPaint);
        y += 28;
        canvas.DrawLine(30, y, W - 30, y, dividerPaint);
        y += 5;
    }

    // Total row
    y += 12;
    canvas.DrawLine(490, y, W - 30, y, totalLinePaint);
    y += 10;
    canvas.DrawText("TOTAL:",          490, y + 17, headerFont, blackPaint);
    canvas.DrawText($"${data.Total:F2}", 660, y + 17, headerFont, blackPaint);

    // Save PNG
    using var image   = SKImage.FromBitmap(bitmap);
    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream  = File.OpenWrite(filename);
    encoded.SaveTo(stream);
}

record InvoiceData
{
    public string VendorName    { get; init; } = "";
    public string InvoiceNumber { get; init; } = "";
    public string Date          { get; init; } = "";
    public (string desc, int qty, decimal unitPrice, decimal lineTotal)[] LineItems { get; init; } = [];
    public decimal Total        { get; init; }
}
