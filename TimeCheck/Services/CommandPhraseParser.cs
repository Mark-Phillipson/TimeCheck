using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimeCheck.Services
{
    public static class CommandPhraseParser
    {
        private static readonly string[] LookupPrefixes =
        {
            "take me to ",
            "show me ",
            "open up ",
            "open ",
            "launch ",
            "start ",
            "browse ",
            "search ",
            "find ",
            "go to "
        };

        private static readonly string[] SearchPrefixes =
        {
            "launch ",
            "start ",
            "browse ",
            "search ",
            "find "
        };

        public static bool IsOpenVerb(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return NormalizeWhitespace(text).StartsWith("open ", StringComparison.OrdinalIgnoreCase)
                || NormalizeWhitespace(text).StartsWith("open up ", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsExplicitUrlOpen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var normalized = NormalizeWhitespace(text);
            return normalized.StartsWith("open http", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("open https", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("open www.", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("go to ", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSearchIntentVerb(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var normalized = NormalizeWhitespace(text);
            return SearchPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        public static string ExtractLookupText(string text)
        {
            var value = NormalizeWhitespace(text);
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            foreach (var prefix in LookupPrefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(prefix.Length).Trim();
                    break;
                }
            }

            value = Regex.Replace(value, "^((for|the|a|an|my)\\s+)+", string.Empty, RegexOptions.IgnoreCase);
            value = Regex.Replace(value, "\\s+(please|now)$", string.Empty, RegexOptions.IgnoreCase);
            return NormalizeWhitespace(value);
        }

        public static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Regex.Replace(text.Trim(), "\\s+", " ");
        }
    }
}