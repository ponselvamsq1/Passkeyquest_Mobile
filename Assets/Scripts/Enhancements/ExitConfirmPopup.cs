using System;
using UnityEngine;
using UnityEngine.UI;

// "Are you sure you want to quit?" popup with No / Yes.
// The look is the prefab Assets/Resources/ExitConfirmPopup.prefab - open it to edit text, colours, sizes.
// Keep the button names "NoButton" and "YesButton" so this script can find them.
// Usage: ExitConfirmPopup.Show(Application.Quit);
public static class ExitConfirmPopup
{
    const string PrefabPath = "ExitConfirmPopup"; // under a Resources folder
    static GameObject _root;

    public static void Show(Action onYes)
    {
        if (_root) return; // already open

        var prefab = Resources.Load<GameObject>(PrefabPath);
        if (!prefab)
        {
            Debug.LogError("ExitConfirmPopup prefab not found in Resources - quitting without confirmation.");
            onYes?.Invoke();
            return;
        }
        _root = UnityEngine.Object.Instantiate(prefab);
        _root.name = prefab.name;

        // make sure clicks work even if the scene has no EventSystem
        if (!UnityEngine.EventSystems.EventSystem.current)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        Hook("NoButton", Close);
        Hook("YesButton", () => { Close(); onYes?.Invoke(); });

        var panel = _root.transform.Find("Panel");
        if (panel)
        {
            // same gold frame + "Exit" heading box as every other popup
            var img = panel.GetComponent<Image>();
            if (img) EnhancementsBootstrap.Decorate(img, "Exit");
            if (JuiceUI.I) JuiceUI.I.StartCoroutine(JuiceUI.Punch(panel, 0.15f, 0.3f));
        }
    }

    public static void Close()
    {
        if (_root) UnityEngine.Object.Destroy(_root);
        _root = null;
    }

    static void Hook(string name, Action onClick)
    {
        foreach (var b in _root.GetComponentsInChildren<Button>(true))
            if (b.name == name) { b.onClick.AddListener(() => onClick()); return; }
        Debug.LogWarning("ExitConfirmPopup: button '" + name + "' not found in prefab.");
    }
}
