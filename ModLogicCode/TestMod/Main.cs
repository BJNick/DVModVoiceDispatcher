using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.Booklets;
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
        
        private static AudioSource source;
        
        private static string assetBundlePath;
        private static AssetBundle voicedLines;
        
        private static int currentLineIndex = 0;
        
        private static List<string> readJobs = new List<string>();
        private static bool currentlyReading;

        static bool Load(UnityModManager.ModEntry modEntry) {
            mod = modEntry;
            assetBundlePath = Path.Combine(mod.Path, "voiced_lines");
            modEntry.OnToggle = OnToggle;
            return true;
        }
        
        private const string SHORT_PAUSE = "ShortSilence";

        private static string[] testVoiceLines = new string[]
            { "YouHave", "JobTypeShuntingUnload", "Move", "3Cars", "ToTrackTypeS", "D", "7", "Move", "1Cars", "ToTrackTypeI", "B", "1" };

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* to active or deactivate */)
        {
            if (value) {
                Enable(); // Perform all necessary steps to start mod.
            } else {
                Disable(); // Perform all necessary steps to stop mod.
            }
            
            enabled = value;
            return true; // If true, the mod will switch the state. If not, the state will not change.
        }

        static void Enable() {
            mod.OnUpdate = OnUpdate;
            mod.OnSessionStart = OnSessionStart;
            try {
                mod.Logger.Log("Loading voiced lines from: " + assetBundlePath);
                voicedLines = AssetBundle.LoadFromFile(assetBundlePath);
                if (voicedLines == null) {
                    mod.Logger.Error("Failed to load voiced lines asset bundle.");
                }
            } catch (Exception e) {
                mod.Logger.Error("Failed to load asset bundle: " + e.Message);
            }
            PlayerManager.CarChanged += OnCarChanged;
        }
        
        static void Disable() {
            mod.OnUpdate = null;
            PlayerManager.CarChanged -= OnCarChanged;
        }
        
        static void OnCarChanged(TrainCar car) {
            //ReadJobOverview(JobsManager.Instance.currentJobs.First());
        }

        static void OnSessionStart(UnityModManager.ModEntry modEntry) {
            SetupSource();
        }
        
        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt) {
            if (Input.GetKeyDown(KeyCode.L)) {
                SetupSource();
                var behaviour = source.gameObject.GetComponent<CoroutineRunner>();
                behaviour.StartCoroutine(PlayVoiceLinesCoroutine(testVoiceLines));
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                var lineBuilder = new List<string>();
                AddGenericJobLines(lineBuilder, JobsManager.Instance.currentJobs.First());
                var line = string.Join(" ", lineBuilder);
                mod.Logger.Log("Generated voice line: " + line);
                SetupSource();
                var behaviour = source.gameObject.GetComponent<CoroutineRunner>();
                behaviour.StartCoroutine(PlayVoiceLinesCoroutine(lineBuilder.ToArray()));
            }

            if (Input.GetKeyDown(KeyCode.O)) {
                ReadJobOverview(JobsManager.Instance.currentJobs.First());
            }
            
            if (JobsManager.Instance != null) {
                foreach (var job in JobsManager.Instance.currentJobs) {
                    if (!readJobs.Contains(job.ID) && !currentlyReading) {
                        readJobs.Add(job.ID);
                        ReadJobOverview(job);
                    }
                }
            }
        }

        private static void AddGenericJobLines(List<string> lineBuilder, Job job) {
            lineBuilder.Add("YouHave");

            var typeLine = "JobType" + job.jobType;
            lineBuilder.Add(typeLine);

            foreach (var task in job.tasks) {
                AddTaskLines(task, lineBuilder);
            }
        }

        private static void ReadJobOverview(Job job) {
            if (currentlyReading) {
                return;
            }
            currentlyReading = true;
            try {
                var lineBuilder = new List<string>();
                AddJobSpecificLines(lineBuilder, job);
                var line = string.Join(" ", lineBuilder);
                mod.Logger.Log("Generated voice line: " + line);
                SetupSource();
                var behaviour = source.gameObject.GetComponent<CoroutineRunner>();
                behaviour.StartCoroutine(PlayVoiceLinesCoroutine(lineBuilder.ToArray()));
            } finally {
                currentlyReading = false;
            }
        }

        private static string[] VoicedTrackId(TrackID trackId) {
            // TODO: Some types have two letters, like "SP" for storage passenger track.
            return trackId != null ? SeparateIntoLetters(trackId.TrackPartOnly.Substring(0,2)) :
                new[] { "Unknown", "Track" };
        }
        
        private static string GetTrackTypeLetter(TrackID trackId) {
            if (trackId == null) {
                return "M";
            }
            return trackId.FullID.Split('-').Last();
        }
        
        private static void AddJobSpecificLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                mod.Logger.Error("Invalid job or no tasks found.");
                return;
            }
            switch (job.jobType) {
                case JobType.ShuntingLoad:
                    AddShuntingLoadJobLines(lineBuilder, job);
                    break;
                case JobType.ShuntingUnload:
                    AddShuntingUnloadJobLines(lineBuilder, job);
                    break;
                default:
                    AddGenericJobLines(lineBuilder, job);
                    break;
            }
        }
        
        private static void AddShuntingLoadJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                mod.Logger.Error("Invalid job or no tasks found.");
                return;
            }
            var jobInfo = JobDataExtractor.ExtractShuntingLoadJobData(new Job_data(job));
            
            lineBuilder.Add("YouHave");
            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add(SHORT_PAUSE);
            
            lineBuilder.Add("Couple");

            for (var index = 0; index < jobInfo.startingTracksData.Count; index++) {
                var carDataPerTrackID = jobInfo.startingTracksData[index];
                var track = carDataPerTrackID.track;
                var carCount = carDataPerTrackID.cars.Count;
                if (index == jobInfo.startingTracksData.Count - 1) {
                    lineBuilder.Add("And");
                }
                lineBuilder.Add(carCount + "Cars");
                lineBuilder.Add("AtTrack");
                lineBuilder.AddRange(VoicedTrackId(track));
                lineBuilder.Add(SHORT_PAUSE);
            }

            lineBuilder.Add("ThenMove");
            lineBuilder.Add(jobInfo.allCarsToLoad.Count+"Cars");
            lineBuilder.Add("ToTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.loadMachineTrack));
            lineBuilder.Add("ForLoading");
            lineBuilder.Add(SHORT_PAUSE);
            
            lineBuilder.Add("ThenUncouple");
            lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(jobInfo.destinationTrack));
            lineBuilder.AddRange(VoicedTrackId(jobInfo.destinationTrack));
            lineBuilder.Add("ForDeparture");
        }
        
        private static void AddShuntingUnloadJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                mod.Logger.Error("Invalid job or no tasks found.");
                return;
            }
            var jobInfo = JobDataExtractor.ExtractShuntingUnloadJobData(new Job_data(job));
            
            lineBuilder.Add("YouHave");
            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add(SHORT_PAUSE);
            
            lineBuilder.Add("PickUp");
            lineBuilder.Add(jobInfo.allCarsToUnload.Count+"Cars");
            lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(jobInfo.startingTrack));
            lineBuilder.AddRange(VoicedTrackId(jobInfo.startingTrack));
            lineBuilder.Add(SHORT_PAUSE);
            
            lineBuilder.Add("ThenUnloadThoseCars");
            lineBuilder.Add("AtTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.unloadMachineTrack));
            lineBuilder.Add(SHORT_PAUSE);
            
            lineBuilder.Add("ThenUncouple");
            for (var index = 0; index < jobInfo.destinationTracksData.Count; index++) {
                var carDataPerTrackID = jobInfo.destinationTracksData[index];
                var track = carDataPerTrackID.track;
                var carCount = carDataPerTrackID.cars.Count;
                if (index == jobInfo.destinationTracksData.Count - 1) {
                    lineBuilder.Add("And");
                }
                lineBuilder.Add(carCount + "Cars");
                lineBuilder.Add("AtTrack");
                lineBuilder.AddRange(VoicedTrackId(track));
                if (index < jobInfo.destinationTracksData.Count - 1) {
                    lineBuilder.Add(SHORT_PAUSE);
                }
            }
            lineBuilder.Add("ToCompleteTheOrder");
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
                lineBuilder.Add("FromTrackType"+GetTrackTypeLetter(start.ID));
                lineBuilder.AddRange(VoicedTrackId(start.ID));
            }
            if (end?.ID != null) {
                if (taskData.warehouseTaskType == WarehouseTaskType.None) {
                    lineBuilder.Add("ToTrackType"+GetTrackTypeLetter(end.ID));
                } else {
                    lineBuilder.Add("AtTrackType"+GetTrackTypeLetter(end.ID));
                }
                lineBuilder.AddRange(VoicedTrackId(end.ID));
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
            currentlyReading = false;
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
        
        static void PlaySound(AudioClip clip)
        {
            SetupSource();
            //if (source.isPlaying) {return;}
            source.clip = clip;
            source.Play();
        }

        private static void SetupSource() {
            if (voicedLines == null) {
                voicedLines = AssetBundle.LoadFromFile(assetBundlePath);
                mod.Logger.Error("Failed to load voiced lines asset bundle.");
            }
            if (source == null)
            {
                source = new GameObject("AudioSource").AddComponent<AudioSource>();
                source.transform.position = Camera.main.transform.position;//PlayerManager.PlayerTransform.position;
                source.loop = false;
            }
            if (source.gameObject.GetComponent<CoroutineRunner>() == null) {
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