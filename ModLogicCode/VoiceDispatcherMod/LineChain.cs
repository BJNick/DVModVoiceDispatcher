using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DVVoiceAssistant.ModLogicCode {
    // Abstract base class for a subtitle line
    public abstract class Line {
        public string SubtitleText { get; set; }
        public AudioClip AudioClip { get; set; }
        protected Task<AudioClip> ClipCreationTask;
        
        public virtual bool IsClipReady => ClipCreationTask is { IsCompleted: true, IsFaulted: false };
        public bool IsClipInProgress => ClipCreationTask is { IsCompleted: false };

        public Line(string subtitleText) {
            SubtitleText = subtitleText;
            AudioClip = null;
            ClipCreationTask = null;
        }

        public abstract void CreateClip();
    }

    // Represents a line with an in-memory audio clip
    public class InMemoryLine : Line {
        public InMemoryLine(string subtitleText, AudioClip audioClip) : base(subtitleText) {
            AudioClip = audioClip;
        }

        public override bool IsClipReady => true;

        public override void CreateClip() {
            if (ClipCreationTask == null) {
                ClipCreationTask = Task.FromResult(AudioClip);
            }
        }
    }

    // Represents a line with a filename to load audio from disk
    public class FileLine : Line {
        public string Filename { get; set; }

        public FileLine(string subtitleText, string filename) : base(subtitleText) {
            Filename = filename;
        }

        public override void CreateClip() {
            if (ClipCreationTask == null) {
                ClipCreationTask = Task.Run(() => {
                    // TODO: Implement logic to load audio from Filename into AudioClip
                    // Simulate loading
                    // AudioClip = ...;
                    return AudioClip;
                });
            }
        }
    }

    // Represents a line that requires audio generation from a prompt
    public class PromptLine : Line {
        public string Prompt { get; set; }

        public PromptLine(string subtitleText, string prompt) : base(subtitleText) {
            Prompt = prompt;
        }

        public override void CreateClip() {
            if (ClipCreationTask == null) {
                ClipCreationTask = Task.Run(() => {
                    // TODO: Implement logic to generate audio from Prompt and store in AudioClip
                    // Simulate generation
                    // AudioClip = ...;
                    return AudioClip;
                });
            }
        }
    }

    // Represents a chain of lines (sentences) to be played in sequence
    public class LineChain {
        public List<Line> Lines { get; set; }
        private int currentIndex = 0;

        public LineChain() {
            Lines = new List<Line>();
        }

        // Checks if the next line in the chain is ready for playback
        public bool IsNextLineReady() {
            if (currentIndex < Lines.Count) {
                return Lines[currentIndex].IsClipReady;
            }

            return false;
        }

        // Advances to the next line in the chain
        public void MoveToNextLine() {
            if (currentIndex < Lines.Count - 1) {
                currentIndex++;
            }
            // else: End of chain
        }

        // Gets the current line
        public Line GetCurrentLine() {
            if (currentIndex < Lines.Count) {
                return Lines[currentIndex];
            }

            return null;
        }

        // TODO: Add more methods as needed for playback control, etc.
    }
}