using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[assembly: System.Reflection.AssemblyTitle("New LvlUp Effect")]
[assembly: System.Reflection.AssemblyDescription("Minimal Nordic skill level notifications for Valheim")]
[assembly: System.Reflection.AssemblyCompany("Crevitka")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 Crevitka")]
[assembly: System.Reflection.AssemblyVersion("1.2.1.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.2.1.0")]

namespace NewLvlUpEffect
{
    [BepInPlugin(Id, "New LvlUp Effect", "1.2.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.valheim.newlvlupeffect";
        internal static Plugin Instance;
        internal ConfigEntry<bool> Enabled, ReplaceVanilla;
        internal ConfigEntry<float> Duration, VerticalPosition, Scale;
        private Harmony harmony;
        private LevelUpView view;
        private Player owner;
        private Hud ownerHud;
        internal void LogLayout(string message) { Logger.LogInfo(message); }

        private void Awake()
        {
            Instance = this;
            Enabled = Config.Bind("General", "Enabled", true, "Enable skill level notifications.");
            ReplaceVanilla = Config.Bind("General", "ReplaceVanillaMessage", true, "Replace only the vanilla skill-up text. Preserve game sound and effects.");
            Duration = Config.Bind("Appearance", "Duration", 3.6f, new ConfigDescription("Seconds per notification.", new AcceptableValueRange<float>(1.5f, 8f)));
            VerticalPosition = Config.Bind("Appearance", "VerticalPosition", .77f, new ConfigDescription("Vertical screen anchor: 0 bottom, 1 top.", new AcceptableValueRange<float>(.2f, .9f)));
            Scale = Config.Bind("Appearance", "Scale", 1f, new ConfigDescription("Notification scale.", new AcceptableValueRange<float>(.6f, 1.5f)));
            harmony = new Harmony(Id);
            harmony.PatchAll();
            new Terminal.ConsoleCommand("newlvlupeffect_preview", "Preview skill notification without changing skills.", (Terminal.ConsoleEvent)(args =>
            {
                if (Player.m_localPlayer != null && EnsureView())
                    view.Enqueue(Skills.SkillType.Run, 6, 5);
            }));
            Logger.LogInfo("New LvlUp Effect 1.2.1 ready (animated level replacement).");
        }

        private bool EnsureView()
        {
            if (!Enabled.Value || Player.m_localPlayer == null || Hud.instance == null) return false;
            if (view != null) return true;
            var fonts = Hud.instance.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t => t.font).Where(f => f != null).ToArray();
            var font = fonts.FirstOrDefault(f => f.name.IndexOf("Averia", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.IndexOf("Averia", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? fonts.FirstOrDefault();
            if (font == null) return false;
            var root = new GameObject("NewLvlUpEffect", typeof(RectTransform));
            // A nested Canvas inherits the HUD's rect and scaling. CanvasScaler
            // only drives root canvases, so keep this overlay at scene root.
            try
            {
                var candidate = root.AddComponent<LevelUpView>();
                candidate.Build(font, this);
                view = candidate;
            }
            catch { Destroy(root); throw; }
            owner = Player.m_localPlayer;
            ownerHud = Hud.instance;
            return true;
        }

        private void Update()
        {
            if (view != null && (!Enabled.Value || owner != Player.m_localPlayer || Player.m_localPlayer == null || ownerHud == null || ownerHud != Hud.instance))
            {
                Destroy(view.gameObject);
                view = null;
            }
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            if (view != null) Destroy(view.gameObject);
            // Remove our command so a plugin reload does not leave a stale delegate.
            var commands = AccessTools.Field(typeof(Terminal), "commands")?.GetValue(null) as Dictionary<string, Terminal.ConsoleCommand>;
            commands?.Remove("newlvlupeffect_preview");
            Instance = null;
        }

        [HarmonyPatch(typeof(Player), "Message")]
        private static class SkillMessagePatch
        {
            private static bool Prefix(Player __instance, string __1)
            {
                var plugin = Instance;
                if (plugin == null || !plugin.Enabled.Value || !plugin.ReplaceVanilla.Value ||
                    __instance != Player.m_localPlayer || __1 == null) return true;
                bool skillMessage = __1.StartsWith("$msg_skillup $skill_", StringComparison.Ordinal) ||
                    __1.StartsWith("Skill increased ", StringComparison.Ordinal) ||
                    __1.StartsWith("All skills increased by ", StringComparison.Ordinal);
                if (!skillMessage) return true;
                // CheatRaiseSkill emits its message before our postfix creates
                // the view, so initialise it here too. Keep vanilla on failure.
                try
                {
                    if (!plugin.EnsureView()) return true;
                    plugin.Logger.LogDebug("Suppressed vanilla skill HUD message: " + __1);
                    return false;
                }
                catch (Exception error)
                {
                    plugin.Logger.LogError(error);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "OnSkillLevelup")]
        private static class LevelPatch
        {
            private static void Postfix(Player __instance, Skills.SkillType __0, float __1)
            {
                var plugin = Instance;
                if (plugin == null || __instance != Player.m_localPlayer) return;
                try
                {
                    if (plugin.EnsureView()) plugin.view.Enqueue(__0, Mathf.FloorToInt(__1));
                }
                catch (Exception error) { plugin.Logger.LogError(error); }
            }
        }

        // CheatRaiseSkill bypasses OnSkillLevelup. The 'all' branch recursively
        // calls this method for each skill, so handle only the individual calls.
        [HarmonyPatch(typeof(Skills), "CheatRaiseSkill")]
        private static class CheatLevelPatch
        {
            private sealed class Snapshot
            {
                internal Skills.SkillType Skill;
                internal int Level;
            }

            private static void Prefix(Skills __instance, string __0, out Snapshot __state)
            {
                __state = null;
                if (Instance == null || !Instance.Enabled.Value || Player.m_localPlayer == null ||
                    Player.m_localPlayer.GetSkills() != __instance) return;
                Skills.SkillType skill;
                if (!Enum.TryParse(__0, true, out skill) || !Enum.IsDefined(typeof(Skills.SkillType), skill) ||
                    skill == Skills.SkillType.None || skill == Skills.SkillType.All) return;
                __state = new Snapshot { Skill = skill, Level = Mathf.FloorToInt(__instance.GetSkillLevel(skill)) };
            }

            private static void Postfix(Skills __instance, Snapshot __state)
            {
                if (__state == null || Instance == null) return;
                int level = Mathf.FloorToInt(__instance.GetSkillLevel(__state.Skill));
                if (level <= __state.Level) return;
                try
                {
                    if (Instance.EnsureView())
                    {
                        Instance.view.Enqueue(__state.Skill, level, __state.Level);
                        Instance.Logger.LogInfo("Queued raiseskill notification: " + __state.Skill + " " + __state.Level + " -> " + level);
                    }
                    else Instance.Logger.LogWarning("Skill notification could not be created: check HUD, font and Enabled setting.");
                }
                catch (Exception error) { Instance.Logger.LogError(error); }
            }
        }
    }

    public sealed class LevelUpView : MonoBehaviour
    {
        private sealed class Notice { public Skills.SkillType Skill; public int Level; public int Previous; }
        private readonly List<Notice> queue = new List<Notice>();
        private readonly List<Image> sparks = new List<Image>();
        private readonly List<Sprite> ownedSprites = new List<Sprite>();
        private Plugin plugin;
        private RectTransform card, ornament, medallion;
        private CanvasGroup group, words, crest;
        private TextMeshProUGUI heading, title, number, subtitle;
        private Image icon, halo, flare, levelGlow;
        private TMP_FontAsset font;
        private Notice current;
        private float elapsed;
        private bool layoutLogged;
        private static readonly Color Gold = new Color(.94f, .76f, .44f);

        public void Build(TMP_FontAsset gameFont, Plugin settings)
        {
            plugin = settings;
            font = gameFont;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 45;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            card = Rect("Skill celebration", transform, new Vector2(520, 210), Vector2.zero);
            group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0;
            var soft = CreateSprite(false);
            var ring = CreateSprite(true);
            var shadow = Box("Soft vignette", card, new Vector2(620, 250), new Vector2(0, -5), new Color(.015f, .018f, .024f, .65f));
            shadow.sprite = soft;
            halo = Box("Amber halo", card, new Vector2(240, 130), new Vector2(0, 65), new Color(.86f, .48f, .12f, .16f));
            halo.sprite = soft;

            // All particles sit behind the typography and have feathered edges.
            for (int i = 0; i < 8; i++)
            {
                var spark = Box("Ember " + i, card, new Vector2(5, 5), Vector2.zero, Gold);
                spark.sprite = soft;
                sparks.Add(spark);
            }
            medallion = Rect("Medallion", card, new Vector2(76, 76), new Vector2(0, 72));
            crest = medallion.gameObject.AddComponent<CanvasGroup>();
            var disc = Box("Smoked centre", medallion, new Vector2(82, 82), Vector2.zero, new Color(.035f, .04f, .04f, .95f));
            disc.sprite = soft;
            var outer = Box("Outer engraved ring", medallion, new Vector2(76, 76), Vector2.zero, new Color(Gold.r, Gold.g, Gold.b, .7f));
            outer.sprite = ring;
            var inner = Box("Inner engraved ring", medallion, new Vector2(66, 66), Vector2.zero, new Color(Gold.r, Gold.g, Gold.b, .22f));
            inner.sprite = ring;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f;
                var marker = Box("Compass engraving", medallion, new Vector2(4, 4), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 38, Gold);
                marker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            icon = Box("Skill icon", medallion, new Vector2(45, 45), Vector2.zero, new Color(1, .94f, .8f));
            icon.preserveAspect = true;

            ornament = Rect("Unfolding ornament", card, new Vector2(420, 12), new Vector2(0, 72));
            for (int side = -1; side <= 1; side += 2)
            {
                // Taper the strokes towards the outside instead of using a box border.
                for (int i = 0; i < 22; i++)
                {
                    float fade = 1f - i / 22f;
                    Box("Gold stroke", ornament, new Vector2(6.5f, 1), new Vector2(side * (58 + i * 6.5f), 0), new Color(Gold.r, Gold.g, Gold.b, .55f * fade));
                }
                Stroke(ornament, new Vector2(side * 76, 0), new Vector2(side * 87, 6), .8f);
                Stroke(ornament, new Vector2(side * 87, 6), new Vector2(side * 101, 0), .8f);
                Stroke(ornament, new Vector2(side * 76, 0), new Vector2(side * 87, -6), .8f);
                Stroke(ornament, new Vector2(side * 87, -6), new Vector2(side * 101, 0), .8f);
            }
            var textRoot = Rect("Typography", card, new Vector2(600, 180), Vector2.zero);
            words = textRoot.gameObject.AddComponent<CanvasGroup>();
            levelGlow = Box("Level change glow", textRoot, new Vector2(120, 64), new Vector2(0, -57), new Color(Gold.r, Gold.g, Gold.b, 0));
            levelGlow.sprite = soft;
            heading = Label("Heading", new Vector2(480, 24), new Vector2(0, 21), 13, Gold);
            heading.characterSpacing = 3;
            title = Label("Skill name", new Vector2(480, 50), new Vector2(0, -13), 34, new Color(1f, .96f, .84f));
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 34;
            foreach (var label in new[] { heading, title }) label.transform.SetParent(textRoot, false);
            var levelSlot = Rect("Rolling level viewport", textRoot, new Vector2(132, 46), new Vector2(0, -57));
            levelSlot.gameObject.AddComponent<RectMask2D>();
            subtitle = Label("Previous level", new Vector2(120, 42), Vector2.zero, 28, new Color(.78f, .76f, .68f));
            number = Label("New level", new Vector2(120, 42), Vector2.zero, 28, Gold);
            subtitle.transform.SetParent(levelSlot, false);
            number.transform.SetParent(levelSlot, false);
            flare = Box("Brief light bloom", card, new Vector2(190, 2), new Vector2(0, 72), new Color(1, .87f, .55f, 0));
            flare.sprite = soft;
        }

        public void Enqueue(Skills.SkillType skill, int level, int previous = -1)
        {
            var pending = queue.FirstOrDefault(n => n.Skill == skill);
            if (pending != null) { pending.Level = Math.Max(pending.Level, level); return; }
            if (queue.Count >= 32) queue.RemoveAt(0);
            queue.Add(new Notice { Skill = skill, Level = level, Previous = previous < 0 ? Math.Max(0, level - 1) : previous });
        }

        private void Update()
        {
            if (group == null) return;
            if (global::Console.IsVisible()) { group.alpha = 0; return; }
            if (current == null)
            {
                if (queue.Count == 0) return;
                current = queue[0];
                queue.RemoveAt(0);
                elapsed = 0;
                layoutLogged = false;
                heading.text = Localization.instance.Localize("$msg_skillup").ToUpperInvariant();
                title.text = Localization.instance.Localize("$skill_" + current.Skill.ToString().ToLowerInvariant());
                number.text = current.Level.ToString();
                subtitle.text = current.Previous.ToString();
                var skill = Player.m_localPlayer.GetSkills().m_skills.FirstOrDefault(s => s.m_skill == current.Skill);
                icon.sprite = skill == null ? null : skill.m_icon;
                icon.enabled = icon.sprite != null;
            }
            elapsed += Time.unscaledDeltaTime;
            float duration = plugin.Duration.Value;
            float enter = Mathf.Clamp01(elapsed / .65f);
            float leave = Mathf.SmoothStep(0, 1, Mathf.Clamp01((duration - elapsed) / .65f));
            float ease = 1 - Mathf.Pow(1 - enter, 3);
            // Hold the old value briefly, then replace it in the same slot.
            // The transition also fits the shortest configurable notification.
            float rollStart = Mathf.Min(.75f, duration * .28f);
            float rollDuration = Mathf.Min(.42f, duration * .22f);
            float roll = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - rollStart) / rollDuration));
            subtitle.rectTransform.anchoredPosition = new Vector2(0, 38 * roll);
            number.rectTransform.anchoredPosition = new Vector2(0, -38 * (1 - roll));
            subtitle.alpha = 1 - roll;
            number.alpha = roll;
            float settle = Mathf.Clamp01((elapsed - rollStart - rollDuration) / .45f);
            float accent = roll >= 1 ? Mathf.Sin(settle * Mathf.PI) : 0;
            number.rectTransform.localScale = Vector3.one * (1 + .08f * accent);
            levelGlow.color = new Color(Gold.r, Gold.g, Gold.b, .22f * accent);
            group.alpha = Mathf.Min(Mathf.SmoothStep(0, 1, enter), leave);
            crest.alpha = Mathf.Clamp01(elapsed / .32f);
            words.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - .18f) / .45f));
            card.anchorMin = card.anchorMax = new Vector2(.5f, plugin.VerticalPosition.Value);
            card.anchoredPosition = new Vector2(0, (1 - ease) * -10 + (1 - leave) * 8);
            // Match CanvasScaler's 0.5 width/height blend, using actual screen
            // dimensions rather than a potentially uninitialised layout rect.
            float width = Mathf.Max(1, Screen.width);
            float height = Mathf.Max(1, Screen.height);
            float canvasScale = Mathf.Sqrt((width / 1920f) * (height / 1080f));
            float fitScale = width / (canvasScale * 640f);
            card.localScale = Vector3.one * Mathf.Min(plugin.Scale.Value, fitScale);
            medallion.localScale = Vector3.one * (.7f + .12f * ease);
            ornament.localScale = new Vector3(ease, 1, 1);
            words.transform.localPosition = new Vector3(0, -8 * (1 - ease), 0);
            float pulse = Mathf.Exp(-Mathf.Pow((elapsed - .38f) / .22f, 2));
            halo.color = new Color(.86f, .48f, .12f, .12f + .12f * pulse);
            flare.color = new Color(1, .87f, .55f, pulse * .6f);
            for (int i = 0; i < sparks.Count; i++)
            {
                float phase = Mathf.Clamp01((elapsed - (i % 4) * .12f) / (1.4f + (i % 3) * .3f));
                float x = (i % 2 == 0 ? -1 : 1) * (50 + (i * 37 % 155));
                sparks[i].rectTransform.anchoredPosition = new Vector2(x + Mathf.Sin(i * 2f + phase) * 12, 54 + phase * (40 + i % 4 * 10));
                sparks[i].color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Sin(phase * Mathf.PI) * .65f);
            }
            if (!layoutLogged && elapsed >= .7f)
            {
                layoutLogged = true;
                var canvas = GetComponent<Canvas>();
                plugin.LogLayout("Showing " + current.Skill + ": rootCanvas=" + canvas.isRootCanvas +
                    ", canvasRect=" + ((RectTransform)transform).rect.size + ", scale=" + card.localScale.x +
                    ", alpha=" + group.alpha + ", active=" + gameObject.activeInHierarchy);
            }
            if (elapsed >= duration) { current = null; group.alpha = 0; }
        }

        private static void Stroke(Transform parent, Vector2 from, Vector2 to, float alpha)
        {
            Vector2 delta = to - from;
            var line = Box("Engraved stroke", parent, new Vector2(delta.magnitude, 1), (from + to) * .5f, new Color(Gold.r, Gold.g, Gold.b, alpha));
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        // Small reusable UI masks, generated once per HUD. No external assets.
        private Sprite CreateSprite(bool ring)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                float alpha = ring ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.009f, .035f, Mathf.Abs(radius - .94f)))
                    : Mathf.Pow(Mathf.Clamp01(1 - radius * radius), 3);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
            ownedSprites.Add(sprite);
            return sprite;
        }

        private void OnDestroy()
        {
            foreach (var sprite in ownedSprites)
            {
                if (sprite == null) continue;
                Destroy(sprite.texture);
                Destroy(sprite);
            }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI Label(string name, Vector2 size, Vector2 position, float fontSize, Color color)
        {
            var text = Rect(name, card, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .8f);
            shadow.effectDistance = new Vector2(0, -2);
            return text;
        }
    }
}
