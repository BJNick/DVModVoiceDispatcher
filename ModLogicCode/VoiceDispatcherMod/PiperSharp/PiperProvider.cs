using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiperSharp.Models;
using UnityEngine;

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

        /*
        private async Task<byte[]> ConvertToArray(RawSourceWaveStream stream, AudioOutputType outputType, CancellationToken token)
        {
            using var output = new MemoryStream();
            switch (outputType)
            {
                case AudioOutputType.Mp3:
                {
                    await stream.FlushAsync(token);
                    MediaFoundationEncoder.EncodeToMp3(stream, output);
                } break;
                case AudioOutputType.Raw:
                {
                    await stream.CopyToAsync(output, 81920, token);
                    await stream.FlushAsync(token);
                } break;
                case AudioOutputType.Wav:
                default:
                {
                    var waveStream = new WaveFileWriter(output, stream.WaveFormat);
                    await stream.CopyToAsync(waveStream, 81920, token);
                    await stream.FlushAsync(token);
                    await waveStream.FlushAsync(token);
                } break;
            }
            await output.FlushAsync(token);
            return output.ToArray();
        }
    */
    }
}