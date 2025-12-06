using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scrape
{
    public class PushoverClient
    {
        // Pushover API Endpoint
        private const string ApiUrl = "https://api.pushover.net/1/messages.json";

        // **Your User Key (Recipient)**
        private readonly string _userKey = "ume9u9de1a7yxu3wfsph77g83s83bx";

        // **Your Application Token (Sender)**
        private readonly string _applicationToken = "aogy7x1mwy9b4fp6rotp3p2cqmjvo3";

        /// <summary>
        /// Sends a push notification to your Pushover devices.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The main message content.</param>
        /// <returns>True if the message was sent successfully, false otherwise.</returns>
        public async Task<bool> SendNotification(string title, string message)
        {
            // 1. Prepare the data payload for the POST request
            var parameters = new Dictionary<string, string>
            {
                ["token"] = _applicationToken,
                ["user"] = _userKey,
                ["title"] = title,
                ["message"] = message,
                // Optional: You can add ["priority"] = "1" for high priority
            };

            // 2. Use HttpClient to send the request
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(parameters);

                try
                {
                    // Send the POST request to the Pushover API
                    HttpResponseMessage response = await client.PostAsync(ApiUrl, content);

                    // 3. Check for a successful response (HTTP 200 OK)
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Pushover notification sent successfully. 🚀");
                        return true;
                    }
                    else
                    {
                        // Handle API-specific errors
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Pushover API Error ({response.StatusCode}): {errorResponse}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Handle lower-level errors (e.g., network issues)
                    Console.WriteLine($"A network error occurred while sending notification: {ex.Message}");
                    return false;
                }
            }
        }
    }
}