using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Adds layered, animated graphics around an existing popup frame image (Menu3 / red menu 3):
// drop shadow, colour halo, rotating sunburst, inner highlight, rim line, header ribbon,
// sweeping shine, floating sparkles, twinkling corner stars and pulsing glows behind buttons.
// Everything is extra child/sibling objects with raycastTarget off, so the original image,
// text and buttons are untouched and still clickable.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class PopupDecorator : MonoBehaviour
{
    // Visible card area inside the frame sprite (the PNG has transparent margins).
    public static readonly Vector2 CardMin = new Vector2(0.09f, 0.161f);
    public static readonly Vector2 CardMax = new Vector2(0.908f, 0.902f);

    public Color Accent = new Color(1f, 0.82f, 0.3f);

    // Heading shown in the box at the top centre of the popup.
    public string Heading = "";
    // Optional existing title text inside the popup. It is hidden, and when game code changes it
    // (e.g. the tip title) its new text is shown in the heading box instead.
    public TMPro.TMP_Text HeadSource;
    // Reference units per local unit of this frame (set when the popup is resized), so the
    // heading box is exactly the same size on screen in every popup.
    public float RefScale = 1f;
    string _headSourceStart;
    TMPro.TextMeshProUGUI _headTxt;
    RectTransform _headBox;

    RectTransform _rt, _back, _front, _top, _rays, _shine, _shineArea;
    CanvasGroup _backCg, _frontCg, _topCg;
    Image _dim;
    readonly List<RectTransform> _stars = new List<RectTransform>();
    readonly List<RectTransform> _dots = new List<RectTransform>();
    readonly List<float> _dotSpeed = new List<float>();
    readonly List<RectTransform> _btnGlows = new List<RectTransform>();
    readonly List<GameObject> _all = new List<GameObject>();
    bool _built;
    float _openTime;

    // Number of open popups that should duck the music (the settings popup with sliders is excluded).
    public static int OpenCount;
    bool _counted, _hasSlider;

    void OnEnable()
    {
        if (!EnhancementsBootstrap.IsActive) { SetVisible(false); return; }
        if (!_built) Build();
        SetVisible(true);
        _openTime = Time.unscaledTime;
        if (!_hasSlider && !_counted) { _counted = true; OpenCount++; }
        JuiceAudio.Play(JuiceAudio.Fx.Whoosh, 0.5f);
    }

    void OnDisable()
    {
        SetVisible(false);
        if (_counted) { _counted = false; OpenCount = Mathf.Max(0, OpenCount - 1); }
    }

    void SetVisible(bool v)
    {
        foreach (var go in _all) if (go) go.SetActive(v);
    }

    void Build()
    {
        _built = true;
        _rt = (RectTransform)transform;
        _hasSlider = GetComponentInChildren<Slider>(true) != null;

        // Full-screen dim behind the popup so background text/HUD doesn't compete with it.
        var dimImg = Img(_rt.parent, "PopupFX_Dim", JuiceSprites.White, new Color(0.02f, 0.03f, 0.08f, 0.6f), Vector2.zero, Vector2.zero);
        dimImg.SetSiblingIndex(_rt.GetSiblingIndex());
        dimImg.anchorMin = new Vector2(-1, -1); dimImg.anchorMax = new Vector2(2, 2); // covers the screen even if the parent isn't full-screen
        dimImg.offsetMin = dimImg.offsetMax = Vector2.zero;
        dimImg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _dim = dimImg.GetComponent<Image>();
        _all.Add(dimImg.gameObject);
        Vector2 card = new Vector2(_rt.rect.width * (CardMax.x - CardMin.x), _rt.rect.height * (CardMax.y - CardMin.y));
        float big = Mathf.Max(card.x, card.y);
        Color dark = Color.Lerp(Accent, Color.black, 0.45f);

        // ---------- BEHIND the frame (sibling placed just before it) ----------
        var backGo = new GameObject("PopupFX_Back", typeof(RectTransform), typeof(CanvasGroup));
        _back = (RectTransform)backGo.transform;
        _back.SetParent(_rt.parent, false);
        _back.SetSiblingIndex(_rt.GetSiblingIndex());
        CopyRect(_rt, _back);
        _backCg = backGo.GetComponent<CanvasGroup>();
        _backCg.blocksRaycasts = false; _backCg.interactable = false;
        backGo.AddComponent<LayoutElement>().ignoreLayout = true;
        _all.Add(backGo);

        var backCard = CardArea(_back, "Card");
        Img(backCard, "Shadow", JuiceSprites.Glow, new Color(0, 0, 0, 0.55f), new Vector2(card.x * 1.15f, card.y * 1.25f), new Vector2(0, -card.y * 0.06f));
        Img(backCard, "Halo", JuiceSprites.Glow, WithA(Accent, 0.7f), new Vector2(card.x * 1.45f, card.y * 1.6f), Vector2.zero);

        // ---------- ON the card, behind existing content ----------
        var frontGo = new GameObject("PopupFX_Front", typeof(RectTransform), typeof(CanvasGroup));
        _front = (RectTransform)frontGo.transform;
        _front.SetParent(_rt, false);
        _front.SetAsFirstSibling();
        Stretch(_front, CardMin, CardMax);
        _frontCg = frontGo.GetComponent<CanvasGroup>();
        _frontCg.blocksRaycasts = false; _frontCg.interactable = false;
        frontGo.AddComponent<LayoutElement>().ignoreLayout = true;
        _all.Add(frontGo);

        // top highlight
        var hl = Img(_front, "Highlight", JuiceSprites.VGradient, new Color(1, 1, 1, 0.22f), Vector2.zero, Vector2.zero);
        Stretch(hl, new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.98f));
        // bottom shade
        var sh = Img(_front, "Shade", JuiceSprites.VGradient, new Color(0, 0, 0, 0.18f), Vector2.zero, Vector2.zero);
        Stretch(sh, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.35f));
        sh.localScale = new Vector3(1, -1, 1);
        // inner rim
        var rim = Img(_front, "Rim", JuiceSprites.Border, new Color(1, 1, 1, 0.55f), Vector2.zero, Vector2.zero);
        rim.GetComponent<Image>().type = Image.Type.Sliced;
        Stretch(rim, Vector2.zero, Vector2.one);
        rim.offsetMin = new Vector2(14, 14); rim.offsetMax = new Vector2(-14, -14);
        var rim2 = Img(_front, "RimOuter", JuiceSprites.Border, WithA(Color.Lerp(Accent, Color.white, 0.5f), 0.9f), Vector2.zero, Vector2.zero);
        rim2.GetComponent<Image>().type = Image.Type.Sliced;
        Stretch(rim2, Vector2.zero, Vector2.one);
        rim2.offsetMin = new Vector2(2, 2); rim2.offsetMax = new Vector2(-2, -2);

        // masked area for shine + floating sparkles
        var areaGo = new GameObject("FXArea", typeof(RectTransform), typeof(RectMask2D));
        _shineArea = (RectTransform)areaGo.transform;
        _shineArea.SetParent(_front, false);
        Stretch(_shineArea, Vector2.zero, Vector2.one);
        _shineArea.offsetMin = new Vector2(16, 16); _shineArea.offsetMax = new Vector2(-16, -16);
        for (int i = 0; i < 14; i++)
        {
            float sz = Random.Range(6f, 16f);
            var d = Img(_shineArea, "Mote", JuiceSprites.Dot, new Color(1, 1, 0.85f, 0.7f), Vector2.one * sz,
                new Vector2(Random.Range(-card.x / 2, card.x / 2), Random.Range(-card.y / 2, card.y / 2)));
            _dots.Add(d); _dotSpeed.Add(Random.Range(20f, 55f));
        }


        BuildHeading(card);

        // ---------- ON TOP of everything (small, at the corners only) ----------
        var topGo = new GameObject("PopupFX_Top", typeof(RectTransform), typeof(CanvasGroup));
        _top = (RectTransform)topGo.transform;
        _top.SetParent(_rt, false);
        _top.SetAsLastSibling();
        Stretch(_top, CardMin, CardMax);
        _topCg = topGo.GetComponent<CanvasGroup>();
        _topCg.blocksRaycasts = false; _topCg.interactable = false;
        topGo.AddComponent<LayoutElement>().ignoreLayout = true;
        _all.Add(topGo);
        Vector2[] corners = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.25f, 1), new Vector2(0.8f, 0) };
        foreach (var c in corners)
        {
            var s = Img(_top, "Sparkle", JuiceSprites.Star, Color.white, Vector2.one * Random.Range(34f, 56f), Vector2.zero);
            s.anchorMin = s.anchorMax = c;
            s.anchoredPosition = new Vector2((c.x - 0.5f) * 14f, (c.y - 0.5f) * 14f);
            _stars.Add(s);
        }

        // ---------- glow behind every button inside the popup ----------
        foreach (var b in GetComponentsInChildren<Button>(true))
            ButtonGlow.AttachTo(b, Accent, card.x * 0.7f);
    }

    void Update()
    {
        if (!_built || !EnhancementsBootstrap.IsActive) return;
        float t = Time.unscaledTime;
        float open = Mathf.Clamp01((t - _openTime) / 0.4f);
        _backCg.alpha = JuiceTween.EaseOutCubic(open);
        if (_dim) _dim.color = new Color(0.02f, 0.03f, 0.08f, 0.6f * open);
        _topCg.alpha = open;

        if (_rays) _rays.localRotation = Quaternion.Euler(0, 0, t * 12f);
        if (_rays) _rays.localScale = Vector3.one * (0.6f + 0.4f * JuiceTween.EaseOutBack(open));

        // shine sweep: every 3.5s pass from left to right
        if (_shine)
        {
            float w = _shineArea.rect.width;
            float p = Mathf.Repeat(t - _openTime + 0.3f, 3.5f) / 1.1f;
            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.75f, w * 0.75f, Mathf.Clamp01(p)), 0);
        }

        float h = _shineArea ? _shineArea.rect.height : 0f;
        for (int i = 0; i < _dots.Count; i++)
        {
            var d = _dots[i];
            var pos = d.anchoredPosition;
            pos.y += _dotSpeed[i] * Time.unscaledDeltaTime;
            pos.x += Mathf.Sin(t * 1.3f + i) * 8f * Time.unscaledDeltaTime;
            if (pos.y > h / 2f + 10f) pos.y = -h / 2f - 10f;
            d.anchoredPosition = pos;
            var img = d.GetComponent<Image>();
            var c = img.color; c.a = 0.35f + 0.35f * Mathf.Sin(t * 3f + i * 1.7f); img.color = c;
        }

        for (int i = 0; i < _stars.Count; i++)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(t * 2.6f + i * 1.9f);
            _stars[i].localScale = Vector3.one * Mathf.Lerp(0.55f, 1.1f, k) * open;
            _stars[i].localRotation = Quaternion.Euler(0, 0, t * 40f * (i % 2 == 0 ? 1 : -1));
        }
    }

    // A short single-line text that is a direct child of the frame and sits in the top 35% of the card.
    TMPro.TMP_Text FindTitle(Vector2 card)
    {
        TMPro.TMP_Text best = null; float bestY = float.MinValue;
        float cardTop = _rt.rect.yMin + _rt.rect.height * CardMax.y;
        foreach (Transform c in _rt)
        {
            var t = c.GetComponent<TMPro.TMP_Text>();
            if (!t || string.IsNullOrWhiteSpace(t.text) || t.text.Length > 24 || t.text.Contains("\n")) continue;
            Vector3 local = _rt.InverseTransformPoint(t.rectTransform.TransformPoint(t.rectTransform.rect.center));
            if (local.y < cardTop - card.y * 0.35f) continue;
            if (local.y > bestY) { bestY = local.y; best = t; }
        }
        return best;
    }

    // ---------- heading box (top centre of the card) ----------
    static TMPro.TMP_FontAsset _font;
    static TMPro.TMP_FontAsset Font
    {
        get
        {
            if (_font) return _font;
            foreach (var f in Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>())
                if (f.name.StartsWith("DiloWorld")) return _font = f;
            return _font = Resources.Load<TMPro.TMP_FontAsset>("DiloWorld-mLJLv SDF");
        }
    }

    float _headCardWidth;
    string _shownHeading;

    void BuildHeading(Vector2 card)
    {
        if (HeadSource) _headSourceStart = HeadSource.text;
        if (string.IsNullOrEmpty(Heading) && !HeadSource) return;

        var area = CardArea(_rt, "PopupFX_Heading");
        area.SetAsLastSibling();
        area.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _all.Add(area.gameObject);

        Color dark = Color.Lerp(Accent, Color.black, 0.55f);
        _headBox = Img(area, "HeadingBox", JuiceSprites.Pill, dark, new Vector2(card.x * 0.5f, 96f), Vector2.zero);
        _headBox.GetComponent<Image>().type = Image.Type.Sliced;
        _headBox.anchorMin = _headBox.anchorMax = new Vector2(0.5f, 1f);
        _headBox.anchoredPosition = new Vector2(0f, 6f / Mathf.Max(1e-4f, RefScale));
        _headBox.localScale = Vector3.one / Mathf.Max(1e-4f, RefScale);
        var rim = Img(_headBox, "HeadingRim", JuiceSprites.Border, WithA(Color.Lerp(Accent, Color.white, 0.55f), 0.95f), Vector2.zero, Vector2.zero);
        rim.GetComponent<Image>().type = Image.Type.Sliced;
        Stretch(rim, Vector2.zero, Vector2.one);

        // created inactive so TMP starts with the game font (setting the font on a live text leaves garbled glyphs)
        var go = new GameObject("HeadingText", typeof(RectTransform));
        go.SetActive(false);
        go.transform.SetParent(_headBox, false);
        _headTxt = go.AddComponent<TMPro.TextMeshProUGUI>();
        if (Font) { _headTxt.font = Font; _headTxt.fontSharedMaterial = Font.material; }
        _headTxt.alignment = TMPro.TextAlignmentOptions.Center;
        _headTxt.color = Color.white;
        _headTxt.raycastTarget = false;
        _headTxt.enableWordWrapping = true;
        _headTxt.enableAutoSizing = true;
        _headTxt.fontSizeMin = 28f; _headTxt.fontSizeMax = 45f;
        var trt = _headTxt.rectTransform;
        Stretch(trt, Vector2.zero, Vector2.one);
        trt.offsetMin = new Vector2(36f, 8f); trt.offsetMax = new Vector2(-36f, -8f);
        _headCardWidth = card.x * RefScale; // heading box works in reference units
        go.SetActive(true);
        RefreshHeading();
    }

    void RefreshHeading()
    {
        if (!_headTxt) return;
        string text = Heading;
        if (HeadSource)
        {
            if (!string.IsNullOrWhiteSpace(HeadSource.text) && HeadSource.text != _headSourceStart) text = HeadSource.text;
            HeadSource.alpha = 0f; // the original title stays hidden; the box shows it
        }
        if (text == _shownHeading) return;
        _shownHeading = text;
        _headTxt.fontSizeMin = 28f; _headTxt.fontSizeMax = 45f;
        _headTxt.text = text;
        _headBox.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));

        // box fits the text: one line up to 85% of the card width, longer titles wrap to two lines
        float maxW = _headCardWidth * 0.85f;
        float oneLine = _headTxt.GetPreferredValues(text, 100000f, 100f).x * (45f / Mathf.Max(1f, _headTxt.fontSize)) + 110f;
        _headBox.sizeDelta = new Vector2(Mathf.Clamp(oneLine, 300f, maxW), oneLine > maxW ? 140f : 96f);
    }

    void LateUpdate()
    {
        if (_built && _headTxt) RefreshHeading();
    }

    // ---------- helpers ----------
    static Color WithA(Color c, float a) { c.a = a; return c; }

    static RectTransform CardArea(RectTransform parent, string name)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        Stretch(rt, CardMin, CardMax);
        return rt;
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void CopyRect(RectTransform src, RectTransform dst)
    {
        dst.anchorMin = src.anchorMin; dst.anchorMax = src.anchorMax; dst.pivot = src.pivot;
        dst.anchoredPosition = src.anchoredPosition; dst.sizeDelta = src.sizeDelta;
        dst.localRotation = src.localRotation; dst.localScale = src.localScale;
    }

    static RectTransform Img(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return rt;
    }
}
