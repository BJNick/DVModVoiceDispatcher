using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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
            AddHighestPayingJobLines(lineBuilder);
            //lineBuilder.AddRange(SayApproximateNumber(523456789));
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
            "EnteringYard"+Random.Range(1,6), "YardOWC", SHORT_PAUSE,
            "EnteringStation"+Random.Range(1,6), "YardMF", SHORT_PAUSE,
            "ExitingYard"+Random.Range(1,6), "YardIME", SHORT_PAUSE,
        });
    }
    
    private static void AddNumberTest(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "0", "8", "2", "BoundFor", "YardOWC", SHORT_PAUSE,
            "ToTrack", "C", "3", "ForLoading", SHORT_PAUSE,
            "4", "100", "20", "3", "1000000", "7", "100", "19", "1000", "9", "100", "90", "9"
        });
    }
    
    private static void AddHighestPayingJobLines(List<string> lineBuilder) {
        lineBuilder.AddRange(new[] {
            "EnteringStation"+Random.Range(1,6), "YardMF", SHORT_PAUSE,
            "HighestPayingJob", "JobTypeTransport", "BoundFor", "YardIME", SHORT_PAUSE,
            "Over", "70", "1000", "Dollars",
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
    
    public static string[] SayApproximateNumber(int number) {
        var leadingDigit = number.ToString()[0] - '0';
        var totalDigits = number.ToString().Length;
        // If the leading digit is 1, say 2 digits, otherwise say 1 digit.
        if (leadingDigit == 1) {
            var tenthPower = (int)Math.Pow(10, totalDigits - 2);
            return SayFullNumber(number / tenthPower * tenthPower);
        } else {
            var tenthPower = (int)Math.Pow(10, totalDigits - 1);
            return SayFullNumber(number / tenthPower * tenthPower);
        }
    }
    
    public static string[] SayFullNumber(int number) {
        var numberParts = new List<string>();
        if (number >= 1_000_000) {
            var subMillion = number / 1_000_000 % 1000;
            if (subMillion == 0) {
                subMillion = 999; // Max possible value
            }
            numberParts.AddRange(SayTripleDigitNumber(subMillion));
            numberParts.Add("1000000");
        }
        if (number >= 1000) {
            var subThousand = number / 1000 % 1000;
            if (subThousand != 0) {
                numberParts.AddRange(SayTripleDigitNumber(subThousand));
                numberParts.Add("1000");
            }
        }
        if (number % 1000 > 0) {
            numberParts.AddRange(SayTripleDigitNumber(number % 1000));
        }
        return numberParts.ToArray();
    }
        
    public static string[] SayTripleDigitNumber(int number) {
        if (number < 100) {
            return SayDoubleDigitNumber(number);
        }
        var numberParts = new List<string>();
        numberParts.Add(NthDigit(number, 3).ToString());
        numberParts.Add("100");
        if (number % 100 > 0) {
            numberParts.AddRange(SayDoubleDigitNumber(number%100));
        }
        return numberParts.ToArray();
    }
        
    public static string[] SayDoubleDigitNumber(int number) {
        if (number <= 20) {
            return new[] { number.ToString() };
        }
        if (number / 10 * 10 == number) {
            return new[] { number.ToString() };
        }
        return new[] { (NthDigit(number, 2) * 10).ToString(), NthDigit(number, 1).ToString() };
    }

    public static int NthDigit(int number, int n) {
        return number / (int)Math.Pow(10, n-1) % 10;
    }
}
