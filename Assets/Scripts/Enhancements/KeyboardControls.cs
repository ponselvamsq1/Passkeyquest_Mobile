using UnityEngine;
using UnityEngine.EventSystems;

// Keyboard play for the Editor / Windows build. Touch buttons keep working as before.
// PlayerStateManager handles: A/D or Left/Right = move, Space = normal jump.
// Added here: W or Up = high jump, and Space no longer re-clicks the last pressed UI button.
[RequireComponent(typeof(PlayerStateManager))]
public class KeyboardControls : MonoBehaviour
{
    PlayerStateManager _p;

    void Awake() => _p = GetComponent<PlayerStateManager>();

    void Update()
    {
        if (!EnhancementsBootstrap.IsActive || Time.timeScale == 0f) return;

        // A clicked UI button stays "selected", and Space would press it again. Drop that selection.
        var es = EventSystem.current;
        if (es && es.currentSelectedGameObject && (Input.GetKey(KeyCode.Space) || Input.GetAxisRaw("Horizontal") != 0f
            || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)))
            es.SetSelectedGameObject(null);

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            _p.HighJumpButtonDown();
    }
}
