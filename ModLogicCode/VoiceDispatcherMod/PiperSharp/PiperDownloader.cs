using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Threading.Tasks;
using PiperSharp.Models;
using VoiceDispatcherMod;

namespace PiperSharp
{
    public static class PiperDownloader
    {
        private const string PIPER_REPO_URL = "https://github.com/rhasspy/piper";
        private const string MODEL_REPO_URL = "https://huggingface.co/rhasspy/piper-voices";
        private const string MODEL_LIST_URL = "https://huggingface.co/rhasspy/piper-voices/raw/main/voices.json";
        private const string MODEL_DOWNLOAD_URL =
            "https://huggingface.co/rhasspy/piper-voices/resolve/main/MODEL_FILE_URL?download=true";

        public static string DefaultLocation => Path.Combine(Main.mod.Path, "Piper");
        public static string DefaultModelLocation => Path.Combine(DefaultLocation, "models");
        public static string DefaultPiperLocation => Path.Combine(DefaultLocation, "piper");
        public static string DefaultPiperExecutableLocation => Path.Combine(DefaultPiperLocation, PiperExecutable);
        public static string PiperExecutable => Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper";
    
    
        private static Dictionary<string, VoiceModel>? _voiceModels;
        private static Regex RemoveLastSlash = new Regex(@"\/$", RegexOptions.Compiled);

        public static async Task<Stream> DownloadPiper(string version = "latest", string repo = PIPER_REPO_URL)
        {
            if (!Environment.Is64BitOperatingSystem)
                throw new NotSupportedException();
            var arch = typeof(object).Assembly.GetName().ProcessorArchitecture;
            var os = Environment.OSVersion.Platform;
            var fileName = os switch
            {
                PlatformID.Win32NT => "piper_windows_amd64.zip",
                PlatformID.Unix => arch == ProcessorArchitecture.Arm
                    ? "piper_linux_aarch64.tar.gz"
                    : "piper_linux_x86_64.tar.gz",
                PlatformID.MacOSX => arch == ProcessorArchitecture.Arm
                    ? "piper_macos_aarch64.tar.gz"
                    : "piper_macos_x64.tar.gz",
                _ => throw new NotSupportedException()
            };
            version = version == "latest" ? "latest/download" : $"download/{version}";
            var url = $"{RemoveLastSlash.Replace(repo, "")}/releases/{version}/{fileName}";
            var client = new HttpClient();
            var downloadStream = await client.GetStreamAsync(url);
            return downloadStream;
        }

        public static Task<string> ExtractPiper(this Task<Stream> downloadStream)
            => ExtractPiper(downloadStream, DefaultLocation);
        public static async Task<string> ExtractPiper(this Task<Stream> downloadStream, string extractTo)
            => ExtractPiper(await downloadStream, extractTo);
        public static string ExtractPiper(this Stream downloadStream, string extractTo)
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (isWindows)
            {
                //using var archive = ZipArchive.Open(downloadStream);
                // archive.ExtractToDirectory(extractTo);
               var ms = new MemoryStream();
               downloadStream.CopyTo(ms);
               ms.Position = 0;
               using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
               {
                   archive.ExtractToDirectory(extractTo);
               }
            }
            else
            {
                Main.Logger.Error("Non-windows not supported! Extract manually!");
                /*using (var reader = ReaderFactory.Open(downloadStream))
                {
                    var piperPath = Path.Combine(extractTo, "piper")!;
                    Queue<(string from, string to)> expectedSymlinks =
                        new Queue<(string from, string to)>(); 
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            reader.WriteEntryToDirectory(extractTo, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true,
                                WriteSymbolicLink = ((to, from) =>
                                {
                                    expectedSymlinks.Enqueue((from, Path.GetFileName(to)));
                                })
                            });
                        }
                    }
                    while (expectedSymlinks.Count > 0)
                    {
                        var link = expectedSymlinks.Dequeue();
                        var fromPath = Path.Combine(piperPath, link.from);
                        var toPath = Path.Combine(piperPath, link.to);
                        if (File.Exists(fromPath))
                        {
                            File.Copy(fromPath, toPath);
                            continue;
                        }
                        if (File.Exists(toPath)) continue;
                        
                        // Could not copy and file does not exist, back in queue
                        expectedSymlinks.Enqueue(link);
                    }
                }*/
            }
            return extractTo;
        }

        public static async Task<VoiceModel?> GetModelByKey(string modelName)
        {
            await GetHuggingFaceModelList();
            if (_voiceModels != null && _voiceModels.TryGetValue(modelName, out var value))
                return value;
            return null;
        }
    
        public static async Task<VoiceModel?> TryGetModelByKey(string modelKey)
        {
            await GetHuggingFaceModelList();
            if (_voiceModels != null && _voiceModels.TryGetValue(modelKey, out var value))
                return value;
            return null;
        }
    
        public static async Task<Dictionary<string, VoiceModel>?> GetHuggingFaceModelList()
        {
            if (_voiceModels != null) return _voiceModels;
            var client = new HttpClient();
            var response = await client.GetAsync(MODEL_LIST_URL);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException();
            var data = await response.Content.ReadAsStringAsync();
            if (data is null) throw new ApplicationException();
            _voiceModels = JsonConvert.DeserializeObject<Dictionary<string, VoiceModel>>(data);
            return _voiceModels;
        }

        public static async Task<VoiceModel> DownloadModelByKey(string modelKey)
        {
            var model = await GetModelByKey(modelKey);
            if (model is null)
            {
                throw new ArgumentException($"Model {modelKey} does not exist!", nameof(modelKey));
            }

            return await model.DownloadModel();
        }
        public static Task<VoiceModel> DownloadModel(this VoiceModel model)
            => model.DownloadModel(DefaultModelLocation);
        public static async Task<VoiceModel> DownloadModel(this VoiceModel model, string saveModelTo)
        {
            var path = Path.Combine(saveModelTo, model.Key);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var client = new HttpClient();
            foreach (var file in model.Files.Keys)
            {
                var fileName = Path.GetFileName(file);
                var filePath = Path.Combine(path, fileName);
                var downloadStream = await client.GetStreamAsync(MODEL_DOWNLOAD_URL.Replace("MODEL_FILE_URL", file));
                // Load Audio configuration from .onnx.json file
                if (fileName.EndsWith(".onnx.json"))
                {
                    var ms = new MemoryStream();
                    await downloadStream.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms);
                    var json = await reader.ReadToEndAsync();
                    var modelJson = JsonConvert.DeserializeObject<VoiceModel>(json);
                    model.Audio = modelJson!.Audio;
                    ms.Seek(0, SeekOrigin.Begin);
                    using var fs = File.OpenWrite(filePath);
                    await ms.CopyToAsync(fs);
                    fs.Close();
                }
                else
                {
                    using var fs = File.OpenWrite(filePath);
                    await downloadStream.CopyToAsync(fs);    
                    fs.Close();
                }
                downloadStream.Close();
            }
        
            model.ModelLocation = path;
            using var modelInfoFile = File.OpenWrite(Path.Combine(path, "model.json"));
            using var sw = new StreamWriter(modelInfoFile);
            var json2 = JsonConvert.SerializeObject( model, Formatting.Indented);
            await sw.WriteAsync(json2);

            return model;
        }
    }
}