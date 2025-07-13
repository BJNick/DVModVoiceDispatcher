using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DV.Logic.Job;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using static VoiceDispatcherMod.JobHelper;

namespace VoiceDispatcherMod {
    [EnableReloading]
    static class Main {
        public static bool enabled;

        public static UnityModManager.ModEntry.ModLogger Logger;

        private const string AssetBundleName = "voiced_lines";
        private static string _assetBundlePath;

        private static AssetBundle voicedLines;
        public static Settings settings;
        public static UnityModManager.ModEntry mod;

        private static List<string> readJobs = new();

        static bool Load(UnityModManager.ModEntry modEntry) {
            mod = modEntry;
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _assetBundlePath = Path.Combine(modEntry.Path, AssetBundleName);
            Logger = modEntry.Logger;

            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = Unload;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            
            VoiceGenerator.Init();
            JsonLinesLoader.Init( "D:\\Projects\\Mods\\DVVoiceAssistant\\ModLogicCode\\VoiceDispatcherMod\\lines.json");
            JsonLinesLoader.LogError = Logger.Error;

            return true;
        }

        static bool Unload(UnityModManager.ModEntry modEntry) {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.UnpatchAll(modEntry.Info.Id);
            if (enabled) {
                Disable(modEntry);
            }

            CommsRadioNarrator.UnpatchRadio();
            return true; // If true, the mod will be unloaded. If not, the mod will not be unloaded.
        }

        private static List<string> testVoiceLines = new() {
            "YouHave", "JobTypeShuntingUnload", "Move", "3Cars", "ToTrackTypeS", "D", "7", "Move", "1Cars",
            "ToTrackTypeI", "B", "1"
        };

        // Called when the mod is turned to on/off.
        // With this function you control an operation of the mod and inform users whether it is enabled or not.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* to active or deactivate */) {
            if (value) {
                Enable(modEntry); // Perform all necessary steps to start mod.
            } else {
                Disable(modEntry); // Perform all necessary steps to stop mod.
            }

            enabled = value;
            return true; // If true, the mod will switch the state. If not, the state will not change.
        }

        static void Enable(UnityModManager.ModEntry modEntry) {
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnSessionStart = OnSessionStart;
            TryLoadAssetBundle();

            PlayerManager.CarChanged += OnCarChanged;
            CommsRadioNarrator.OnCarClicked += OnCarClicked;
            StationHelper.OnYardEntered += OnYardEntered;
            StationHelper.OnYardExited += OnYardExited;
            StationHelper.OnStationEntered += OnStationEntered;
            CommsRadioNarrator.OnEnableMod();
        }

        static void Disable(UnityModManager.ModEntry modEntry) {
            modEntry.OnUpdate = null;
            modEntry.OnSessionStart = null;
            PlayerManager.CarChanged -= OnCarChanged;
            if (voicedLines) {
                voicedLines.Unload(true);
            }
            CommsRadioNarrator.OnDisableMod();
        }

        static void OnGUI(UnityModManager.ModEntry modEntry) {
            settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            settings.Save(modEntry);
        }

        private static bool TryLoadAssetBundle() {
            if (voicedLines) {
                return true;
            }
            try {
                Logger.Log("Loading voiced lines from: " + _assetBundlePath);
                voicedLines = AssetBundle.LoadFromFile(_assetBundlePath);
                if (voicedLines) return true;
                Logger.Warning("Failed to load voiced lines asset bundle. Attempting to load existing bundle.");
                voicedLines = AssetBundle.GetAllLoadedAssetBundles().First(it => it.name == AssetBundleName);
                if (voicedLines) return true;
                Logger.Error("No existing asset bundle found with name: " + AssetBundleName);
            } catch (Exception e) {
                Logger.Error("Failed to load asset bundle: " + e.Message);
            }

            return false;
        }

        public static AssetBundle GetVoiceLinesBundle() {
            return voicedLines;
        }

        static void OnCarChanged(TrainCar car) {
            //ReadJobOverview(JobsManager.Instance.currentJobs.First());
        }

        static void OnCarClicked(TrainCar car) {
            CarHelper.OnCarClicked(car);
        }

        static void OnYardEntered(StationController station) {
            if (station == null) {
                return;
            }

            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + station.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var line = StationHelper.CreateWelcomeToYardLine(station);
            Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        static void OnYardExited(StationController previousStation) {
            if (previousStation == null) {
                return;
            }

            if (RateLimiter.CannotYetPlay("YardEnterOrExit" + previousStation.stationInfo.YardID,
                    RateLimiter.Minutes(3))) {
                return;
            }

            var line = StationHelper.CreateExitingYardLine(previousStation);
            Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        static void OnStationEntered(StationController station) {
            if (station == null || station.logicStation?.availableJobs?.Count == 0) {
                return;
            }

            if (RateLimiter.CannotYetPlay("StationWelcome" + station.stationInfo.YardID, RateLimiter.Minutes(2))) {
                return;
            }

            var line = StationHelper.CreateWelcomeToStationOfficeLine(station);
            if (RateLimiter.CannotYetPlay("AutoHighestJobRead" + station.stationInfo.YardID, RateLimiter.Minutes(10))) {
                line += " " + StationHelper.CreateHighestPayingJobLine(station);
            }
            
            Logger.Log(line);
            CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
        }

        static void OnSessionStart(UnityModManager.ModEntry modEntry) {
            TryLoadAssetBundle();
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt) {
            StationHelper.OnUpdate();

            if (Input.GetKeyDown(KeyCode.L)) {
                CommsRadioNarrator.PlayWithClick(LineChain.FromAssetBundleLines(testVoiceLines));
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                var line = CreateGenericJobLine(JobsManager.Instance.currentJobs.First());
                Logger.Log(line);
                CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
            }

            if (Input.GetKeyDown(KeyCode.O)) {
                ReadJobOverview(JobsManager.Instance.currentJobs.First());
            }
            
            if (Input.GetKeyDown(KeyCode.Semicolon)) {
                List<Line> lines = new List<Line>();
                lines.Add(new PromptLine("This is"));
                lines.Add(new PromptLine("CFF 4 3 1"));
                lines.Add(new PromptLine("First line of a prompt"));
                lines.Add(new PromptLine("Second line of a prompt"));
                lines.Add(new PromptLine("Third line of a prompt"));
                lines.Add(new PromptLine("Final line of a prompt"));
                LineChain.AddNoiseClicks(lines);
                var coroutineRunner = new GameObject().AddComponent<CommsRadioNarrator.CoroutineRunner>();
                coroutineRunner.StartCoroutine(
                    LineChain.PlayLinesInCoroutine(lines, coroutineRunner));
            }

            if (JobsManager.Instance != null) {
                foreach (var job in JobsManager.Instance.currentJobs) {
                    if (!readJobs.Contains(job.ID) && !CommsRadioNarrator.currentlyReading) {
                        readJobs.Add(job.ID);
                        ReadJobOverview(job);
                        job.JobCompleted += PlayJobCompletedLine;
                    }
                }
            }

            SignHelper.CheckSpeedLimits();
        }
    }
}