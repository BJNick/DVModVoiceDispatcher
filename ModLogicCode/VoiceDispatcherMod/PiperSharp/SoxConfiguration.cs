using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PiperSharp;

namespace VoiceDispatcherMod.PiperSharp {
    public class SoxConfiguration {
        
        public string ExecutablePath { get; set; }
        
        public int InputSampleRate { get; set; } = 16000;
        public int OutputSampleRate { get; set; } = 8000;
        
        public bool OutputRaw { get; set; } = false;
        public string OutputFilePath { get; set; } = null;
        
        public float VolumeDb { get; set; } = 2f;
        
        public static Process CreateSoxProcess(SoxConfiguration soxConfiguration) {
            var soxArgs = soxConfiguration.BuildSoxArguments();
            return new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = soxConfiguration.ExecutablePath,
                    Arguments = soxArgs,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = soxConfiguration.OutputRaw,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        public string BuildSoxArguments() {
            var args = new List<string> {
                "-V0",
                "-R",
            };

            args.AddRange(new List<string> {
                "-t raw",
                "-e signed-integer",
                "-b 16",
                "-c 1",
                $"-r {InputSampleRate}",
                "-",
            });

            if (OutputRaw) {
                args.AddRange(new List<string> {
                    "-t raw",
                    "-e signed-integer",
                    "-b 16",
                    "-c 1",
                    $"-r {OutputSampleRate}",
                    "-"
                });
            } else {
                if (string.IsNullOrEmpty(OutputFilePath)) {
                    Main.Logger.Error("SoxConfiguration: Output file path is empty");
                } else {
                    args.AddRange(new List<string> {
                        "-t wav",
                        "-e signed-integer",
                        "-b 16",
                        "-c 1",
                        $"-r {OutputSampleRate}",
                        OutputFilePath.AddPathQuotesIfRequired()
                    });
                }
            }

            args.AddRange(new List<string> {
                "highpass 300",
                "lowpass 3400",
                "compand 0.0,0.1 6:-80,-70,-5",
                "overdrive 10",
                "fade t 0.05",
                $"rate {OutputSampleRate}",
                "norm",
                $"vol {VolumeDb}dB"
            });

            return string.Join(" ", args);
        }
    }
}