using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace VoiceDispatcherMod {
    [Serializable]
    public class DialogueData {
        public Dictionary<string, LineGroup> line_groups;
        public LanguageSettings language_settings;
    }

    [Serializable]
    public class LineGroup {
        public string description;
        public List<string> placeholders;
        
        public string line;
        public List<string> lines;
        
        public string match_string;
        public Dictionary<string, LineGroup> match_map;
        public Dictionary<string, string> simple_match_map;
    }

    [Serializable]
    public class TypeMap {
        public string description;
        public Dictionary<string, string> map;
    }

    [Serializable]
    public class LanguageSettings {
        public string sentence_delimiter;
        public string clause_delimiter;
        public string sentence_split_regex;
    }

    public static class JsonLinesLoader {
        private static string Path;

        public static DialogueData DialogueData;

        public static Action<string> LogError = Console.WriteLine;

        public static void Init(string path) {
            Path = path;
            DialogueData = LoadDialogueData();
            if (DialogueData == null) {
                LogError("Failed to load dialogue data.");
            } else {
                LogError("Dialogue data loaded successfully.");
            }
        }

        public static DialogueData LoadDialogueData() {
            try {
                string json = System.IO.File.ReadAllText(Path);
                return JsonConvert.DeserializeObject<DialogueData>(json);
            } catch (Exception e) {
                LogError($"Error loading dialogue data: {e.Message}");
                return null;
            }
        }

        public static LineGroup GetBaseLineGroup(string groupName) {
            if (DialogueData.line_groups.TryGetValue(groupName, out var group)) {
                return group;
            }

            LogError($"Line group '{groupName}' not found.");
            return null;
        }

        public static LineGroup GetMatchedLineGroup(LineGroup baseLineGroup, Dictionary<string, string> replacements) {
            if (string.IsNullOrEmpty(baseLineGroup.match_string)) {
                return baseLineGroup;
            }

            var replacedMatchString = ReplacePlaceholders(baseLineGroup.match_string, replacements);
            if (TryFindMatchingSimpleGroup(baseLineGroup, replacedMatchString, out var simpleGroup)) {
                return simpleGroup;
            }
            
            if (TryFindMatchingSimpleGroup(baseLineGroup, "default", out var matchedGroup)) {
                return GetMatchedLineGroup(matchedGroup, replacements);
            }

            if (TryFindMatchingInnerGroup(baseLineGroup, replacedMatchString, out var innerGroup)) {
                return GetMatchedLineGroup(innerGroup, replacements);
            }

            // Try match string 'default' if available
            if (TryFindMatchingInnerGroup(baseLineGroup, "default", out innerGroup)) {
                return GetMatchedLineGroup(innerGroup, replacements);
            }

            LogError(
                $"Could not find matched line group for '{baseLineGroup.description}' with match string '{replacedMatchString}' or default.");
            // Try any element in the match map
            if (baseLineGroup.match_map.Count > 0) {
                return baseLineGroup.match_map.Values.First();
            }

            LogError(
                $"Could not find any match for '{baseLineGroup.description}' with match string '{baseLineGroup.match_string}'");
            return baseLineGroup;
        }

        public static bool TryFindMatchingInnerGroup(LineGroup group, string matchString, out LineGroup matchedGroup) {
            if (group.match_map == null || group.match_map.Count == 0) {
                matchedGroup = null;
                return false;
            }
            
            foreach (var groupKey in group.match_map.Keys) {
                if (IsMatchingRegex(matchString, groupKey)) {
                    matchedGroup = group.match_map[groupKey];
                    return true;
                }
            }

            matchedGroup = null;
            return false;
        }
        
        public static bool TryFindMatchingSimpleGroup(LineGroup group, string matchString, out LineGroup matchedGroup) {
            if (group.simple_match_map == null || group.simple_match_map.Count == 0) {
                matchedGroup = null;
                return false;
            }
            
            foreach (var groupKey in group.simple_match_map.Keys) {
                if (IsMatchingRegex(matchString, groupKey)) {
                    matchedGroup = new LineGroup();
                    matchedGroup.line = group.simple_match_map[groupKey];
                    return true;
                }
            }

            matchedGroup = null;
            return false;
        }

        private static bool IsMatchingRegex(string matchString, string caseString) {
            if (matchString == null || caseString == null) {
                return false;
            }

            try {
                // Apply caseString as regex to matchString
                var pattern = "^" + caseString + "$"; // Ensure full match
                var regex = new System.Text.RegularExpressions.Regex(pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return regex.IsMatch(matchString);
            } catch (Exception e) {
                LogError($"Error matching '{matchString}' with case string '{caseString}': {e.Message}");
                return false;
            }
        }

        public static string ReplacePlaceholders(string text, Dictionary<string, string> replacements) {
            if (replacements == null || replacements.Count == 0) {
                return text;
            }

            foreach (var kvp in replacements) {
                var key = kvp.Key.StartsWith("{") ? kvp.Key : "{" + kvp.Key + "}";
                text = text.Replace(key, kvp.Value);
            }

            return text;
        }
        
        /* Must be called after ReplacePlaceholders to ensure all placeholders are replaced before processing calls */
        public static string ReplaceCalls(string text, Dictionary<string, string> replacements) {
            var callPattern = @"\{@(\w+)\(([^)]*)\)\}";
            var matches = System.Text.RegularExpressions.Regex.Matches(text, callPattern);
            foreach (System.Text.RegularExpressions.Match match in matches) {
                var key = match.Groups[1].Value;
                var parameters = match.Groups[2].Value.Split(',');
                if (GetBaseLineGroup(key) == null) {
                    LogError($"Line group '{key}' not found in text: {text}");
                    continue;
                }
                var innerReplacements = new Dictionary<string, string>(replacements);
                foreach (var param in parameters) {
                    var paramParts = param.Split('=');
                    try {
                        if (paramParts.Length == 2) {
                            innerReplacements[paramParts[0].Trim()] = paramParts[1].Trim();
                        } else {
                            LogError($"Invalid parameter format '{param}' in call '{match.Value}' in text: {text}");
                        }
                    } catch (KeyNotFoundException e) {
                        LogError($"Parameter '{paramParts[1].Trim()}' not found in replacements: {e.Message}");
                    }
                }
                
                var value = GetRandomAndReplace(key, innerReplacements);
                if (value != null) {
                    text = text.Replace(match.Value, value);
                } else {
                    LogError($"No replacement found for key '{key}' in text: {text}");
                }
            }

            return text;
        }
        
        public static string ReplaceAll(string text, Dictionary<string, string> replacements) {
            text = ReplacePlaceholders(text, replacements);
            text = ReplaceCalls(text, replacements);
            return text;
        }

        public static string GetRandomAndReplace(string groupName, Dictionary<string, string> replacements = null) {
            var group = GetMatchedLineGroup(GetBaseLineGroup(groupName), replacements);
            var line = Randomizer.GetRandomLine(group);
            return ReplaceAll(line, replacements);
        }

        public static string MapType(string type, string value) {
            var group = GetBaseLineGroup(type);
            if (group?.placeholders == null) {
                LogError($"Proper type mapping for '{type}' not found.");
                return value; // Return the original key if no mapping is found
            }
            var replacements = new Dictionary<string, string> { { group.placeholders[0], value } };
            return GetRandomAndReplace(type, replacements);
        }
        
        public static string SentenceDelimiter() {
            return DialogueData?.language_settings?.sentence_delimiter ?? ".";
        }
        
        public static string ClauseDelimiter() {
            return DialogueData?.language_settings?.clause_delimiter ?? ",";
        }
        
        public static string SentenceSplitRegex() {
            return DialogueData?.language_settings?.sentence_split_regex ?? "[.!?]\\s+";
        }
    }
}