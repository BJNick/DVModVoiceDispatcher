using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using UnityEngine;
using UnityModManagerNet;

namespace TestMod
{
    static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        private static GameObject cube;
        private static AudioClip chime;
        private static AudioClip welcomeMsg;
        
        private static AudioSource source;
        
        private static AssetBundle voicedLines;
        
        private static int currentLineIndex = 0;

        static bool Load(UnityModManager.ModEntry modEntry) {
            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            return true;
        }

        private static string[] testVoiceLines = new string[]
            { "YouHave", "JobTypeShuntingUnload", "Move", "3Cars", "ToTrack", "D", "7", "Move", "1Cars", "ToTrack", "B", "1" };

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* to active or deactivate */)
        {
            if (value)
            {
                Enable(); // Perform all necessary steps to start mod.
            }
            else
            {
                Disable(); // Perform all necessary steps to stop mod.
            }
            
            enabled = value;
            return true; // If true, the mod will switch the state. If not, the state will not change.
        }

        static void Enable() {
            mod.OnUpdate = OnUpdate;
            try {
                var path = Path.Combine(mod.Path, "testbundle");
                mod.Logger.Log("Getting asset bundle from: " + path);
                var assets = AssetBundle.LoadFromFile(path);
                
                path = Path.Combine(mod.Path, "voiced_lines");
                mod.Logger.Log("Loading voiced lines from: " + path);
                voicedLines = AssetBundle.LoadFromFile(path);
                
                if (voicedLines == null) {
                    mod.Logger.Error("Failed to load voiced lines asset bundle.");
                }
                
                mod.Logger.Log("Got asset bundle: " + assets);
                cube = assets.LoadAsset<GameObject>("Cube");
                chime = assets.LoadAsset<AudioClip>("chime");
                welcomeMsg = assets.LoadAsset<AudioClip>("welcome-dm3");
            } catch (Exception e) {
                mod.Logger.Error("Failed to load asset bundle: " + e.Message);
            }
            PlayerManager.CarChanged += OnCarChanged;
        }
        
        static void Disable() {
            mod.OnUpdate = null;
            PlayerManager.CarChanged -= OnCarChanged;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt) {
            // on key press, instantiate a cube and play a sound
            if (Input.GetKeyDown(KeyCode.C)) {
                // Create a gameobject with an audio source and play the audio clip
                var obj = GameObject.Instantiate(cube);
                var pos = Camera.main.transform.position;
                obj.transform.position = pos;

                var audioSource = obj.AddComponent<AudioSource>();
                audioSource.clip = chime;
                audioSource.Play();
                audioSource.loop = true;

            }

            if (Input.GetKeyDown(KeyCode.F)) {
                SetupSource();
                var behaviour = source.gameObject.GetComponent<CoroutineRunner>();
                behaviour.StartCoroutine(PlayVoiceLinesCoroutine(testVoiceLines));
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                var lineBuilder = new List<string>();
                if (JobsManager.Instance.currentJobs.Count == 0) {
                    mod.Logger.Log("No current jobs found.");
                    lineBuilder.Add("YouHaveA");
                    lineBuilder.Add("0");
                    lineBuilder.Add("Cars");
                    lineBuilder.Add("To");
                    lineBuilder.Add("Move");
                }
                
                foreach (var job in JobsManager.Instance.currentJobs) {
                    lineBuilder.Add("YouHave");

                    var typeLine = "JobType" + job.jobType;
                    lineBuilder.Add(typeLine);

                    foreach (var task in job.tasks) {
                        AddTaskLines(task, lineBuilder);
                    }
                }

                if (lineBuilder.Count > 0) {
                    var line = string.Join(" ", lineBuilder);
                    mod.Logger.Log("Generated voice line: " + line);
                    SetupSource();
                    var behaviour = source.gameObject.GetComponent<CoroutineRunner>();
                    behaviour.StartCoroutine(PlayVoiceLinesCoroutine(lineBuilder.ToArray()));
                }
            }
        }

        private static string[] VoicedTrackId(Track track) {
            // TODO: Some types have two letters, like "SP" for storage passenger track.
            return track?.ID != null ? SeparateIntoLetters(track.ID.TrackPartOnly.Substring(0,2)) :
                new[] { "Unknown", "Track" };
        }

        private static void AddTaskLines(Task task, List<string> lineBuilder) {
            var taskData = task.GetTaskData();
            mod.Logger.Log(taskData.type.ToString());
            if (taskData.nestedTasks != null && taskData.nestedTasks.Count > 0) {
                foreach (var nestedTask in taskData.nestedTasks) {
                    AddTaskLines(nestedTask, lineBuilder);
                }
                return;
            }
            var start = taskData.startTrack;
            var end = taskData.destinationTrack;
            var carCount = taskData.cars?.Count;
            
            if (taskData.warehouseTaskType == WarehouseTaskType.Unloading) {
                lineBuilder.Add("Unload");
            } else if (taskData.warehouseTaskType == WarehouseTaskType.Loading) {
                lineBuilder.Add("Load");
            } else {
                lineBuilder.Add("Move");
            }
            
            if (carCount != null) {
                lineBuilder.Add(carCount + "Cars");
            } else {
                lineBuilder.Add("Cars");
            }
            
            if (start?.ID != null) {
                lineBuilder.Add("FromTrack");
                lineBuilder.AddRange(VoicedTrackId(start));
            }
            if (end?.ID != null) {
                if (taskData.warehouseTaskType == null || taskData.warehouseTaskType == WarehouseTaskType.None) {
                    lineBuilder.Add("ToTrack");
                } else {
                    lineBuilder.Add("AtTrack");
                }
                lineBuilder.AddRange(VoicedTrackId(end));
            }
        }

        public static string[] SeparateIntoLetters(string text) {
            return text.ToCharArray().Select(c => c.ToString()).ToArray();
        }
        
        public class CoroutineRunner : MonoBehaviour { }

        static IEnumerator PlayVoiceLinesCoroutine(string[] lines) {
            AudioClip[] clips = lines.Select(GetVoicedClip).Where(clip => clip != null).ToArray();
            if (clips.Length == 0) {
                mod.Logger.Error("No valid voice lines found.");
                yield break;
            }
            foreach (var clip in clips) {
                source.PlayOneShot(clip);
                // Sleep until the next clip is ready to play
                var waitTime = clip.length - 0.05f;
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        static AudioClip GetVoicedClip(string name)
        {
            if (voicedLines == null) {
                mod.Logger.Error("Voiced lines asset bundle is not loaded.");
                return null;
            }
            var clip = voicedLines.LoadAsset<AudioClip>(name);
            if (clip == null) {
                mod.Logger.Error("Failed to load voice line: " + name);
            }
            return clip;
        }
        
        static void OnCarChanged(TrainCar car)
        {
            if (car != null && car.carType == TrainCarType.LocoDM3)
            {
                PlaySound(welcomeMsg);
            }
        }
        
        static void PlaySound(AudioClip clip)
        {
            SetupSource();
            //if (source.isPlaying) {return;}
            source.clip = clip;
            source.Play();
        }

        private static void SetupSource() {
            if (source == null)
            {
                source = new GameObject("AudioSource").AddComponent<AudioSource>();
                source.transform.position = Camera.main.transform.position;//PlayerManager.PlayerTransform.position;
                source.loop = false;
                source.gameObject.AddComponent<CoroutineRunner>();
            }
        }

        static void PlayVoiceLine(string lineName) {
            if (voicedLines == null) {
                mod.Logger.Error("Voiced lines asset bundle is not loaded.");
                return;
            }

            var clip = voicedLines.LoadAsset<AudioClip>(lineName);
            if (clip == null) {
                mod.Logger.Error("Failed to load voice line: " + lineName);
                return;
            }

            PlaySound(clip);
        }
    }
}