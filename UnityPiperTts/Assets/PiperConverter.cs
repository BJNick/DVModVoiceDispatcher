using Piper;
using UnityEngine;

[ExecuteInEditMode]
public class PiperConverter : MonoBehaviour
{
    [TextArea(3, 10)]
    public string textToSynthesize;

    [Tooltip("Output filename (e.g., output.wav)")]
    public string filename = "output.wav";

    [Tooltip("Set to true to generate audio")]
    public bool generate = false;
    
    public PiperManager piperManager;

    private void Update()
    {
        if (generate && !string.IsNullOrEmpty(textToSynthesize) && !string.IsNullOrEmpty(filename))
        {
            generate = false;
            GenerateAudio();
        }
    }

    private void GenerateAudio()
    {
        piperManager.TextToSpeech(textToSynthesize, "Assets/Output/"+filename);
    }
}