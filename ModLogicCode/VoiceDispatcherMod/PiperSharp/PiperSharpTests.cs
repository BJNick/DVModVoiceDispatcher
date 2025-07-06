using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PiperSharp.Models;
using VoiceDispatcherMod;

namespace PiperSharp.Tests.Tests
{
    public static class Assert
    {
        public static void That(bool condition, string message)
        {
            if (!condition)
            {
                Console.WriteLine(message);
                throw new Exception("Assertion failed: " + message);
            }
        }
    }

    public class PiperSharpTests
    {
        const string cwd = "D:\\SteamLibrary\\steamapps\\common\\Derail Valley\\Mods\\VoiceDispatcherMod\\Piper";
        
        public async Task RunAllTests()
        {
            try
            {
                //await TestDownloadPiper();
                //await TestDownloadModel();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await TestTTSInference();
                stopwatch.Stop();
                Main.Logger.Log($"All tests passed successfully. TTS inference duration: {stopwatch.Elapsed.TotalSeconds:0.00} seconds.");
            }
            catch (Exception ex)
            {
                Main.Logger.Error("Test run failed: " + ex.Message);
                Main.Logger.Error("Test run stack: " + ex.StackTrace);
                throw;
            }
        }

        public async Task TestDownloadPiper() {
            var piperPath = Path.Combine(cwd, "piper");
            if (Directory.Exists(piperPath)) Directory.Delete(piperPath, true);
            await PiperDownloader.DownloadPiper().ExtractPiper(cwd);
            Assert.That(Directory.Exists(piperPath), "Piper doesn't exist");

            // For linux we need to mark it as executable
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                var process = Process.Start(new ProcessStartInfo()
                {
                    FileName = "/bin/chmod",
                    Arguments = $"+x {Path.Combine(piperPath, "piper")}",
                    UseShellExecute = false
                });
                await process!.WaitForExitAsync();
            }
        }
        
        public async Task TestDownloadModel()
        {
            string[] modelNames =
            {
                "ru_RU-irina-medium",
                "en_US-ljspeech-high",
            };

            foreach (var modelName in modelNames)
            {
                Console.WriteLine("Downloading model: " + modelName);
                //var cwd = Directory.GetCurrentDirectory();
                var modelPath = Path.Combine(cwd, modelName);
                if (Directory.Exists(modelPath)) Directory.Delete(modelPath, true);
                var models = await PiperDownloader.GetHuggingFaceModelList();
                Assert.That(models is { Count: > 0 }, "Failed to get models from hugging face");
                var model = models![modelName];
                Assert.That(model.Key == modelName, "Expected model doesn't exist!");
                model = await model.DownloadModel(cwd);
                Assert.That(model.ModelLocation == modelPath && Directory.Exists(modelPath), "Model not downloaded!");
                model = await VoiceModel.LoadModel(modelPath);
                Assert.That(model.Key == modelName, "Failed to load model expected model!");
            }
        }

        
        public async Task TestTTSInference()
        {
            //var cwd = Directory.GetCurrentDirectory();
            Main.Logger.Log("Starting TTS inference test...");
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
                UseCuda = true,
            };
            var result = await PiperProvider.InferAsync("Hello there! I am alive! I can talk! and! you have a shunting order!", piperModelConfig);
            var clip = await VoiceGenerator.LoadAudioClipFromFileAsync(result);
            clip.Play2D();
            Main.Logger.Log("Playing TTS inference test...");
        }
    }
}