using UnityEngine;
using UnityEngine.UI;

// Puts a copy of the HUD "Sound" button into the Level 1 pause menu ("Pausepage"), between Resume and Exit.
// The copy keeps the original's click function (opens the sound settings popup).
public static class PauseMuteButton
{
    public static void AttachTo(Transform pausePage)
    {
        var card = pausePage.Find("Image");
        if (!card || card.Find("Sound")) return;
        var resume = card.Find("Resume") as RectTransform;
        var exit = card.Find("Exit") as RectTransform;
        if (!resume || !exit) return;

        Button hudSound = null;
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
            if (b.name == "Sound" && b.gameObject.scene == pausePage.gameObject.scene && b.transform.parent != card)
            { hudSound = b; break; }
        if (!hudSound) return;

        float y = resume.anchoredPosition.y;
        resume.anchoredPosition = new Vector2(-270f, y);
        exit.anchoredPosition = new Vector2(270f, y);

        var copy = Object.Instantiate(hudSound.gameObject, card, false);
        copy.name = "Sound";
        copy.SetActive(true);
        var rt = (RectTransform)copy.transform;
        rt.anchorMin = rt.anchorMax = resume.anchorMin;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.sizeDelta = new Vector2(145f, 145f);
        rt.anchoredPosition = new Vector2(0f, y);
        foreach (var bj in copy.GetComponentsInChildren<ButtonJuice>(true)) Object.Destroy(bj);

        // The sound settings popup lives on a lower canvas than the pause page; lift it on top so it isn't hidden.
        var pauseCanvas = pausePage.GetComponentInParent<Canvas>(true);
        var ui = UIManager.Instance;
        var settings = ui ? UIPolish.GetField<GameObject>(ui, "_settingsPanel") : null;
        var settingsCanvas = settings ? settings.GetComponentInParent<Canvas>(true) : null;
        if (pauseCanvas && settingsCanvas)
        {
            var root = settingsCanvas.rootCanvas;
            if (root.sortingOrder <= pauseCanvas.rootCanvas.sortingOrder)
                root.sortingOrder = pauseCanvas.rootCanvas.sortingOrder + 1;
        }
    }
}
