using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV;
using DV.Logic.Job;
using DvMod.HeadsUpDisplay;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using static VoiceDispatcherMod.JobHelper;
using Object = UnityEngine.Object;

namespace VoiceDispatcherMod {
    static class Main {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static UnityModManager.ModEntry.ModLogger Logger;

        private static string assetBundlePath;
        private static AssetBundle voicedLines;

        private static CommsRadioController commsRadio;


        private static List<string> readJobs = new();

        static bool Load(UnityModManager.ModEntry modEntry) {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            Logger = modEntry.Logger;
            CommsRadioNarrator.mod = mod;

            assetBundlePath = Path.Combine(mod.Path, "voiced_lines");
            modEntry.OnToggle = OnToggle;
            return true;
        }

        private static List<string> testVoiceLines = new() {
            "YouHave", "JobTypeShuntingUnload", "Move", "3Cars", "ToTrackTypeS", "D", "7", "Move", "1Cars",
            "ToTrackTypeI", "B", "1"
        };

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* to active or deactivate */) {
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
            StationHelper.OnYardEntered += OnYardEntered;
            StationHelper.OnYardExited += OnYardExited;
            StationHelper.OnStationEntered += OnStationEntered;
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
            CarHelper.OnCarClicked(car);
        }

        static void OnNothingClicked() {
            ReadAllJobsOverview();
        }

        static void OnYardEntered(StationController station) {
            if (station == null) {
                return;
            }

            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + station.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var lineBuilder = new List<string>();
            StationHelper.AddWelcomeToYardMessage(lineBuilder, station);
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        static void OnYardExited(StationController previousStation) {
            if (previousStation == null) {
                return;
            }

            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + previousStation.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var lineBuilder = new List<string>();
            StationHelper.AddExitingYardMessage(lineBuilder, previousStation);
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        static void OnStationEntered(StationController station) {
            if (station == null) {
                return;
            }

            if (RateLimiter.CannotYetPlay("StationWelcome" + station.stationInfo.YardID, 
                    RateLimiter.Minutes(10))) {
                return;
            }

            var lineBuilder = new List<string>();
            StationHelper.AddWelcomeToStationMessage(lineBuilder, station);
            StationHelper.AddHighestPayingJob(lineBuilder, station);
            CommsRadioNarrator.PlayWithClick(lineBuilder);
        }

        static void OnSessionStart(UnityModManager.ModEntry modEntry) {
            commsRadio = Object.FindObjectOfType<CommsRadioController>();
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt) {
            StationHelper.OnUpdate();

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

            SignHelper.CheckSpeedLimits();
        }

    }
}