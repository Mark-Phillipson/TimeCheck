using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace TimeCheck.Services
{
    public record LaunchRecord(string Id, string Name, string Url, string VoiceKey, bool IsLocal = true, string[]? Aliases = null);

    public interface ILaunchService
    {
        Task<IEnumerable<LaunchRecord>> GetLocalLaunchesAsync();
        Task<LaunchRecord?> FindBestMatchAsync(string recognisedText);
        Task<bool> SyncFromServerAsync(string apiUrl, string? bearerToken = null);
    }

    public class LaunchService : ILaunchService
    {
        private const string FileName = "local_launches.json";
        private const string ExternalJsonPath = @"C:\Users\MPhil\source\repos\VoiceLauncherBlazor\local_launches.json";
        private FileSystemWatcher? _watcherLocal;
        private FileSystemWatcher? _watcherExternal;
        private System.Timers.Timer? _reloadTimer;
        private readonly string _filePath;
        private List<LaunchRecord> _cache = new();

        public LaunchService()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(folder, FileName);
            Load();
            StartWatchers();
        }

        public async Task<bool> SyncFromServerAsync(string apiUrl, string? bearerToken = null)
        {
            if (string.IsNullOrWhiteSpace(apiUrl)) return false;

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                if (!string.IsNullOrEmpty(bearerToken))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var json = await client.GetStringAsync(apiUrl);
                if (string.IsNullOrWhiteSpace(json)) return false;

                // save atomically
                var tmp = _filePath + ".tmp";
                File.WriteAllText(tmp, json);
                File.Copy(tmp, _filePath, true);
                File.Delete(tmp);

                // reload cache
                Load();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Load()
        {
            try
            {
                var packagedJson = TryReadPackagedJson();
                if (!string.IsNullOrWhiteSpace(packagedJson))
                {
                    try
                    {
                        _cache = JsonSerializer.Deserialize<List<LaunchRecord>>(packagedJson) ?? new List<LaunchRecord>();
                        EnsureAliases();
                        try { System.Diagnostics.Debug.WriteLine($"LaunchService: loaded packaged {_cache.Count} entries"); } catch { }
                        return;
                    }
                    catch { /* fall through to other locations */ }
                }

                // If a development JSON export exists, prefer that (convenient for local testing)
                var externalJson = ExternalJsonPath;
                if (File.Exists(externalJson))
                {
                    try
                    {
                        var jsonExt = File.ReadAllText(externalJson);
                        _cache = JsonSerializer.Deserialize<List<LaunchRecord>>(jsonExt) ?? new List<LaunchRecord>();
                        EnsureAliases();
                        try { System.Diagnostics.Debug.WriteLine($"LaunchService: loaded { _cache.Count} entries (external)"); } catch { }
                        return;
                    }
                    catch { /* fall through to internal cache */ }
                }
                if (!File.Exists(_filePath))
                {
                    // create sample file to make it easy to add entries
                    var sample = new List<LaunchRecord>
                    {
                        new LaunchRecord("1", "Fairies Little Helper", "https://www.fairieslittlehelper.online", "fairies little helper", true),
                        new LaunchRecord("2", "Work Calendar", "https://calendar.google.com", "work calendar", true)
                    };
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? string.Empty);
                    File.WriteAllText(_filePath, JsonSerializer.Serialize(sample, new JsonSerializerOptions{WriteIndented = true}));
                    _cache = sample;
                    return;
                }

                var json = File.ReadAllText(_filePath);
                _cache = JsonSerializer.Deserialize<List<LaunchRecord>>(json) ?? new List<LaunchRecord>();
                EnsureAliases();
                try
                {
                    var names = string.Join(", ", _cache.Take(5).Select(x => x.Name));
                    System.Diagnostics.Debug.WriteLine($"LaunchService: loaded {_cache.Count} entries; samples: {names}");
                    var hasSql = _cache.Any(x => (x.Name ?? string.Empty).IndexOf("sql server cheat", StringComparison.OrdinalIgnoreCase) >= 0 || (x.VoiceKey ?? string.Empty).IndexOf("sql server cheat", StringComparison.OrdinalIgnoreCase) >= 0);
                    System.Diagnostics.Debug.WriteLine($"LaunchService: contains SQL Server cheat entry: {hasSql}");
                }
                catch { }
            }
            catch
            {
                _cache = new List<LaunchRecord>();
            }
        }

        private void EnsureAliases()
        {
            try
            {
                for (int i = 0; i < _cache.Count; i++)
                {
                    var rec = _cache[i];
                    var existing = rec.Aliases ?? Array.Empty<string>();
                    var generated = GenerateAliases(rec.VoiceKey ?? rec.Name ?? string.Empty);
                    var merged = existing.Concat(generated).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    _cache[i] = rec with { Aliases = merged };
                }
            }
            catch { /* non-fatal */ }
        }

        private static IEnumerable<string> GenerateAliases(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) yield break;
            key = key.Trim();
            // basic variants: singular/plural for trailing -ies / -y / -s
            var norm = key.ToLowerInvariant();
            yield return norm;

            // if ends with 'ies' -> singular by replacing with 'y'
            if (norm.EndsWith("ies"))
            {
                yield return norm.Substring(0, norm.Length - 3) + "y";
            }
            // if ends with 'y' -> plural 'ies'
            if (norm.EndsWith("y") && norm.Length > 1)
            {
                yield return norm.Substring(0, norm.Length - 1) + "ies";
            }
            // if ends with simple 's' -> singular
            if (norm.EndsWith("s") && !norm.EndsWith("ss"))
            {
                yield return norm.Substring(0, norm.Length - 1);
            }

            // common confusions for 'fairies' token specifically
            if (norm.Contains("fairies"))
            {
                yield return norm.Replace("fairies", "fairy");
                yield return norm.Replace("fairies", "ferries");
                yield return norm.Replace("fairies", "ferris");
            }
            if (norm.Contains("fairy"))
            {
                yield return norm.Replace("fairy", "fairies");
            }

            // also produce variants without punctuation and double-spaces collapsed
            yield return System.Text.RegularExpressions.Regex.Replace(norm, "[^a-z0-9 ]", " ").Replace("  ", " ").Trim();
        }

        private void StartWatchers()
        {
            try
            {
                var localDir = Path.GetDirectoryName(_filePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _watcherLocal = new FileSystemWatcher(localDir)
                {
                    Filter = Path.GetFileName(_filePath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };
                _watcherLocal.Changed += OnJsonChanged;
                _watcherLocal.Created += OnJsonChanged;
                _watcherLocal.Renamed += OnJsonChanged;

                var extDir = Path.GetDirectoryName(ExternalJsonPath);
                if (!string.IsNullOrWhiteSpace(extDir) && Directory.Exists(extDir))
                {
                    _watcherExternal = new FileSystemWatcher(extDir)
                    {
                        Filter = Path.GetFileName(ExternalJsonPath),
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false
                    };
                    _watcherExternal.Changed += OnJsonChanged;
                    _watcherExternal.Created += OnJsonChanged;
                    _watcherExternal.Renamed += OnJsonChanged;
                }

                _reloadTimer = new System.Timers.Timer(500) { AutoReset = false };
                _reloadTimer.Elapsed += (s, e) => {
                    try
                    {
                        Load();
                        System.Diagnostics.Debug.WriteLine("LaunchService: reloaded local launches.");
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LaunchService: StartWatchers failed: " + ex.Message);
            }
        }

        private void OnJsonChanged(object? sender, FileSystemEventArgs e)
        {
            try
            {
                _reloadTimer?.Stop();
                _reloadTimer?.Start();
            }
            catch { }
        }

        public Task<IEnumerable<LaunchRecord>> GetLocalLaunchesAsync()
        {
            return Task.FromResult<IEnumerable<LaunchRecord>>(_cache.Where(x => x.IsLocal));
        }

        public Task<LaunchRecord?> FindBestMatchAsync(string recognisedText)
        {
            if (string.IsNullOrWhiteSpace(recognisedText))
                return Task.FromResult<LaunchRecord?>(null);

            var lookupInputs = BuildLookupInputs(recognisedText)
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (lookupInputs.Count == 0)
                return Task.FromResult<LaunchRecord?>(null);

            LaunchRecord? best = null;
            double bestScore = 0;

            foreach (var candidate in _cache.Where(c => c.IsLocal))
            {
                var key = Normalize(candidate.VoiceKey ?? candidate.Name ?? string.Empty);
                var keyTokens = Tokenize(key);
                var keySoundex = keyTokens.Select(t => GetSoundex(t)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var aliases = candidate.Aliases ?? Array.Empty<string>();

                if (!keyTokens.Any())
                    continue;

                var score = lookupInputs.Max(input => ScoreNormalizedPhrase(input, key, keyTokens, keySoundex));

                // check aliases for verbatim containment or token overlap
                foreach (var alias in aliases)
                {
                    var aNorm = Normalize(alias);
                    var aTokens = Tokenize(aNorm);
                    var aSoundex = aTokens.Select(t => GetSoundex(t)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var aScore = lookupInputs.Max(input => ScoreNormalizedPhrase(input, aNorm, aTokens, aSoundex, 0.98));
                    if (aScore > score)
                        score = aScore;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            // threshold: require reasonably high overlap (60%)
            if (best != null && bestScore >= 0.6)
                return Task.FromResult<LaunchRecord?>(best);

            // Fallback: try Levenshtein similarity on full phrase and aliases
            LaunchRecord? fallback = null;
            double fallbackScore = 0;
            foreach (var candidate in _cache.Where(c => c.IsLocal))
            {
                var key = Normalize(candidate.VoiceKey ?? candidate.Name ?? string.Empty);
                var sim = lookupInputs.Max(input => Similarity(input, key));
                if (sim > fallbackScore)
                {
                    fallbackScore = sim;
                    fallback = candidate;
                }

                foreach (var alias in candidate.Aliases ?? Array.Empty<string>())
                {
                    var aNorm = Normalize(alias);
                    var aSound = Tokenize(aNorm).Select(t => GetSoundex(t)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var simA = lookupInputs.Max(input => BlendSimilarityWithPhonetics(input, aNorm, aSound));
                    if (simA > fallbackScore)
                    {
                        fallbackScore = simA;
                        fallback = candidate;
                    }
                }
            }

            if (fallback != null && fallbackScore >= 0.70)
                return Task.FromResult<LaunchRecord?>(fallback);

            return Task.FromResult<LaunchRecord?>(null);
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private static double Similarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
            var dist = LevenshteinDistance(a, b);
            var max = Math.Max(a.Length, b.Length);
            if (max == 0) return 1.0;
            return 1.0 - (double)dist / max;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.ToLowerInvariant();
            s = Regex.Replace(s, "[^a-z0-9 ]", " ");
            s = Regex.Replace(s, "\\s+", " ").Trim();
            return s;
        }

        private static List<string> Tokenize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static IEnumerable<string> BuildLookupInputs(string recognisedText)
        {
            var raw = CommandPhraseParser.NormalizeWhitespace(recognisedText);
            if (!string.IsNullOrWhiteSpace(raw))
                yield return raw;

            var extracted = CommandPhraseParser.ExtractLookupText(recognisedText);
            if (!string.IsNullOrWhiteSpace(extracted) && !string.Equals(raw, extracted, StringComparison.OrdinalIgnoreCase))
                yield return extracted;
        }

        private static double ScoreNormalizedPhrase(string input, string candidateText, List<string> candidateTokens, List<string> candidateSoundex, double containmentBoost = 0.95)
        {
            var tokens = Tokenize(input);
            var inputSoundex = tokens.Select(GetSoundex).Where(s => !string.IsNullOrEmpty(s)).ToList();

            var common = tokens.Intersect(candidateTokens).Count();
            var tokenScore = tokens.Count == 0 || candidateTokens.Count == 0 ? 0.0 : (double)common / Math.Max(tokens.Count, candidateTokens.Count);

            var phoneticScore = 0.0;
            if (inputSoundex.Count > 0 && candidateSoundex.Count > 0)
            {
                var commonPhonetic = inputSoundex.Intersect(candidateSoundex).Count();
                phoneticScore = (double)commonPhonetic / Math.Max(inputSoundex.Count, candidateSoundex.Count);
            }

            var score = 0.6 * tokenScore + 0.4 * phoneticScore;
            if (!string.IsNullOrEmpty(candidateText) && input.Contains(candidateText, StringComparison.OrdinalIgnoreCase))
                score = Math.Max(score, containmentBoost);

            return score;
        }

        private static double BlendSimilarityWithPhonetics(string input, string candidateText, List<string> candidateSoundex)
        {
            var similarity = Similarity(input, candidateText);
            var tokens = Tokenize(input);
            var inputSoundex = tokens.Select(GetSoundex).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (inputSoundex.Count == 0 || candidateSoundex.Count == 0)
                return similarity;

            var commonPh = inputSoundex.Intersect(candidateSoundex).Count();
            var phoneticSim = (double)commonPh / Math.Max(inputSoundex.Count, candidateSoundex.Count);
            return Math.Max(similarity, 0.5 * phoneticSim + 0.5 * similarity);
        }

        private static string? TryReadPackagedJson()
        {
            try
            {
                var packagedPath = Path.Combine(AppContext.BaseDirectory ?? string.Empty, FileName);
                if (File.Exists(packagedPath))
                    return File.ReadAllText(packagedPath);
            }
            catch { }

            try
            {
                using var stream = FileSystem.Current.OpenAppPackageFileAsync(FileName).GetAwaiter().GetResult();
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        // Basic Soundex implementation for English-like phonetic matching
        private static string GetSoundex(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return string.Empty;
            term = term.ToUpperInvariant();
            var first = term[0];
            var map = new Dictionary<char, char>
            {
                {'B','1'},{'F','1'},{'P','1'},{'V','1'},
                {'C','2'},{'G','2'},{'J','2'},{'K','2'},{'Q','2'},{'S','2'},{'X','2'},{'Z','2'},
                {'D','3'},{'T','3'},
                {'L','4'},
                {'M','5'},{'N','5'},
                {'R','6'}
            };

            var sb = new System.Text.StringBuilder();
            sb.Append(first);
            char? lastCode = null;
            for (int i = 1; i < term.Length && sb.Length < 4; i++)
            {
                var ch = term[i];
                if (!map.TryGetValue(ch, out var code))
                {
                    lastCode = null; // vowels and other chars reset
                    continue;
                }
                if (lastCode == code) continue;
                sb.Append(code);
                lastCode = code;
            }

            // pad with 0s
            while (sb.Length < 4) sb.Append('0');
            return sb.ToString();
        }
    }
}
