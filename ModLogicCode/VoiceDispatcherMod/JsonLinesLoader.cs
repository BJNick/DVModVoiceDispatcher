using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VoiceDispatcherMod {
    [Serializable]
    public class DialogueData {
        public Dictionary<string, LineGroup> status_lines;
    }

    [Serializable]
    public class LineGroup {
        public string description;
        public List<string> placeholders;
        public List<string> lines;
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
        
        public static LineGroup GetLineGroup(string groupName) {
            if (DialogueData.status_lines.TryGetValue(groupName, out var group)) {
                return group;
            }
            LogError($"Line group '{groupName}' not found.");
            return null;
        }
        
        public static string Replace(string text, Dictionary<string, string> replacements) {
            if (replacements == null || replacements.Count == 0) {
                return text;
            }
            foreach (var kvp in replacements) {
                text = text.Replace(kvp.Key, kvp.Value);
            }
            return text;
        }
        
        public static string GetRandomAndReplace(string groupName, Dictionary<string, string> replacements = null) {
            var group = GetLineGroup(groupName);
            var line = Randomizer.GetRandomLine(group);
            return Replace(line, replacements);
        }
    }
}