using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

/// <summary>Local rule-based parser. Works with NO API key, so the AI search demo always works.</summary>
public class RuleBasedQueryParser : IQueryParser
{
    public string Name => "Built-in rule-based parser";

    private static readonly Dictionary<string, string[]> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Electrician"] = new[] { "electrician", "electric", "electrical", "wiring", "wire", "fan", "switch", "socket", "ups", "short circuit", "breaker", "light" },
        ["Plumber"] = new[] { "plumber", "plumbing", "pipe", "leak", "leakage", "tap", "drain", "geyser", "sink", "toilet", "commode", "tank" },
        ["Painter"] = new[] { "painter", "paint", "painting", "whitewash", "distemper", "polish" },
        ["Carpenter"] = new[] { "carpenter", "carpentry", "wood", "furniture", "door", "cabinet", "wardrobe", "table", "cupboard" },
        ["Cleaner"] = new[] { "cleaner", "cleaning", "clean", "maid", "sofa", "carpet", "deep clean", "housekeeping" },
        ["Mechanic"] = new[] { "mechanic", "car", "bike", "motorcycle", "engine", "vehicle", "tyre", "tire", "oil change" }
    };

    public Task<ParsedQuery> ParseAsync(string text, IReadOnlyList<string> categories, IReadOnlyList<string> locations, DateOnly today)
    {
        var lower = text.ToLowerInvariant();
        var parsed = new ParsedQuery
        {
            ServiceType = FindCategory(lower, categories),
            Location = FindLocation(text, lower, locations),
            MaxBudget = FindBudget(lower),
            Date = FindDate(lower, today)
        };
        return Task.FromResult(parsed);
    }

    private static string? FindCategory(string lower, IReadOnlyList<string> categories)
    {
        string? best = null; var bestScore = 0;
        foreach (var cat in categories)
        {
            var score = 0;
            if (Regex.IsMatch(lower, $@"\b{Regex.Escape(cat.ToLowerInvariant())}")) score += 3;
            if (Keywords.TryGetValue(cat, out var words))
                score += words.Count(w => Regex.IsMatch(lower, $@"\b{Regex.Escape(w)}\b"));
            if (score > bestScore) { best = cat; bestScore = score; }
        }
        return best;
    }

    private static string? FindLocation(string original, string lower, IReadOnlyList<string> known)
    {
        foreach (var loc in known.OrderByDescending(l => l.Length))
            if (Regex.IsMatch(lower, $@"\b{Regex.Escape(loc.ToLowerInvariant())}\b")) return loc;

        var m = Regex.Match(original, @"\b(?:near|in|around|at)\s+([A-Za-z][A-Za-z ]{2,24}?)(?=\s+(?:under|below|within|for|tomorrow|today|on|with|budget|by|next|upto|up)\b|[,.?!]|$)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var candidate = m.Groups[1].Value.Trim();
            var first = candidate.Split(' ')[0].ToLowerInvariant();
            if (!new[] { "my", "the", "a", "an", "budget", "me", "our", "your", "need", "this", "that" }.Contains(first))
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(candidate.ToLowerInvariant());
        }
        return null;
    }

    private static decimal? FindBudget(string lower)
    {
        var patterns = new[]
        {
            @"(?:under|below|within|less than|upto|up to|max(?:imum)?|budget(?: of| is)?|around|not more than|at most)\s*(?:rs\.?|pkr|rupees)?\s*(\d[\d,]*(?:\.\d+)?)\s*(k)?",
            @"(?:rs\.?|pkr)\s*(\d[\d,]*(?:\.\d+)?)\s*(k)?",
            @"(\d[\d,]*(?:\.\d+)?)\s*(k)?\s*(?:rs\b|pkr|rupees)"
        };
        foreach (var p in patterns)
        {
            var m = Regex.Match(lower, p);
            if (!m.Success) continue;
            if (decimal.TryParse(m.Groups[1].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                return m.Groups[2].Success ? n * 1000 : n;
        }
        return null;
    }

    private static DateOnly? FindDate(string lower, DateOnly today)
    {
        var iso = Regex.Match(lower, @"\b(\d{4}-\d{2}-\d{2})\b");
        if (iso.Success && DateOnly.TryParse(iso.Value, out var d)) return d;
        if (lower.Contains("day after tomorrow")) return today.AddDays(2);
        if (lower.Contains("tomorrow")) return today.AddDays(1);
        if (lower.Contains("today") || lower.Contains("tonight")) return today;
        foreach (var dow in Enum.GetValues<DayOfWeek>())
        {
            if (!Regex.IsMatch(lower, $@"\b{dow.ToString().ToLowerInvariant()}\b")) continue;
            var diff = ((int)dow - (int)today.DayOfWeek + 7) % 7;
            if (diff == 0 && lower.Contains("next")) diff = 7;
            return today.AddDays(diff);
        }
        return null;
    }
}

/// <summary>Real AI parser (Anthropic Messages API). Used only when AI:ApiKey is configured.</summary>
public class AnthropicQueryParser(HttpClient http, IConfiguration config) : IQueryParser
{
    public string Name => "Claude AI";

    public async Task<ParsedQuery> ParseAsync(string text, IReadOnlyList<string> categories, IReadOnlyList<string> locations, DateOnly today)
    {
        var system = new StringBuilder()
            .AppendLine("You extract search filters from a customer's request for a local home-services marketplace in Pakistan.")
            .AppendLine($"Today is {today:yyyy-MM-dd} ({today.DayOfWeek}).")
            .AppendLine($"Valid service types: {string.Join(", ", categories)}.")
            .AppendLine($"Known locations: {string.Join(", ", locations)}.")
            .AppendLine("Reply with ONLY a JSON object, no other text: {\"serviceType\": string|null, \"location\": string|null, \"maxBudget\": number|null, \"date\": \"yyyy-MM-dd\"|null}.")
            .AppendLine("serviceType must be one of the valid service types or null. maxBudget is in Pakistani rupees. Resolve relative dates like 'tomorrow' using today's date.")
            .ToString();

        var body = JsonSerializer.Serialize(new
        {
            model = config["AI:Model"] ?? "claude-haiku-4-5-20251001",
            max_tokens = 300,
            system,
            messages = new[] { new { role = "user", content = text } }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", config["AI:ApiKey"]);
        req.Headers.Add("anthropic-version", "2023-06-01");

        using var res = await http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var reply = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        var start = reply.IndexOf('{'); var end = reply.LastIndexOf('}');
        if (start < 0 || end < start) throw new InvalidOperationException("AI returned no JSON.");
        using var json = JsonDocument.Parse(reply[start..(end + 1)]);
        var root = json.RootElement;

        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var parsed = new ParsedQuery { ServiceType = Str("serviceType"), Location = Str("location") };
        if (root.TryGetProperty("maxBudget", out var b) && b.ValueKind == JsonValueKind.Number) parsed.MaxBudget = b.GetDecimal();
        if (DateOnly.TryParse(Str("date"), out var d)) parsed.Date = d;
        return parsed;
    }
}

public class AiSearchService(HomeFixDbContext db, ISearchService search, RuleBasedQueryParser rules,
    AnthropicQueryParser ai, IConfiguration config, ILogger<AiSearchService> logger) : IAiSearchService
{
    public async Task<AiSearchResult> SearchAsync(string text)
    {
        var cats = await db.ServiceCategories.AsNoTracking().Where(c => c.IsActive).Select(c => new { c.Id, c.Name }).ToListAsync();
        var areas = await db.ProviderProfiles.AsNoTracking().Where(p => p.ApprovalStatus == Models.ApprovalStatus.Approved)
            .Select(p => p.ServiceArea).ToListAsync();
        var locations = areas.SelectMany(a => a.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var catNames = cats.Select(c => c.Name).ToList();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = new AiSearchResult { Query = text };
        ParsedQuery parsed;
        if (!string.IsNullOrWhiteSpace(config["AI:ApiKey"]))
        {
            try
            {
                parsed = await ai.ParseAsync(text, catNames, locations, today);
                result.UsedAi = true; result.Parser = ai.Name;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI parsing failed, using the built-in parser instead.");
                parsed = await rules.ParseAsync(text, catNames, locations, today);
                result.Parser = rules.Name; result.Note = "The AI service was unavailable, so the built-in parser was used.";
            }
        }
        else
        {
            parsed = await rules.ParseAsync(text, catNames, locations, today);
            result.Parser = rules.Name;
        }
        result.Parsed = parsed;

        var categoryId = cats.FirstOrDefault(c => c.Name.Equals(parsed.ServiceType, StringComparison.OrdinalIgnoreCase))?.Id;
        var q = new SearchQuery { CategoryId = categoryId, Location = parsed.Location, MaxRate = parsed.MaxBudget, Date = parsed.Date };

        var found = await search.SearchProvidersAsync(q);
        var notes = new List<string>();
        if (found.Count == 0 && q.Date.HasValue)
        {
            q.Date = null; q.AvailableOnly = true;
            found = await search.SearchProvidersAsync(q);
            if (found.Count > 0) notes.Add("No provider is free on that exact date, so these are providers who are currently available.");
        }
        if (found.Count == 0 && !string.IsNullOrWhiteSpace(q.Location))
        {
            var where = q.Location; q.Location = null;
            found = await search.SearchProvidersAsync(q);
            if (found.Count > 0) notes.Add($"No exact match in {where}, so these are the closest alternatives.");
        }
        if (found.Count == 0 && q.MaxRate.HasValue)
        {
            q.MaxRate = null;
            found = await search.SearchProvidersAsync(q);
            if (found.Count > 0) notes.Add("Nothing matched your budget, so these are shown without the budget filter.");
        }
        if (notes.Count > 0) result.Note = string.Join(" ", new[] { result.Note }.Concat(notes).Where(n => !string.IsNullOrEmpty(n)));
        result.Results = found;
        return result;
    }
}
