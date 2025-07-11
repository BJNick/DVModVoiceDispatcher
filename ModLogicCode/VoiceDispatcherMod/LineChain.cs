using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VoiceDispatcherMod {
    // Abstract base class for a subtitle line
    public abstract class Line {
        public string SubtitleText { get; set; }
        public AudioClip AudioClip { get; set; }
        
        protected bool clipCreationStarted;
        protected bool clipCreationCompleted;
        protected bool clipCreationFailed;

        public bool IsClipReady => clipCreationCompleted && !clipCreationFailed;
        public bool IsClipInProgress => clipCreationStarted && !clipCreationCompleted && !clipCreationFailed;
        public bool IsClipFailed => clipCreationFailed;
        
        protected Coroutine ClipCreationCoroutine;
        protected MonoBehaviour CoroutineRunner;

        public Line(string subtitleText) {
            SubtitleText = subtitleText;
            AudioClip = null;
            ClipCreationCoroutine = null;
            clipCreationStarted = false;
            clipCreationCompleted = false;
            clipCreationFailed = false;
        }

        // Accepts a MonoBehaviour to run the coroutine
        public void CreateClip(MonoBehaviour coroutineRunner) {
            if (!clipCreationStarted) {
                ClipCreationCoroutine = coroutineRunner.StartCoroutine(CreateClipCoroutine());
                CoroutineRunner = coroutineRunner;
            }
        }
        
        public void CancelClipCreation() {
            if (ClipCreationCoroutine != null && CoroutineRunner && !clipCreationCompleted && !clipCreationFailed) {
                CoroutineRunner.StopCoroutine(ClipCreationCoroutine);
                ClipCreationCoroutine = null;
                clipCreationStarted = true;
                clipCreationCompleted = false;
                clipCreationFailed = true;
            }
        }

        // Subclasses implement this coroutine
        protected abstract IEnumerator CreateClipCoroutine();
        
        public IEnumerator GetClipWhenReady() {
            while (!IsClipReady) {
                if (!IsClipInProgress) {
                    yield return CreateClipCoroutine();
                }
                if (clipCreationFailed) {
                    Main.Logger.Error($"Failed to create audio clip for subtitle: {SubtitleText}");
                    yield break;
                }
            }
            yield return AudioClip;
        }
        
        public override string ToString() {
            return SubtitleText;
        }
    }

    // Represents a line with an in-memory audio clip
    public class InMemoryLine : Line {
        public InMemoryLine(string subtitleText, AudioClip audioClip) : base(subtitleText) {
            AudioClip = audioClip;
            clipCreationCompleted = false;
        }

        protected override IEnumerator CreateClipCoroutine() {
            clipCreationStarted = true;
            AudioClip.LoadAudioData();
            clipCreationCompleted = true;
            yield break;
        }
    }

    // Represents a line with a filename to load audio from disk
    public class FileLine : Line {
        public string Filename { get; set; }

        public FileLine(string subtitleText, string filename) : base(subtitleText) {
            Filename = filename;
        }

        protected override IEnumerator CreateClipCoroutine() {
            clipCreationStarted = true;
            // TODO: Replace with actual Unity async loading if needed
            yield return VoiceGenerator.LoadAudioClipFromFileCoroutine(Filename, clip => {
                AudioClip = clip;
            });
            if (!AudioClip) {
                clipCreationFailed = true;
            } else {
                clipCreationCompleted = true;
            }
        }
    }

    // Represents a line that requires audio generation from a prompt
    public class PromptLine : Line {
        public string Prompt { get; set; }

        public PromptLine(string prompt) : base(prompt) {
            Prompt = prompt;
        }

        protected override IEnumerator CreateClipCoroutine() {
            clipCreationStarted = true;
            
            var task = VoiceGenerator.GenerateWithSox(Prompt);
            while (!task.IsCompleted) {
                yield return null; // Wait for the task to complete
            }
            var filename = task.Result;
            if (string.IsNullOrEmpty(filename)) {
                clipCreationFailed = true;
                yield break;
            }
            yield return VoiceGenerator.LoadAudioClipFromFileCoroutine(filename, clip => {
                AudioClip = clip;
            });
            
            if (!AudioClip) {
                clipCreationFailed = true;
            } else {
                clipCreationCompleted = true;
            }
        }
    }

    public class AssetBundleLine : Line {
        public string AssetName { get; set; }

        public AssetBundleLine(string assetName, string subtitleText) : base(assetName) {
            AssetName = assetName;
            SubtitleText = subtitleText;
        }

        protected override IEnumerator CreateClipCoroutine() {
            clipCreationStarted = true;
            AudioClip = CommsRadioNarrator.GetVoicedClip(AssetName);
            if (!AudioClip) {
                clipCreationFailed = true;
            } else {
                clipCreationCompleted = true;
            }
            yield break;
        }
    }
    
    public class PauseLine : Line {
        public float Duration { get; set; }

        public PauseLine(float duration) : base("") {
            Duration = duration;
            SubtitleText = "*pause*";
        }

        protected override IEnumerator CreateClipCoroutine() {
            clipCreationStarted = true;
            AudioClip = AudioClip.Create("Pause", (int)(Duration * 8000), 1, 8000, false);
            clipCreationCompleted = true;
            yield return null;
        }
    }

    // Represents a chain of lines (sentences) to be played in sequence
    public class LineChain {

        public static List<Line> SplitIntoChain(string text) {
            var split = JsonLinesLoader.SentenceSplitRegex();
            var splitPattern = $"(?<={split})";
            var lines = new List<Line>();
            var sentences = Regex.Split(text, splitPattern, RegexOptions.Compiled);
            foreach (var sentence in sentences) {
                if (string.IsNullOrWhiteSpace(sentence)) continue;
                var trimmedSentence = sentence.Trim();
                if (trimmedSentence.Length > 0) {
                    lines.Add(new PromptLine(trimmedSentence));
                }
            }
            return lines;
        }
        
        public static void AddNoiseClicks(List<Line> lineChain) {
            lineChain.Insert(0, new AssetBundleLine("NoiseClick", "*click*"));
            lineChain.Add(new AssetBundleLine("NoiseClick", "*click*"));
        }
        
        public static List<Line> FromAssetBundleLines(List<string> oldLines) {
            return oldLines.Select(it => new AssetBundleLine(it, it)).ToList<Line>();
        }
        
        public static IEnumerator PlayLinesInCoroutine(List<Line> lineChain, MonoBehaviour coroutineRunner, Action onComplete = null) {
            var index = 0;
            
            while (index < lineChain.Count) {
                var currentLine = lineChain[index];
                currentLine.CreateClip(coroutineRunner);
            
                while (!currentLine.IsClipReady && !currentLine.IsClipFailed) {
                    yield return null;
                }
                
                //Main.Logger.Log("Playing line: " + lineChain[index].SubtitleText);
                CommsRadioNarrator.PlayRadioClip(currentLine.AudioClip);
                
                while (CommsRadioNarrator.source.isPlaying) {
                    // Pre-generate the upcoming line clips while the current one is playing
                    for (int i = index + 1; i < lineChain.Count; i++) {
                        var upcomingLine = lineChain[i];
                        if (upcomingLine.IsClipInProgress) {
                            break;
                        }
                        if (!upcomingLine.IsClipReady && !upcomingLine.IsClipFailed) {
                            //Main.Logger.Log("Creating line: " + lineChain[i].SubtitleText);
                            upcomingLine.CreateClip(coroutineRunner);
                            break;
                        }
                    }
                    
                    yield return null;
                }
                index++;
            }
            onComplete?.Invoke();
        }
    }
}