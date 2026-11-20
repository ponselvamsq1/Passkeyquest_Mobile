using TMPro;
using UnityEngine;

// Keeps an icon centred in the gap of spaces left for it inside a text ("Click     to skip..."),
// whatever size or wrapping the text ends up with.
public class IconInTextGap : MonoBehaviour
{
    public TMP_Text Text;
    public RectTransform Icon;

    void LateUpdate()
    {
        if (!Text || !Icon) return;
        string s = Text.text;
        int start = s.IndexOf("   ");
        if (start < 0) return;
        int end = start;
        while (end + 1 < s.Length && s[end + 1] == ' ') end++;

        Text.ForceMeshUpdate();
        var info = Text.textInfo;
        if (end >= info.characterCount) return;
        var a = info.characterInfo[start];
        var b = info.characterInfo[end];
        var line = info.lineInfo[a.lineNumber];
        float x = (a.origin + b.xAdvance) * 0.5f;
        float y = (line.ascender + line.descender) * 0.5f;
        Icon.position = Text.transform.TransformPoint(new Vector3(x, y, 0f));
    }
}
