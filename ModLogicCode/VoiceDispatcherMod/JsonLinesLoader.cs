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
        private const string Path =
            "D:\\Projects\\Mods\\DVVoiceAssistant\\ModLogicCode\\VoiceDispatcherMod\\lines.json";

        public static DialogueData DialogueData;
        
        public static Action<string> LogError = Console.WriteLine;

        public static void Init() {
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
    }
}