using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace NexusSales.Core.Commanding.Handlers
{
    /// <summary>
    /// Handles Facebook-related commands.
    /// </summary>
    public static class FacebookHandler
    {
        // Example: Reads comments for a given post ID
        public static string ReadComments(string postId, string parameters = null)
        {
            string accessToken = "EAAD6V7os0gcBPAZCwpaKFvAbJbngYZCAwdEFaGZAYQIaIUFypIQfgMpQR015WDuPlZChRaonbV2ipA5SCBkZARHSgkQofUTERQuveaZAKqBEWYYk21y1QOGPiri8AdYDV2sGudXzjCCtw6tJw0AZBPZCSuPYLJOK3CvPWnuxasG1TYD6wTdCZAvFy6mZCGQjHdZAsyHYZAkOLL20ldrDnKOAZAAZDZD"; // TODO: Get from parameters or config
            if (string.IsNullOrWhiteSpace(accessToken))
                return "[]";

            var allComments = new System.Collections.Generic.List<object>();
            string url = $"https://graph.facebook.com/v18.0/{postId}/comments?access_token={accessToken}";
            using (var client = new HttpClient())
            {
                while (!string.IsNullOrEmpty(url))
                {
                    var response = client.GetStringAsync(url).Result;
                    var json = JObject.Parse(response);

                    var comments = json["data"]
                        .Select(c => new
                        {
                            author = c["from"]?["name"]?.ToString() ?? "Unknown",
                            comment = c["message"]?.ToString() ?? "",
                            postId = postId
                        })
                        .ToList();

                    allComments.AddRange(comments);

                    // Check for next page
                    url = json["paging"]?["next"]?.ToString();
                }
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(allComments);
        }

        // Extracts a Facebook post ID from a URL or input
        public static string ExtractId(string input)
        {
            // Try to extract a numeric ID from the input using regex
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // This regex matches the last sequence of digits in the URL (works for most FB URLs)
            var match = Regex.Match(input, @"(\d{5,})\D*$");
            if (match.Success)
                return match.Groups[1].Value;

            // Fallback: try to find any sequence of 5+ digits
            match = Regex.Match(input, @"\d{5,}");
            if (match.Success)
                return match.Value;

            // If nothing found, return empty
            return string.Empty;
        }
    }
}
