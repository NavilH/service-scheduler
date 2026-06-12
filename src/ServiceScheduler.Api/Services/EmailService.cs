using Azure.Communication.Email;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private const string Sender = "DoNotReply@b66a0aa2-f325-48b5-94cf-5774032faff8.azurecomm.net";

    public async Task SendBookingConfirmationAsync(string toEmail, string customerName, string serviceName, DateTime startTime)
    {
        await SendAsync(
            toEmail,
            "Appointment Confirmed",
            $"""
            <p>Hi {customerName},</p>
            <p>Your appointment has been booked:</p>
            <ul>
                <li><strong>Service:</strong> {serviceName}</li>
                <li><strong>Date:</strong> {startTime:dddd, MMMM d, yyyy}</li>
                <li><strong>Time:</strong> {startTime:h:mm tt}</li>
            </ul>
            <p>We'll see you then!</p>
            """
        );
    }

    public async Task SendStatusUpdateAsync(string toEmail, string customerName, string serviceName, DateTime startTime, AppointmentStatus status)
    {
        var subject = status switch
        {
            AppointmentStatus.Confirmed => "Appointment Confirmed",
            AppointmentStatus.Cancelled => "Appointment Cancelled",
            AppointmentStatus.Completed => "Appointment Completed",
            _ => "Appointment Update"
        };

        var body = status switch
        {
            AppointmentStatus.Confirmed =>
                $"""
                <p>Hi {customerName},</p>
                <p>Your appointment for <strong>{serviceName}</strong> on {startTime:dddd, MMMM d} at {startTime:h:mm tt} has been <strong>confirmed</strong>.</p>
                """,
            AppointmentStatus.Cancelled =>
                $"""
                <p>Hi {customerName},</p>
                <p>Your appointment for <strong>{serviceName}</strong> on {startTime:dddd, MMMM d} at {startTime:h:mm tt} has been <strong>cancelled</strong>.</p>
                <p>Please contact us to reschedule.</p>
                """,
            _ =>
                $"""
                <p>Hi {customerName},</p>
                <p>Your appointment for <strong>{serviceName}</strong> has been updated to: <strong>{status}</strong>.</p>
                """
        };

        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var connectionString = config["ACS_CONNECTION_STRING"];
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogWarning("ACS_CONNECTION_STRING not configured — email not sent.");
            return;
        }

        try
        {
            var client = new EmailClient(connectionString);
            await client.SendAsync(
                Azure.WaitUntil.Completed,
                Sender,
                toEmail,
                subject,
                htmlBody
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
