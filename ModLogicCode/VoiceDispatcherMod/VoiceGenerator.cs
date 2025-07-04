using System;
using System.IO;
using System.Threading.Tasks;
using PiperSharp;
using PiperSharp.Models;
using UnityEngine;
using VoiceDispatcherMod.PiperSharp;

namespace VoiceDispatcherMod {
    public static class VoiceGenerator {
        
        private static string _workingDirectory;
        private static string _outputDirectory;
        
        public static void Init() {
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
            var onlyAlphanumeric = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-zA-Z0-9\s]", "");
            var trimmedToMaxLength = onlyAlphanumeric.Length > 100 ? onlyAlphanumeric.Substring(0, 100) : onlyAlphanumeric;
            return trimmedToMaxLength + "_" + text.GetHashCode() + ".wav";
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
            
            const string modelName = "en_US-ljspeech-high";
            var modelPath = Path.Combine(_workingDirectory, modelName);
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
            
            const string modelName = "en_US-ljspeech-high";
            var modelPath = Path.Combine(_workingDirectory, modelName);
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

        public static async Task<AudioClip> CreateClip(string text, bool sox = true) {
            var filePath = sox ? await GenerateWithSox(text) : await Generate(text);
            if (string.IsNullOrEmpty(filePath)) {
                Main.Logger.Error($"Failed to generate clip: {text}");
                throw new InvalidOperationException("Failed to generate audio clip.");
            }
            return await PiperProvider.LoadAudioClipFromFileAsync(filePath);
        }

    }
}