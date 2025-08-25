using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using NexusSales.Utils;
using System.IO;
using System.Text;

namespace NexusSales.Core.Commanding.Handlers
{
    /// <summary>
    /// Handles Facebook-related commands.
    /// </summary>
    public static class FacebookHandler
    {
        // [EXPLANATION: These patterns are used for extracting Facebook post IDs from various URL formats.]
        static readonly string[] Patterns =
        {
            @"/permalink/(\d+)",
            @"/videos/(\d+)",
            @"fbid=(\d+)",
            @"/(?:(?:posts)|(?:photos))/(\d+)",
            @"""post_id"":""(\d+)""",
            @"""video_id"":""(\d+)""",
            @"""id"":""(\d{8,})""",
            @"/share/p/([A-Za-z0-9]+)",
            @"/share/v/([A-Za-z0-9]+)"
        };

        static readonly string[] HighSuccessPatterns =
        {
            @"story_fbid=(\d+)",
            @"fbid=(\d+)",
            @"video_id=(\d+)",
            @"<meta[^>]+content=\""fb:\/\/\w+\/\?id=(\d+)\""",
            @"<meta[^>]+property=\""(?:og:url|al:android:url)\""[^>]+content=\""(?:https:\/\/www\.facebook\.com\/(?:.+?)\/(?:posts|videos)\/)(\d+)\""",
            @"""post_id"":""(\d+)""",
            @"""story"":{[^}]*""id"":""(\d+)""",
            @"""target_id"":""(\d+)""",
            @"""feedback_id"":""(\d+)""",
            @"post_id=(\d+)",
            @"story_fbid=(\d+)",
            @"\/posts\/(\d{10,})",
            @"\/permalink\/(\d{10,})",
            @"\/videos\/(\d{10,})"
        };

        // Path to the tamper-resistant audit log (per-user, appdata, not world-readable)
        private static readonly string AuditLogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NexusSales", "audit.log"
        );

        // Logs a security-relevant event to the audit log (append-only, restricted access, optionally encrypted)
        private static void LogAuditEvent(
            string eventType,
            string userEmail,
            string description,
            string outcome,
            string errorCode = null,
            Exception ex = null,
            object context = null)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AuditLogPath));
                var entry = new
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    EventType = eventType,
                    User = userEmail ?? "(unknown)",
                    Description = description,
                    Outcome = outcome,
                    ErrorCode = errorCode,
                    Exception = ex?.ToString(),
                    Context = context
                };
                string line = JsonConvert.SerializeObject(entry);

                byte[] logBytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
                byte[] encrypted = SecureDataUtil.Protect(logBytes);

                using (var fs = new FileStream(AuditLogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    fs.Write(encrypted, 0, encrypted.Length);
                }

                SecureDataUtil.SetFilePermissionsCurrentUserOnly(AuditLogPath);
            }
            catch (Exception logEx)
            {
                Log("ERROR", "FacebookHandler", "LogAuditEvent", "Failed to write audit event.", logEx, "AUDIT-LOG-005", new { AuditLogPathPresent = !string.IsNullOrEmpty(AuditLogPath) });
            }
        }

        // Centralized logger for structured, secure logging
        private static void Log(string level, string module, string method, string message, Exception ex = null, string errorCode = null, object context = null)
        {
            // Never log sensitive data (tokens, passwords, connection strings)
            string logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] {module}.{method} {message}";
            if (!string.IsNullOrEmpty(errorCode)) logLine += $" [ErrorCode: {errorCode}]";
            if (context != null) logLine += $" [Context: {JsonConvert.SerializeObject(context)}]";
            if (ex != null) logLine += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            System.Diagnostics.Debug.WriteLine(logLine);
        }

        // --- Day 12: Input Validation and Output Encoding Policy (Non-destructive) ---
        // These helpers are additive and do not remove or replace any existing logic.
        private static bool Day12_IsValidFacebookUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Regex.IsMatch(url, @"^https?://(www\.|m\.|mbasic\.|mobile\.)?facebook\.com/", RegexOptions.IgnoreCase);
        }
        private static bool Day12_IsValidFacebookPostId(string postId)
        {
            return !string.IsNullOrWhiteSpace(postId) && Regex.IsMatch(postId, @"^\d{8,}$");
        }
        private static bool Day12_IsValidCookie(string cookie)
        {
            // Accept both plain and URL-encoded cookies (for xs_token)
            if (string.IsNullOrWhiteSpace(cookie)) return false;
            // Accept URL-encoded xs_token (contains %)
            if (cookie.Contains("%")) return true;
            // Accept plain Facebook cookie (digits for c_user, alphanum for xs)
            return Regex.IsMatch(cookie, @"^[A-Za-z0-9=._-]+$");
        }
        // --- End Day 12 Policy Additions ---

        // --- Day 12: Additive validation logging (does not block original logic) ---
        private static void Day12_LogInputValidation(string input, string cUserCookie, string xsCookie)
        {
            if (string.IsNullOrWhiteSpace(input) || !Day12_IsValidFacebookUrl(input))
            {
                Log("WARNING", "FacebookHandler", "Day12", "Input is null, empty, or not a valid Facebook URL.");
            }
            if (!string.IsNullOrEmpty(cUserCookie) && !Day12_IsValidCookie(cUserCookie))
            {
                Log("WARNING", "FacebookHandler", "Day12", "cUserCookie is invalid.");
            }
            if (!string.IsNullOrEmpty(xsCookie) && !Day12_IsValidCookie(xsCookie))
            {
                Log("WARNING", "FacebookHandler", "Day12", "xsCookie is invalid.");
            }
        }
        private static void Day12_LogPostIdValidation(string postId)
        {
            if (!Day12_IsValidFacebookPostId(postId))
            {
                Log("WARNING", "FacebookHandler", "Day12", "PostId format is suspicious or invalid.");
            }
        }
        // --- End Day 12 Additive Logging ---

        // --- Day 12: Non-intrusive hooks in main entry points ---
        // ExtractIdWithTokens: log input validation but do not block original logic
        public static string ExtractIdWithTokens(string input, string cUserCookie = null, string xsCookie = null)
        {
            Day12_LogInputValidation(input, cUserCookie, xsCookie); // Additive, does not block

            try
            {
                Log("INFO", "FacebookHandler", "ExtractIdWithTokens", "Starting extraction.", null, null, new { input = input != null ? "[REDACTED]" : null, cUserCookie = !string.IsNullOrEmpty(cUserCookie), xsCookie = !string.IsNullOrEmpty(xsCookie) });
                LogAuditEvent("FacebookExtractId", null, "Starting Facebook ID extraction.", "Info");

                if (string.IsNullOrWhiteSpace(input))
                {
                    Log("WARNING", "FacebookHandler", "ExtractIdWithTokens", "Input is null or empty.");
                    LogAuditEvent("FacebookExtractId", null, "Input is null or empty.", "Failure");
                    return string.Empty;
                }

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    var task = Task.Run(async () => await ExtractIdAsync(input, cUserCookie, xsCookie), cts.Token);
                    try
                    {
                        string result = task.Result;
                        if (!string.IsNullOrEmpty(result))
                        {
                            Log("INFO", "FacebookHandler", "ExtractIdWithTokens", $"Successfully extracted Post ID.", null, null, new { input = "[REDACTED]", result });
                            LogAuditEvent("FacebookExtractId", null, "Successfully extracted Facebook Post ID.", "Success", null, null, new { result });
                        }
                        else
                        {
                            Log("WARNING", "FacebookHandler", "ExtractIdWithTokens", "No Post ID was extracted from the input.", null, null, new { input = "[REDACTED]" });
                            LogAuditEvent("FacebookExtractId", null, "No Post ID was extracted from the input.", "Failure");
                        }
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        Log("ERROR", "FacebookHandler", "ExtractIdWithTokens", "Operation timed out after 60 seconds.", null, "FB-EXT-TIMEOUT-003", new { input = "[REDACTED]" });
                        LogAuditEvent("FacebookExtractId", null, "Operation timed out after 60 seconds.", "Timeout", "FB-EXT-TIMEOUT-003");
                        return string.Empty;
                    }
                }
            }
            catch (AggregateException ex)
            {
                Log("FATAL", "FacebookHandler", "ExtractIdWithTokens", "AggregateException during extraction.", ex, "FB-EXT-AGG-001", new { input = "[REDACTED]", innerCount = ex.InnerExceptions.Count });
                LogAuditEvent("FacebookExtractId", null, "AggregateException during extraction.", "Error", "FB-EXT-AGG-001", ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log("FATAL", "FacebookHandler", "ExtractIdWithTokens", "Unexpected exception during extraction.", ex, "FB-EXT-GEN-002", new { input = "[REDACTED]" });
                LogAuditEvent("FacebookExtractId", null, "Unexpected exception during extraction.", "Error", "FB-EXT-GEN-002", ex);
                return string.Empty;
            }
        }

        // ReadComments: log postId validation but do not block original logic
        public static string ReadComments(string postId, string parameters = null)
        {
            Day12_LogPostIdValidation(postId); // Additive, does not block

            try
            {
                Log("INFO", "FacebookHandler", "ReadComments", "Starting comment extraction.", null, null, new { postId });
                LogAuditEvent("FacebookReadComments", null, "Starting Facebook comment extraction.", "Info", null, null, new { postId });

                string accessToken = ConfigurationManager.AppSettings["FacebookAccessToken"];
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    Log("ERROR", "FacebookHandler", "ReadComments", "Access token is missing.");
                    LogAuditEvent("FacebookReadComments", null, "Access token is missing.", "Failure");
                    return "[]";
                }

                int maxPages = 1000;
                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    var match = Regex.Match(parameters, @"maxPages=(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed))
                        maxPages = parsed;
                }

                var allComments = new System.Collections.Generic.List<object>();
                string url = $"https://graph.facebook.com/v18.0/{postId}/comments?access_token={accessToken}";

                Log("DEBUG", "FacebookHandler", "ReadComments", $"API URL: {url}", null, null, new { postId });

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    int pageCount = 0;

                    while (!string.IsNullOrEmpty(url) && pageCount < maxPages)
                    {
                        try
                        {
                            Log("DEBUG", "FacebookHandler", "ReadComments", $"Fetching page {pageCount + 1}...", null, null, new { postId, pageCount });

                            string response;
                            try
                            {
                                var httpResponse = client.GetAsync(url).GetAwaiter().GetResult();
                                response = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                                if (!httpResponse.IsSuccessStatusCode)
                                {
                                    Log("ERROR", "FacebookHandler", "ReadComments", "Non-success HTTP status.", null, null, new { postId, status = httpResponse.StatusCode, responseLength = response?.Length ?? 0 });
                                    LogAuditEvent("FacebookReadComments", null, "Non-success HTTP status from Facebook API.", "Failure", null, null, new { postId, status = httpResponse.StatusCode });

                                    try
                                    {
                                        var errorJson = JObject.Parse(response);
                                        if (errorJson["error"] != null)
                                        {
                                            var errorMsg = errorJson["error"]["message"]?.ToString() ?? "Unknown Facebook error";
                                            var errorCode = errorJson["error"]["code"]?.ToString() ?? "Unknown";
                                            var errorType = errorJson["error"]["type"]?.ToString() ?? "Unknown";
                                            var errorSubcode = errorJson["error"]["error_subcode"]?.ToString() ?? "";

                                            Log("ERROR", "FacebookHandler", "ReadComments", "Facebook API Error.", null, null, new { postId, errorMsg, errorCode, errorType, errorSubcode });
                                            LogAuditEvent("FacebookReadComments", null, $"Facebook API Error: {errorMsg}", "Failure", errorCode, null, new { postId, errorType, errorSubcode });

                                            return JsonConvert.SerializeObject(new[]
                                            {
                                                new
                                                {
                                                    author = "System",
                                                    comment = $"Facebook API Error: {errorMsg} (Code: {errorCode}, Type: {errorType})",
                                                    postId = postId,
                                                    error = "FACEBOOK_API_ERROR",
                                                    errorCode = errorCode,
                                                    errorType = errorType,
                                                    errorSubcode = errorSubcode,
                                                    httpStatus = httpResponse.StatusCode.ToString(),
                                                    fullErrorResponse = "[REDACTED]"
                                                }
                                            });
                                        }
                                    }
                                    catch (JsonReaderException)
                                    {
                                        Log("ERROR", "FacebookHandler", "ReadComments", "Could not parse error response as JSON.", null, null, new { postId });
                                        LogAuditEvent("FacebookReadComments", null, "Could not parse error response as JSON.", "Error", null, null, new { postId });
                                    }

                                    return JsonConvert.SerializeObject(new[]
                                    {
                                        new
                                        {
                                            author = "System",
                                            comment = $"HTTP {httpResponse.StatusCode}: [REDACTED]",
                                            postId = postId,
                                            error = "HTTP_ERROR",
                                            httpStatus = httpResponse.StatusCode.ToString(),
                                            rawResponse = "[REDACTED]"
                                        }
                                    });
                                }
                            }
                            catch (HttpRequestException httpEx)
                            {
                                Log("ERROR", "FacebookHandler", "ReadComments", "HTTP Error.", httpEx, null, new { postId });
                                LogAuditEvent("FacebookReadComments", null, "HTTP Error during Facebook comment extraction.", "Error", null, httpEx, new { postId });
                                return JsonConvert.SerializeObject(new[]
                                {
                                    new
                                    {
                                        author = "System",
                                        comment = $"HTTP Error: {httpEx.Message}. This might indicate the post is private, deleted, or your access token doesn't have permission to read comments.",
                                        postId = postId,
                                        error = "HTTP_ERROR"
                                    }
                                });
                            }
                            catch (TaskCanceledException timeoutEx)
                            {
                                Log("ERROR", "FacebookHandler", "ReadComments", "Request timed out.", timeoutEx, null, new { postId });
                                LogAuditEvent("FacebookReadComments", null, "Request timed out during Facebook comment extraction.", "Timeout", null, timeoutEx, new { postId });
                                return JsonConvert.SerializeObject(new[]
                                {
                                    new
                                    {
                                        author = "System",
                                        comment = $"Request timed out: {timeoutEx.Message}. The Facebook API might be slow or unreachable.",
                                        postId = postId,
                                        error = "TIMEOUT_ERROR"
                                    }
                                });
                            }

                            if (string.IsNullOrEmpty(response))
                            {
                                Log("WARNING", "FacebookHandler", "ReadComments", "Empty response from Facebook API.", null, null, new { postId });
                                LogAuditEvent("FacebookReadComments", null, "Empty response from Facebook API.", "Warning", null, null, new { postId });
                                break;
                            }

                            JObject json;
                            try
                            {
                                json = JObject.Parse(response);
                            }
                            catch (JsonReaderException jsonEx)
                            {
                                Log("ERROR", "FacebookHandler", "ReadComments", "JSON Parse Error.", jsonEx, null, new { postId });
                                LogAuditEvent("FacebookReadComments", null, "JSON Parse Error during Facebook comment extraction.", "Error", null, jsonEx, new { postId });
                                return JsonConvert.SerializeObject(new[]
                                {
                                    new
                                    {
                                        author = "System",
                                        comment = $"Invalid JSON response from Facebook API: {jsonEx.Message}",
                                        postId = postId,
                                        error = "JSON_PARSE_ERROR",
                                        responseContent = "[REDACTED]"
                                    }
                                });
                            }

                            if (json["error"] != null)
                            {
                                var errorMsg = json["error"]["message"]?.ToString() ?? "Unknown error";
                                var errorCode = json["error"]["code"]?.ToString() ?? "Unknown";
                                var errorType = json["error"]["type"]?.ToString() ?? "Unknown";
                                Log("ERROR", "FacebookHandler", "ReadComments", "Facebook API Error in JSON.", null, null, new { postId, errorMsg, errorCode, errorType });
                                LogAuditEvent("FacebookReadComments", null, $"Facebook API Error in JSON: {errorMsg}", "Failure", errorCode, null, new { postId, errorType });

                                return JsonConvert.SerializeObject(new[]
                                {
                                    new
                                    {
                                        author = "System",
                                        comment = $"Facebook API Error: {errorMsg} (Code: {errorCode}, Type: {errorType})",
                                        postId = postId,
                                        error = "FACEBOOK_API_ERROR",
                                        errorCode = errorCode,
                                        errorType = errorType
                                    }
                                });
                            }

                            var commentsData = json["data"];
                            if (commentsData != null)
                            {
                                var comments = commentsData
                                    .Select(c => new
                                    {
                                        author = c["from"]?["name"]?.ToString() ?? "Unknown",
                                        comment = c["message"]?.ToString() ?? "",
                                        postId = postId,
                                        commentId = c["id"]?.ToString() ?? "",
                                        createdTime = c["created_time"]?.ToString() ?? ""
                                    })
                                    .ToList();

                                allComments.AddRange(comments);
                                Log("DEBUG", "FacebookHandler", "ReadComments", $"Page {pageCount + 1}: Found {comments.Count} comments. Total: {allComments.Count}", null, null, new { postId, pageCount });
                            }
                            else
                            {
                                Log("WARNING", "FacebookHandler", "ReadComments", $"No 'data' field in response for page {pageCount + 1}", null, null, new { postId, pageCount });
                                LogAuditEvent("FacebookReadComments", null, $"No 'data' field in response for page {pageCount + 1}", "Warning", null, null, new { postId, pageCount });
                            }

                            url = json["paging"]?["next"]?.ToString();
                            pageCount++;
                        }
                        catch (Exception pageEx)
                        {
                            Log("ERROR", "FacebookHandler", "ReadComments", $"Error on page {pageCount + 1}.", pageEx, null, new { postId, pageCount });
                            LogAuditEvent("FacebookReadComments", null, $"Error on page {pageCount + 1}.", "Error", null, pageEx, new { postId, pageCount });
                            if (pageCount == 0)
                            {
                                return JsonConvert.SerializeObject(new[]
                                {
                                    new
                                    {
                                        author = "System",
                                        comment = $"Error on first page: {pageEx.Message}",
                                        postId = postId,
                                        error = "FIRST_PAGE_ERROR"
                                    }
                                });
                            }
                            break;
                        }
                    }

                    if (pageCount >= maxPages)
                    {
                        Log("WARNING", "FacebookHandler", "ReadComments", $"Reached maximum page limit ({maxPages}).", null, null, new { postId });
                        LogAuditEvent("FacebookReadComments", null, $"Reached maximum page limit ({maxPages}).", "Warning", null, null, new { postId });
                    }
                }

                Log("INFO", "FacebookHandler", "ReadComments", $"Successfully extracted {allComments.Count} comments.", null, null, new { postId });
                LogAuditEvent("FacebookReadComments", null, $"Successfully extracted {allComments.Count} comments.", "Success", null, null, new { postId });

                if (allComments.Count == 0)
                {
                    return JsonConvert.SerializeObject(new[]
                    {
                        new
                        {
                            author = "System",
                            comment = "No comments found on this post. The post might have no comments, or comments might be disabled.",
                            postId = postId,
                            error = "NO_COMMENTS_FOUND"
                        }
                    });
                }

                return JsonConvert.SerializeObject(allComments);
            }
            catch (Exception ex)
            {
                Log("FATAL", "FacebookHandler", "ReadComments", "Unexpected Exception.", ex, "FB-READ-GEN-001", new { postId });
                LogAuditEvent("FacebookReadComments", null, "Unexpected Exception during comment extraction.", "Error", "FB-READ-GEN-001", ex, new { postId });
                return JsonConvert.SerializeObject(new[]
                {
                    new
                    {
                        author = "System",
                        comment = $"Unexpected error reading comments: {ex.GetType().Name}: {ex.Message}",
                        postId = postId,
                        error = "UNEXPECTED_ERROR",
                        exceptionType = ex.GetType().Name
                    }
                });
            }
        }

        /// <summary>
        /// Reads all comments from a Facebook post using c_user and xs cookies (browser simulation, not Graph API).
        /// </summary>
        /// <param name="postId">The Facebook post ID.</param>
        /// <param name="cUserCookie">The c_user cookie value.</param>
        /// <param name="xsCookie">The xs cookie value.</param>
        /// <returns>JSON string with all comments and their IDs.</returns>
        public static string ReadCommentsWithCookies(string postId, string cUserCookie, string xsCookie)
        {
            // Do NOT overwrite cookies with App.config values; use what is passed in

            // Validate input
            Day12_LogPostIdValidation(postId);
            if (!Day12_IsValidFacebookPostId(postId) || !Day12_IsValidCookie(cUserCookie) || !Day12_IsValidCookie(xsCookie))
            {
                Log("ERROR", "FacebookHandler", "ReadCommentsWithCookies", "Invalid input.", null, "FB-COOKIES-VAL-001", new { postId });
                return JsonConvert.SerializeObject(new[]
                {
                    new { error = "INVALID_INPUT", message = "Invalid postId or cookies." }
                });
            }

            var comments = new System.Collections.Generic.List<object>();
            string url = $"https://mbasic.facebook.com/{postId}";
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Cookie("c_user", cUserCookie, "/", ".facebook.com"));
            cookieContainer.Add(new Cookie("xs", xsCookie, "/", ".facebook.com"));

            try
            {
                using (var handler = new HttpClientHandler { CookieContainer = cookieContainer, AllowAutoRedirect = true })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    string html = client.GetStringAsync(url).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(html))
                        return JsonConvert.SerializeObject(new[] { new { error = "NO_HTML", message = "No HTML content returned." } });

                    // Parse comments (simple regex for mbasic)
                    // Example: <div id="comment_1234567890"> ... <a href="/profile.php?id=...">Author</a> ... <span>Comment text</span>
                    var commentRegex = new Regex("<div[^>]+id=\\\"comment_(\\d+)\\\"[\\s\\S]*?<a[^>]+href=\\\"[^\\\"]+\\\"[^>]*>(.*?)</a>[\\s\\S]*?<span[^>]*>(.*?)</span>", RegexOptions.IgnoreCase);
                    var matches = commentRegex.Matches(html);
                    foreach (Match match in matches)
                    {
                        string commentId = match.Groups[1].Value;
                        string author = WebUtility.HtmlDecode(match.Groups[2].Value);
                        string comment = WebUtility.HtmlDecode(match.Groups[3].Value);
                        comments.Add(new { commentId, author, comment });
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", "FacebookHandler", "ReadCommentsWithCookies", "Exception reading comments.", ex, "FB-COOKIES-READ-002", new { postId });
                return JsonConvert.SerializeObject(new[] { new { error = "EXCEPTION", message = ex.Message } });
            }
            return JsonConvert.SerializeObject(comments);
        }

        /// <summary>
        /// Reacts to all comments on a Facebook post using c_user and xs cookies.
        /// </summary>
        /// <param name="postId">The Facebook post ID.</param>
        /// <param name="cUserCookie">The c_user cookie value.</param>
        /// <param name="xsCookie">The xs cookie value.</param>
        /// <param name="reactionType">The reaction type (1=Like, 2=Love, etc.).</param>
        /// <returns>JSON summary of reactions, including per-type counts and author info.</returns>
        public static string ReactToComments(string postId, string cUserCookie, string xsCookie, int reactionType)
        {
            Logger.Log("INFO", "FacebookHandler", "ReactToComments", $"Starting reaction process for postId={postId}, reactionType={reactionType}");
            // Do NOT overwrite cookies with App.config values; use what is passed in

            var commentsJson = ReadCommentsWithCookies(postId, cUserCookie, xsCookie);
            var comments = new System.Collections.Generic.List<dynamic>();
            try
            {
                comments = JsonConvert.DeserializeObject<System.Collections.Generic.List<dynamic>>(commentsJson);
            }
            catch (Exception ex)
            {
                Logger.Log("ERROR", "FacebookHandler", "ReactToComments", "Failed to parse comments JSON.", ex);
                return JsonConvert.SerializeObject(new { error = "COMMENT_PARSE_ERROR", message = "Failed to parse comments." });
            }
            if (comments == null || comments.Count == 0 || (comments.Count == 1 && comments[0].error != null))
            {
                Logger.Log("WARNING", "FacebookHandler", "ReactToComments", "No comments to react to.");
                return JsonConvert.SerializeObject(new { error = "NO_COMMENTS", message = "No comments to react to." });
            }

            int success = 0, fail = 0;
            var results = new System.Collections.Generic.List<object>();
            var reactionCounts = new System.Collections.Generic.Dictionary<int, int>();
            foreach (int t in new[] { 1, 2, 3, 4, 7, 8, 16 }) reactionCounts[t] = 0;

            foreach (var c in comments)
            {
                string commentId = c.commentId;
                string author = c.author;
                string reactUrl = $"https://www.facebook.com/ufi/reaction/?ft_ent_identifier={commentId}&reaction_type={reactionType}";
                var cookieContainer = new CookieContainer();
                cookieContainer.Add(new Cookie("c_user", cUserCookie, "/", ".facebook.com"));
                cookieContainer.Add(new Cookie("xs", xsCookie, "/", ".facebook.com"));
                string status = "";
                try
                {
                    using (var handler = new HttpClientHandler { CookieContainer = cookieContainer, AllowAutoRedirect = true })
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(20);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        var response = client.PostAsync(reactUrl, new StringContent(string.Empty)).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                        {
                            success++;
                            status = "OK";
                            if (reactionCounts.ContainsKey(reactionType)) reactionCounts[reactionType]++;
                            Logger.Log("INFO", "FacebookHandler", "ReactToComments", $"Reacted OK to commentId={commentId}, author={author}");
                        }
                        else
                        {
                            fail++;
                            status = "FAIL";
                            Logger.Log("WARNING", "FacebookHandler", "ReactToComments", $"Failed to react to commentId={commentId}, author={author}, status={response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    status = "EXCEPTION";
                    Logger.Log("ERROR", "FacebookHandler", "ReactToComments", $"Exception for commentId={commentId}, author={author}", ex);
                }
                results.Add(new { commentId, author, status, reactionType });
            }
            Logger.Log("INFO", "FacebookHandler", "ReactToComments", $"Finished reactions. Total={comments.Count}, Success={success}, Fail={fail}");
            return JsonConvert.SerializeObject(new { total = comments.Count, success, fail, reactionCounts, results });
        }

        public static string ExtractId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            foreach (var pattern in Patterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        public static async Task<string> ExtractIdAsync(string url, string cUserCookie = null, string xsCookie = null)
        {
            if (string.IsNullOrEmpty(url)) return null;

            string basicId = ExtractId(url);
            if (!string.IsNullOrEmpty(basicId))
            {
                return basicId;
            }

            if (!string.IsNullOrEmpty(cUserCookie) && !string.IsNullOrEmpty(xsCookie))
            {
                return await ExtractIdWithCookiesAsync(url, cUserCookie, xsCookie);
            }

            return null;
        }

        private static bool IsBlacklistedId(string id, string cUserCookie)
        {
            string[] blacklistedIds = {
                "409962623085609", "100041584152497", "100000000000000", "100001000000000",
                "100002000000000", "100003000000000", "100004000000000", "100005000000000"
            };

            return blacklistedIds.Contains(id);
        }

        private static async Task<string> ExtractIdWithCookiesAsync(string url, string cUserCookie, string xsCookie)
        {
            try
            {
                string mobileUrl = url;
                if (url.Contains("www.facebook.com"))
                {
                    mobileUrl = url.Replace("www.facebook.com", "mbasic.facebook.com");
                }
                else if (url.Contains("facebook.com") && !url.Contains("mbasic.facebook.com"))
                {
                    mobileUrl = url.Replace("facebook.com", "mbasic.facebook.com");
                }
                if (mobileUrl.Contains("mbasic.mbasic"))
                {
                    mobileUrl = mobileUrl.Replace("mbasic.mbasic.facebook.com", "mbasic.facebook.com");
                }

                Log("DEBUG", "FacebookHandler", "ExtractIdWithCookiesAsync", $"Trying mbasic URL.", null, null, new { mobileUrl = "[REDACTED]" });

                var cookieContainer = new CookieContainer();
                cookieContainer.Add(new Cookie("c_user", cUserCookie, "/", ".facebook.com"));
                cookieContainer.Add(new Cookie("xs", xsCookie, "/", ".facebook.com"));

                using (var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    AllowAutoRedirect = true
                })
                {
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1");
                        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

                        var response = await client.GetAsync(mobileUrl);
                        string finalUrl = response.RequestMessage.RequestUri.AbsoluteUri;

                        Log("DEBUG", "FacebookHandler", "ExtractIdWithCookiesAsync", $"mbasic response URL and status.", null, null, new { finalUrl = "[REDACTED]", status = response.StatusCode });

                        string html = string.Empty;
                        try
                        {
                            html = await response.Content.ReadAsStringAsync();
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("character set"))
                        {
                            Log("WARNING", "FacebookHandler", "ExtractIdWithCookiesAsync", "Character set error in mbasic, trying binary read.", ex);
                            try
                            {
                                var bytes = await response.Content.ReadAsByteArrayAsync();
                                html = System.Text.Encoding.UTF8.GetString(bytes);
                            }
                            catch (Exception binaryEx)
                            {
                                Log("ERROR", "FacebookHandler", "ExtractIdWithCookiesAsync", "mbasic binary read failed.", binaryEx);
                                return string.Empty;
                            }
                        }

                        if (string.IsNullOrEmpty(html))
                        {
                            Log("WARNING", "FacebookHandler", "ExtractIdWithCookiesAsync", "No content received from mbasic.");
                            return string.Empty;
                        }

                        var blacklistedIds = new System.Collections.Generic.HashSet<string>
                        {
                            "409962623085609", "100041584152497", cUserCookie,
                            "100000000000000", "100001000000000", "100002000000000",
                            "100003000000000", "100004000000000", "100005000000000"
                        };

                        var urlMatch = Regex.Match(finalUrl, @"(?:fbid|story_fbid|video_id)=([0-9]+)");
                        if (urlMatch.Success && !blacklistedIds.Contains(urlMatch.Groups[1].Value))
                        {
                            return urlMatch.Groups[1].Value;
                        }

                        urlMatch = Regex.Match(finalUrl, @"\/(?:permalink|posts|videos)\/([0-9]+)");
                        if (urlMatch.Success && !blacklistedIds.Contains(urlMatch.Groups[1].Value))
                        {
                            return urlMatch.Groups[1].Value;
                        }

                        string[] priorityHtmlPatterns = {
                            @"story_fbid=(\d+)", @"fbid=(\d+)", @"video_id=(\d+)",
                            @"<meta[^>]+content=\""fb:\/\/\w+\/\?id=(\d+)\""",
                            @"<meta[^>]+property=\""(?:og:url|al:android:url)\""[^>]+content=\""(?:https:\/\/www\.facebook\.com\/(?:.+?)\/(?:posts|videos)\/)(\d+)\""",
                            @"""post_id"":""(\d+)""", @"""story"":{[^}]*""id"":""(\d+)""", @"""target_id"":""(\d+)"""
                        };

                        foreach (var pattern in priorityHtmlPatterns)
                        {
                            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                            if (match.Success && !blacklistedIds.Contains(match.Groups[1].Value))
                            {
                                return match.Groups[1].Value;
                            }
                        }

                        var longNumbers = Regex.Matches(html, @"\b(\d{15,16})\b");
                        if (longNumbers.Count > 0)
                        {
                            var validCandidates = new System.Collections.Generic.List<string>();
                            foreach (Match longMatch in longNumbers)
                            {
                                string potentialId = longMatch.Groups[1].Value;
                                if (blacklistedIds.Contains(potentialId)) continue;
                                if (potentialId.StartsWith("10000") || potentialId.StartsWith("10001") ||
                                    potentialId.StartsWith("10002") || potentialId.StartsWith("10003") ||
                                    potentialId.StartsWith("10004") || potentialId.StartsWith("10005")) continue;
                                validCandidates.Add(potentialId);
                            }
                            if (validCandidates.Count > 0)
                            {
                                foreach (var candidate in validCandidates)
                                {
                                    if (html.Contains($"\"post_id\":\"{candidate}\"") ||
                                        html.Contains($"story_fbid={candidate}") ||
                                        html.Contains($"\"story\":{{\"id\":\"{candidate}\""))
                                    {
                                        return candidate;
                                    }
                                }
                                return validCandidates[0];
                            }
                        }

                        var allNumbers = Regex.Matches(html, @"\b(\d{10,})\b");
                        if (allNumbers.Count > 0)
                        {
                            var fallbackCandidates = new System.Collections.Generic.List<string>();
                            foreach (Match numMatch in allNumbers)
                            {
                                string potentialId = numMatch.Groups[1].Value;
                                if (blacklistedIds.Contains(potentialId)) continue;
                                if (potentialId.Length < 10) continue;
                                if (potentialId.StartsWith("10000") || potentialId.StartsWith("10001") ||
                                    potentialId.StartsWith("10002") || potentialId.StartsWith("10003") ||
                                    potentialId.StartsWith("10004") || potentialId.StartsWith("10005")) continue;
                                if (potentialId.Length >= 13)
                                {
                                    fallbackCandidates.Add(potentialId);
                                }
                            }
                            if (fallbackCandidates.Count > 0)
                            {
                                var bestCandidate = fallbackCandidates.OrderByDescending(id => id.Length).First();
                                return bestCandidate;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", "FacebookHandler", "ExtractIdWithCookiesAsync", "Error in mbasic extraction.", ex, "FB-EXT-MBASIC-008");
            }

            return string.Empty;
        }

        private static async Task<string> ExtractWithDirectHttpAsync(string url, string cUserCookie, string xsCookie)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseCookies = true
                };

                if (!string.IsNullOrEmpty(cUserCookie) && !string.IsNullOrEmpty(xsCookie))
                {
                    handler.CookieContainer.Add(new Cookie("c_user", cUserCookie, "/", ".facebook.com"));
                    handler.CookieContainer.Add(new Cookie("xs", xsCookie, "/", ".facebook.com"));
                }

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                        "AppleWebKit/537.36 (KHTML, like Gecko) " +
                        "Chrome/120.0.0.0 Safari/537.36");

                    var response = await client.GetAsync(url);
                    var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

                    foreach (var pattern in HighSuccessPatterns.Take(5))
                    {
                        var match = Regex.Match(finalUrl, pattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string extractedId = match.Groups[1].Value;
                            if (!IsBlacklistedId(extractedId, cUserCookie))
                            {
                                return extractedId;
                            }
                        }
                    }

                    string html = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(html))
                    {
                        foreach (var pattern in HighSuccessPatterns)
                        {
                            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string extractedId = match.Groups[1].Value;
                                if (!IsBlacklistedId(extractedId, cUserCookie))
                                {
                                    return extractedId;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", "FacebookHandler", "ExtractWithDirectHttpAsync", "Error in direct HTTP extraction.", ex);
            }

            return string.Empty;
        }

        private static async Task<string> TryAlternativeUrlFormats(string url, string cUserCookie, string xsCookie)
        {
            try
            {
                string[] alternativeUrls = {
                    url.Replace("m.facebook.com", "www.facebook.com"),
                    url.Replace("mobile.facebook.com", "www.facebook.com"),
                    url.Replace("www.facebook.com", "m.facebook.com"),
                    url.Replace("facebook.com", "m.facebook.com")
                };

                foreach (var altUrl in alternativeUrls)
                {
                    if (altUrl != url)
                    {
                        foreach (var pattern in HighSuccessPatterns.Take(3))
                        {
                            var match = Regex.Match(altUrl, pattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string extractedId = match.Groups[1].Value;
                                if (!IsBlacklistedId(extractedId, cUserCookie))
                                {
                                    return extractedId;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", "FacebookHandler", "TryAlternativeUrlFormats", "Error in alternative URL extraction.", ex);
            }

            return string.Empty;
        }

        public static async Task<string> ExtractPostId(string url, string c_user_token, string xs_token)
        {
            return await ExtractIdAsync(url, c_user_token, xs_token);
        }

        /// <summary>
        /// Replies to a Facebook comment using the Graph API v19.0.
        /// </summary>
        /// <param name="commentId">The comment ID to reply to.</param>
        /// <param name="replyMessage">The reply message.</param>
        /// <param name="accessToken">The Facebook Page Access Token.</param>
        /// <returns>JSON result with success/failure and error details if any.</returns>
        public static string ReplyToComment(string commentId, string replyMessage, string accessToken)
        {
            Logger.Log("INFO", "FacebookHandler", "ReplyToComment", $"Called with commentId={commentId}, replyMessage={replyMessage}, accessToken={(string.IsNullOrEmpty(accessToken) ? "(empty)" : "(provided)")}");
            Log("INFO", "FacebookHandler", "ReplyToComment", $"Replying to commentId={commentId}");
            LogAuditEvent("FacebookReplyToComment", null, $"Replying to comment {commentId}", "Info", null, null, new { commentId });
            if (string.IsNullOrWhiteSpace(commentId) || string.IsNullOrWhiteSpace(replyMessage) || string.IsNullOrWhiteSpace(accessToken))
            {
                Logger.Log("ERROR", "FacebookHandler", "ReplyToComment", "Missing required parameters.", null, null, new { commentId, replyMessage, accessToken });
                Log("ERROR", "FacebookHandler", "ReplyToComment", "Missing required parameters.");
                return JsonConvert.SerializeObject(new { success = false, error = "MISSING_PARAMETERS" });
            }
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"https://graph.facebook.com/v19.0/{commentId}/comments";
                    var payload = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "message", replyMessage },
                        { "access_token", accessToken }
                    };
                    var content = new FormUrlEncodedContent(payload);
                    Logger.Log("INFO", "FacebookHandler", "ReplyToComment", $"Sending POST to {url} with message: {replyMessage}");
                    var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                    var respText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Logger.Log("INFO", "FacebookHandler", "ReplyToComment", $"Response: {respText}");
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log("INFO", "FacebookHandler", "ReplyToComment", $"Reply sent to commentId={commentId}");
                        Log("INFO", "FacebookHandler", "ReplyToComment", $"Reply sent to commentId={commentId}");
                        LogAuditEvent("FacebookReplyToComment", null, $"Reply sent to comment {commentId}", "Success", null, null, new { commentId });
                        return JsonConvert.SerializeObject(new { success = true, response = respText });
                    }
                    else
                    {
                        Logger.Log("ERROR", "FacebookHandler", "ReplyToComment", $"Failed to reply. Status: {response.StatusCode}", null, null, new { commentId, status = response.StatusCode, respText });
                        Log("ERROR", "FacebookHandler", "ReplyToComment", $"Failed to reply. Status: {response.StatusCode}");
                        LogAuditEvent("FacebookReplyToComment", null, $"Failed to reply to comment {commentId}", "Failure", null, null, new { commentId, status = response.StatusCode });
                        return JsonConvert.SerializeObject(new { success = false, error = "API_ERROR", status = response.StatusCode.ToString(), response = respText });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "FacebookHandler", "ReplyToComment", "Exception replying to comment.", ex, null, new { commentId, replyMessage, accessToken });
                Log("FATAL", "FacebookHandler", "ReplyToComment", "Exception replying to comment.", ex);
                LogAuditEvent("FacebookReplyToComment", null, $"Exception replying to comment {commentId}", "Error", null, ex, new { commentId });
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
    }
}
