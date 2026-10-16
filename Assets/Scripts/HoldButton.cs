using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ButtonType { Left, Right, LowJump, HighJump }

    [SerializeField] private ButtonType buttonType;
    [SerializeField] private PlayerStateManager player;

    private int _pointerId = int.MinValue;   // finger holding a Left/Right button
    private bool _isHeld;

    private bool IsMoveButton => buttonType == ButtonType.Left || buttonType == ButtonType.Right;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerStateManager>();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (player == null) return;

        // Jump buttons: fire on every tap, never blocked by held state
        if (buttonType == ButtonType.LowJump)  { player.LowJumpButtonDown();  return; }
        if (buttonType == ButtonType.HighJump) { player.HighJumpButtonDown(); return; }

        // Left/Right: hold, owned by one finger
        if (_isHeld) return;
        _isHeld = true;
        _pointerId = e.pointerId;

        if (buttonType == ButtonType.Left) player.LeftButtonDown();
        else                               player.RightButtonDown();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (IsMoveButton && _isHeld && e.pointerId == _pointerId)
            Release();
    }

    public void OnPointerExit(PointerEventData e)
    {
        // Finger slid off the button, so release to avoid stuck movement
        if (IsMoveButton && _isHeld && e.pointerId == _pointerId)
            Release();
    }

    private void OnDisable()
    {
        if (_isHeld) Release();
    }

    private void Release()
    {
        _isHeld = false;
        _pointerId = int.MinValue;

        if (player == null) return;

        if (buttonType == ButtonType.Left) player.LeftButtonUp();
        else if (buttonType == ButtonType.Right) player.RightButtonUp();
    }
}