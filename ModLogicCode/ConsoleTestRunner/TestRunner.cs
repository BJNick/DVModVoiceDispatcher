using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PiperSharp;
using PiperSharp.Models;
using PiperSharp.Tests.Tests;
using VoiceDispatcherMod.PiperSharp;

namespace VoiceDispatcherMod {
    public class TestRunner {
        
        const string cwd = "D:\\SteamLibrary\\steamapps\\common\\Derail Valley\\Mods\\VoiceDispatcherMod\\Piper";
        
        const string lines = "D:\\Projects\\Mods\\DVVoiceAssistant\\ModLogicCode\\VoiceDispatcherMod\\lines.json";
        
        public static void Main(string[] args) {
            JsonLinesLoader.Init(lines);
            string lastGroupName = "speed_report";
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            do {
                Console.WriteLine("Groups: " + string.Join(", ", JsonLinesLoader.DialogueData.line_groups.Keys));
                Console.WriteLine("Enter a line group name (enter to regenerate last, or 'q' to quit):");
                var lineGroupName = Console.ReadLine();
                if (lineGroupName?.ToLower() == "q") {
                    break;
                }
                
                JsonLinesLoader.Init(lines);
                
                var groupName = string.IsNullOrEmpty(lineGroupName) ? lastGroupName : lineGroupName;
                var baseGroup = JsonLinesLoader.GetBaseLineGroup(groupName);
                if (baseGroup == null) {
                    Console.WriteLine($"Line group '{groupName}' not found. Please try again.");
                    continue;
                }
                lastGroupName = groupName;
                var toReplace = baseGroup.placeholders;
                
                foreach (var key in toReplace) {
                    Console.WriteLine($"Enter replacement for {key} (or press Enter to skip):");
                    var replacement = Console.ReadLine();
                    if (!string.IsNullOrEmpty(replacement)) {
                        replacements[key] = replacement;
                    }
                }
                
                var matchedGroup = JsonLinesLoader.GetMatchedLineGroup(baseGroup, replacements);

                string selectedLine;
                if (matchedGroup.line != null) {
                    selectedLine = matchedGroup.line;
                } else {
                    Console.WriteLine($"Which line do you want to use? (or press Enter for random):");
                    for (var i = 0; i < matchedGroup.lines.Count; i++) {
                        var line = matchedGroup.lines[i];
                        Console.WriteLine($"{i + 1} {line}");
                    }

                    var lineIndexInput = Console.ReadLine();
                    int lineIndex = 0;
                    if (string.IsNullOrEmpty(lineIndexInput)) {
                        lineIndex = new Random().Next(0, matchedGroup.lines.Count);
                    } else {
                        int.TryParse(lineIndexInput, out var lineIndexInputInt);
                        lineIndex = lineIndexInputInt - 1;
                    }

                    selectedLine = matchedGroup.lines[lineIndex];
                }

                foreach (var placeholder in replacements) {
                    selectedLine = selectedLine.Replace(placeholder.Key, placeholder.Value);
                }
                Console.WriteLine($"Generating line: {selectedLine}");
                GenerateWithSox(selectedLine).GetAwaiter().GetResult();
                Play();
                Console.WriteLine();

            } while (true);
        }
        
        public static string GetRandomLine(List<string> lines) {
            if (lines.Count == 0) return string.Empty;
            var randomIndex = new Random().Next(lines.Count);
            return lines[randomIndex];
        }

        public static async Task Generate(string text) {
            const string modelName = "en_US-ljspeech-high";
            var modelPath = Path.Combine(cwd, modelName);
            var piperPath = Path.Combine(cwd, "piper",
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");
            var model = await VoiceModel.LoadModel(modelPath);
            var piperModelConfig = new PiperConfiguration()
            {
                ExecutableLocation = piperPath,
                Model = model,
                WorkingDirectory = cwd,
                OutputFilePath = "D:\\Projects\\Mods\\DVVoiceAssistant\\output.wav",
            };
            var result = await PiperProvider.InferAsync(text, piperModelConfig);
        }

        public static async Task GenerateWithSox(string text) {
            const string modelName = "glados";
            var modelPath = Path.Combine(cwd, modelName);
            var piperPath = Path.Combine(cwd, "piper",
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");
            var model = await VoiceModel.LoadModel(modelPath);
            var piperModelConfig = new PiperConfiguration()
            {
                ExecutableLocation = piperPath,
                Model = model,
                WorkingDirectory = cwd,
                OutputRaw = true,
            };
            int sampleRate = (int)(model.Audio?.SampleRate ?? 16000);
            var soxConfiguration = new SoxConfiguration {
                ExecutablePath = Path.Combine(cwd, "sox", "sox.exe").AddPathQuotesIfRequired(),
                InputSampleRate = sampleRate,
                OutputSampleRate = 16000,
                OutputRaw = false,
                OutputFilePath = "D:\\Projects\\Mods\\DVVoiceAssistant\\output.wav",
            };
            var result = await PiperProvider.InferAsyncWithSox(text, piperModelConfig, soxConfiguration);
        }

        public static void Play() {
            // Play using the default system audio player
            var outputFilePath = "D:\\Projects\\Mods\\DVVoiceAssistant\\output.wav";
            if (File.Exists(outputFilePath)) {
                var process = new System.Diagnostics.Process {
                    StartInfo = new System.Diagnostics.ProcessStartInfo {
                        FileName = outputFilePath,
                        UseShellExecute = true
                    }
                };
                process.Start();
            } else {
                Console.WriteLine("Output file does not exist: " + outputFilePath);
            }
        }

        public static void DownloadModels() {
            try {
                new PiperSharpTests().TestDownloadModel().Wait();
            } catch (Exception ex) {
                Console.WriteLine("Error downloading models: " + ex.Message);
            }
        }
        
        
    }
}