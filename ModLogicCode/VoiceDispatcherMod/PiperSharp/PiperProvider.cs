using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiperSharp.Models;
using UnityEngine;
using UnityEngine.Networking;
using VoiceDispatcherMod.PiperSharp;

namespace PiperSharp {
    public static class PiperProvider {
        public static Process CreatePiperProcess(PiperConfiguration configuration) {
            if (configuration.Model is null)
                throw new ArgumentNullException(nameof(PiperConfiguration.Model), "VoiceModel not configured!");

            return new Process() {
                StartInfo = new ProcessStartInfo() {
                    FileName = configuration.ExecutableLocation.AddPathQuotesIfRequired(),
                    Arguments = configuration.BuildArguments(),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = configuration.OutputRaw,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = configuration.WorkingDirectory,
                    StandardOutputEncoding = configuration.OutputRaw ? Encoding.UTF8 : null,
                },
            };
        }
        
        public static async Task<string> InferAsync(string text, PiperConfiguration configuration,
            CancellationToken token = default) {
            var process = CreatePiperProcess(configuration);
            process.Start();
            await process.StandardInput.WriteLineAsync(text.ToUtf8());
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            
            await process.WaitForExitAsync(token);
            
            return configuration.OutputFilePath;
        }

        public static async Task<string> InferAsyncWithSox(string text, PiperConfiguration configuration, SoxConfiguration soxConfiguration, CancellationToken token = default) {
            var piperProcess = CreatePiperProcess(configuration);
            piperProcess.Start();
            
            var soxProcess = SoxConfiguration.CreateSoxProcess(soxConfiguration);
            soxProcess.Start();
            
            await piperProcess.StandardInput.WriteLineAsync(text.ToUtf8());
            await piperProcess.StandardInput.FlushAsync();
            piperProcess.StandardInput.Close();
            
            using var ms = new MemoryStream();
            
            // Start piping and reading in parallel
            var pipeTask = piperProcess.StandardOutput.BaseStream.CopyToAsync(soxProcess.StandardInput.BaseStream, 81920, token)
                .ContinueWith(_ => soxProcess.StandardInput.Close(), token); // Close SoX input after piping

            await Task.WhenAll(pipeTask, piperProcess.WaitForExitAsync(token), soxProcess.WaitForExitAsync(token));

            return soxConfiguration.OutputFilePath;
        }

        private static void ToSamples(MemoryStream ms, out int sampleCount, out float[] samples) {
            ms.Seek(0, SeekOrigin.Begin);
            var floats = new List<float>();
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) > 0) {
                for (int i = 0; i < bytesRead; i += 2) {
                    if (i + 1 >= bytesRead) break;
                    short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    floats.Add(sample / 32768f);
                }
            }
            sampleCount = floats.Count;
            samples = floats.ToArray();
        }
    }
}