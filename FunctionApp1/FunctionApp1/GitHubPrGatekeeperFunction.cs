using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FunctionApp1;

public sealed class GitHubPrGatekeeperFunction
{
    private static readonly Regex TitlePrefixRegex = new(
        @"^(bug|feature|perf|docs|refactor|test|chore):",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BranchRegex = new(
        @"^[A-Za-z0-9._-]+/.+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<GitHubPrGatekeeperFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubPrGatekeeperFunction(
        ILogger<GitHubPrGatekeeperFunction> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /*
	Example `appsettings.json.user` (do NOT commit secrets):
	{
	  "GitHub": {
		"WebhookSecret": "...",
		"PatToken": "..."
	  }
	}
	*/

    [Function("GitHubPrGatekeeper")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "github/webhooks/pr-gatekeeper")] HttpRequestData req,
        FunctionContext executionContext,
        CancellationToken cancellationToken)
    {
        var deliveryId = GetHeader(req, "X-GitHub-Delivery");
        var gitHubEvent = GetHeader(req, "X-GitHub-Event");
        var signatureHeader = GetHeader(req, "X-Hub-Signature-256");

        _logger.LogInformation(
            "GitHub webhook received. delivery={DeliveryId} event={Event}",
            deliveryId,
            gitHubEvent);

        byte[] bodyBytes;
        try
        {
            bodyBytes = await ReadBodyBytesAsync(req, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading request body. delivery={DeliveryId}", deliveryId);
            return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new
            {
                error = "Invalid request body."
            }, cancellationToken).ConfigureAwait(false);
        }

        var webhookSecret = _configuration["GitHub:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogError("Missing configuration: GitHub:WebhookSecret");
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new
            {
                error = "Server misconfiguration."
            }, cancellationToken).ConfigureAwait(false);
        }

        if (!IsSignatureValid(signatureHeader, bodyBytes, webhookSecret))
        {
            _logger.LogWarning(
                "Invalid signature. delivery={DeliveryId} event={Event}",
                deliveryId,
                gitHubEvent);

            return await CreateJsonResponseAsync(req, HttpStatusCode.Unauthorized, new
            {
                error = "Invalid or missing signature."
            }, cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(gitHubEvent, "pull_request", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateJsonResponseAsync(req, HttpStatusCode.Accepted, new
            {
                handled = false,
                reason = "Ignored event.",
                eventName = gitHubEvent
            }, cancellationToken).ConfigureAwait(false);
        }

        GitHubPullRequestEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GitHubPullRequestEvent>(bodyBytes, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON payload. delivery={DeliveryId}", deliveryId);
            return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new
            {
                error = "Invalid JSON payload."
            }, cancellationToken).ConfigureAwait(false);
        }

        var action = payload?.Action;
        if (!IsSupportedAction(action))
        {
            return await CreateJsonResponseAsync(req, HttpStatusCode.Accepted, new
            {
                handled = false,
                reason = "Ignored action.",
                action
            }, cancellationToken).ConfigureAwait(false);
        }

        var pr = payload?.PullRequest;
        var owner = pr?.Base?.Repo?.Owner?.Login;
        var repo = pr?.Base?.Repo?.Name;
        var prNumber = pr?.Number;

        var failures = Evaluate(pr);
        var passed = failures.Count == 0;

        _logger.LogInformation(
            "PR gatekeeper evaluated. delivery={DeliveryId} repo={Owner}/{Repo} pr={PrNumber} action={Action} passed={Passed}",
            deliveryId,
            owner,
            repo,
            prNumber,
            action,
            passed);

        // If we can't locate required identifiers, treat as failure and avoid calling GitHub.
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo) || prNumber is null)
        {
            failures.Add(new Failure(
                "payload.required_fields",
                "Missing required PR identifiers (owner/repo/number) in payload.",
                "Ensure the webhook is configured for `pull_request` events and sends full payload."));
            passed = false;
        }

        var commentBody = BuildComment(passed, failures);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo) || prNumber is null)
        {
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, new
            {
                handled = true,
                passed,
                failures
            }, cancellationToken).ConfigureAwait(false);
        }

        var patToken = _configuration["GitHub:PatToken"];
        if (string.IsNullOrWhiteSpace(patToken))
        {
            _logger.LogError("Missing configuration: GitHub:PatToken");
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new
            {
                error = "Server misconfiguration."
            }, cancellationToken).ConfigureAwait(false);
        }

        var posted = await PostPrCommentAsync(owner, repo, prNumber.Value, commentBody, patToken, deliveryId, cancellationToken)
            .ConfigureAwait(false);

        return await CreateJsonResponseAsync(req, HttpStatusCode.OK, new
        {
            handled = true,
            passed,
            failures,
            commentPosted = posted
        }, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsSupportedAction(string? action) =>
        string.Equals(action, "opened", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, "edited", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, "reopened", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, "synchronize", StringComparison.OrdinalIgnoreCase);

    private List<Failure> Evaluate(GitHubPullRequest? pr)
    {
        var failures = new List<Failure>();

        var title = pr?.Title;
        if (string.IsNullOrWhiteSpace(title) || !TitlePrefixRegex.IsMatch(title))
        {
            failures.Add(new Failure(
                "title.prefix",
                "Title must start with a valid prefix followed by ':' (bug|feature|perf|docs|refactor|test|chore).",
                "Update the title to e.g. 'feature: add X' (prefix is case-insensitive)."));
        }

        var branch = pr?.Head?.Ref;
        if (string.IsNullOrWhiteSpace(branch) || !BranchRegex.IsMatch(branch) || HasMoreThanOneSlash(branch))
        {
            failures.Add(new Failure(
                "branch.format",
                "Source branch must match: {dev-username}/{description} (exactly one '/').",
                "Rename the branch to e.g. 'alice/fix-crash' (username may contain letters, numbers, '.', '_' or '-')."));
        }

        var bodyLen = pr?.Body?.Length ?? 0;
        if (bodyLen < 100)
        {
            failures.Add(new Failure(
                "description.length",
                "PR description must be at least 100 characters.",
                "Expand the PR description with context, rationale, and testing notes (>= 100 chars)."));
        }

        return failures;
    }

    private static bool HasMoreThanOneSlash(string value)
    {
        var first = value.IndexOf('/');
        if (first < 0)
            return false;
        var second = value.IndexOf('/', first + 1);
        return second >= 0;
    }

    private static string BuildComment(bool passed, IReadOnlyList<Failure> failures)
    {
        var sb = new StringBuilder();
        if (passed)
        {
            sb.AppendLine("## PR Gatekeeper: PASS");
            sb.AppendLine();
            sb.AppendLine("All checks passed.");
        }
        else
        {
            sb.AppendLine("## PR Gatekeeper: FAIL");
            sb.AppendLine();
            sb.AppendLine("The following checks did not pass:");
            sb.AppendLine();
            foreach (var f in failures)
            {
                sb.Append("- [ ] ").AppendLine(ToTitle(f.Code));
                sb.Append("  - **Issue:** ").AppendLine(f.Message);
                if (!string.IsNullOrWhiteSpace(f.HowToFix))
                    sb.Append("  - **How to fix:** ").AppendLine(f.HowToFix);
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("<!-- pr-gatekeeper -->");
        return sb.ToString();
    }

    private static string ToTitle(string? code) => code switch
    {
        "title.prefix" => "Title prefix",
        "branch.format" => "Branch name format",
        "description.length" => "PR description length",
        "payload.required_fields" => "Payload fields",
        null or "" => "Check failed",
        _ => code
    };

    private async Task<bool> PostPrCommentAsync(
        string owner,
        string repo,
        int issueNumber,
        string commentBody,
        string patToken,
        string? deliveryId,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{issueNumber}/comments");

        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", patToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var payload = JsonSerializer.Serialize(new { body = commentBody }, JsonOptions);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage resp;
        try
        {
            resp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to call GitHub API. delivery={DeliveryId} repo={Owner}/{Repo} pr={PrNumber}",
                deliveryId,
                owner,
                repo,
                issueNumber);
            return false;
        }

        if (resp.IsSuccessStatusCode)
            return true;

        var content = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning(
            "GitHub API returned non-success. status={Status} delivery={DeliveryId} repo={Owner}/{Repo} pr={PrNumber} body={Body}",
            (int)resp.StatusCode,
            deliveryId,
            owner,
            repo,
            issueNumber,
            Truncate(content, 1500));

        return false;
    }

    private static string? GetHeader(HttpRequestData req, string name)
    {
        if (req.Headers.TryGetValues(name, out var values))
        {
            foreach (var v in values)
                return v;
        }
        return null;
    }

    private static async Task<byte[]> ReadBodyBytesAsync(HttpRequestData req, CancellationToken cancellationToken)
    {
        using var ms = new System.IO.MemoryStream();
        await req.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    private static bool IsSignatureValid(string? signatureHeader, byte[] bodyBytes, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        if (!signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var hex = signatureHeader["sha256=".Length..].Trim();
        if (hex.Length == 0)
            return false;

        Span<byte> provided = stackalloc byte[32];
        if (!TryDecodeHex32(hex, provided))
            return false;

        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var computed = hmac.ComputeHash(bodyBytes);

        return CryptographicOperations.FixedTimeEquals(provided, computed);
    }

    private static bool TryDecodeHex32(string hex, Span<byte> destination32)
    {
        if (hex.Length != 64)
            return false;

        for (var i = 0; i < 32; i++)
        {
            var hi = ParseHexNibble(hex[i * 2]);
            var lo = ParseHexNibble(hex[i * 2 + 1]);
            if (hi < 0 || lo < 0)
                return false;
            destination32[i] = (byte)((hi << 4) | lo);
        }
        return true;
    }

    private static int ParseHexNibble(char c)
    {
        if (c is >= '0' and <= '9') return c - '0';
        if (c is >= 'a' and <= 'f') return c - 'a' + 10;
        if (c is >= 'A' and <= 'F') return c - 'A' + 10;
        return -1;
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max];
    }

    private static async Task<HttpResponseData> CreateJsonResponseAsync(
        HttpRequestData req,
        HttpStatusCode status,
        object payload,
        CancellationToken cancellationToken)
    {
        var resp = req.CreateResponse(status);
        resp.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await resp.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions), cancellationToken).ConfigureAwait(false);
        return resp;
    }

    public sealed record Failure(string Code, string Message, string HowToFix);

    public sealed class GitHubPullRequestEvent
    {
        public string? Action { get; init; }
        [JsonPropertyName("pull_request")]
        public GitHubPullRequest? PullRequest { get; init; }
    }

    public sealed class GitHubPullRequest
    {
        public int? Number { get; init; }
        public string? Title { get; init; }
        public string? Body { get; init; }
        public GitHubRef? Head { get; init; }
        public GitHubPullRequestBase? Base { get; init; }
    }

    public sealed class GitHubRef
    {
        [JsonPropertyName("ref")]
        public string? Ref { get; init; }
    }

    public sealed class GitHubPullRequestBase
    {
        public GitHubRepo? Repo { get; init; }
    }

    public sealed class GitHubRepo
    {
        public string? Name { get; init; }
        public GitHubOwner? Owner { get; init; }
    }

    public sealed class GitHubOwner
    {
        public string? Login { get; init; }
    }
}
