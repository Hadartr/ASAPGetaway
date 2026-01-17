using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ASAPGetaway.Services
{
    // Email service using Gmail SMTP for sending notifications
    public class EmailService
    {
        // Gmail SMTP configuration
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail = "orihadarsce@gmail.com";
        private readonly string _fromName = "ASAPGetaway Travel";
        private readonly string _password = "enqsmbhjadgzxokp"; // Gmail App Password

        // Send generic email
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send via Gmail SMTP
                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_fromEmail, _password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ EMAIL SENT to {toEmail} - Subject: {subject}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email error: {ex.Message}");
                return false;
            }
        }

        // Booking confirmation email
        public async Task SendBookingConfirmationAsync(string toEmail, int bookingId, string packageName, decimal totalPrice)
        {
            string subject = "Booking Confirmation - ASAPGetaway";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #007bff;'>🎉 Booking Confirmed!</h2>
                    <p>Dear Customer,</p>
                    <p>Your booking has been successfully confirmed.</p>
                    <div style='background-color: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <strong>Booking ID:</strong> {bookingId}<br/>
                        <strong>Package:</strong> {packageName}<br/>
                        <strong>Total Price:</strong> ${totalPrice}
                    </div>
                    <p>Thank you for choosing ASAPGetaway!</p>
                    <p style='color: #888; font-size: 0.9em;'>This is an automated email. Please do not reply.</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        // Waiting list confirmation email with position
        public async Task SendWaitingListConfirmationAsync(string toEmail, string packageName, int position)
        {
            string subject = $"Added to Waiting List - {packageName}";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #f39c12;'>⏳ Added to Waiting List</h2>
                    <p>Dear Customer,</p>
                    <p>You have been added to the waiting list for <strong>{packageName}</strong>.</p>
                    <div style='background-color: #fff8e1; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #f39c12;'>
                        <strong>Your Position:</strong> #{position} in line<br/>
                        <strong>Status:</strong> Waiting for availability
                    </div>
                    <p>We will notify you by email as soon as a spot becomes available.</p>
                    <p><strong>Estimated wait time:</strong> {(position <= 3 ? "1-2 weeks" : "2-4 weeks")}</p>
                    <p>Thank you for your patience!</p>
                    <p><a href='https://localhost:5001/WaitingList/My' style='background-color: #f39c12; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin-top: 10px;'>View My Waiting List</a></p>
                    <p style='color: #888; font-size: 0.9em;'>This is an automated email. Please do not reply.</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        // Room available notification (first in waiting list)
        public async Task SendRoomAvailableAsync(string toEmail, string packageName, int tripId)
        {
            string subject = $"🎉 A Spot is Available for {packageName}!";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #28a745;'>🎊 Great News!</h2>
                    <p>Dear Customer,</p>
                    <p>A spot has become available for <strong>{packageName}</strong>!</p>
                    <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                        <strong>Status:</strong> It's your turn to book!<br/>
                        <strong>Action Required:</strong> Book within 24 hours
                    </div>
                    <p><strong>⚠️ Important:</strong> Please book within 24 hours or you'll lose your spot.</p>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='https://localhost:5001/Bookings/Create?tripId={tripId}' style='background-color: #28a745; color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold; font-size: 16px;'>Book Now →</a>
                    </p>
                    <p>Don't miss this opportunity!</p>
                    <p style='color: #888; font-size: 0.9em;'>This is an automated email. Please do not reply.</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        // Trip reminder email (sent X days before departure)
        public async Task<bool> SendTripReminderAsync(string toEmail, string packageName, DateTime startDate)
        {
            string subject = "Trip Reminder - ASAPGetaway";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #007bff;'>✈️ Trip Reminder</h2>
                    <p>Dear Customer,</p>
                    <p>Your trip to <strong>{packageName}</strong> is coming up soon!</p>
                    <div style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff;'>
                        <strong>Departure Date:</strong> {startDate:dd MMM yyyy}<br/>
                        <strong>Days Remaining:</strong> {(startDate - DateTime.Now).Days} days
                    </div>
                    <p><strong>Checklist:</strong></p>
                    <ul>
                        <li>✓ Valid ID/Passport</li>
                        <li>✓ Booking confirmation</li>
                        <li>✓ Travel insurance</li>
                        <li>✓ Arrive 30 minutes early</li>
                    </ul>
                    <p>Have a wonderful trip!</p>
                    <p style='color: #888; font-size: 0.9em;'>This is an automated email. Please do not reply.</p>
                </body>
                </html>
            ";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}