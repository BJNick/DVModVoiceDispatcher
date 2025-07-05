using System.Collections;
using System.Collections.Generic;
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
    /*CommsRadioNarrator.GetVoicedClip(string)*/
    public class AssetBundleLine : Line {
        public string AssetName { get; set; }

        public AssetBundleLine(string assetName) : base(assetName) {
            AssetName = assetName;
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

    // Represents a chain of lines (sentences) to be played in sequence
    public class LineChain {

        // TODO: Add more methods as needed for playback control, etc.
        
        public static IEnumerator PlayClipsInCoroutine(List<Line> lineChain, MonoBehaviour coroutineRunner) {
            var source = CommsRadioNarrator.source;
            var index = 0;
            
            while (index < lineChain.Count) {
                Main.Logger.Log("Creating line: " + lineChain[index].SubtitleText);
                var currentLine = lineChain[index];
                currentLine.CreateClip(coroutineRunner);
            
                while (!currentLine.IsClipReady && !currentLine.IsClipFailed) {
                    yield return null;
                }
            
                if (index + 1 < lineChain.Count) {
                    var nextLine = lineChain[index + 1];
                    nextLine.CreateClip(coroutineRunner);
                }
                
                Main.Logger.Log("Playing line: " + lineChain[index].SubtitleText);
                CommsRadioNarrator.PlayRadioClip(currentLine.AudioClip);
                
                while (source.isPlaying) {
                    yield return null;
                }
                index++;
            }
        }
    }
}