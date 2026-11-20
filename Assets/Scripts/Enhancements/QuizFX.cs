using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A/B/C/D badges on the quiz answers + a bright outline on the selected answer.
public class QuizOptionStyler : MonoBehaviour
{
    Quiz _quiz;
    Image[] _outlines;
    static readonly string[] Letters = { "A", "B", "C", "D", "E", "F" };

    void Awake()
    {
        _quiz = GetComponent<Quiz>();
        var opts = _quiz.optionButtons;
        _outlines = new Image[opts.Length];
        for (int i = 0; i < opts.Length; i++)
        {
            var rt = (RectTransform)opts[i].transform;
            // (A/B/C/D badges removed: the answer text already starts with its letter)

            var o = JuiceUI.NewImage(rt, "SelectOutline", JuiceSprites.Border, new Color(1f, 0.95f, 0.4f), Vector2.zero);
            o.type = Image.Type.Sliced;
            var ort = o.rectTransform;
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = new Vector2(-8, -8); ort.offsetMax = new Vector2(8, 8);
            o.enabled = false;
            _outlines[i] = o;
        }
    }

    void LateUpdate()
    {
        if (!EnhancementsBootstrap.IsActive) return;
        var opts = _quiz.optionButtons;
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
        for (int i = 0; i < opts.Length; i++)
        {
            var c = opts[i].GetComponent<Image>().color;
            bool selected = Mathf.Abs(c.r - c.g) < 0.02f && Mathf.Abs(c.g - c.b) < 0.02f && c.r > 0.3f && c.r < 0.7f; // the game marks the pick grey
            _outlines[i].enabled = selected;
            if (selected) _outlines[i].color = new Color(1f, 0.95f, 0.4f, pulse);
        }
    }
}

// Feedback when a quiz result panel opens.
public class QuizPanelFX : MonoBehaviour
{
    public enum Kind { Correct, Wrong, Reveal, Finish }
    public Kind Type;

    void OnEnable()
    {
        if (!EnhancementsBootstrap.IsActive) return;
        StartCoroutine(Play());
    }

    RectTransform Frame()
    {
        foreach (var d in GetComponentsInChildren<PopupDecorator>(true)) return (RectTransform)d.transform;
        return (RectTransform)transform;
    }

    IEnumerator Play()
    {
        yield return null; // let PanelPopIn start first
        switch (Type)
        {
            case Kind.Correct:
                JuiceAudio.Play(JuiceAudio.Fx.Chime);
                JuiceUI.I.Flash(new Color(0.3f, 1f, 0.45f, 0.35f), 0.5f);
                JuiceUI.I.ConfettiBurst(60);
                break;
            case Kind.Wrong:
            case Kind.Reveal:
                JuiceAudio.Play(JuiceAudio.Fx.Buzz, Type == Kind.Wrong ? 1f : 0.6f);
                JuiceUI.I.Flash(new Color(1f, 0.15f, 0.15f, 0.4f), 0.45f);
                Haptics.Buzz();
                yield return new WaitForSecondsRealtime(0.3f); // after the pop-in settles
                yield return JuiceUI.Shake(Frame(), 32f, 0.45f);
                break;
            case Kind.Finish:
                yield return FinishStars();
                break;
        }
    }

    IEnumerator FinishStars()
    {
        JuiceUI.I.ConfettiBurst(90);
        JuiceAudio.Play(JuiceAudio.Fx.Chime);
        var frame = Frame();
        var row = new GameObject("FinishStars", typeof(RectTransform));
        var rt = (RectTransform)row.transform;
        rt.SetParent(frame, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.8f);
        rt.SetAsLastSibling();
        for (int i = 0; i < 3; i++)
        {
            var s = JuiceUI.NewImage(rt, "Star", JuiceSprites.Star, new Color(1f, 0.85f, 0.2f), Vector2.one * (i == 1 ? 150 : 115));
            s.rectTransform.anchoredPosition = new Vector2((i - 1) * 150, i == 1 ? 25 : 0);
            s.transform.localScale = Vector3.zero;
        }
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            var s = rt.GetChild(i);
            JuiceAudio.Play(JuiceAudio.Fx.StarDing, 0.9f, 1f + i * 0.15f);
            yield return JuiceTween.Run(0.35f, p => { if (s) s.localScale = Vector3.one * JuiceTween.EaseOutBack(p); });
        }
    }

    void OnDisable()
    {
        var t = transform.GetComponentsInChildren<Transform>(true);
        foreach (var x in t) if (x && x.name == "FinishStars") Destroy(x.gameObject);
    }
}
