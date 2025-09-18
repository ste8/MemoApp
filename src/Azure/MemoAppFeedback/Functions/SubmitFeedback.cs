using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MemoAppFeedback.Functions
{
    public static class SubmitFeedback
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        // Configuration - These should be in Application Settings
        private static readonly string GITHUB_TOKEN = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        private static readonly string GITHUB_OWNER = Environment.GetEnvironmentVariable("GITHUB_OWNER");
        private static readonly string GITHUB_REPO = Environment.GetEnvironmentVariable("GITHUB_REPO");
        private static readonly string API_KEY = Environment.GetEnvironmentVariable("FEEDBACK_API_KEY");

        [FunctionName("SubmitFeedback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing feedback submission");

            try
            {
                // Validate API Key
                if (!ValidateApiKey(req))
                {
                    return new UnauthorizedResult();
                }

                // Parse request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var feedback = JsonConvert.DeserializeObject<FeedbackModel>(requestBody);

                // Validate input
                var validationError = ValidateFeedback(feedback);
                if (!string.IsNullOrEmpty(validationError))
                {
                    return new BadRequestObjectResult(new { error = validationError });
                }

                // Sanitize input
                SanitizeFeedback(feedback);

                // Create GitHub issue
                var issueNumber = await CreateGitHubIssue(feedback, log);

                // Optional: Store in Azure Table Storage for analytics
                // await StoreFeedbackInDatabase(feedback, issueNumber);

                return new OkObjectResult(new
                {
                    success = true,
                    issueNumber = issueNumber,
                    message = "Thank you for your feedback! We'll review it shortly."
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error processing feedback");
                return new StatusCodeResult(500);
            }
        }

        private static bool ValidateApiKey(HttpRequest req)
        {
            req.Headers.TryGetValue("X-API-Key", out var apiKey);
            return !string.IsNullOrEmpty(API_KEY) && apiKey == API_KEY;
        }

        private static string ValidateFeedback(FeedbackModel feedback)
        {
            if (feedback == null)
                return "Invalid feedback data";

            if (string.IsNullOrWhiteSpace(feedback.Type))
                return "Feedback type is required";

            if (!new[] { "Bug", "Feature", "Other" }.Contains(feedback.Type))
                return "Invalid feedback type";

            if (string.IsNullOrWhiteSpace(feedback.Description) || feedback.Description.Length < 20)
                return "Description must be at least 20 characters";

            if (string.IsNullOrWhiteSpace(feedback.Email) || !IsValidEmail(feedback.Email))
                return "Valid email is required";

            if (feedback.Screenshot != null && feedback.Screenshot.Length > 5 * 1024 * 1024) // 5MB limit
                return "Screenshot must be less than 5MB";

            return null;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        private static void SanitizeFeedback(FeedbackModel feedback)
        {
            // Remove HTML/Script tags
            var htmlPattern = @"<[^>]*>";
            feedback.Description = Regex.Replace(feedback.Description, htmlPattern, "");
            if (!string.IsNullOrEmpty(feedback.Title))
            {
                feedback.Title = Regex.Replace(feedback.Title, htmlPattern, "");
            }

            // Trim whitespace
            feedback.Title = feedback.Title?.Trim();
            feedback.Description = feedback.Description?.Trim();
            feedback.Email = feedback.Email?.Trim().ToLower();
        }

        private static async Task<int> CreateGitHubIssue(FeedbackModel feedback, ILogger log)
        {
            var issueTitle = GenerateIssueTitle(feedback);
            var issueBody = GenerateIssueBody(feedback);
            var labels = DetermineLabels(feedback);

            var issue = new
            {
                title = issueTitle,
                body = issueBody,
                labels = labels
            };

            var json = JsonConvert.SerializeObject(issue);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN}");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "FeedbackAzureFunction");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var url = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/issues";
            var response = await HttpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                log.LogError($"GitHub API error: {response.StatusCode} - {error}");
                throw new Exception($"Failed to create GitHub issue: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            return (int)result!.number;
        }

        private static string GenerateIssueTitle(FeedbackModel feedback)
        {
            if (!string.IsNullOrWhiteSpace(feedback.Title))
            {
                return $"[{feedback.Type}] {feedback.Title}";
            }

            // Auto-generate title from description
            var descriptionPreview = feedback.Description.Length > 60
                ? feedback.Description.Substring(0, 57) + "..."
                : feedback.Description;

            return feedback.Type switch
            {
                "Bug" => $"[Bug] {descriptionPreview}",
                "Feature" => $"[Feature Request] {descriptionPreview}",
                _ => $"[Feedback] {descriptionPreview}"
            };
        }

        private static string GenerateIssueBody(FeedbackModel feedback)
        {
            var body = new StringBuilder();

            // Header information
            body.AppendLine($"**Type:** {feedback.Type}");
            body.AppendLine($"**Submitted by:** {feedback.Email}");
            body.AppendLine($"**Date:** {feedback.SubmittedAt:yyyy-MM-dd HH:mm:ss UTC}");
            body.AppendLine();

            // App context
            body.AppendLine("## App Information");
            body.AppendLine($"- **Version:** {feedback.AppVersion ?? "Unknown"}");
            body.AppendLine($"- **Platform:** {feedback.Platform ?? "Unknown"}");
            body.AppendLine($"- **OS Version:** {feedback.OSVersion ?? "Unknown"}");
            body.AppendLine($"- **Device:** {feedback.DeviceModel ?? "Unknown"}");
            body.AppendLine($"- **Language Pack:** {feedback.ActivePack ?? "None"}");
            body.AppendLine($"- **Subscription:** {(feedback.IsPremium ? "Premium" : "Free")}");
            body.AppendLine($"- **Locale:** {feedback.Locale ?? "Unknown"}");
            body.AppendLine();

            // User description
            body.AppendLine("## Description");
            body.AppendLine(feedback.Description);
            body.AppendLine();

            // Screenshot if provided
            if (feedback.Screenshot != null && feedback.Screenshot.Length > 0)
            {
                body.AppendLine("## Screenshot");
                var base64Image = Convert.ToBase64String(feedback.Screenshot);
                var imageFormat = DetectImageFormat(feedback.Screenshot);
                body.AppendLine($"![Screenshot](data:image/{imageFormat};base64,{base64Image})");
                body.AppendLine();
            }

            // Auto-detected severity for bugs
            //if (feedback.Type == "Bug")
            //{
            //    var severity = DetectSeverity(feedback.Description);
            //    body.AppendLine("---");
            //    body.AppendLine($"*Auto-detected severity: {severity}*");
            //}

            return body.ToString();
        }

        private static List<string> DetermineLabels(FeedbackModel feedback)
        {
            var labels = new List<string> { feedback.Type.ToLower() };

            // Add platform label
            if (!string.IsNullOrEmpty(feedback.Platform))
            {
                labels.Add(feedback.Platform.ToLower());
            }

            // Add subscription status
            labels.Add(feedback.IsPremium ? "premium-user" : "free-user");

            // For "Other" type, try to auto-categorize
            //if (feedback.Type == "Other")
            //{
            //    var description = feedback.Description.ToLower();

            //    if (ContainsAny(description, "slow", "performance", "lag", "freeze", "hang"))
            //        labels.Add("performance");
            //    else if (ContainsAny(description, "translation", "wrong", "incorrect", "mistake", "pack"))
            //        labels.Add("content");
            //    else if (ContainsAny(description, "thanks", "great", "love", "awesome", "good job"))
            //        labels.Add("praise");
            //    else if (ContainsAny(description, "ui", "button", "screen", "display", "layout"))
            //        labels.Add("ui-ux");
            //}

            // Add severity for bugs
            //if (feedback.Type == "Bug")
            //{
            //    var severity = DetectSeverity(feedback.Description);
            //    labels.Add($"severity-{severity}");
            //}

            return labels;
        }

        //private static string DetectSeverity(string description)
        //{
        //    var lowerDescription = description.ToLower();

        //    // Critical severity indicators
        //    if (ContainsAny(lowerDescription, "crash", "freezes", "can't open", "won't start",
        //        "lost data", "stuck", "completely broken", "unusable"))
        //        return "high";

        //    // Medium severity indicators
        //    if (ContainsAny(lowerDescription, "doesn't work", "error", "fails", "broken",
        //        "can't", "unable", "not working"))
        //        return "medium";

        //    // Otherwise low
        //    return "low";
        //}

        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        private static string DetectImageFormat(byte[] imageData)
        {
            // Check first few bytes for image format
            if (imageData.Length > 3)
            {
                // PNG signature
                if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                    return "png";

                // JPEG signature
                if (imageData[0] == 0xFF && imageData[1] == 0xD8 && imageData[2] == 0xFF)
                    return "jpeg";

                // GIF signature
                if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                    return "gif";
            }

            return "png"; // Default
        }
    }

    public class FeedbackModel
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public byte[] Screenshot { get; set; }

        // Metadata
        public string AppVersion { get; set; }
        public string Platform { get; set; }
        public string OSVersion { get; set; }
        public string DeviceModel { get; set; }
        public string ActivePack { get; set; }
        public bool IsPremium { get; set; }
        public string Locale { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}