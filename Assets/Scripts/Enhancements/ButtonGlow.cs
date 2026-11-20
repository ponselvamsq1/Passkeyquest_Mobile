using UnityEngine;
using UnityEngine.UI;

// Pulsing glow behind a popup button. The glow exists only while the button itself is visible,
// so a button that the game reveals later (e.g. "Continue" after the typewriter) gets its glow
// at that moment, with a small pop-in.
[DisallowMultipleComponent]
public class ButtonGlow : MonoBehaviour
{
    RectTransform _btn, _glow;
    Image _img;
    Color _color;
    float _shownAt;

    public static void AttachTo(Button b, Color accent, float maxWidth = float.MaxValue)
    {
        if (!b || b.GetComponent<ButtonGlow>()) return;
        var rt = (RectTransform)b.transform;
        if (rt.rect.width > maxWidth || rt.name == "MuteBtn") return; // skip wide answer bars
        if (IsContinue(b)) return; // no glow behind any "Continue" button
        var g = b.gameObject.AddComponent<ButtonGlow>();
        g._color = Color.Lerp(accent, Color.white, 0.6f);
        g._color.a = 0.75f;
        if (b.gameObject.activeInHierarchy) g.OnEnable();
    }

    static bool IsContinue(Button b)
    {
        if (b.name.ToLower().Contains("continue")) return true;
        foreach (var t in b.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (t.text.ToLower().Contains("continue")) return true;
        return false;
    }

    void Init()
    {
        if (_glow) return;
        _btn = (RectTransform)transform;
        var go = new GameObject("BtnGlow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        _glow = (RectTransform)go.transform;
        _glow.SetParent(_btn.parent, false);
        _img = go.GetComponent<Image>();
        _img.sprite = JuiceSprites.Glow;
        _img.raycastTarget = false;
        go.SetActive(false);
    }

    void OnEnable()
    {
        if (_color.a <= 0f || !EnhancementsBootstrap.IsActive) return;
        Init();
        _glow.gameObject.SetActive(true);
        _shownAt = Time.unscaledTime;
        Sync();
        if (_btn.gameObject.activeInHierarchy) StartCoroutine(JuiceUI.Punch(_btn, 0.25f, 0.35f));
    }

    void OnDisable()
    {
        if (_glow) _glow.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_glow) Destroy(_glow.gameObject);
    }

    void LateUpdate()
    {
        if (!_glow) return;
        if (!EnhancementsBootstrap.IsActive) { _glow.gameObject.SetActive(false); return; }
        Sync();
        float open = Mathf.Clamp01((Time.unscaledTime - _shownAt) / 0.3f);
        float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f + _btn.GetInstanceID() * 0.1f);
        _glow.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.1f, k) * JuiceTween.EaseOutBack(open);
    }

    // Keep the glow centred under the button and just behind it.
    void Sync()
    {
        if (_glow.parent != _btn.parent) _glow.SetParent(_btn.parent, false);
        int bi = _btn.GetSiblingIndex(), gi = _glow.GetSiblingIndex();
        if (gi != bi - 1) _glow.SetSiblingIndex(gi < bi ? bi - 1 : bi);
        _glow.anchorMin = _glow.anchorMax = new Vector2(0.5f, 0.5f);
        _glow.pivot = new Vector2(0.5f, 0.5f);
        _glow.position = _btn.TransformPoint(_btn.rect.center);
        _glow.sizeDelta = _btn.rect.size * 1.7f;
        _img.color = _color;
    }
}
