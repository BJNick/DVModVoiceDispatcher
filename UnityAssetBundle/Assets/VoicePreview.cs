using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoicePreview : MonoBehaviour
{
    private const string SHORT_PAUSE = "ShortSilence";
    private static AudioSource audioSource;
    
    private static AssetBundle voicedLines;
    
    public bool play = true;
    
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        voicedLines = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/voiced_lines");
    }

    void Update() {
        if (play) { 
            play = false;
            
            var lineBuilder = new List<string>();
            lineBuilder.Add("NoiseClick");
            AddStationGreetings(lineBuilder);
            lineBuilder.Add("NoiseClick");

            var line = string.Join(" ", lineBuilder);
            Debug.Log(line);
            StartCoroutine(PlayVoiceLinesCoroutine(lineBuilder.ToArray()));
        }
}
    
    private static void AddShuntingLoadJobLines(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "YouHave", "JobTypeShuntingLoad", SHORT_PAUSE,
            "Couple", "4Cars", "AtTrack", "D", "1", SHORT_PAUSE,
            "And", "2Cars", "AtTrack", "A", "5", SHORT_PAUSE,
            "ThenMove", "6Cars", "ToTrack", "C", "3", "ForLoading", SHORT_PAUSE,
            "ThenUncouple", "AtTrackTypeO", "B", "7", "ForDeparture"
        });
    }
    
    private static void AddShuntingUnloadJobLines(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "YouHave", "JobTypeShuntingUnload", SHORT_PAUSE,
            "PickUp", "6Cars", "AtTrackTypeI", "E", "2", SHORT_PAUSE,
            "ThenUnloadThoseCars", "AtTrack", "F", "4", SHORT_PAUSE,
            "ThenUncouple", "2Cars", "AtTrack", "G", "1", SHORT_PAUSE,
            "3Cars", "AtTrack", "H", "3", SHORT_PAUSE,
            "And", "1Cars", "AtTrack", "H", "3", "ToCompleteTheOrder"
        });
    }
    
    private static void AddTransportJobLines(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "YouHave", "JobTypeTransport", SHORT_PAUSE,
            "PickUp", "12Cars", "FromTrack", "A", "1", "In", "YardGF", SHORT_PAUSE,
            "ThenDropOffThoseCars", "AtTrack", "B", "2", "In", "YardIME"
        });
    }
    
    private static void AddCarDetailedDescription(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "0", "8", "2", "BoundFor", "YardOWC", SHORT_PAUSE,
            "4", "7", "1", "WaitingForUnloading", SHORT_PAUSE,
            "5", "3", "9", "PartOf", "JobTypeComplexTransport", SHORT_PAUSE,
            "0", "0", "3", "NotPartOfAnyOrder",
        });
    }
    
    private static void AddStationGreetings(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "EnteringYard5", "YardOWC", SHORT_PAUSE,
            "EnteringStation2", "YardMF", SHORT_PAUSE,
            "ExitingYard4", "YardIME", SHORT_PAUSE,
        });
    }
    
    static IEnumerator PlayVoiceLinesCoroutine(string[] lines) {
        AudioClip[] clips = lines.Select(GetVoicedClip).Where(clip => clip != null).ToArray();
        if (clips.Length == 0) {
            Debug.LogError("No valid voice lines found.");
            yield break;
        }
        foreach (var clip in clips) {
            audioSource.PlayOneShot(clip);
            // Sleep until the next clip is ready to play
            var waitTime = clip.length - 0.05f;
            yield return new WaitForSeconds(waitTime);
        }
    }
        
    static AudioClip GetVoicedClip(string name)
    {
        if (voicedLines == null) {
            Debug.LogError("Voiced lines asset bundle is not loaded.");
            return null;
        }
        var clip = voicedLines.LoadAsset<AudioClip>(name);
        if (clip == null) {
            Debug.LogError("Failed to load voice line: " + name);
        }
        return clip;
    }
}
