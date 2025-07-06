using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using PiperSharp;
using PiperSharp.Models;
using UnityEngine;
using UnityEngine.Networking;
using VoiceDispatcherMod.PiperSharp;

namespace VoiceDispatcherMod {
    public static class VoiceGenerator {
        private static string _modelName;
        
        private static string _workingDirectory;
        private static string _outputDirectory;
        
        public static void Init() {
            _modelName = Main.settings.Model;
            _workingDirectory = Path.Combine(Main.mod.Path, "Piper");
            _outputDirectory = Path.Combine(_workingDirectory, "output");
            if (!Directory.Exists(_workingDirectory)) {
                Directory.CreateDirectory(_workingDirectory);
            }
            if (!Directory.Exists(_outputDirectory)) {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        public static string FilenameOf(string text) {
            var onlyAlphanumeric = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-zA-Z0-9\s]", "").Trim();
            var trimmedToMaxLength = onlyAlphanumeric.Length > 50 ? onlyAlphanumeric.Substring(0, 50) : onlyAlphanumeric;
            if (string.IsNullOrWhiteSpace(trimmedToMaxLength)) {
                return (uint)text.GetHashCode() + ".wav";;
            }
            return trimmedToMaxLength + "_" + (uint)text.GetHashCode() + ".wav";
        }
        
        public static string CheckIfExists(string text) {
            var filename = FilenameOf(text);
            var filePath = Path.Combine(_outputDirectory, filename);
            if (File.Exists(filePath)) {
                return filePath;
            }
            return string.Empty;
        }
        
        public static async Task<string> Generate(string text) {
            var existingFile = CheckIfExists(text);
            if (!string.IsNullOrEmpty(existingFile)) {
                return existingFile;
            }
            
            var modelPath = Path.Combine(_workingDirectory, _modelName);
            var piperPath = Path.Combine(_workingDirectory, "piper",
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");
            var model = await VoiceModel.LoadModel(modelPath);
            var piperModelConfig = new PiperConfiguration()
            {
                ExecutableLocation = piperPath,
                Model = model,
                WorkingDirectory = _workingDirectory,
                OutputFilePath = Path.Combine(_outputDirectory, FilenameOf(text)),
            };
            return await PiperProvider.InferAsync(text, piperModelConfig);
        }

        public static async Task<string> GenerateWithSox(string text) {
            var existingFile = CheckIfExists(text);
            if (!string.IsNullOrEmpty(existingFile)) {
                return existingFile;
            }
            
            var modelPath = Path.Combine(_workingDirectory, _modelName);
            var piperPath = Path.Combine(_workingDirectory, "piper",
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");
            var model = await VoiceModel.LoadModel(modelPath);
            var piperModelConfig = new PiperConfiguration()
            {
                ExecutableLocation = piperPath,
                Model = model,
                WorkingDirectory = _workingDirectory,
                OutputRaw = true,
            };
            int sampleRate = (int)(model.Audio?.SampleRate ?? 16000);
            var soxConfiguration = new SoxConfiguration {
                ExecutablePath = Path.Combine(_workingDirectory, "sox", "sox.exe").AddPathQuotesIfRequired(),
                InputSampleRate = sampleRate,
                OutputSampleRate = 16000,
                OutputRaw = false,
                OutputFilePath = Path.Combine(_outputDirectory, FilenameOf(text)),
            };
            return await PiperProvider.InferAsyncWithSox(text, piperModelConfig, soxConfiguration);
        }
        
        public static async Task<AudioClip> LoadAudioClipFromFileAsync(string filePath, AudioType audioType = AudioType.WAV) {
            string uri = "file://" + filePath;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType)) {
                var request = www.SendWebRequest();
                while (!request.isDone)
                    await Task.Yield();

                if (www.responseCode != 200)
                    throw new IOException($"Failed to load audio: {www.error}");

                return DownloadHandlerAudioClip.GetContent(www);
            }
        }
        
        public static IEnumerator LoadAudioClipFromFileCoroutine(string filePath, Action<AudioClip> onLoaded, AudioType audioType = AudioType.WAV) {
            string uri = "file://" + filePath;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType)) {
                yield return www.SendWebRequest();

                if (www.responseCode != 200) {
                    Main.Logger.Error($"Failed to load audio: {www.error}");
                    onLoaded?.Invoke(null);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);
                onLoaded?.Invoke(clip);
            }
        }

        public static async Task<AudioClip> CreateClip(string text, bool sox = true) {
            var filePath = sox ? await GenerateWithSox(text) : await Generate(text);
            if (string.IsNullOrEmpty(filePath)) {
                Main.Logger.Error($"Failed to generate clip: {text}");
                throw new InvalidOperationException("Failed to generate audio clip.");
            }
            return await LoadAudioClipFromFileAsync(filePath);
        }

    }
}