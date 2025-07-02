using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PiperSharp;

namespace VoiceDispatcherMod.PiperSharp {
    public class SoxEffects {
        
        public static Process CreateSoxProcess(int sampleRate = 16000) {
            var soxArgs = BuildSoxArguments(sampleRate);
            return new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = Path.Combine(PiperDownloader.DefaultLocation, "sox", "sox.exe").AddPathQuotesIfRequired(),
                    Arguments = soxArgs,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }
        
        public static string BuildSoxArguments(int inputSampleRate = 16000, int outputSampleRate = 8000, float volumeDb = 2f)
        {
            var args = new List<string>
            {
                "-V0",
                "-R",
                "-t raw",
                "-e signed-integer",
                "-b 16",
                "-c 1",
                $"-r {inputSampleRate}",
                "-",
                "-t raw",
                "-e signed-integer",
                "-b 16",
                "-c 1",
                $"-r {outputSampleRate}",
                "-",
                "highpass 300",
                "lowpass 3400",
                "compand 0.0,0.1 6:-80,-70,-5",
                "overdrive 10",
                "fade t 0.05",
                $"rate {outputSampleRate}",
                "norm",
                $"vol {volumeDb}dB"
            };
            return string.Join(" ", args);
        }
    }
}