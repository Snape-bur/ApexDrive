using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApexDrive.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                // ✅ Basic validation
                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return Json(new { reply = "Please enter a message." });
                }

                if (request.Message.Length > 500)
                {
                    return Json(new { reply = "Please keep your message under 500 characters." });
                }

                var apiKey = _configuration["GeminiApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Json(new { reply = "AI service is not configured properly." });
                }

                var systemContext = @"
You are a helpful customer service assistant for ApexDrive Car Rental System.

About ApexDrive:
- We are a car rental company with multiple branches
- Customers can book vehicles online through our website
- We offer sedans, SUVs, and various vehicle types

You can help with:
- How to search and book a vehicle
- Rental policies (age requirements, insurance options, fuel policy)
- Account creation and login
- Booking management (view, modify bookings)
- Pickup and drop-off procedures
- Pricing information and payment methods
- Available vehicle types and features
- Branch locations and contact information

Important policies:
- Minimum age: Usually 21 years old (advise checking specific requirements)
- Valid driver's license required
- Credit card required for booking
- Insurance options available (basic and comprehensive)
- Fuel policy: Return with the same fuel level

Rules:
- Keep answers friendly, brief, and clear
- Do NOT guess prices or legal details
- If unsure, advise contacting ApexDrive support
";

                var fullPrompt =
                    systemContext +
                    "\n\nCustomer: " + request.Message +
                    "\n\nAssistant:";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 400
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ Uses API key from appsettings.json (no hardcoding)
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        reply = "I'm having trouble responding right now. Please try again shortly."
                    });
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GeminiResponse>(responseBody);

                var aiMessage =
                    result?.candidates?[0]?.content?.parts?[0]?.text
                    ?? "I'm sorry, I didn't quite understand that. Could you rephrase your question?";

                return Json(new { reply = aiMessage });
            }
            catch
            {
                return Json(new
                {
                    reply = "Sorry, something went wrong on our side. Please try again later."
                });
            }
        }
    }

    // ===================== MODELS =====================

    public class ChatRequest
    {
        public string Message { get; set; }
    }

    public class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }

    public class Content
    {
        public Part[] parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }
}
