using Azure.Storage.Blobs;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;
using InsuranceManagementSystem.Functions.BillingService.Data;
using InsuranceManagementSystem.Functions.BillingService.Config;

namespace InsuranceManagementSystem.Functions.BillingService.Services;

/// <summary>
/// Interface for invoice generation operations
/// </summary>
public interface IInvoiceGenerator
{
    /// <summary>
    /// Generate an invoice for the specified insurance
    /// </summary>
    Task<EmailInvoiceNotificationMessage?> GenerateInvoiceAsync(Guid vehicleInsuranceId, DateTime startDate, DateTime endDate, decimal amount);
}

/// <summary>
/// Implementation of invoice generator
/// </summary>
public class InvoiceGenerator : IInvoiceGenerator
{
    private readonly ILogger<InvoiceGenerator> _logger;
    private readonly InsuranceDbContext _dbContext;
    private readonly TelemetryClient _telemetryClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AppConfig _appConfig;

    public InvoiceGenerator(
        ILogger<InvoiceGenerator> logger,
        InsuranceDbContext dbContext,
        TelemetryClient telemetryClient,
        BlobServiceClient blobServiceClient,
        AppConfig appConfig)
    {
        _logger = logger;
        _dbContext = dbContext;
        _telemetryClient = telemetryClient;
        _blobServiceClient = blobServiceClient;
        _appConfig = appConfig;
    }

    /// <summary>
    /// Generate an invoice for the specified insurance
    /// </summary>
    public async Task<EmailInvoiceNotificationMessage?> GenerateInvoiceAsync(Guid vehicleInsuranceId, DateTime startDate, DateTime endDate, decimal amount)
    {
        try
        {
            _logger.LogInformation("Generating invoice for insurance {InsuranceId} for period {StartDate} to {EndDate}",
                vehicleInsuranceId, startDate, endDate);

            // Get insurance and user details
            var insurance = await _dbContext.VehicleInsurances
                .Include(vi => vi.User)
                .FirstOrDefaultAsync(vi => vi.Id == vehicleInsuranceId);

            if (insurance == null)
            {
                _logger.LogWarning("Insurance {InsuranceId} not found", vehicleInsuranceId);
                return null;
            }

            var user = insurance.User;

            if (user == null)
            {
                _logger.LogWarning("User not found for insurance {InsuranceId}", vehicleInsuranceId);
                return null;
            }

            // Create invoice record
            var invoice = CreateInvoiceRecord(vehicleInsuranceId, startDate, endDate, amount);

            // Generate PDF invoice
            var pdfUrl = await GenerateAndUploadInvoicePdfAsync(invoice, user, startDate, endDate);
            invoice.Url = pdfUrl;

            // Save invoice and document to database
            await SaveInvoiceAndDocumentAsync(invoice);

            // Create email notification message
            var emailMessage = CreateEmailNotificationMessage(user, invoice, pdfUrl);

            _logger.LogInformation("Successfully generated invoice {InvoiceId} for insurance {InsuranceId}",
                invoice.Id, vehicleInsuranceId);

            return emailMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for insurance {InsuranceId}", vehicleInsuranceId);
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "InsuranceId", vehicleInsuranceId.ToString() },
                { "StartDate", startDate.ToString() },
                { "EndDate", endDate.ToString() }
            });
            return null;
        }
    }

    /// <summary>
    /// Create a new invoice record
    /// </summary>
    private Invoice CreateInvoiceRecord(Guid vehicleInsuranceId, DateTime startDate, DateTime endDate, decimal amount)
    {
        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            VehicleInsuranceId = vehicleInsuranceId,
            Status = InvoiceStatus.Pending,
            Amount = amount,
            PaidAmount = 0,
            IssuedDate = now,
            DueDate = now.AddDays(30),
            StartDate = startDate,
            EndDate = endDate,
            PaymentMethod = string.Empty,
            TransactionRef = null,
            PaidAt = null,
            Notes = $"Invoice for insurance {vehicleInsuranceId} for the period {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}",
            InvoiceNumber = $"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            CreatedAt = now,
            UpdatedAt = now,
            DiscountAmount = 0,
            TaxAmount = 0
        };

        _logger.LogInformation("Created invoice record {InvoiceId} for insurance {InsuranceId} with amount {Amount:C}",
            invoice.Id, vehicleInsuranceId, amount);

        return invoice;
    }

    /// <summary>
    /// Generate PDF invoice and upload to blob storage
    /// </summary>
    private async Task<string> GenerateAndUploadInvoicePdfAsync(Invoice invoice, User user, DateTime startDate, DateTime endDate)
    {
        try
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 50, 50, 25, 25);
            var writer = PdfWriter.GetInstance(document, ms);

            document.Open();
            // Add title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
            var title = new Paragraph("INSURANCE INVOICE", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
            document.Add(title);
            document.Add(new Paragraph(" ")); // Spacer

            // Add invoice details
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, new BaseColor(0, 0, 0));
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(0, 0, 0));

            document.Add(new Paragraph($"Invoice Number: {invoice.InvoiceNumber}", boldFont));
            document.Add(new Paragraph($"Invoice ID: {invoice.Id}", normalFont));
            document.Add(new Paragraph($"Issue Date: {invoice.IssuedDate:MM/dd/yyyy}", normalFont));
            document.Add(new Paragraph($"Due Date: {invoice.DueDate:MM/dd/yyyy}", normalFont));
            document.Add(new Paragraph(" ")); // Spacer

            // Add customer details
            document.Add(new Paragraph("Bill To:", boldFont));
            document.Add(new Paragraph($"Name: {user.Name}", normalFont));
            document.Add(new Paragraph($"Email: {user.Email}", normalFont));
            document.Add(new Paragraph($"Phone: {user.PhoneNumber}", normalFont));
            document.Add(new Paragraph($"Address: {user.Address}", normalFont));
            document.Add(new Paragraph(" ")); // Spacer

            // Add invoice period
            document.Add(new Paragraph("Coverage Period:", boldFont));
            document.Add(new Paragraph($"From: {startDate:MM/dd/yyyy}", normalFont));
            document.Add(new Paragraph($"To: {endDate:MM/dd/yyyy}", normalFont));
            document.Add(new Paragraph(" ")); // Spacer

            // Add amount details
            document.Add(new Paragraph("Payment Details:", boldFont));
            document.Add(new Paragraph($"Total Amount: ${invoice.Amount:F2}", titleFont));
            document.Add(new Paragraph(" ")); // Spacer

            // Add notes if any
            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                document.Add(new Paragraph("Notes:", boldFont));
                document.Add(new Paragraph(invoice.Notes, normalFont));
            }

            document.Close();

            // Upload to Azure Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(_appConfig.InvoicesContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"invoice-{invoice.Id}.pdf";
            var blobClient = containerClient.GetBlobClient(blobName);

            ms.Position = 0;
            await blobClient.UploadAsync(ms, overwrite: true);

            _logger.LogInformation("Generated and uploaded PDF invoice {InvoiceId} to blob storage", invoice.Id);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF invoice {InvoiceId}", invoice.Id);
            _telemetryClient.TrackException(ex);
            throw;
        }
    }

    /// <summary>
    /// Save invoice and document to database
    /// </summary>
    private async Task SaveInvoiceAndDocumentAsync(Invoice invoice)
    {
        try
        {
            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Saved invoice {InvoiceId} to database", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving invoice {InvoiceId} and document to database", invoice.Id);
            throw;
        }
    }

    /// <summary>
    /// Create email notification message
    /// </summary>
    private EmailInvoiceNotificationMessage CreateEmailNotificationMessage(User user, Invoice invoice, string pdfUrl)
    {
        var emailMessage = new EmailInvoiceNotificationMessage
        {
            UserEmail = user.Email,
            InvoiceId = invoice.Id,
            AttachmentPdfUrl = pdfUrl,
            RecipientName = user.Name,
            MessageSubject = $"Your Insurance Invoice #{invoice.InvoiceNumber} - Due {invoice.DueDate:MMM dd, yyyy}",
            MessageHtmlBody = $@"
                <html>
                <body>
                    <h2>Insurance Invoice</h2>
                    <p>Dear {user.Name},</p>
                    <p>Please find attached your insurance invoice for the period {invoice.StartDate:MMM dd, yyyy} to {invoice.EndDate:MMM dd, yyyy}.</p>
                    <p><strong>Invoice Details:</strong></p>
                    <ul>
                        <li>Invoice Number: {invoice.InvoiceNumber}</li>
                        <li>Amount: ${invoice.Amount:F2}</li>
                        <li>Due Date: {invoice.DueDate:MMM dd, yyyy}</li>
                    </ul>
                    <p>Please ensure payment is made by the due date to avoid any service interruption.</p>
                    <p>Thank you for choosing our insurance services.</p>
                    <p>Best regards,<br/>Insurance Management Team</p>
                </body>
                </html>"
        };

        return emailMessage;
    }
}
