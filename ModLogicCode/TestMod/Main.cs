using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TestMod
{
    static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        
        private static string assetBundlePath;
        private static AssetBundle voicedLines;
        
        private static CommsRadioController commsRadio;
        
        private static string lastClickedCarId = string.Empty;
        
        private static List<string> readJobs = new();
        
        static bool Load(UnityModManager.ModEntry modEntry) {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            CommsRadioNarrator.mod = mod;
            
            assetBundlePath = Path.Combine(mod.Path, "voiced_lines");
            modEntry.OnToggle = OnToggle;
            return true;
        }
        
        private const string SHORT_SILENCE = "ShortSilence";

        private static List<string> testVoiceLines = new List<string>
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
            CommsRadioNarrator.OnCarClicked += OnCarClicked;
            CommsRadioNarrator.OnNothingClicked += OnNothingClicked;
        }
        
        public static AssetBundle GetVoiceLinesBundle() {
            if (!voicedLines) {
                voicedLines = AssetBundle.LoadFromFile(assetBundlePath);
                if (!voicedLines) {
                    mod.Logger.Error("Failed to load voiced lines asset bundle from: " + assetBundlePath);
                    return null;
                }
            }
            return voicedLines;
        }
        
        static void Disable() {
            mod.OnUpdate = null;
            PlayerManager.CarChanged -= OnCarChanged;
        }
        
        static void OnCarChanged(TrainCar car) {
            //ReadJobOverview(JobsManager.Instance.currentJobs.First());
        }

        static void OnCarClicked(TrainCar car) {
            var job = JobsManager.Instance.GetJobOfCar(car.logicCar);
            if (job != null && lastClickedCarId != car.ID && JobsManager.Instance.currentJobs.Count > 0) {
                TerseCommentOnCarJob(job);
                lastClickedCarId = car.ID;
            } else {
                DetailedCommentOnCar(car, job);
                lastClickedCarId = "";
            }
        }

        private static void TerseCommentOnCarJob(Job job) {
            var lineBuilder = new List<string>();
            var isPartOfYourJob = JobsManager.Instance.currentJobs.Contains(job);
            if (isPartOfYourJob) {
                lineBuilder.Add(Randomizer.GetRandomLine("CarInJob", 1, 3));
            } else {
                lineBuilder.Add(Randomizer.GetRandomLine("CarNotInJob", 1, 3));
            }
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        private static void DetailedCommentOnCar(TrainCar car, Job job) {
            var lineBuilder = new List<string>();
            lineBuilder.AddRange(VoicedCarNumber(car.ID));

            switch (job?.jobType) {
                case null:
                    lineBuilder.Add("NotPartOfAnyOrder");
                    break;
                case JobType.ShuntingLoad:
                    lineBuilder.Add("WaitingForLoading");
                    break;
                case JobType.ShuntingUnload:
                    lineBuilder.Add("WaitingForUnloading");
                    break;
                case JobType.Transport:
                    var transportJobData = JobDataExtractor.ExtractTransportJobData(new Job_data(job));
                    lineBuilder.Add("BoundFor");
                    lineBuilder.Add(GetYardName(transportJobData.destinationTrack));
                    break;
                case JobType.EmptyHaul:
                    var haulJobData = JobDataExtractor.ExtractEmptyHaulJobData(new Job_data(job));
                    lineBuilder.Add("BoundFor");
                    lineBuilder.Add(GetYardName(haulJobData.destinationTrack));
                    break;
                default:
                    lineBuilder.Add("PartOf");
                    lineBuilder.Add("JobType" + job.jobType);
                    break;
            }
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        static void OnNothingClicked() {
            ReadAllJobsOverview();
        }

        static void OnSessionStart(UnityModManager.ModEntry modEntry) {
            commsRadio = Object.FindObjectOfType<CommsRadioController>();
        }
        
        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt) {
            if (Input.GetKeyDown(KeyCode.L)) {
                CommsRadioNarrator.PlayWithClick(testVoiceLines);
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                var lineBuilder = new List<string>();
                AddGenericJobLines(lineBuilder, JobsManager.Instance.currentJobs.First());
                CommsRadioNarrator.PlayWithClick(lineBuilder);
            }

            if (Input.GetKeyDown(KeyCode.O)) {
                ReadJobOverview(JobsManager.Instance.currentJobs.First());
            }
            
            if (JobsManager.Instance != null) {
                foreach (var job in JobsManager.Instance.currentJobs) {
                    if (!readJobs.Contains(job.ID) && !CommsRadioNarrator.currentlyReading) {
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
        
        private static void ReadFirstJobOverview() {
            if (JobsManager.Instance == null || JobsManager.Instance.currentJobs == null || JobsManager.Instance.currentJobs.Count == 0) {
                mod.Logger.Error("No jobs available to read.");
                return;
            }
            var firstJob = JobsManager.Instance.currentJobs.FirstOrDefault();
            if (firstJob != null) {
                ReadJobOverview(firstJob);
            } else {
                mod.Logger.Error("No valid job found to read.");
            }
        }
        
        private static void ReadAllJobsOverview() {
            var jobs = JobsManager.Instance?.currentJobs ?? new List<Job>();
            if (jobs.Count == 1) {
                ReadJobOverview(jobs[0]);
                return;
            }
            var lineBuilder = new List<string>();
            lineBuilder.Add("YouHave");
            lineBuilder.Add(jobs.Count.ToString());
            lineBuilder.Add("Orders");
            lineBuilder.Add(SHORT_SILENCE);
            
            foreach (var job in jobs) {
                AddJobSpecificLines(lineBuilder, job);
                lineBuilder.Add(SHORT_SILENCE);
                lineBuilder.Add(SHORT_SILENCE);
            }
            
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        private static void ReadJobOverview(Job job) {
            var lineBuilder = new List<string>();
            lineBuilder.Add("YouHave");
            AddJobSpecificLines(lineBuilder, job);
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        private static string[] VoicedTrackId(TrackID trackId) {
            // TODO: Some types have two letters, like "SP" for storage passenger track.
            return trackId != null ? SeparateIntoLetters(trackId.TrackPartOnly.Substring(0,2)) :
                new[] { "Unknown", "Track" };
        }
        
        private static string[] VoicedCarNumber(string carId) {
            if (carId == null) {
                return new[] { "Unknown", "Car" };
            }
            return SeparateIntoLetters(carId.Substring(carId.Length - 3, 3));
        }
        
        private static string GetTrackTypeLetter(TrackID trackId) {
            if (trackId == null) {
                return "M";
            }
            return trackId.FullID.Split('-').Last();
        }
        
        private static string GetYardName(TrackID trackId) {
            if (trackId == null) {
                return "Unknown Yard";
            }
            return "Yard" + trackId.yardId;
        }

        public static string[] SeparateIntoLetters(string text) {
            return text.ToCharArray().Select(c => c.ToString()).ToArray();
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
                case JobType.Transport:
                    AddTransportJobLines(lineBuilder, job);
                    break;
                case JobType.EmptyHaul:
                    AddEmptyHaulJobLines(lineBuilder, job);
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
            
            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add(SHORT_SILENCE);
            
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
                lineBuilder.Add(SHORT_SILENCE);
            }

            lineBuilder.Add("ThenMove");
            lineBuilder.Add(jobInfo.allCarsToLoad.Count+"Cars");
            lineBuilder.Add("ToTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.loadMachineTrack));
            lineBuilder.Add("ForLoading");
            lineBuilder.Add(SHORT_SILENCE);
            
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
            
            lineBuilder.Add("JobType" + job.jobType);
            lineBuilder.Add(SHORT_SILENCE);
            
            lineBuilder.Add("PickUp");
            lineBuilder.Add(jobInfo.allCarsToUnload.Count+"Cars");
            lineBuilder.Add("AtTrackType" + GetTrackTypeLetter(jobInfo.startingTrack));
            lineBuilder.AddRange(VoicedTrackId(jobInfo.startingTrack));
            lineBuilder.Add(SHORT_SILENCE);
            
            lineBuilder.Add("ThenUnloadThoseCars");
            lineBuilder.Add("AtTrack");
            lineBuilder.AddRange(VoicedTrackId(jobInfo.unloadMachineTrack));
            lineBuilder.Add(SHORT_SILENCE);
            
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
                    lineBuilder.Add(SHORT_SILENCE);
                }
            }
            lineBuilder.Add("ToCompleteTheOrder");
        }
        
        private static void AddTransportJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                mod.Logger.Error("Invalid job or no tasks found.");
                return;
            }
            var jobInfo = JobDataExtractor.ExtractTransportJobData(new Job_data(job));
            AddBasicTransportJobLines(lineBuilder, job.jobType, jobInfo.transportingCars, jobInfo.startingTrack, jobInfo.destinationTrack);
        }
        
        private static void AddEmptyHaulJobLines(List<string> lineBuilder, Job job) {
            if (job == null || job.tasks == null || job.tasks.Count == 0) {
                mod.Logger.Error("Invalid job or no tasks found.");
                return;
            }
            var jobInfo = JobDataExtractor.ExtractEmptyHaulJobData(new Job_data(job));
            AddBasicTransportJobLines(lineBuilder, job.jobType, jobInfo.transportingCars, jobInfo.startingTrack, jobInfo.destinationTrack);
        }

        private static void AddBasicTransportJobLines(List<string> lineBuilder, JobType jobType, List<Car_data> transportingCars,
            TrackID startingTrack, TrackID destinationTrack) {
            lineBuilder.Add("JobType" + jobType);
            lineBuilder.Add(SHORT_SILENCE);

            lineBuilder.Add("PickUp");
            lineBuilder.Add(transportingCars.Count+"Cars");
            lineBuilder.Add("FromTrack");
            lineBuilder.AddRange(VoicedTrackId(startingTrack));
            lineBuilder.Add("In");
            lineBuilder.Add(GetYardName(startingTrack));
            lineBuilder.Add(SHORT_SILENCE);

            lineBuilder.Add("ThenDropOffThoseCars");
            lineBuilder.Add("AtTrack");
            lineBuilder.AddRange(VoicedTrackId(destinationTrack));
            lineBuilder.Add("In");
            lineBuilder.Add(GetYardName(destinationTrack));
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

        /** Never return the same line for the same id in a row. */
        static class Randomizer {
            // Map of id to last selected index
            private static readonly Dictionary<string, int> LastSelectedIndexMap = new();
            
            public static string GetRandomLine(string id, int startIndex, int endIndex) {
                int lastIndex = LastSelectedIndexMap.ContainsKey(id) ? LastSelectedIndexMap[id] : -1;
                int generatedIndex = startIndex-1;
                while (generatedIndex == lastIndex || generatedIndex < startIndex) {
                    generatedIndex = Random.Range(startIndex, endIndex + 1);
                }
                LastSelectedIndexMap[id] = generatedIndex;
                return $"{id}{generatedIndex}";
            }
        }
    }
}