using UnityEngine;

// Procedurally generated decoration sprites (no new image files needed).
public static class JuiceSprites
{
    static Sprite _glow, _rays, _star, _border, _pill, _vgrad, _dot;

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static Sprite Make(Texture2D t, Vector4 border = default)
    {
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    // Soft radial glow, white -> transparent.
    public static Sprite Glow
    {
        get
        {
            if (_glow) return _glow;
            const int s = 128; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            return _glow = Make(t);
        }
    }

    // Sunburst rays fading outwards.
    public static Sprite Rays
    {
        get
        {
            if (_rays) return _rays;
            const int s = 256; const int count = 14; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                Vector2 v = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f);
                float d = v.magnitude / (s / 2f);
                float ang = Mathf.Atan2(v.y, v.x) / (Mathf.PI * 2f) * count;
                float ray = Mathf.Abs(ang - Mathf.Round(ang)) * 2f;           // 0 at ray center, 1 between rays
                float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.25f, 0.6f, ray));
                a *= Mathf.Clamp01(1f - d) * Mathf.Clamp01(d * 4f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            return _rays = Make(t);
        }
    }

    // Four-point sparkle star.
    public static Sprite Star
    {
        get
        {
            if (_star) return _star;
            const int s = 64; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - s / 2f) / (s / 2f), dy = Mathf.Abs(y + 0.5f - s / 2f) / (s / 2f);
                float arms = Mathf.Max(Mathf.Clamp01(1f - dx * 6f) * (1f - dy), Mathf.Clamp01(1f - dy * 6f) * (1f - dx));
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.5f);
                float a = Mathf.Clamp01(arms + core * core);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            return _star = Make(t);
        }
    }

    public static Sprite Dot
    {
        get
        {
            if (_dot) return _dot;
            const int s = 16; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 2f)));
            }
            return _dot = Make(t);
        }
    }

    static Sprite _circle, _white;

    // Solid anti-aliased circle.
    public static Sprite Circle
    {
        get
        {
            if (_circle) return _circle;
            const int s = 64; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f));
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(s / 2f - d)));
            }
            return _circle = Make(t);
        }
    }

    public static Sprite White
    {
        get
        {
            if (_white) return _white;
            var t = NewTex(4, 4);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
            return _white = Make(t);
        }
    }

    // Rounded-rectangle outline, 9-sliced.
    public static Sprite Border
    {
        get
        {
            if (_border) return _border;
            const int s = 96, r = 30; const float w = 5f; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, s, s, r);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d + w) / w * 1.0f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            return _border = Make(t, new Vector4(r + 4, r + 4, r + 4, r + 4));
        }
    }

    // Filled rounded pill with a lighter top half, 9-sliced (for the header ribbon).
    public static Sprite Pill
    {
        get
        {
            if (_pill) return _pill;
            const int s = 96, r = 40; var t = NewTex(s, s);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, s, s, r);
                float a = Mathf.Clamp01(-d);
                float shade = Mathf.Lerp(0.78f, 1f, y / (float)s);
                if (d > -6f && d <= 0f) shade *= 0.7f; // darker rim
                t.SetPixel(x, y, new Color(shade, shade, shade, a));
            }
            return _pill = Make(t, new Vector4(r + 2, r + 2, r + 2, r + 2));
        }
    }

    // Vertical gradient: opaque at top, transparent at bottom.
    public static Sprite VGradient
    {
        get
        {
            if (_vgrad) return _vgrad;
            var t = NewTex(4, 64);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 4; x++)
            {
                float a = Mathf.Pow(y / 63f, 2f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            return _vgrad = Make(t);
        }
    }

    // Signed distance to a rounded rect (negative inside).
    static float RoundRectDist(float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
        float qy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
        float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }
}
