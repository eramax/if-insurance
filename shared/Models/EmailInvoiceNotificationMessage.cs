namespace Shared.Models;

/// <summary>
/// Message for sending email notifications with invoice attachments
/// </summary>
public class EmailInvoiceNotificationMessage
{
    public string UserEmail { get; set; } = string.Empty;

    public Guid InvoiceId { get; set; }

    public string AttachmentPdfUrl { get; set; } = string.Empty;

    public string MessageSubject { get; set; } = string.Empty;

    public string MessageHtmlBody { get; set; } = string.Empty;

    public string? RecipientName { get; set; }

    public string? SenderName { get; set; }
}
