# Sample Invoices

Three test invoice images are included for testing the Billing Extractor application.

## invoice-valid.png

- **Vendor:** TechSupplies Co.
- **Invoice #:** INV-2024-001
- **Date:** 2024-01-15
- **Line Items:**
  - Laptop Stand x1 @ $45.00 = $45.00
  - USB-C Hub x2 @ $35.00 = $70.00
  - Keyboard x1 @ $85.00 = $85.00
- **Total:** $200.00 (matches sum)
- **Expected:** Saves successfully, no warnings

## invoice-duplicate.png

- Identical to `invoice-valid.png` (same invoice number: INV-2024-001)
- **Expected:** Triggers "Duplicate Invoice Detected" warning, NOT saved again

## invoice-mismatch.png

- **Vendor:** Office Depot
- **Invoice #:** INV-2024-002
- **Date:** 2024-01-20
- **Line Items:**
  - Paper Ream x5 @ $12.00 = $60.00
  - Pens Box x2 @ $8.00 = $16.00
- **Line Items Sum:** $76.00
- **Stated Total:** $95.00 <- INTENTIONAL MISMATCH
- **Expected:** Triggers "Amount Mismatch" warning

## Generating Images

Run the helper script to generate PNG images:

```bash
cd sample-invoices
dotnet script GenerateSampleInvoices.csx
```

Or create them manually using any image editor with the above specifications.
