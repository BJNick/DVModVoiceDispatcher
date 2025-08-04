using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VoiceDispatcherMod {
    public class JsonVersionConverter {
        // Major: Removing lines, breaking old logic
        // Minor: Renaming lines, new logic
        // Patch: Adding lines, no logic changes
        private const string InitialVersion = "0.0.1";

        private const string VersionKey = "formatVersion";

        public static Action<string> LogError = Console.WriteLine;
        
        public static bool IsUpToDate(string path) {
            return GetVersionStr(path) == GetBuiltInVersionStr();
        }

        public static void CreateBackup(string path) {
            try {
                var version = GetVersionStr(path);
                var backupPath = path.Replace(".json", "") + "." + version + ".json";
                if (File.Exists(backupPath)) {
                    return;
                }
                File.Copy(path, backupPath);
            } catch (Exception ex) {
                LogError($"Error creating backup for {path}: {ex.Message}");
            }
        }

        public static void FixFile(string path) {
            string json;
            try {
                json = File.ReadAllText(path);
            } catch (FileNotFoundException ex) {
                LogError($"File not found: {path}. Exception: {ex.Message}");
                throw;
            } catch (IOException ex) {
                LogError($"IO error reading file {path}: {ex.Message}");
                throw;
            }

            JObject translated;
            try {
                translated = JObject.Parse(json);
            } catch (JsonReaderException ex) {
                LogError("Missing a comma, bracket or quote in the JSON file at line " + ex.LineNumber + ", position " + ex.LinePosition);
                LogError(ex.Message);
                throw;
            }

            var original = ReadBuiltIn();

            MergeJson(original, translated, "root");
            Transition(original, translated);

            // Write the merged JSON back to the file
            try {
                File.WriteAllText(path, translated.ToString(Formatting.Indented));
            } catch (Exception ex) {
                LogError($"Error writing to file {path}: {ex.Message}");
                throw;
            }
        }
        
        public static void CreateFileIfMissing(string path) {
            if (!File.Exists(path)) {
                try {
                    var builtInJson = ReadBuiltIn();
                    File.WriteAllText(path, builtInJson.ToString(Formatting.Indented));
                } catch (Exception ex) {
                    LogError($"Error creating file {path}: {ex.Message}");
                    return;
                }
            }
        }

        public static void Transition(JObject original, JObject translated) {
            var versionFrom = new Version(translated["metadata"]?[VersionKey]?.ToString() ?? InitialVersion);
            var finalVersion = GetBuiltInVersionStr();
            /*switch (versionFrom.ToString()) {
                case "0.0.1": {
                    
                    break;
                }
            }*/
            // No changes needed, just update version
            translated["metadata"][VersionKey] = finalVersion;
        }

        public static JObject ReadBuiltIn() {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("VoiceDispatcherMod.lines.json"))
            using (StreamReader reader = new StreamReader(stream)) {
                string json = reader.ReadToEnd();
                return JObject.Parse(json);
            }
        }

        public static void MergeJson(JObject original, JObject translated, string parentName) {
            foreach (var prop in original.Properties()) {
                if (translated[prop.Name] == null) {
                    if (prop.Name == "metadata") {
                        // Put metadata at the start
                        translated.AddFirst(prop.DeepClone());
                    } else {
                        translated.Add(prop.DeepClone());
                    }
                } else if (prop.Value.Type == JTokenType.Object && parentName != "line_groups") {
                    // Recurse for nested objects
                    MergeJson((JObject)prop.Value, (JObject)translated[prop.Name], prop.Name);
                }
            }
        }

        public static string GetVersionStr(string path) {
            try {
                var json = File.ReadAllText(path);
                var root = JObject.Parse(json);
                return root["metadata"]?[VersionKey]?.ToString() ?? InitialVersion;
            } catch (Exception) {
                return InitialVersion;
            }
        }
        
        public static string GetBuiltInVersionStr() {
            try {
                var builtInJson = ReadBuiltIn();
                return builtInJson["metadata"]?[VersionKey]?.ToString() ?? InitialVersion;
            } catch (Exception) {
                return InitialVersion;
            }
        }
    }
}