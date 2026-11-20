using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pops a panel in (fade + overshoot scale) every time it is enabled, and plays a
// shrink + fade "pop out" when it is closed. The game still closes popups with
// SetActive(false) exactly as before: the close animation is played on a non-clickable
// visual copy, so no button, timing or game logic changes.
[DisallowMultipleComponent]
public class PanelPopIn : MonoBehaviour
{
    [SerializeField] float _duration = 0.35f;
    [SerializeField] float _startScale = 0.8f;
    public bool PlayClose = true;

    CanvasGroup _cg;
    readonly List<Transform> _targets = new List<Transform>();
    readonly List<Vector3> _baseScales = new List<Vector3>();
    bool _init;

    void Init()
    {
        if (_init) return;
        _init = true;
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();
        foreach (var t in ScaleTargets(transform)) { _targets.Add(t); _baseScales.Add(t.localScale); }
    }

    // Puts the popup at its full (non-animated) size so it can be measured.
    public void SnapToBase()
    {
        if (_init) SetScale(1f);
    }

    // Called after the popup was resized, so the animation keeps the new size as its base.
    public void Rebase()
    {
        if (!_init) return;
        _baseScales.Clear();
        // the animation may be part-way (scale = base * _current), so divide that out
        foreach (var t in _targets) _baseScales.Add(t ? t.localScale / Mathf.Max(0.01f, _current) : Vector3.one);
    }

    // A top-level canvas is re-scaled by its CanvasScaler every frame, so its children are animated instead.
    public static List<Transform> ScaleTargets(Transform root)
    {
        var list = new List<Transform>();
        var c = root.GetComponent<Canvas>();
        if (c && c.isRootCanvas) { foreach (Transform ch in root) list.Add(ch); }
        else list.Add(root);
        return list;
    }

    float _current = 1f;

    void SetScale(float s)
    {
        _current = s;
        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i]) _targets[i].localScale = _baseScales[i] * s;
    }

    bool _duplicate;

    void OnEnable()
    {
        // a parent already animates this popup: a second animation would save a half-animated size
        if (_duplicate) return;
        if (!_init && transform.parent)
            foreach (var p in transform.parent.GetComponentsInParent<PanelPopIn>(true))
                if (p != this) { _duplicate = true; Destroy(this); return; }
        Init();
        if (!EnhancementsBootstrap.IsActive) { SetScale(1f); _cg.alpha = 1f; return; }
        StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (!_init || _duplicate) return;
        SetScale(1f);
        _cg.alpha = 1f;
        if (PlayClose && EnhancementsBootstrap.IsActive && Application.isPlaying) PopupCloseGhost.Request(transform);
    }

    IEnumerator Play()
    {
        SetScale(_startScale);
        _cg.alpha = 0f;
        yield return JuiceTween.Run(_duration, p =>
        {
            SetScale(Mathf.LerpUnclamped(_startScale, 1f, JuiceTween.EaseOutBack(p)));
            _cg.alpha = JuiceTween.EaseOutCubic(Mathf.Clamp01(p * 1.6f));
        });
    }
}

// Plays the close animation of a popup that was just switched off.
// In LateUpdate of the same frame (before anything is drawn) it copies the object that was
// switched off, strips every game script from the copy, and shrinks + fades the copy away.
public class PopupCloseGhost : MonoBehaviour
{
    const float Duration = 0.22f;
    static PopupCloseGhost _runner;
    static readonly List<Transform> _pending = new List<Transform>();
    static bool _quitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _quitting = false;
        _pending.Clear();
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    static void OnQuitting() => _quitting = true;

    public static void Request(Transform popup)
    {
        if (_quitting || !popup) return;
        if (!_runner)
        {
            var go = new GameObject("PopupCloseGhost") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _runner = go.AddComponent<PopupCloseGhost>();
        }
        if (!_pending.Contains(popup)) _pending.Add(popup);
    }

    void LateUpdate()
    {
        if (_pending.Count == 0) return;
        var roots = new List<Transform>();
        foreach (var p in _pending)
        {
            var r = ClosedRoot(p);
            if (r && !roots.Contains(r)) roots.Add(r);
        }
        _pending.Clear();
        foreach (var r in roots) StartCoroutine(Animate(r));
    }

    // The object whose SetActive(false) hid the popup: the popup itself or a close ancestor
    // (e.g. the full-screen container that holds the frame). A whole canvas only counts when it
    // IS the popup (e.g. the settings canvas); the menu canvas switched off on a scene change
    // is ignored, as is anything that is visible again.
    static Transform ClosedRoot(Transform popup)
    {
        if (!popup || popup.gameObject.activeInHierarchy) return null;
        Transform t = popup;
        for (int i = 0; i < 4 && t; i++, t = t.parent)
        {
            var c = t.GetComponent<Canvas>();
            if (c && c.isRootCanvas && t != popup) return null;
            if (!t.gameObject.activeSelf)
                return !t.parent || t.parent.gameObject.activeInHierarchy ? t : null;
        }
        return null;
    }

    IEnumerator Animate(Transform root)
    {
        // copied while the original is inactive, so no Awake/OnEnable runs on the copy's scripts
        var ghost = Instantiate(root.gameObject, root.parent, false);
        ghost.name = root.name + " (closing)";
        if (!root.parent) SceneManagerMoveToScene(ghost, root.gameObject);
        StripScripts(ghost);
        // decorations that PopupDecorator switched off together with the popup
        foreach (var t in ghost.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("PopupFX_")) t.gameObject.SetActive(true);

        var rt = ghost.transform;
        if (root.parent) rt.SetSiblingIndex(root.GetSiblingIndex() + 1);
        var cg = ghost.GetComponent<CanvasGroup>();
        if (!cg) cg = ghost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;   // never clickable; interactable stays on so buttons keep their normal tint
        cg.alpha = 1f;
        var targets = PanelPopIn.ScaleTargets(rt);
        var bases = new List<Vector3>();
        foreach (var t in targets) bases.Add(t.localScale);
        ghost.SetActive(true);

        yield return JuiceTween.Run(Duration, p =>
        {
            if (!ghost) return;
            // a small anticipation up, then shrink away
            float s = p < 0.25f ? Mathf.Lerp(1f, 1.04f, p / 0.25f)
                                : Mathf.Lerp(1.04f, 0.78f, JuiceTween.EaseInCubic((p - 0.25f) / 0.75f));
            for (int i = 0; i < targets.Count; i++) if (targets[i]) targets[i].localScale = bases[i] * s;
            cg.alpha = 1f - JuiceTween.EaseInCubic(p);
        });
        if (ghost) Destroy(ghost);
    }

    static void SceneManagerMoveToScene(GameObject ghost, GameObject original)
    {
        if (original.scene.IsValid() && ghost.scene != original.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ghost, original.scene);
    }

    // Keeps only visuals (UnityEngine.UI, TextMeshPro, CanvasGroup...). Game scripts are removed
    // so the copy can't run any logic. Repeated passes respect [RequireComponent] chains.
    static void StripScripts(GameObject go)
    {
        foreach (var a in go.GetComponentsInChildren<Animator>(true)) DestroyImmediate(a);
        foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
        for (int pass = 0; pass < 4; pass++)
        {
            bool left = false;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!mb) continue;
                string ns = mb.GetType().Namespace ?? "";
                if (ns.StartsWith("UnityEngine.UI") || ns.StartsWith("TMPro")) continue;
                if (IsRequiredByOther(mb)) { left = true; continue; }
                DestroyImmediate(mb);
            }
            if (!left) break;
        }
    }

    static bool IsRequiredByOther(Component c)
    {
        var type = c.GetType();
        foreach (var other in c.GetComponents<MonoBehaviour>())
        {
            if (!other || other == c) continue;
            foreach (RequireComponent rc in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                if ((rc.m_Type0 != null && rc.m_Type0.IsAssignableFrom(type)) ||
                    (rc.m_Type1 != null && rc.m_Type1.IsAssignableFrom(type)) ||
                    (rc.m_Type2 != null && rc.m_Type2.IsAssignableFrom(type))) return true;
        }
        return false;
    }
}
