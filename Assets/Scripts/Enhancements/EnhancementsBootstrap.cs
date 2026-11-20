using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Placed once in the Start Menu scene. Survives scene loads, but every effect is only
// switched on while the active scene is Start Menu (build 0) or Level 1 (build 1).
// It only ADDS components/overlays; no existing script, sprite or button function is changed.
public class EnhancementsBootstrap : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [SerializeField] int[] _enabledBuildIndexes = { 0, 1 };
    [SerializeField] float _musicFadeInTime = 1.5f;
    [SerializeField] string _level1Title = "LEVEL 1";
    [SerializeField] string _level1Subtitle = "The Quest Begins";

    static EnhancementsBootstrap _instance;
    // Button methods that already play their own sound; ButtonJuice skips its click sfx for these.
    static readonly string[] _methodsWithOwnSfx = { "PopUpSettingsPanel", "PopUpCreditsPanel", "Soundsetting",
        "PopDownSkillAchievedPanel", "PopDownAllPanels" };

    void Awake()
    {
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    void OnDestroy()
    {
        if (_instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsActive = System.Array.IndexOf(_enabledBuildIndexes, scene.buildIndex) >= 0;
        if (!IsActive) return;

        StopAllCoroutines();
        StartCoroutine(SetupNextFrame(scene.buildIndex));
    }

    IEnumerator SetupNextFrame(int buildIndex)
    {
        yield return null; // let the scene's own Start() run first
        UIPolish.ApplyAll(buildIndex);
        if (buildIndex == 1) // before the decor pass, so the new Mute button gets the same glow
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.name == "Pausepage" && t.gameObject.scene.IsValid()) PauseMuteButton.AttachTo(t);
        AttachButtonJuice();
        AttachPanelPops();
        AttachPopupDecor();
        if (buildIndex == 1) AttachGameplayFX();
        StartCoroutine(FadeInMusic());
        if (buildIndex == 1) StartCoroutine(ShowTitleCard(_level1Title, _level1Subtitle));
    }

    static void AttachButtonJuice()
    {
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!b.gameObject.scene.IsValid() || b.GetComponent<ButtonJuice>()) continue;
            var j = b.gameObject.AddComponent<ButtonJuice>();
            j.PlayClickSfx = !HasOwnSfx(b);
        }
    }

    static bool HasOwnSfx(Button b)
    {
        for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            if (System.Array.IndexOf(_methodsWithOwnSfx, b.onClick.GetPersistentMethodName(i)) >= 0) return true;
        return false;
    }

    static void AttachPanelPops()
    {
        var ui = UIManager.Instance;
        if (!ui) return;
        foreach (var name in new[] { "_creditsPanel", "_settingsPanel", "_winPanel", "_loosePanel", "_skillsAchievedPanel" })
        {
            var f = typeof(UIManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var go = f?.GetValue(ui) as GameObject;
            if (go && !go.GetComponent<PanelPopIn>()) go.AddComponent<PanelPopIn>();
        }
    }

    // Every popup frame in the game uses one of these two sprites. All of them are shown with the
    // gold "Menu3" frame and get a heading box at the top centre.
    // { part of the frame's path, heading text, name of an existing title text to move into the box }
    static readonly string[][] Headings =
    {
        new[] { "SoundCanvas", "Settings", "Text (TMP)" },
        new[] { "InfoCanvas", "Credits", null },
        new[] { "LooseCanvas", "Mission Failed", "TxtYouLose" },
        new[] { "WinCanvas", "You Win", "TxtYouWin" },
        new[] { "SkillsUnlockedCanvas", "Skill Unlocked", "TxtSkillUnlocked" },
        new[] { "Initpage", "Learn as You Play!", "Text (TMP)" },
        new[] { "TipsSkip/Tip", "Tips", "Head" },
        new[] { "TipsPanel/Tip", "Tips", "Head" },
        new[] { "WinCongrats", "Congratulations", null },
        new[] { "LastSummary", "Summary", "Head" },
        new[] { "Lastpage", "Level Complete", "Head" },
        new[] { "WithoutKey", "Try Again", "Head" },
        new[] { "CorrectansPanel", "Correct Answer", "Text (TMP)" },
        new[] { "Wrongpanel", "Wrong Answer", "Text (TMP)" },
        new[] { "Finalanswer", "Feedback", null },
        new[] { "quizfinish", "Summary", "Text (TMP)" },
        new[] { "Questionpanel/bg", "Quiz", "Text (TMP)" },
        new[] { "TIPHint", "Hint", null },
        new[] { "LastContinue", "Well Done", null },
        new[] { "Pausepage", "Pause Menu", null },
    };

    // ---------- one look for every popup ----------
    // Two standard sizes (reference units, 1920x1080 screen). Same aspect as the frame sprite (1060x807).
    public static readonly Vector2 SmallPopup = new Vector2(1100f, 837f);
    public static readonly Vector2 LargePopup = new Vector2(1500f, 1142f);
    // Popups that hold a lot of text use the large size; every other popup uses the small size.
    static readonly string[] LargePopups = { "TipsSkip/Tip", "TipsPanel/Tip", "LastSummary", "Questionpanel/bg", "quizfinish" };
    public const float ButtonTextSize = 40f;   // reference units, same for every popup button label

    // Reference units (1920-wide screen) per local unit of this transform.
    public static float RefPerLocal(Transform t)
    {
        var canvas = t.GetComponentInParent<Canvas>(true);
        if (!canvas) return 1f;
        var root = canvas.rootCanvas ? canvas.rootCanvas : canvas;
        // multiply local scales up to the canvas (lossyScale is 0 while a canvas is switched off)
        float localToCanvas = 1f;
        for (var x = t; x && x != root.transform; x = x.parent) localToCanvas *= x.localScale.x;
        var sc = root.GetComponent<CanvasScaler>();
        float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
        float factor = 1f; // canvas units -> pixels
        if (sc)
        {
            if (sc.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                float lw = Mathf.Log(sw / sc.referenceResolution.x, 2), lh = Mathf.Log(sh / sc.referenceResolution.y, 2);
                factor = Mathf.Pow(2, Mathf.Lerp(lw, lh, sc.matchWidthOrHeight));
            }
            else if (sc.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize) factor = sc.scaleFactor;
        }
        return localToCanvas * factor * 1920f / sw;
    }

    // Scales the popup so its frame is exactly the standard size on screen. Content that sits
    // next to the frame (not inside it) in the same popup container is scaled with it.
    // Returns reference units per local unit of the frame afterwards.
    // Space (reference units) kept free above the heading box and below the card on every screen.
    public const float ScreenMargin = 60f;

    // Wide phones (19.5:9, 20:9...) are shorter than a 16:9 screen in reference units, so a popup
    // sized for 1920x1080 would touch the top. Shrink it just enough to keep a margin; 16:9 is unchanged.
    static float FitToScreen(Vector2 frameSize)
    {
        float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
        float screenH = 1920f * sh / sw;                       // screen height in reference units
        float cardH = frameSize.y * (PopupDecorator.CardMax.y - PopupDecorator.CardMin.y);
        float needed = cardH + 60f /* heading above the card */ + ScreenMargin * 2f;
        return Mathf.Min(1f, screenH / needed);
    }

    public static float NormalizeSize(Image frame, bool large)
    {
        var rt = frame.rectTransform;
        Vector2 target = large ? LargePopup : SmallPopup;
        target *= FitToScreen(target);

        // stretched frames (e.g. the full-width tips frame) get a fixed size first
        if (rt.anchorMin != rt.anchorMax)
        {
            Vector2 size = rt.rect.size; Vector3 pos = rt.localPosition;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.localPosition = pos;
        }
        // keep the frame sprite's aspect so every popup has the same shape
        float aspect = target.y / target.x;
        if (Mathf.Abs(rt.rect.height / Mathf.Max(1f, rt.rect.width) - aspect) > 0.002f)
            rt.sizeDelta = new Vector2(rt.rect.width, rt.rect.width * aspect) + (rt.sizeDelta - rt.rect.size);

        float current = rt.rect.width * RefPerLocal(rt);
        float k = target.x / Mathf.Max(1f, current);
        if (k > 4f || k < 0.25f) k = 1f;   // measurement went wrong: leave the size alone
        if (Mathf.Abs(k - 1f) > 0.01f)
        {
            var parent = rt.parent;
            bool soloContainer = parent && !parent.GetComponent<Canvas>() || parent && parent.GetComponent<Canvas>() && parent.GetComponent<Canvas>().isRootCanvas;
            int frames = 0;
            if (parent) foreach (Transform c in parent) { var im = c.GetComponent<Image>(); if (im && im.sprite && (im.sprite.name == "Menu3" || im.sprite.name == "red menu 3" || im == frame)) frames++; }
            bool withSiblings = soloContainer && frames == 1 && parent.name != "Canvas";
            Vector3 center = rt.localPosition;
            if (withSiblings)
            {
                foreach (Transform c in parent)
                {
                    var crt = c as RectTransform;
                    if (!crt) continue;
                    if (crt != rt && crt.anchorMin == Vector2.zero && crt.anchorMax == Vector2.one) continue; // full-screen backgrounds stay
                    if (crt != rt && ContainsFrame(crt)) continue; // nested popups (e.g. quiz answer panels) size themselves
                    crt.localPosition = center + (crt.localPosition - center) * k;
                    crt.localScale *= k;
                }
            }
            else rt.localScale *= k;
        }
        return target.x / Mathf.Max(1f, rt.rect.width);
    }

    static bool ContainsFrame(Transform t)
    {
        foreach (var im in t.GetComponentsInChildren<Image>(true))
            if (im.sprite && (im.sprite.name == "Menu3" || im.sprite.name == "red menu 3")) return true;
        return false;
    }

    // Switches a text to another font. TMP keeps glyph data from the old font on a text that is
    // already showing (glyphs come out garbled), so an active text is re-enabled to rebuild cleanly.
    public static void SetFont(TMP_Text t, TMP_FontAsset font)
    {
        t.font = font;
        t.fontSharedMaterial = font.material;
        if (t.gameObject.activeInHierarchy)
        {
            t.gameObject.SetActive(false);
            t.gameObject.SetActive(true);
        }
        else if (!t.GetComponent<TmpFontRefresh>()) t.gameObject.AddComponent<TmpFontRefresh>(); // when it is first shown
    }

    // Same font for every text in the popup, and the same size for every button label.
    public const float BodyTextSize = 35f;   // reference units, popup content text (largest)
    public const float BodyTextMin = 22f;
    public const float ShortTextSize = 48f;  // short one-line messages such as "You win!"    // content text may shrink to this when there is a lot of it

    static void NormalizeText(Transform popupRoot, RectTransform frame = null, TMP_Text headSource = null)
    {
        var font = Resources.Load<TMP_FontAsset>("DiloWorld-mLJLv SDF");
        foreach (var t in popupRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == "HeadingText") continue;
            if (font && t.font != font) SetFont(t, font);
            // action-button labels only; quiz answer options are buttons too but hold whole sentences
            bool answerOption = t.transform.parent && t.transform.parent.parent && t.transform.parent.parent.name == "OPtions";
            if (t.GetComponentInParent<Button>(true) && !answerOption && t.text.Length <= 16)
            {
                t.enableAutoSizing = false;
                t.fontSize = ButtonTextSize / Mathf.Max(1e-4f, RefPerLocal(t.transform));
            }
            else if (!t.GetComponentInParent<Button>(true) && (!frame || !popupRoot.GetComponent<Canvas>() || t.transform.IsChildOf(frame)))
            {
                // content text sitting on the frame gets a box wide/tall enough for the 45 size
                var trt = t.rectTransform;
                if (frame && trt.parent == frame && trt.anchorMin == trt.anchorMax && t != headSource && trt.rect.height > 80f)
                {
                    // fill the visible card: below the heading box, above the buttons, with side margins
                    Rect c = CardRect(frame);
                    float per0 = Mathf.Max(1e-4f, RefPerLocal(frame));
                    bool hasButtons = false;
                    foreach (var sb in popupRoot.GetComponentsInChildren<Button>(true))
                    {
                        if (sb.name.ToLower().Contains("close") || BelongsToOtherFrame(sb.transform, frame, popupRoot)) continue;
                        // only buttons in the lower half of the card push the text up
                        var lp = frame.InverseTransformPoint(sb.transform.position);
                        if (lp.y < frame.rect.center.y) { hasButtons = true; break; }
                    }
                    float top = c.yMax - 120f / per0;
                    float bottom = c.yMin + (hasButtons ? 150f : 50f) / per0;
                    float w = c.width * 0.88f;
                    trt.pivot = new Vector2(0.5f, 0.5f);
                    trt.sizeDelta = new Vector2(w, Mathf.Max(80f, top - bottom));
                    trt.localPosition = new Vector3(c.center.x, (top + bottom) * 0.5f, trt.localPosition.z);
                    t.margin = Vector4.zero;
                }
                // body text: 45 when it fits, shrinks (down to 24) when the popup holds a lot of text
                float per = Mathf.Max(1e-4f, RefPerLocal(t.transform));
                t.enableWordWrapping = true;
                t.enableAutoSizing = true;
                // a short one-line message ("You win!") is shown a bit bigger than paragraph text
                bool shortMsg = t.text.Trim().Length > 0 && t.text.Trim().Length <= 20 && !t.text.Contains("\n");
                t.fontSizeMax = shortMsg ? ShortTextSize : BodyTextSize;   // same number as the Inspector font size
                t.fontSizeMin = BodyTextMin;

                // a gap of spaces left for an icon ("Click     to skip"): keep the icon inside the gap
                if (t.text.Contains("   ") && t.transform.parent && !t.GetComponent<IconInTextGap>())
                {
                    // the icon is a small plain image next to the text, or next to the popup frame
                    RectTransform found = null;
                    var levels = new[] { t.transform.parent, t.transform.parent.parent };
                    foreach (var level in levels)
                    {
                        if (!level || found) continue;
                        foreach (Transform s in level)
                        {
                            var icon = s as RectTransform;
                            if (!icon || s == t.transform || s.GetComponent<TMP_Text>() || !s.GetComponent<Image>()) continue;
                            if (s.GetComponent<Button>() || s.childCount > 0 || icon.rect.width > 120f || s.name.StartsWith("PopupFX_")) continue;
                            found = icon; break;
                        }
                    }
                    if (found)
                    {
                        var g = t.gameObject.AddComponent<IconInTextGap>();
                        g.Text = t; g.Icon = found;
                    }
                }
            }
        }
    }

    // The visible card area of a popup frame, in the frame's local units.
    static Rect CardRect(RectTransform frame)
    {
        var r = frame.rect;
        return Rect.MinMaxRect(r.xMin + r.width * PopupDecorator.CardMin.x, r.yMin + r.height * PopupDecorator.CardMin.y,
                               r.xMin + r.width * PopupDecorator.CardMax.x, r.yMin + r.height * PopupDecorator.CardMax.y);
    }

    // How far (reference units) the centre of a close (X) button sits inside the frame's top-right corner.
    public const float CloseInset = 30f;

    // Puts every close (X) button of the popup on the frame's top-right corner.
    static void PlaceCloseButtons(RectTransform frame, Transform popupRoot, float refScale)
    {
        var r = CardRect(frame);   // the visible gold card, not the sprite's transparent edge
        float inset = CloseInset / Mathf.Max(1e-4f, refScale);
        Vector3 corner = frame.TransformPoint(new Vector3(r.xMax - inset, r.yMax - inset, 0f));
        foreach (var b in popupRoot.GetComponentsInChildren<Button>(true))
        {
            var n = b.name.ToLower();
            var bim = b.GetComponent<Image>();
            bool isClose = n.Contains("close") || n == "x" || n == "btnx" || (bim && bim.sprite && bim.sprite.name.ToLower().Contains("close"));
            if (!isClose) continue;
            if (BelongsToOtherFrame(b.transform, frame, popupRoot)) continue;
            MoveCentreTo((RectTransform)b.transform, frame, new Vector2(r.xMax - inset, r.yMax - inset));
        }
    }

    // Moves a button so its centre lands on a point given in the frame's local units.
    // Works in local space, so it also works while the canvas is switched off (its scale is 0 then).
    static void MoveCentreTo(RectTransform btn, RectTransform frame, Vector2 frameLocal)
    {
        // the point in the button's parent space
        Vector3 p = frameLocal;
        for (var x = (Transform)frame; x && x != btn.parent; x = x.parent)
        {
            p = Vector3.Scale(p, x.localScale);
            p = x.localRotation * p + x.localPosition;
            if (!x.parent) return;   // the button is not under the frame's parent chain
        }
        Vector3 centre = btn.localPosition + Vector3.Scale((Vector3)btn.rect.center, btn.localScale);
        btn.localPosition += new Vector3(p.x - centre.x, p.y - centre.y, 0f);
    }

    // Slider handles (the frog icon) keep the icon's own shape instead of being stretched to the slider height.
    public const float SliderHandleScale = 0.8f;   // size of the frog handle on the sound sliders (1 = full)

    static void FixSliderHandles(Transform popupRoot)
    {
        foreach (var s in popupRoot.GetComponentsInChildren<Slider>(true))
        {
            var h = s.handleRect;
            var im = h ? h.GetComponent<Image>() : null;
            if (!im || !im.sprite) continue;
            im.preserveAspect = true;
            float aspect = im.sprite.rect.width / Mathf.Max(1f, im.sprite.rect.height);
            float height = h.rect.height;
            if (height > 1f) h.sizeDelta = new Vector2(height * aspect, h.sizeDelta.y);
            h.localScale = Vector3.one * SliderHandleScale;
        }
    }

    // Previous / Next arrows go to the bottom-left / bottom-right corners inside the card.
    static void PlaceNavButtons(RectTransform frame, Transform popupRoot, float refScale)
    {
        var r = CardRect(frame);
        float inset = 90f / Mathf.Max(1e-4f, refScale);
        foreach (var b in popupRoot.GetComponentsInChildren<Button>(true))
        {
            var n = b.name.ToLower();
            bool prev = n.StartsWith("prev");
            bool next = n.StartsWith("next") && !n.Contains("question");   // NextQuestion is a Continue button
            bool quiz = n.Contains("takequiz");   // between the Previous / Next arrows
            bool cont = n.Contains("continue") || n.Contains("contine") || n.Contains("nextquestion") || n == "submit" || quiz;
            if (!prev && !next && !cont) continue;
            if (BelongsToOtherFrame(b.transform, frame, popupRoot)) continue;
            float x = prev ? r.xMin + inset : next ? r.xMax - inset : r.center.x;   // Continue: bottom centre
            float y = r.yMin + (cont && !quiz ? 70f / Mathf.Max(1e-4f, refScale) : inset);
            MoveCentreTo((RectTransform)b.transform, frame, new Vector2(x, y));
        }
    }

    // True when the object sits inside another popup frame that shares this container
    // (e.g. TipsPanel holds both the Tip and the Congratulations popups).
    static bool BelongsToOtherFrame(Transform t, RectTransform frame, Transform popupRoot)
    {
        // when the popup sits straight on a whole canvas (e.g. the Start Menu), only its own children count
        if (popupRoot.GetComponent<Canvas>() && !t.IsChildOf(frame))
        {
            var top = t;
            while (top.parent && top.parent != popupRoot) top = top.parent;
            if (top != t) return true;   // inside some other panel of that canvas
        }
        for (var x = t.parent; x && x != popupRoot; x = x.parent)
        {
            if (x == frame) return false;
            var im = x.GetComponent<Image>();
            if (im && (im.GetComponent<PopupDecorator>() || (im.sprite && (im.sprite.name == "Menu3" || im.sprite.name == "red menu 3"))))
                return true;
        }
        return false;
    }

    static Sprite _goldFrame;
    public static Sprite GoldFrame
    {
        get
        {
            if (_goldFrame) return _goldFrame;
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                if (s.name == "Menu3") return _goldFrame = s;
            return null;
        }
    }

    static string PathOf(Transform t)
    {
        string p = t.name;
        for (var x = t.parent; x; x = x.parent) p = x.name + "/" + p;
        return p;
    }

    // Gold frame + decoration + heading box for a popup frame image (also used by the exit popup).
    public static PopupDecorator Decorate(Image img, string heading = null, TMP_Text headSource = null)
    {
        if (GoldFrame && img.sprite != GoldFrame) img.sprite = GoldFrame;
        var d = img.GetComponent<PopupDecorator>();
        if (d) return d;

        string framePath = PathOf(img.transform);
        bool large = false;
        foreach (var key in LargePopups) if (framePath.Contains(key)) { large = true; break; }
        var pops = img.GetComponentsInParent<PanelPopIn>(true);
        foreach (var pop in pops) pop.SnapToBase();   // measure at full size, not mid pop-in
        float refScale = NormalizeSize(img, large);
        var popupRoot = img.transform.parent && img.transform.parent.name != "Canvas" ? img.transform.parent : img.transform;
        PlaceCloseButtons(img.rectTransform, popupRoot, refScale);
        PlaceNavButtons(img.rectTransform, popupRoot, refScale);
        FixSliderHandles(popupRoot);
        foreach (var pop in pops) pop.Rebase();
        if (heading == null)
        {
            string path = PathOf(img.transform);
            foreach (var h in Headings)
            {
                if (!path.Contains(h[0])) continue;
                heading = h[1];
                if (h[2] != null && !headSource)
                    foreach (Transform c in img.transform)
                        if (c.name == h[2] && c.GetComponent<TMP_Text>()) { headSource = c.GetComponent<TMP_Text>(); break; }
                break;
            }
        }
        NormalizeText(popupRoot, img.rectTransform, headSource);

        // fields must be set before OnEnable builds the decoration
        bool wasActive = img.gameObject.activeSelf;
        img.gameObject.SetActive(false);
        d = img.gameObject.AddComponent<PopupDecorator>();
        d.Accent = new Color(1f, 0.82f, 0.3f);
        d.Heading = heading ?? "";
        d.HeadSource = headSource;
        d.RefScale = refScale;
        img.gameObject.SetActive(wasActive);
        return d;
    }

    static void AttachPopupDecor()
    {
        foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (!img.gameObject.scene.IsValid() || !img.sprite) continue;
            string n = img.sprite.name;
            if (n.StartsWith("Speech Bubble")) continue; // no glow behind the intro "Continue" button
            if (n != "Menu3" && n != "red menu 3") continue;

            var popTarget = PopTargetFor(img.transform);
            Decorate(img);
            if (!popTarget.GetComponent<PanelPopIn>()) popTarget.gameObject.AddComponent<PanelPopIn>();
        }
    }

    // The object that is switched on/off to show this popup frame: the frame itself or the
    // nearest hidden container above it (e.g. "Questionpanel", which holds the frame AND the
    // question/options beside it). Animating that object moves everything in the popup together.
    static Transform PopTargetFor(Transform frame)
    {
        for (Transform t = frame; t && !t.GetComponent<Canvas>(); t = t.parent)
        {
            if (t.GetComponent<PanelPopIn>()) return t;   // e.g. a UIManager panel already set up
            if (!t.gameObject.activeSelf) return t;
        }
        return frame;
    }

    static void AttachGameplayFX()
    {
        foreach (var p in FindObjectsOfType<PlayerStateManager>(true))
        {
            if (!p.GetComponent<PlayerFX>()) p.gameObject.AddComponent<PlayerFX>();
            // smooth rendering between physics steps (removes jitter while jumping with a LateUpdate camera)
            var prb = p.GetComponent<Rigidbody2D>();
            if (prb) prb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (!Application.isMobilePlatform && !p.GetComponent<KeyboardControls>()) p.gameObject.AddComponent<KeyboardControls>();
        }
        foreach (var it in FindObjectsOfType<ItemsController>(true))
            if (!it.GetComponent<PickupFX>()) it.gameObject.AddComponent<PickupFX>();
        foreach (var hp in FindObjectsOfType<PlayerHealthManager>(true))
            if (!hp.GetComponent<HPBarFX>()) hp.gameObject.AddComponent<HPBarFX>();
        foreach (var cp in FindObjectsOfType<CheckPointController>(true))
            if (!cp.GetComponent<CheckpointFX>()) cp.gameObject.AddComponent<CheckpointFX>();
    }

    // ---------- music: fade in after scene load, duck while a popup is open ----------
    AudioSource _music;
    float _userVol = -1f, _applied = -1f, _fade = 1f, _duck = 1f;

    AudioSource Music
    {
        get
        {
            if (_music || !SoundsManager.Instance) return _music;
            var f = typeof(SoundsManager).GetField("_musicSource", BindingFlags.Instance | BindingFlags.NonPublic);
            return _music = f?.GetValue(SoundsManager.Instance) as AudioSource;
        }
    }

    IEnumerator FadeInMusic()
    {
        _fade = 0f;
        yield return JuiceTween.Run(_musicFadeInTime, p => _fade = p);
    }

    void Update()
    {
        var src = Music;
        if (!src) return;
        if (_userVol < 0f) _userVol = src.volume;
        // The settings slider writes src.volume directly; treat any outside change as the new user volume.
        if (_applied >= 0f && Mathf.Abs(src.volume - _applied) > 0.0005f)
            _userVol = (_fade * _duck) > 0.01f ? src.volume / (_fade * _duck) : src.volume;

        float duckTarget = IsActive && PopupDecorator.OpenCount > 0 ? 0.35f : 1f;
        _duck = Mathf.MoveTowards(_duck, duckTarget, Time.unscaledDeltaTime * 2f);
        float f = IsActive ? _fade : 1f;
        src.volume = Mathf.Clamp01(_userVol * f * _duck);
        _applied = src.volume;
    }

    // Animated "LEVEL 1" banner that slides in, holds, and fades out.
    IEnumerator ShowTitleCard(string title, string subtitle)
    {
        yield return new WaitForSecondsRealtime(1.2f); // wait for the existing fade transition

        var canvasGo = new GameObject("TitleCardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var cg = canvasGo.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // dark ribbon behind the text
        var ribbon = new GameObject("Ribbon", typeof(Image)).GetComponent<Image>();
        ribbon.transform.SetParent(canvasGo.transform, false);
        ribbon.color = new Color(0f, 0f, 0f, 0.55f);
        ribbon.raycastTarget = false;
        var rr = ribbon.rectTransform;
        rr.anchorMin = new Vector2(0f, 0.5f); rr.anchorMax = new Vector2(1f, 0.5f);
        rr.sizeDelta = new Vector2(0f, 220f);
        rr.anchoredPosition = new Vector2(0f, 120f);

        var titleTxt = MakeText(rr, title, 110, new Vector2(0f, 30f), Color.white);
        var subTxt = MakeText(rr, subtitle, 44, new Vector2(0f, -60f), new Color(1f, 0.85f, 0.35f));

        cg.alpha = 0f;
        rr.localScale = new Vector3(1f, 0f, 1f);
        Vector2 tFrom = new Vector2(-1400f, 30f), tTo = new Vector2(0f, 30f);
        Vector2 sFrom = new Vector2(1400f, -60f), sTo = new Vector2(0f, -60f);

        // in
        yield return JuiceTween.Run(0.5f, p =>
        {
            cg.alpha = p;
            rr.localScale = new Vector3(1f, JuiceTween.EaseOutBack(p), 1f);
            titleTxt.rectTransform.anchoredPosition = Vector2.LerpUnclamped(tFrom, tTo, JuiceTween.EaseOutBack(p));
            subTxt.rectTransform.anchoredPosition = Vector2.LerpUnclamped(sFrom, sTo, JuiceTween.EaseOutBack(Mathf.Clamp01(p * 1.2f - 0.2f)));
        });
        if (SoundsManager.Instance) SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.CheckpointSfx, 0.6f);

        yield return new WaitForSecondsRealtime(1.6f);

        // out
        yield return JuiceTween.Run(0.45f, p =>
        {
            cg.alpha = 1f - p;
            rr.localScale = new Vector3(1f, 1f - JuiceTween.EaseInCubic(p), 1f);
        });
        Destroy(canvasGo);
    }

    static TextMeshProUGUI MakeText(Transform parent, string text, float size, Vector2 pos, Color color)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 200);
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1600f, 140f);
        r.anchoredPosition = pos;
        return t;
    }
}
