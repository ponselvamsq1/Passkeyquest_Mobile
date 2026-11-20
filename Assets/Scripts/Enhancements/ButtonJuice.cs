using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds hover grow, press squash and release bounce to a UI Button.
// Does not touch onClick, so every existing button function keeps working.
[DisallowMultipleComponent]
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float _hoverScale = 1.06f;
    [SerializeField] float _pressScale = 0.9f;

    Vector3 _baseScale;
    Coroutine _co;
    Selectable _sel;
    bool _hovering;

    void Awake()
    {
        _baseScale = transform.localScale;
        _sel = GetComponent<Selectable>();
    }

    void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        _hovering = false;
        transform.localScale = _baseScale;
    }

    bool Usable => EnhancementsBootstrap.IsActive && (_sel == null || _sel.IsInteractable());

    void TweenTo(float mul, float dur, bool bounce)
    {
        if (!isActiveAndEnabled) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(JuiceTween.Scale(transform, transform.localScale, _baseScale * mul, dur,
            bounce ? (System.Func<float, float>)JuiceTween.EaseOutBack : JuiceTween.EaseOutCubic));
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!Usable) return;
        _hovering = true;
        TweenTo(_hoverScale, 0.15f, false);
        // no hover sound on buttons
    }
    public void OnPointerExit(PointerEventData e) { _hovering = false; if (!EnhancementsBootstrap.IsActive) return; TweenTo(1f, 0.15f, false); }
    public bool PlayClickSfx { get; set; } = true;

    public void OnPointerDown(PointerEventData e)
    {
        if (!Usable) return;
        TweenTo(_pressScale, 0.08f, false);
        if (PlayClickSfx && SoundsManager.Instance)
            SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 0.5f);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (!EnhancementsBootstrap.IsActive) return;
        TweenTo(_hovering && Input.mousePresent ? _hoverScale : 1f, 0.3f, true);
    }
}
