using UnityEngine;

// Added to a text whose font was switched while it was hidden. The first frame it is shown it is
// re-enabled once, so TMP rebuilds it with the new font (otherwise glyphs can come out garbled).
public class TmpFontRefresh : MonoBehaviour
{
    void LateUpdate()
    {
        gameObject.SetActive(false);
        Destroy(this);
        gameObject.SetActive(true);
    }
}
