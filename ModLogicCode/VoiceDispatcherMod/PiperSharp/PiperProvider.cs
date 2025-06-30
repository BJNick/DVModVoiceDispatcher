using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiperSharp.Models;
using UnityEngine;
using VoiceDispatcherMod;
using VoiceDispatcherMod.PiperSharp;

namespace PiperSharp {
    public class PiperProvider {
        public PiperConfiguration Configuration { get; set; }

        public PiperProvider(PiperConfiguration configuration) {
            Configuration = configuration;
        }

        public static Process ConfigureProcess(PiperConfiguration configuration) {
            if (configuration.Model is null)
                throw new ArgumentNullException(nameof(PiperConfiguration.Model), "VoiceModel not configured!");

            return new Process() {
                StartInfo = new ProcessStartInfo() {
                    FileName = configuration.ExecutableLocation.AddPathQuotesIfRequired(),
                    Arguments = configuration.BuildArguments(),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = configuration.WorkingDirectory,
                    StandardOutputEncoding = Encoding.UTF8,
                },
            };
        }
        
        public async Task<AudioClip> InferAsync(string text, AudioOutputType outputType = AudioOutputType.Wav,
            CancellationToken token = default(CancellationToken)) {
            var process = ConfigureProcess(Configuration);
            process.Start();
            await process.StandardInput.WriteLineAsync(text.ToUtf8());
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            using var ms = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(ms, 81920, token);
            await process.WaitForExitAsync(token);
            ms.Seek(0, SeekOrigin.Begin);

            //using var fs = new RawSourceWaveStream(ms, new WaveFormat((int)(Configuration.Model.Audio?.SampleRate ?? 16000), 1));
            //return await ConvertToArray(fs, outputType, token);
            ms.Seek(0, SeekOrigin.Begin);
            byte[] pcmData = ms.ToArray();
            int sampleRate = (int)(Configuration.Model.Audio?.SampleRate ?? 16000);
            int sampleCount = pcmData.Length / 2; // 16-bit PCM
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++) {
                short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                samples[i] = sample / 32768f;
            }

            AudioClip clip = AudioClip.Create("PiperClip", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }


        public async Task<AudioClip> InferAsyncWithSox(string text, AudioOutputType outputType = AudioOutputType.Wav,
            CancellationToken token = default(CancellationToken)) {
            var piperProcess = ConfigureProcess(Configuration);
            piperProcess.Start();
            
            int sampleRate = (int)(Configuration.Model.Audio?.SampleRate ?? 16000);
            
            var soxProcess = SoxEffects.ConfigureSoxProcess(sampleRate);
            soxProcess.Start();
            
            await piperProcess.StandardInput.WriteLineAsync(text.ToUtf8());
            await piperProcess.StandardInput.FlushAsync();
            piperProcess.StandardInput.Close();
            
            // Prepare memory stream for SoX output
            using var ms = new MemoryStream();
            
            // Start piping and reading in parallel
            var pipeTask = piperProcess.StandardOutput.BaseStream.CopyToAsync(soxProcess.StandardInput.BaseStream, 81920, token)
                .ContinueWith(_ => soxProcess.StandardInput.Close(), token); // Close SoX input after piping

            var readTask = soxProcess.StandardOutput.BaseStream.CopyToAsync(ms, 81920, token);
            
            var soxErrorTask = Task.Run(async () => {
                string? line;
                while ((line = await soxProcess.StandardError.ReadLineAsync()) != null) {
                    Main.Logger.Warning($"SoX STDERR: {line}");
                }
            });
            
            Main.Logger.Log("Started all processes, waiting for output...");
            
            await Task.WhenAll(pipeTask, readTask, soxErrorTask, piperProcess.WaitForExitAsync(token), soxProcess.WaitForExitAsync(token));
            
            ms.Seek(0, SeekOrigin.Begin);
            byte[] pcmData = ms.ToArray();
            int soxSampleRate = 8000;
            int sampleCount = pcmData.Length / 2; // 16-bit PCM
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++) {
                short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                samples[i] = sample / 32768f;
            }

            AudioClip clip = AudioClip.Create("PiperClip", sampleCount, 1, soxSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}