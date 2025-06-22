using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.Logic.Job;
using DV.Utils;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using static TestMod.CommsRadioNarrator;

// Thank you Skin Manager for this beautiful code 
namespace TestMod {
    public class CommsRadioNarrator : MonoBehaviour, ICommsRadioMode {
        private const float SIGNAL_RANGE = 300f;
        public static UnityModManager.ModEntry mod;

        public static CommsRadioNarrator Instance;

        public static CommsRadioController Controller;

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
        private TrainCar SelectedCar;

        //private PaintArea AreaToPaint = PaintArea.All;
        //private PaintArea AlreadyPainted = PaintArea.None;

        //private List<CustomPaintTheme> SkinsForCarType = null;
        private int SelectedSkinIdx = 0;
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

        public static void PlayRadioClip(AudioClip clip) {
            SetUpSource();
            MoveSourceIntoPosition();
            //clip.Play(playAt.position, volume: 1, minDistance: 1, maxDistance: 10f, parent: playAt, mixerGroup: SingletonBehaviour<AudioManager>.Instance.cabGroup);
            source.clip = clip;
            source.Play();
        }

        public static void MoveSourceIntoPosition() {
            var radio = Instance;
            var playAt = PlayerManager.PlayerTransform;
            if (radio && radio.isActiveAndEnabled) {
                playAt = radio.transform;
                var distanceFromListener = Vector3.Distance(playAt.position, Camera.main.transform.transform.position);
                // Decrease spacial blend to 0 at distance 0.4 and lower, increase to 1 at distance 0.8 and beyond
                source.volume = 1;
                source.spatialBlend = Mathf.Clamp01((distanceFromListener - 0.4f) / 0.4f);
            } else {
                if (!playAt) playAt = Camera.main.transform;
                // in inventory
                source.volume = 0.75f;
                source.spatialBlend = 0;
            }

            source.transform.position = playAt.position;
            source.transform.rotation = playAt.rotation;
        }

        public class CoroutineRunner : MonoBehaviour { }

        protected enum State {
            SelectCar,
            SelectSkin,
            SelectAreas
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
                var boomboxGroups = audioManager.mix.FindMatchingGroups("Boombox");
                source.outputAudioMixerGroup = boomboxGroups.Length > 0 ? boomboxGroups.First() : audioManager.cabGroup;
            } catch (Exception e) {
                mod.Logger.Error($"CommsRadioNarrator: Failed to set audio mixer group: {e.Message}");
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
                    display.SetContentAndAction("Speaking...\n" + source.clip.name, "Stop");
                    return;
                }

                var jobCount = JobsManager.Instance.currentJobs.Count;
                if (jobCount == 0)
                    display.SetContentAndAction("No ongoing orders", "Check");
                else if (jobCount == 1)
                    display.SetContentAndAction("1 ongoing order", "Check");
                else
                    display.SetContentAndAction(jobCount + " ongoing orders", "Check");
            }
        }

        #endregion

        #region State Machine Actions

        private void SetState(State newState) {
            if (newState == CurrentState) return;

            CurrentState = newState;
            switch (CurrentState) {
                case State.SelectCar:
                    SetStartingDisplay();
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    break;

                case State.SelectSkin:
                    //UpdateAvailableSkinsList(SelectedCar.carLivery);
                    //SetSelectedSkin(SkinsForCarType?.FirstOrDefault());
                    //CurrentThemeName = SkinManager.GetCurrentCarSkin(SelectedCar, false);

                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;

                case State.SelectAreas:
                    // AreaToPaint = PaintArea.All;
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;
            }
        }

        private void ResetState() {
            PointedCar = null;

            SelectedCar = null;
            ClearHighlightedCar();

            SetState(State.SelectCar);
        }

        public void OnUpdate() {
            MoveSourceIntoPosition();

            TrainCar trainCar;

            switch (CurrentState) {
                case State.SelectCar:
                    if (!(SelectedCar == null)) {
                        Debug.LogError("Invalid setup for current state, reseting flags!", this);
                        ResetState();
                        return;
                    }

                    // Check if not pointing at anything
                    if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE,
                            TrainCarMask)) {
                        PointToCar(null);
                    } else {
                        // Try to get the traincar we're pointing at
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        if (!trainCar.IsLoco) {
                            PointToCar(trainCar);
                        } else {
                            PointToCar(null);
                        }
                    }

                    break;

                case State.SelectSkin:
                    /*if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) &&
                        (trainCar = TrainCar.Resolve(Hit.transform.root)) && (trainCar == SelectedCar))
                    {
                        PointToCar(trainCar);

                        if (!HasInterior && !SkinProvider.IsBuiltInTheme(SelectedSkin) && (SelectedSkin.name == CurrentThemeName.exterior))
                        {
                            display.SetAction(Translations.ReloadAction);
                        }
                        else
                        {
                            display.SetAction(Translations.SelectAction);
                        }
                    }
                    else
                    {
                        PointToCar(null);
                        display.SetAction(Translations.CancelAction);
                    }*/

                    break;

                case State.SelectAreas:
                /*display.SetContent($"{Translations.SelectAreasPrompt}\n{AreaToPaintName}");

                if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) &&
                    (trainCar = TrainCar.Resolve(Hit.transform.root)) && (trainCar == SelectedCar))
                {
                    PointToCar(trainCar);

                    if ((AlreadyPainted == AreaToPaint) && !SkinProvider.IsBuiltInTheme(SelectedSkin))
                    {
                        display.SetAction(Translations.ReloadAction);
                    }
                    else
                    {
                        display.SetAction(Translations.ConfirmAction);
                    }
                }
                else
                {
                    PointToCar(null);
                    display.SetAction(Translations.CancelAction);
                }
                break;*/

                default:
                    ResetState();
                    break;
            }
        }

        /*private string AreaToPaintName
        {
            get
            {
                return AreaToPaint switch
                {
                    PaintArea.Exterior => CommsRadioLocalization.MODE_PAINTJOB_EXTERIOR,
                    PaintArea.Interior => CommsRadioLocalization.MODE_PAINTJOB_INTERIOR,
                    _ => CommsRadioLocalization.MODE_PAINTJOB_ALL,
                };
                ;
            }
        }*/

        public void OnUse() {
            switch (CurrentState) {
                case State.SelectCar:
                    /*if (PointedCar != null)
                    {
                        SelectedCar = PointedCar;
                        HasInterior = SelectedCar.GetComponents<TrainCarPaint>().Any(tcp => tcp.TargetArea == TrainCarPaint.Target.Interior);
                        PointedCar = null;

                        HighlightCar(SelectedCar, skinningMaterial);
                        CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
                        SetState(State.SelectSkin);
                    }*/

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
                        OnNothingClicked?.Invoke();
                    }

                    break;

                case State.SelectSkin:
                    if (PointedCar != null && PointedCar == SelectedCar) {
                        if (HasInterior) SetState(State.SelectAreas);

                        // for regular cars, skip area selection
                        /*if (!SkinProvider.IsBuiltInTheme(SelectedSkin) && (SelectedSkin.name == CurrentThemeName.exterior))
                            {
                                ReloadAndPrepareApplySelectedSkin();
                            }

                            ApplySelectedSkin();
                            ResetState();*/
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    } else {
                        // clicked off the selected car, this means cancel
                        CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                        ResetState();
                    }

                    break;

                case State.SelectAreas:
                    /*if ((PointedCar != null) && (PointedCar == SelectedCar))
                    {
                        // clicked on the selected car again, this means confirm
                        if ((AlreadyPainted == AreaToPaint) && !SkinProvider.IsBuiltInTheme(SelectedSkin))
                        {
                            ReloadAndPrepareApplySelectedSkin();
                        }

                        ApplySelectedSkin();
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    }*/

                    ResetState();
                    break;
            }
        }

        public bool ButtonACustomAction() {
            /*if (CurrentState == State.SelectSkin)
            {
                if ((SkinsForCarType == null) || (SkinsForCarType.Count == 0)) return false;

                SelectedSkinIdx -= 1;
                if (SelectedSkinIdx < 0) SelectedSkinIdx = SkinsForCarType.Count - 1;

                var selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else if (CurrentState == State.SelectAreas)
            {
                AreaToPaint -= 1;
                if (AreaToPaint == 0) AreaToPaint = PaintArea.All;
                return true;
            }
            else
            {
                Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                return false;
            }*/
            return true;
        }

        public bool ButtonBCustomAction() {
            /*if (CurrentState == State.SelectSkin)
            {
                if ((SkinsForCarType == null) || (SkinsForCarType.Count == 0)) return false;

                SelectedSkinIdx += 1;
                if (SelectedSkinIdx >= SkinsForCarType.Count) SelectedSkinIdx = 0;

                var selectedSkin = SkinsForCarType[SelectedSkinIdx];
                SetSelectedSkin(selectedSkin);
                return true;
            }
            else if (CurrentState == State.SelectAreas)
            {
                AreaToPaint += 1;
                if (AreaToPaint > PaintArea.All) AreaToPaint = PaintArea.Exterior;
                return true;
            }
            else
            {
                Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                return false;
            }*/
            return true;
        }

        #endregion

        #region Coroutine Management

        public static void PlayWithClick(List<string> lineBuilder) {
            if (currentlyReading) {
                mod.Logger.Warning("Already reading a voice line, skipping this one.");
                return;
            }

            currentlyReading = true;
            mod.Logger.Log("Generated voice line: " + string.Join(" ", lineBuilder));
            lineBuilder.Insert(0, "NoiseClick");
            lineBuilder.Add("NoiseClick");
            SetupCoroutineRunner();
            currentCoroutine = coroutineRunner.StartCoroutine(PlayVoiceLinesCoroutine(lineBuilder.ToArray()));
        }

        private static IEnumerator PlayVoiceLinesCoroutine(string[] lines) {
            var clips = lines.Select(GetVoicedClip).Where(clip => clip != null).ToArray();
            return PlayClipsInCoroutine(clips);
        }

        private static IEnumerator PlayClipsInCoroutine(AudioClip[] clips) {
            if (clips.Length == 0) {
                mod.Logger.Error("No valid voice lines found.");
                yield break;
            }

            clips[0].LoadAudioData();
            foreach (var clip in clips) {
                PlayRadioClip(clip);
                // Sleep until the next clip is ready to play
                var waitTime = clip.length - 0.05f;
                yield return new WaitForSeconds(waitTime);
            }

            currentlyReading = false;
        }

        private static void CutCoroutineShort(bool playClick = true) {
            if (currentCoroutine != null) {
                if (coroutineRunner != null) coroutineRunner.StopCoroutine(currentCoroutine);
                currentlyReading = false;
                if (playClick) {
                    PlayRadioClip(GetVoicedClip("NoiseClick"));
                } else {
                    source.Stop();
                }
                mod.Logger.Log("Narrator line cut short.");
            } else {
                mod.Logger.Warning("No narrator line playing to cut short.");
            }
        }

        private static AudioClip GetVoicedClip(string name) {
            var voicedLines = Main.GetVoiceLinesBundle();
            if (!voicedLines) {
                mod.Logger.Error("Voiced lines asset bundle is not loaded.");
                return null;
            }

            var clip = voicedLines.LoadAsset<AudioClip>(name);
            if (!clip) mod.Logger.Error("Failed to load voice line: " + name);
            return clip;
        }

        private static void SetupCoroutineRunner() {
            if (!coroutineRunner)
                coroutineRunner = new GameObject("NarratorCoroutineRunner").AddComponent<CoroutineRunner>();
        }

        #endregion
    }

    [HarmonyPatch(typeof(CommsRadioController))]
    internal static class CommsRadio_Awake_Patch {
        public static CommsRadioNarrator RadioNarrator;

        [HarmonyPatch(nameof(CommsRadioController.Awake))]
        [HarmonyPostfix]
        private static void AfterAwake(CommsRadioController __instance, List<ICommsRadioMode> ___allModes) {
            Controller = __instance;
            RadioNarrator = __instance.gameObject.AddComponent<CommsRadioNarrator>();

            //int paintModeIndex = ___allModes.IndexOf(__instance.carPaintjobControl);
            //___allModes[paintModeIndex] = RadioNarrator;
            ___allModes.Add(RadioNarrator);
        }

        [HarmonyPatch(nameof(CommsRadioController.UpdateModesAvailability))]
        [HarmonyPostfix]
        private static void AfterUpdateModesAvailability(CommsRadioController __instance) {
            var radioNarratorIdx = __instance.allModes.IndexOf(RadioNarrator);
            __instance.disabledModeIndices.Remove(radioNarratorIdx);
        }
    }
}