using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DV;
using DV.Logic.Job;
using DV.Utils;
using HarmonyLib;
using UnityEngine;
using static VoiceDispatcherMod.CommsRadioNarrator;
using Task = System.Threading.Tasks.Task;

// Thank you Skin Manager for this beautiful code 
namespace VoiceDispatcherMod {
    public class CommsRadioNarrator : MonoBehaviour, ICommsRadioMode {
        private const float SIGNAL_RANGE = 300f;

        public static CommsRadioNarrator Instance;

        public static CommsRadioController Controller;

        public static NarratorLineQueue NarratorQueue = new();
        public const float delayBetweenLinesInQueue = 1f;

        public static AudioSource source;
        private static AudioManager audioManager;

        public static bool currentlyReading;
        private static CoroutineRunner coroutineRunner;
        private static Coroutine currentCoroutine;
        private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new(0.25f, 0.8f, 0f);
        private static readonly Color LASER_COLOR = new(1f, 0.5f, 0f);

        public CommsRadioDisplay display;
        public Transform signalOrigin;
        public Material selectionMaterial;
        public Material skinningMaterial;
        public GameObject trainHighlighter;

        // Sounds
        public AudioClip HoverCarSound;
        public AudioClip SelectedCarSound;
        public AudioClip ConfirmSound;
        public AudioClip CancelSound;

        private State CurrentState;

        //private CustomPaintTheme SelectedSkin = null;
        private (string exterior, string interior) CurrentThemeName;
        private readonly bool HasInterior = false;
        private MeshRenderer HighlighterRender;
        private RaycastHit Hit;
        private TrainCar PointedCar;

        private RadioMenuList menuList;
        private List<ActionItem> mainMenuActions;
        private List<ActionItem> settingsActions;

        private LayerMask TrainCarMask;

        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        public Color GetLaserBeamColor() {
            return LASER_COLOR;
        }

        public void OverrideSignalOrigin(Transform signalOrigin) {
            this.signalOrigin = signalOrigin;
        }

        public static event Action<TrainCar> OnCarClicked;
        public static event Action OnNothingClicked;

        public class CoroutineRunner : MonoBehaviour { }

        protected enum State {
            MainView,
            SelectActions,
            ChangeSettings,
        }

        #region Initialization

        public void Awake() {
            Instance = this;
            // steal components from other radio modes
            var deleter = Controller.deleteControl;

            if (deleter) {
                signalOrigin = deleter.signalOrigin;
                display = deleter.display;
                selectionMaterial = new Material(deleter.selectionMaterial);
                skinningMaterial = new Material(deleter.deleteMaterial);
                trainHighlighter = deleter.trainHighlighter;

                // sounds
                HoverCarSound = deleter.hoverOverCar;
                SelectedCarSound = deleter.warningSound;
                ConfirmSound = deleter.confirmSound;
                CancelSound = deleter.cancelSound;
            } else {
                Debug.LogError("CommsRadioNarrator: couldn't get properties from siblings");
            }

            SetUpSource();
        }

        private static void SetUpSource() {
            if (!source) {
                var sourceObject = new GameObject("CommsRadioNarratorAudioSource");
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.minDistance = 0.5f;
            source.maxDistance = 10f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.volume = 1;
            try {
                audioManager = SingletonBehaviour<AudioManager>.Instance;
                if (!audioManager) {
                    return;
                }

                var boomboxGroups = audioManager.mix?.FindMatchingGroups("Boombox");
                source.outputAudioMixerGroup =
                    boomboxGroups?.Length > 0 ? boomboxGroups.First() : audioManager.cabGroup;
            } catch (Exception e) {
                Main.Logger.Error($"CommsRadioNarrator: Failed to set audio mixer group: {e.Message}");
            }
        }

        public void Start() {
            if (!signalOrigin) {
                Debug.LogError("CommsRadioNarrator: signalOrigin on isn't set, using this.transform!", this);
                signalOrigin = transform;
            }

            if (display == null) Debug.LogError("CommsRadioNarrator: display not set, can't function properly!", this);

            if (selectionMaterial == null || skinningMaterial == null)
                Debug.LogError("CommsRadioNarrator: Selection material(s) not set. Visuals won't be correct.", this);

            if (trainHighlighter == null)
                Debug.LogError("CommsRadioNarrator: trainHighlighter not set, can't function properly!!", this);

            if (HoverCarSound == null || SelectedCarSound == null || ConfirmSound == null || CancelSound == null)
                Debug.LogError("Not all audio clips set, some sounds won't be played!", this);

            TrainCarMask = LayerMask.GetMask("Train_Big_Collider");

            HighlighterRender = trainHighlighter.GetComponentInChildren<MeshRenderer>(true);
            trainHighlighter.SetActive(false);
            trainHighlighter.transform.SetParent(null);

            menuList = new RadioMenuList(display);
            
            mainMenuActions = new() {
                new ActionItem("Cancel", () => {
                    CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                    SetState(State.MainView);
                }),
                new ActionItem("What's my order?", () => {
                    CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform); 
                    JobHelper.ReadAllJobsOverview();
                    SetState(State.MainView);
                }),
                new ActionItem("Highest job here?", () => {
                    if (StationHelper.playerYard == null) {
                        CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                        return;
                    }
                    CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    var line = StationHelper.CreateHighestPayingJobLine(StationHelper.playerYard);
                    Main.Logger.Log(line);
                    GenerateAndPlay(line);
                    SetState(State.MainView);
                }),
                new ActionItem("Edit Settings", () => {
                    CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    SetState(State.ChangeSettings);
                }),
            };

            void SetVolume(int newVolume) {
                CutCoroutineShort();
                Main.settings.Volume = Mathf.Clamp(newVolume, 1, 10);
                Main.settings.Save(Main.mod);
                Play(new PromptLine(Main.settings.Volume.ToString()));
                menuList.RenderActions();
            }
            settingsActions = new() {
                new ActionItem("Back", () => {
                    CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                    SetState(State.SelectActions);
                }),
                new ActionItem("Increase volume", () => { SetVolume(Main.settings.Volume + 1); },
                    () => $"Increase vol {Main.settings.Volume}"),
                new ActionItem("Decrease volume", () => { SetVolume(Main.settings.Volume - 1); },
                    () => $"Decrease vol {Main.settings.Volume}"),
            };
        }

        public void Enable() { }

        public void Disable() {
            ResetState();
        }

        public void SetStartingDisplay() {
            display.SetDisplay("Narrator", "Ongoing order available", "Check");
        }

        #endregion

        #region Car Highlighting

        private void HighlightCar(TrainCar car, Material highlightMaterial) {
            if (car == null) {
                Debug.LogError("Highlight car is null. Ignoring request.");
                return;
            }

            HighlighterRender.material = highlightMaterial;

            trainHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
            var b = car.transform.up * (trainHighlighter.transform.localScale.y / 2f);
            var b2 = car.transform.forward * car.Bounds.center.z;
            var position = car.transform.position + b + b2;

            trainHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
            trainHighlighter.SetActive(true);
            trainHighlighter.transform.SetParent(car.transform, true);
        }

        private void ClearHighlightedCar() {
            trainHighlighter.SetActive(false);
            trainHighlighter.transform.SetParent(null);
        }

        private void PointToCar(TrainCar car) {
            if (car != null) {
                if (PointedCar != car) {
                    HighlightCar(car, selectionMaterial);
                    CommsRadioController.PlayAudioFromRadio(HoverCarSound, transform);
                }

                PointedCar = car;
                display.SetContentAndAction("What's this car?", "Ask");
            } else {
                PointedCar = null;
                ClearHighlightedCar();

                if (currentlyReading) {
                    // Split camelcase with spaces
                    string subtitle = Regex.Replace(source.clip.name, "(?<=[a-z])([A-Z])", " $1").Trim();
                    display.SetContentAndAction("Speaking...\n" + subtitle, "Interrupt");
                    return;
                }

                display.SetContentAndAction("Open menu?");
            }
        }

        #endregion

        #region State Machine Actions

        private void SetState(State newState) {
            if (newState == CurrentState) return;

            CurrentState = newState;
            switch (newState) {
                case State.MainView:
                    menuList.ClearActions();
                    SetStartingDisplay();
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    break;

                case State.SelectActions:
                    menuList.SetAvailableActions(mainMenuActions);
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;
                
                case State.ChangeSettings:
                    menuList.SetAvailableActions(settingsActions);
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;
            }
        }

        private void ResetState() {
            PointedCar = null;

            ClearHighlightedCar();

            SetState(State.MainView);
        }

        public void OnUpdate() {
            MoveSourceIntoPosition();

            TrainCar trainCar;

            switch (CurrentState) {
                case State.MainView:
                    // Check if not pointing at anything
                    if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE,
                            TrainCarMask)) {
                        PointToCar(null);
                    } else {
                        // Try to get the train car we're pointing at
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        if (!trainCar.IsLoco) {
                            PointToCar(trainCar);
                        } else {
                            PointToCar(null);
                        }
                    }

                    break;
            }
        }

        public void OnUse() {
            if (menuList.IsOpen) {
                menuList.OnUse();
                return;
            }
            
            switch (CurrentState) {
                case State.MainView:

                    if (PointedCar != null) {
                        if (currentlyReading) {
                            CutCoroutineShort(false);
                        }

                        OnCarClicked?.Invoke(PointedCar);
                        PointToCar(null);
                    } else {
                        if (currentlyReading) {
                            CutCoroutineShort();
                            return;
                        }

                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                        SetState(State.SelectActions);
                    }

                    break;
            }
        }

        public bool ButtonACustomAction() {
            if (menuList.IsOpen) {
                return menuList.ButtonACustomAction();
            }

            return false;
        }

        public bool ButtonBCustomAction() {
            if (menuList.IsOpen) {
                return menuList.ButtonBCustomAction();
            }

            return true;
        }

        #endregion

        #region Coroutine Management

        public static void PlayWithClick(List<Line> lineBuilder) {
            Play(lineBuilder, true);
        }
        
        public static void Play(Line line, bool withClick = false) {
            Play(new List<Line> { line }, withClick);
        }

        public static void Play(List<Line> lineBuilder, bool withClick = false) {
            if (currentlyReading) {
                Main.Logger.Warning("Already reading a voice line, queuing this one.");
                NarratorQueue.Enqueue(lineBuilder);
                return;
            }

            if (!Instance) {
                Main.Logger.Warning("No CommsRadioNarrator instance found.");
            }

            currentlyReading = true;
            Main.Logger.Log("Generated voice line: " + string.Join(" ", lineBuilder));
            if (withClick) {
                LineChain.AddNoiseClicks(lineBuilder);
            }

            SetupCoroutineRunner();
            currentCoroutine = coroutineRunner.StartCoroutine(LineChain.PlayLinesInCoroutine(lineBuilder, coroutineRunner));
        }

        private static IEnumerator PlayVoiceLinesCoroutine(string[] lines) {
            var clips = lines.Select(GetVoicedClip).Where(clip => clip != null).ToArray();
            return PlayClipsInCoroutine(clips);
        }
        
        public static async Task GenerateAndPlay(string line, bool withClick = true) {
            Main.Logger.Log("Ignoring voice line: " + line);
            return;
            
            var clip = await VoiceGenerator.CreateClip(line);
            
            var clipList = new List<AudioClip> { clip };
            if (withClick) {
                clipList.Insert(0, GetVoicedClip("NoiseClick"));
                clipList.Add(GetVoicedClip("NoiseClick"));
            }
            
            /*if (currentlyReading) {
                // TODO
                Main.Logger.Warning("Already reading a voice line, CANNOT QUEUE this one.");
                return;
            }*/

            currentlyReading = true;
            SetupCoroutineRunner();
            currentCoroutine = coroutineRunner.StartCoroutine(PlayClipsInCoroutine(clipList.ToArray()));
        }

        private static IEnumerator PlayClipsInCoroutine(AudioClip[] clips) {
            if (clips.Length == 0) {
                Main.Logger.Error("No valid voice lines found.");
                yield break;
            }

            clips[0].LoadAudioData();
            foreach (var clip in clips) {
                PlayRadioClip(clip);
                // Sleep until the next clip is ready to play
                var waitTime = clip.length - 0.05f;
                yield return new WaitForSeconds(waitTime);
            }

            if (!NarratorQueue.HasLines()) {
                currentlyReading = false;
            } else {
                Main.Logger.Log("Waiting to play next queued voice line.");
                yield return new WaitForSeconds(delayBetweenLinesInQueue);
                currentlyReading = false;
                PlayWithClick(NarratorQueue.Dequeue());
            }
        }

        private static void CutCoroutineShort(bool playClick = true) {
            if (currentCoroutine != null && currentlyReading) {
                if (coroutineRunner != null) coroutineRunner.StopCoroutine(currentCoroutine);
                currentlyReading = false;
                if (playClick) {
                    PlayRadioClip(GetVoicedClip("NoiseClick"));
                } else {
                    source.Stop();
                }

                Main.Logger.Log("Narrator line cut short.");
            } else {
                currentlyReading = false;
            }
        }

        public static void PlayRadioClip(AudioClip clip) {
            SetUpSource();
            MoveSourceIntoPosition();
            //clip.Play(playAt.position, volume: 1, minDistance: 1, maxDistance: 10f, parent: playAt, mixerGroup: SingletonBehaviour<AudioManager>.Instance.cabGroup);
            source.clip = clip;
            source.Play();
        }

        public static AudioClip GetVoicedClip(string name) {
            var voicedLines = Main.GetVoiceLinesBundle();
            if (!voicedLines) {
                Main.Logger.Error("Voiced lines asset bundle is not loaded.");
                return null;
            }

            var clip = voicedLines.LoadAsset<AudioClip>(name);
            if (!clip) Main.Logger.Error("Failed to load voice line: " + name);
            return clip;
        }

        private static void SetupCoroutineRunner() {
            if (!coroutineRunner)
                coroutineRunner = new GameObject("NarratorCoroutineRunner").AddComponent<CoroutineRunner>();
        }

        #endregion
        
        public static void MoveSourceIntoPosition() {
            var radio = Instance;
            var playAt = PlayerManager.PlayerTransform;
            if (radio && radio.isActiveAndEnabled) {
                playAt = radio.transform;
                var distanceFromListener = Vector3.Distance(playAt.position, Camera.main.transform.transform.position);
                // Decrease spacial blend to 0 at distance 0.4 and lower, increase to 1 at distance 0.8 and beyond
                source.volume = 1 * (Main.settings.Volume / 10f);
                source.spatialBlend = Mathf.Clamp01((distanceFromListener - 0.4f) / 0.4f);
            } else {
                if (!playAt) playAt = Camera.main.transform;
                // in inventory
                source.volume = 0.75f * (Main.settings.Volume / 10f);
                source.spatialBlend = 0;
            }

            source.transform.position = playAt.position;
            source.transform.rotation = playAt.rotation;
        }

        public static void PatchRadioAfterAwake(CommsRadioController __instance = null) {
            if (Time.time < 2f || Controller) {
                // Awake should have worked by now
                return;
            }

            // Otherwise patch manually
            Controller = __instance ?? FindObjectOfType<CommsRadioController>();
            if (!Controller) {
                Main.Logger.Error("CommsRadioNarrator: Could not find CommsRadioController in the scene.");
                return;
            }

            Instance = Controller.gameObject.AddComponent<CommsRadioNarrator>();
            Controller.allModes.Add(Instance);
            var radioNarratorIdx = Controller.allModes.IndexOf(Instance);
            Controller.disabledModeIndices.Remove(radioNarratorIdx);
            Main.Logger.Log("CommsRadioNarrator: Successfully patched after Awake.");
        }

        public static void UnpatchRadio() {
            var radioNarrator = Instance;
            if (Controller && radioNarrator) {
                var index = Controller.allModes.IndexOf(radioNarrator);
                if (index >= 0) {
                    Controller.disabledModeIndices.Remove(index);
                    Controller.allModes.Remove(radioNarrator);
                } else {
                    Main.Logger.Error("CommsRadioNarrator: Could not find itself in the radio modes list.");
                }
            }

            if (radioNarrator) {
                Destroy(radioNarrator);
            }

            Instance = null;
        }

        public static void OnEnableMod() {
            if (!Controller || !Instance) {
                Main.Logger.Error("CommsRadioNarrator: Controller or Instance is null on mod enable.");
                return;
            }

            var index = Controller.allModes.IndexOf(Instance);
            if (index >= 0) {
                Controller.disabledModeIndices.Remove(index);
            } else {
                Main.Logger.Error("CommsRadioNarrator: Could not find itself in the radio modes list on enable.");
            }
        }

        public static void OnDisableMod() {
            if (Controller && Instance) {
                var index = Controller.allModes.IndexOf(Instance);
                if (index >= 0) {
                    Controller.disabledModeIndices.Add(index);
                }

                Controller.SetMode(Controller.allModes[0]);

                currentlyReading = false;
                currentCoroutine = null;
                NarratorQueue.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(CommsRadioController))]
    internal static class CommsRadio_Awake_Patch {
        [HarmonyPatch(nameof(CommsRadioController.Awake))]
        [HarmonyPostfix]
        private static void AfterAwake(CommsRadioController __instance, List<ICommsRadioMode> ___allModes) {
            if (!Main.enabled) return;

            Controller = __instance;
            Instance = __instance.gameObject.AddComponent<CommsRadioNarrator>();

            //int paintModeIndex = ___allModes.IndexOf(__instance.carPaintjobControl);
            //___allModes[paintModeIndex] = RadioNarrator;
            ___allModes.Add(Instance);
            Main.Logger.Log("CommsRadioNarrator: Successfully patched during Awake.");
        }

        [HarmonyPatch(nameof(CommsRadioController.UpdateModesAvailability))]
        [HarmonyPostfix]
        private static void AfterUpdateModesAvailability(CommsRadioController __instance) {
            if (!Main.enabled) return;
            if (!Instance) return;
            var radioNarratorIdx = __instance.allModes.IndexOf(Instance);
            __instance.disabledModeIndices.Remove(radioNarratorIdx);
        }

        // Patch on enabled to ensure the radio is ready
        [HarmonyPatch(nameof(CommsRadioController.Update))]
        [HarmonyPostfix]
        private static void AfterUpdate(CommsRadioController __instance) {
            if (!Main.enabled) return;
            if (!Instance) {
                PatchRadioAfterAwake(__instance);
            }
        }
    }
}