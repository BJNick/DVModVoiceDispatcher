
using System.Threading.Tasks;
using Piper;
using UnityEngine;


public class LineGenerator : MonoBehaviour {
    [Tooltip("Set to true to generate audio")]
    public bool generate = false;
    
    public bool overwriteExistingFiles = false; // If true, existing files will be overwritten
    
    public int delayBetweenLines = 100; // Delay in milliseconds between generating each line

    public PiperManager piperManager;

    private void Start() {
        if (generate) {
            generate = false;
            GenerateAudio();
        }
    }

    private async void GenerateAudio() {
        foreach (var list in AllLines.lists) {
            foreach (var line in list.lines) {
                await GenerateSingle(line, list.directory);
                // wait to reset the audio generation
                System.Threading.Thread.Sleep(delayBetweenLines); // Adjust the delay as needed
            }
        }
    }

    private async Task GenerateSingle(Line line, string directory) {
        string filename = line.filename + ".wav";
        string path = "Assets/Output/"+directory+filename;
        // Check if file already exists
        if (System.IO.File.Exists(path) && !overwriteExistingFiles) {
            return;
        }
        Debug.Log($"Generating audio for: {line.text} at {path}");
        await piperManager.TextToSpeech(line.text, path);
    }
    
}