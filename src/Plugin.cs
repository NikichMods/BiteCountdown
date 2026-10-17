using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BiteCountdown
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.bitecountdown";
        public const string PluginName = "Bite Countdown";
        public const string PluginVersion = "1.0.6";

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const int RingWidth = 64;
        private const int RingHeight = 32;
        private const float RingStartScale = 1.55f;
        private const float RingEndScale = 0.22f;
        private const float BiteFadeDuration = 0.15f;
        private const float EndScaleStep = 0.02f;
        private const int MinimumEndScaleStep = 4;
        private const int MaximumEndScaleStep = 30;
        private const float FadeDurationStep = 0.05f;
        private const int MaximumFadeDurationStep = 10;
        private const int SpriteOffsetXPixels = -2;
        private const int SpriteOffsetYPixels = -2;
        private const float RingColorR = 0.60f;
        private const float RingColorG = 0.82f;
        private const float RingColorB = 0.92f;
        private const float MinimumAmbientColorFactor = 0.62f;

        internal static Plugin Instance;

        private Harmony _harmony;
        private Type _fishingGuiType;
        private FieldInfo _waitField;
        private FieldInfo _stateField;
        private FieldInfo _canTakeOutField;

        private Transform _cachedBobber;
        private GameObject _ringObject;
        private SpriteRenderer _ringRenderer;
        private Sprite _ringSprite;
        private Texture2D _ringTexture;
        private Coroutine _ringCoroutine;
        private float _ringInitialWait;
        private bool _ringFading;
        private int _ringGeneration;
        private float _ambientColorFactor = 1f;
        private bool _runtimeFailed;
        private ConfigEntry<int> _endSizeStep;
        private ConfigEntry<int> _fadeDurationStep;
        private bool _showCalibration;
        private GUIStyle _calibrationTextStyle;

        private void Awake()
        {
            Instance = this;

            try
            {
                _endSizeStep = Config.Bind(
                    "TemporaryCalibration", "EndSizeStep", 11,
                    new ConfigDescription(
                        "Final ring size = step * 0.02. Open F3, adjust with F4/F5.",
                        new AcceptableValueRange<int>(MinimumEndScaleStep, MaximumEndScaleStep)));
                _fadeDurationStep = Config.Bind(
                    "TemporaryCalibration", "FadeDurationStep", 3,
                    new ConfigDescription(
                        "Bite fade seconds = step * 0.05. Open F3, adjust with F6/F7. Zero disables the tail.",
                        new AcceptableValueRange<int>(0, MaximumFadeDurationStep)));

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                    throw new InvalidOperationException("FishingGUI type was not found.");

                _waitField = _fishingGuiType.GetField(
                    "_waiting_for_bite_delay",
                    AllInstance);
                _stateField = _fishingGuiType.GetField("_state", AllInstance);
                _canTakeOutField = _fishingGuiType.GetField(
                    "can_take_out",
                    AllInstance);

                if (_waitField == null
                    || _stateField == null
                    || _canTakeOutField == null)
                {
                    throw new InvalidOperationException(
                        "Expected FishingGUI fields were not found.");
                }

                _harmony = new Harmony(PluginGuid);
                PatchThrowingAnimationExit();
                PatchChangeState();
            }
            catch (Exception ex)
            {
                Fail("initialization", ex);
            }
        }

        private void Update()
        {
            if (_runtimeFailed || _endSizeStep == null || _fadeDurationStep == null)
                return;

            if (Input.GetKeyDown(KeyCode.F3))
                _showCalibration = !_showCalibration;

            if (!_showCalibration)
                return;

            var sizeAdjustment = (Input.GetKeyDown(KeyCode.F5) ? 1 : 0)
                - (Input.GetKeyDown(KeyCode.F4) ? 1 : 0);
            var fadeAdjustment = (Input.GetKeyDown(KeyCode.F7) ? 1 : 0)
                - (Input.GetKeyDown(KeyCode.F6) ? 1 : 0);
            if (sizeAdjustment != 0)
            {
                _endSizeStep.Value = Mathf.Clamp(_endSizeStep.Value + sizeAdjustment,
                    MinimumEndScaleStep, MaximumEndScaleStep);
            }
            if (fadeAdjustment != 0)
            {
                _fadeDurationStep.Value = Mathf.Clamp(_fadeDurationStep.Value + fadeAdjustment,
                    0, MaximumFadeDurationStep);
            }
        }

        private void OnGUI()
        {
            if (!_showCalibration || _runtimeFailed || _endSizeStep == null || _fadeDurationStep == null)
                return;

            if (_calibrationTextStyle == null)
            {
                _calibrationTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                _calibrationTextStyle.normal.textColor = Color.white;
            }

            GUI.Box(new Rect(16f, 16f, 480f, 218f), GUIContent.none);
            GUI.Label(new Rect(30f, 26f, 452f, 198f),
                "Bite Countdown " + PluginVersion + " — калибровка\n"
                    + "Конечный размер: " + FormatSetting(GetEndScale()) + " (шаг 0.02)\n"
                    + "F4 — меньше    F5 — больше\n"
                    + "Затухание: " + FormatSetting(GetFadeDuration()) + " с (шаг 0.05 с)\n"
                    + "F6 — короче    F7 — дольше\n"
                    + "F3 — скрыть / показать. Значения сохраняются.\n"
                    + "Размер: 0.08–0.60. Затухание: 0–0.50 с.\n"
                    + "0 с — мгновенное исчезновение.\n"
                    + "Затухание фиксируется при начале поклёвки.",
                _calibrationTextStyle);
        }

        private static string FormatSetting(float value)
        {
            return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private float GetEndScale()
        {
            return _endSizeStep == null ? RingEndScale
                : Mathf.Clamp(_endSizeStep.Value, MinimumEndScaleStep, MaximumEndScaleStep) * EndScaleStep;
        }

        private float GetFadeDuration()
        {
            return _fadeDurationStep == null ? BiteFadeDuration
                : Mathf.Clamp(_fadeDurationStep.Value, 0, MaximumFadeDurationStep) * FadeDurationStep;
        }

        private void OnDestroy()
        {
            StopRing();

            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch
            {
            }

            if (_ringObject != null)
                Destroy(_ringObject);

            if (_ringSprite != null)
                Destroy(_ringSprite);

            if (_ringTexture != null)
                Destroy(_ringTexture);

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        private void PatchThrowingAnimationExit()
        {
            var type = AccessTools.TypeByName("FishingThrowingAnim");
            if (type == null)
                throw new InvalidOperationException(
                    "FishingThrowingAnim type was not found.");

            var target = type.GetMethods(AllInstance)
                .FirstOrDefault(m =>
                    m.DeclaringType == type
                    && m.Name == "OnStateExit"
                    && m.GetParameters().Length == 3);

            if (target == null)
            {
                throw new MissingMethodException(
                    "FishingThrowingAnim.OnStateExit override was not found.");
            }

            var postfix = typeof(Plugin).GetMethod(
                nameof(ThrowingOnStateExitPostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private void PatchChangeState()
        {
            var target = _fishingGuiType.GetMethods(AllInstance)
                .FirstOrDefault(m =>
                    m.DeclaringType == _fishingGuiType
                    && m.Name == "ChangeState"
                    && m.GetParameters().Length == 1);

            if (target == null)
                throw new MissingMethodException("FishingGUI.ChangeState was not found.");

            var postfix = typeof(Plugin).GetMethod(
                nameof(ChangeStatePostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void ThrowingOnStateExitPostfix()
        {
            var self = Instance;
            if (self == null || self._runtimeFailed)
                return;

            try
            {
                var fishing = self.GetCurrentFishingGui();
                if (fishing == null
                    || !self.IsState(fishing, "WaitingForBite")
                    || !ReadBool(self._canTakeOutField, fishing))
                {
                    return;
                }

                self.StartCoroutine(self.StartRingAfterIdleFrame(fishing));
            }
            catch (Exception ex)
            {
                self.Fail("throw lifecycle", ex);
            }
        }

        private static void ChangeStatePostfix(object __instance)
        {
            var self = Instance;
            if (self == null || self._runtimeFailed || __instance == null)
                return;

            try
            {
                var state = self.ReadState(__instance);

                if (string.Equals(
                    state,
                    "WaitingForBite",
                    StringComparison.Ordinal))
                {
                    self.StopRing();
                    // Initial casts enter WaitingForBite before the throw animation
                    // allows take-out. Missed hooks return here with can_take_out=true
                    // and no second throw animation, so re-arm from this native state.
                    if (ReadBool(self._canTakeOutField, __instance))
                    {
                        self.StartCoroutine(
                            self.StartRingAfterIdleFrame(__instance));
                    }

                    return;
                }

                if (string.Equals(
                    state,
                    "WaitingForPulling",
                    StringComparison.Ordinal))
                {
                    self.BeginBiteFade(__instance);
                    return;
                }

                self.StopRing();
            }
            catch (Exception ex)
            {
                self.Fail("fishing state lifecycle", ex);
            }
        }

        private IEnumerator StartRingAfterIdleFrame(object fishingGui)
        {
            yield return null;

            if (_runtimeFailed
                || !IsState(fishingGui, "WaitingForBite")
                || !ReadBool(_canTakeOutField, fishingGui))
            {
                yield break;
            }

            StartRing(fishingGui);
        }

        private void StartRing(object fishingGui)
        {
            StopRing();

            var initialWait = ReadFloat(_waitField, fishingGui);
            if (float.IsNaN(initialWait) || initialWait <= 0f)
                return;

            var bobber = FindBobber();
            if (bobber == null)
                return;

            var bobberRenderer = bobber.GetComponent<SpriteRenderer>();
            if (bobberRenderer == null || bobberRenderer.sprite == null)
                return;

            Vector2 anchor;
            if (!TryGetSpriteMeshCenter(bobberRenderer, out anchor))
                return;

            var anchorPpu = bobberRenderer.sprite.pixelsPerUnit;
            if (anchorPpu <= 0f)
                anchorPpu = 48f;

            // The residual correction belongs to the bobber sprite itself.
            // Keep it in sprite-local pixels and let the native bobber/player
            // transform mirror it with fishing direction.
            var spriteOffset = new Vector2(
                SpriteOffsetXPixels
                    * (bobberRenderer.flipX ? -1f : 1f)
                    / anchorPpu,
                SpriteOffsetYPixels
                    * (bobberRenderer.flipY ? -1f : 1f)
                    / anchorPpu);

            EnsureRingObject(
                bobber,
                bobberRenderer,
                anchor + spriteOffset);

            _ringGeneration++;
            var generation = _ringGeneration;

            _ambientColorFactor = 1f;
            _ringObject.SetActive(true);
            ApplyRingVisual(1f);

            StartCoroutine(CaptureAmbientColorFactor(generation));

            _ringInitialWait = initialWait;
            _ringCoroutine = StartCoroutine(
                AnimateRing(fishingGui, initialWait, generation));
        }

        private IEnumerator CaptureAmbientColorFactor(int generation)
        {
            // Keeper's Lantern (when present) applies its final ambient colour in
            // LateUpdate. Sampling at end-of-frame therefore observes the actual
            // scene ambient without any direct dependency on that mod. Vanilla
            // lighting follows the same generic RenderSettings path.
            yield return new WaitForEndOfFrame();

            if (_runtimeFailed
                || generation != _ringGeneration
                || _ringObject == null
                || !_ringObject.activeSelf)
            {
                yield break;
            }

            var ambientLuminance =
                Mathf.Clamp01(RenderSettings.ambientLight.grayscale);

            _ambientColorFactor = Mathf.Lerp(
                MinimumAmbientColorFactor,
                1f,
                ambientLuminance);
        }

        private IEnumerator AnimateRing(
            object fishingGui,
            float initialWait,
            int generation)
        {
            while (!_runtimeFailed
                && generation == _ringGeneration
                && IsState(fishingGui, "WaitingForBite"))
            {
                var remaining = ReadFloat(_waitField, fishingGui);
                if (float.IsNaN(remaining))
                {
                    StopRing();
                    yield break;
                }

                ApplyRingVisual(
                    Mathf.Clamp01(remaining / initialWait));

                yield return null;
            }

            if (generation == _ringGeneration)
                StopRing();
        }

        private void ApplyRingVisual(float remainingRatio)
        {
            if (_ringObject == null || _ringRenderer == null)
                return;

            var scale = Mathf.Lerp(
                GetEndScale(),
                RingStartScale,
                Mathf.Clamp01(remainingRatio));

            _ringObject.transform.localScale =
                new Vector3(scale, scale, 1f);

            var alpha = Mathf.Lerp(
                0.58f,
                0.34f,
                Mathf.Clamp01(remainingRatio));

            _ringRenderer.color = new Color(
                RingColorR * _ambientColorFactor,
                RingColorG * _ambientColorFactor,
                RingColorB * _ambientColorFactor,
                alpha);
        }

        private void BeginBiteFade(object fishingGui)
        {
            if (_ringFading)
                return;

            if (_ringCoroutine == null
                || _ringObject == null
                || !_ringObject.activeSelf
                || _ringRenderer == null)
            {
                StopRing();
                return;
            }

            var duration = GetFadeDuration();
            if (duration <= 0f)
            {
                StopRing();
                return;
            }

            // Transfer sole visual ownership before the wait coroutine can
            // observe the state exit and hide the ring.
            StopCoroutine(_ringCoroutine);
            _ringFading = true;
            ApplyRingVisual(0f);
            var endScale = GetEndScale();
            var shrinkSpeed = (RingStartScale - endScale) / _ringInitialWait;
            _ringCoroutine = StartCoroutine(
                FadeRingAfterBite(fishingGui, _ringGeneration, endScale, shrinkSpeed, duration));
        }

        private IEnumerator FadeRingAfterBite(
            object fishingGui, int generation, float endScale, float shrinkSpeed, float duration)
        {
            var initialColor = _ringRenderer.color;
            var startedAt = Time.time;

            while (!_runtimeFailed
                && generation == _ringGeneration
                && IsState(fishingGui, "WaitingForPulling")
                && _ringObject != null
                && _ringRenderer != null)
            {
                var elapsed = Mathf.Max(0f, Time.time - startedAt);
                var progress = Mathf.Clamp01(elapsed / duration);
                var scale = Mathf.Max(0f, endScale - shrinkSpeed * elapsed);
                _ringObject.transform.localScale = new Vector3(scale, scale, 1f);
                _ringRenderer.color = new Color(
                    initialColor.r,
                    initialColor.g,
                    initialColor.b,
                    initialColor.a * (1f - progress));

                if (progress >= 1f)
                    break;

                yield return null;
            }

            if (generation == _ringGeneration)
                StopRing();
        }

        private void StopRing()
        {
            _ringGeneration++;
            _ringFading = false;

            if (_ringCoroutine != null)
            {
                StopCoroutine(_ringCoroutine);
                _ringCoroutine = null;
            }

            if (_ringObject != null && _ringObject.activeSelf)
                _ringObject.SetActive(false);
        }

        private void EnsureRingObject(
            Transform bobber,
            SpriteRenderer bobberRenderer,
            Vector2 finalAnchor)
        {
            if (_ringObject == null)
            {
                _ringObject = new GameObject(
                    "BiteCountdown_CountdownRing");
                _ringRenderer = _ringObject.AddComponent<SpriteRenderer>();

                if (_ringSprite == null)
                {
                    var ppu = bobberRenderer.sprite.pixelsPerUnit;
                    if (ppu <= 0f)
                        ppu = 100f;

                    if (_ringTexture == null)
                        _ringTexture = CreateRingTexture();

                    _ringSprite = Sprite.Create(
                        _ringTexture,
                        new Rect(0f, 0f, RingWidth, RingHeight),
                        new Vector2(0.5f, 0.5f),
                        ppu,
                        0u,
                        SpriteMeshType.FullRect);

                    _ringSprite.name =
                        "BiteCountdown_CountdownRing";
                }

                _ringRenderer.sprite = _ringSprite;
                _ringRenderer.color =
                    new Color(RingColorR, RingColorG, RingColorB, 0.34f);
                _ringObject.SetActive(false);
            }

            if (_ringObject.transform.parent != bobber)
                _ringObject.transform.SetParent(bobber, false);

            _ringObject.transform.localPosition =
                new Vector3(finalAnchor.x, finalAnchor.y, 0f);
            _ringObject.transform.localRotation = Quaternion.identity;

            _ringRenderer.sortingLayerID =
                bobberRenderer.sortingLayerID;
            _ringRenderer.sortingOrder =
                bobberRenderer.sortingOrder - 1;
        }

        private static bool TryGetSpriteMeshCenter(
            SpriteRenderer renderer,
            out Vector2 center)
        {
            center = Vector2.zero;

            if (renderer == null || renderer.sprite == null)
                return false;

            var vertices = renderer.sprite.vertices;
            if (vertices == null || vertices.Length == 0)
                return false;

            var min = vertices[0];
            var max = vertices[0];

            for (var i = 1; i < vertices.Length; i++)
            {
                min = Vector2.Min(min, vertices[i]);
                max = Vector2.Max(max, vertices[i]);
            }

            center = (min + max) * 0.5f;

            if (renderer.flipX)
                center.x = -center.x;
            if (renderer.flipY)
                center.y = -center.y;

            return true;
        }

        private static Texture2D CreateRingTexture()
        {
            var texture = new Texture2D(
                RingWidth,
                RingHeight,
                TextureFormat.RGBA32,
                false);

            texture.name =
                "BiteCountdown_CountdownRing_Texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[RingWidth * RingHeight];
            var clear = new Color32(255, 255, 255, 0);
            var solid = new Color32(255, 255, 255, 255);

            for (var y = 0; y < RingHeight; y++)
            {
                for (var x = 0; x < RingWidth; x++)
                {
                    var nx = ((x + 0.5f) - RingWidth * 0.5f)
                        / (RingWidth * 0.5f - 1f);
                    var ny = ((y + 0.5f) - RingHeight * 0.5f)
                        / (RingHeight * 0.5f - 1f);

                    var distance = Mathf.Sqrt(nx * nx + ny * ny);

                    pixels[y * RingWidth + x] =
                        distance >= 0.84f && distance <= 1.00f
                            ? solid
                            : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private Transform FindBobber()
        {
            if (_cachedBobber != null)
                return _cachedBobber;

            var player = GameObject.Find("Player(Clone)");
            if (player == null)
                return null;

            _cachedBobber = player
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t =>
                    string.Equals(
                        t.name,
                        "bobber",
                        StringComparison.OrdinalIgnoreCase));

            return _cachedBobber;
        }

        private object GetCurrentFishingGui()
        {
            var type = AccessTools.TypeByName("GUIElements");
            if (type == null)
                return null;

            object me = null;

            var meField = type.GetField("me", AllStatic);
            if (meField != null)
                me = meField.GetValue(null);

            if (me == null)
            {
                var meProperty = type.GetProperty("me", AllStatic);
                if (meProperty != null)
                    me = meProperty.GetValue(null, null);
            }

            if (me == null)
                return null;

            var fishingField = type.GetField("fishing", AllInstance);
            if (fishingField != null)
                return fishingField.GetValue(me);

            var fishingProperty =
                type.GetProperty("fishing", AllInstance);

            return fishingProperty == null
                ? null
                : fishingProperty.GetValue(me, null);
        }

        private bool IsState(object fishingGui, string expected)
        {
            return string.Equals(
                ReadState(fishingGui),
                expected,
                StringComparison.Ordinal);
        }

        private string ReadState(object fishingGui)
        {
            if (_stateField == null || fishingGui == null)
                return "<unknown>";

            var value = _stateField.GetValue(fishingGui);
            return value == null ? "<null>" : value.ToString();
        }

        private static float ReadFloat(
            FieldInfo field,
            object instance)
        {
            if (field == null || instance == null)
                return float.NaN;

            try
            {
                return Convert.ToSingle(field.GetValue(instance));
            }
            catch
            {
                return float.NaN;
            }
        }

        private static bool ReadBool(
            FieldInfo field,
            object instance)
        {
            if (field == null || instance == null)
                return false;

            try
            {
                return Convert.ToBoolean(field.GetValue(instance));
            }
            catch
            {
                return false;
            }
        }

        private void Fail(string stage, Exception ex)
        {
            if (_runtimeFailed)
                return;

            _runtimeFailed = true;
            StopRing();

            Logger.LogError(
                "Bite Countdown disabled its countdown after "
                + stage
                + " failure: "
                + ex.GetType().Name
                + ": "
                + ex.Message);
        }
    }
}
