using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Vẽ icon skill + khung thẻ bài bằng code (SDF), xuất ra PNG trong Assets/SurvivalShooter/Art.
// Menu: Tools > Survival Shooter > Generate Icons
public static class IconGenerator
{
    public const string ArtDir = "Assets/SurvivalShooter/Art/";
    public const string IconDir = ArtDir + "Icons/";
    const int Size = 128;

    // Hàm khoảng cách có dấu: < 0 là bên trong hình
    delegate float Sdf(Vector2 p);

    [MenuItem("Tools/Survival Shooter/Generate Icons")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(IconDir);

        Icon("damage", new Color(1f, 0.35f, 0.25f), Sword());
        Icon("firerate", new Color(1f, 0.85f, 0.2f), Bolt());
        Icon("health", new Color(1f, 0.3f, 0.4f), Heart());
        Icon("speed", new Color(0.4f, 0.9f, 1f), Chevrons());
        Icon("magnet", new Color(0.9f, 0.3f, 0.3f), Magnet());
        Icon("armor", new Color(0.7f, 0.75f, 0.85f), Shield());
        Icon("regen", new Color(0.35f, 1f, 0.45f), Cross());
        Icon("crit", new Color(1f, 0.5f, 0.15f), Eye());
        Icon("xp", new Color(0.3f, 1f, 0.55f), Gem());
        Icon("pierce", new Color(0.6f, 0.85f, 1f), Arrow());
        Icon("lifesteal", new Color(0.85f, 0.1f, 0.2f), Drop());
        Icon("critdmg", new Color(1f, 0.3f, 0.2f), Skull());
        Icon("aura", new Color(1f, 0.55f, 0.1f), Flame());
        Icon("gamble", new Color(0.95f, 0.25f, 0.5f), HalfHeartSword());
        Icon("multishot", new Color(1f, 0.85f, 0.35f), ThreeBullets());
        Icon("frenzy", new Color(1f, 0.4f, 0.1f), Fist());
        Icon("immortal", new Color(1f, 0.85f, 0.4f), Crown());

        CardFrame("card_common", new Color(0.62f, 0.64f, 0.72f), new Color(0.13f, 0.12f, 0.18f), 0);
        CardFrame("card_rare", new Color(0.3f, 0.6f, 1f), new Color(0.08f, 0.13f, 0.26f), 1);
        CardFrame("card_epic", new Color(0.78f, 0.38f, 1f), new Color(0.17f, 0.07f, 0.26f), 2);
        IconSlot();
        FlameRing();

        ConfigureImporters();
        Debug.Log("Icons generated in " + IconDir);
    }

    // ---------- Shapes (toạ độ chuẩn hoá -1..1, y hướng lên) ----------

    static Sdf Sword() => Union(
        Rotate(Box(new Vector2(0f, 0.25f), new Vector2(0.11f, 0.55f)), 0),
        Rotate(Triangle(new Vector2(-0.11f, 0.8f), new Vector2(0.11f, 0.8f), new Vector2(0f, 0.98f)), 0),
        Box(new Vector2(0f, -0.38f), new Vector2(0.38f, 0.07f)),
        Box(new Vector2(0f, -0.62f), new Vector2(0.07f, 0.2f)),
        Circle(new Vector2(0f, -0.86f), 0.1f)).Rot(45);

    static Sdf Bolt() => Union(
        Poly(new Vector2(0.15f, 0.95f), new Vector2(-0.55f, -0.05f), new Vector2(-0.02f, -0.05f), new Vector2(-0.2f, -0.95f), new Vector2(0.58f, 0.15f), new Vector2(0.05f, 0.15f)));

    static Sdf Heart() => p =>
    {
        p.y += 0.15f;
        float a = Circle(new Vector2(-0.32f, 0.2f), 0.4f)(p);
        float b = Circle(new Vector2(0.32f, 0.2f), 0.4f)(p);
        float c = Triangle(new Vector2(-0.7f, 0.05f), new Vector2(0.7f, 0.05f), new Vector2(0f, -0.8f))(p);
        return Mathf.Min(a, Mathf.Min(b, c));
    };

    static Sdf Chevrons() => Union(
        Chevron(new Vector2(-0.35f, 0f)), Chevron(new Vector2(0.2f, 0f)));

    static Sdf Chevron(Vector2 o) => p =>
    {
        p -= o;
        p.y = Mathf.Abs(p.y);
        return Segment(new Vector2(-0.2f, 0.55f), new Vector2(0.2f, 0f), 0.13f)(p);
    };

    static Sdf Magnet() => p =>
    {
        float ring = Mathf.Abs(Vector2.Distance(p, new Vector2(0f, 0.1f)) - 0.5f) - 0.17f;
        if (p.y < 0.1f) ring = Mathf.Max(Mathf.Abs(Mathf.Abs(p.x) - 0.5f) - 0.17f, Mathf.Max(p.y - 0.1f, -0.75f - p.y));
        return ring;
    };

    static Sdf Shield() => p =>
    {
        // Khiên: thân là đa giác nhọn ở dưới, khoét một dải dọc ở giữa làm hoa văn
        float body = Poly(new Vector2(-0.68f, 0.72f), new Vector2(0f, 0.85f), new Vector2(0.68f, 0.72f),
                          new Vector2(0.62f, -0.05f), new Vector2(0.35f, -0.55f), new Vector2(0f, -0.9f),
                          new Vector2(-0.35f, -0.55f), new Vector2(-0.62f, -0.05f))(p);
        float stripe = Box(new Vector2(0f, 0f), new Vector2(0.06f, 0.62f))(p);
        return Mathf.Max(body, -stripe);
    };

    static Sdf Cross() => Union(Box(Vector2.zero, new Vector2(0.22f, 0.7f)), Box(Vector2.zero, new Vector2(0.7f, 0.22f)));

    static Sdf Eye() => p =>
    {
        // Hình hạt đậu (giao 2 hình tròn), khoét lỗ tròng mắt, rồi đặt con ngươi vào giữa
        float lens = Mathf.Max(Circle(new Vector2(0f, -0.55f), 0.95f)(p), Circle(new Vector2(0f, 0.55f), 0.95f)(p));
        float hole = Circle(Vector2.zero, 0.34f)(p);
        float pupil = Circle(Vector2.zero, 0.2f)(p);
        return Mathf.Min(Mathf.Max(lens, -hole), pupil);
    };

    static Sdf Gem() => Poly(
        new Vector2(0f, 0.85f), new Vector2(0.6f, 0.25f), new Vector2(0f, -0.9f), new Vector2(-0.6f, 0.25f));

    static Sdf Arrow() => Union(
        Box(new Vector2(-0.15f, 0f), new Vector2(0.6f, 0.08f)),
        Triangle(new Vector2(0.35f, 0.32f), new Vector2(0.35f, -0.32f), new Vector2(0.9f, 0f)),
        Poly(new Vector2(-0.75f, 0.3f), new Vector2(-0.55f, 0.3f), new Vector2(-0.4f, 0.08f), new Vector2(-0.6f, 0.08f)),
        Poly(new Vector2(-0.75f, -0.3f), new Vector2(-0.55f, -0.3f), new Vector2(-0.4f, -0.08f), new Vector2(-0.6f, -0.08f))).Rot(30);

    static Sdf Drop() => p =>
    {
        float c = Circle(new Vector2(0f, -0.25f), 0.52f)(p);
        float t = Triangle(new Vector2(-0.47f, -0.05f), new Vector2(0.47f, -0.05f), new Vector2(0f, 0.9f))(p);
        return Mathf.Min(c, t);
    };

    static Sdf Skull() => p =>
    {
        float head = Circle(new Vector2(0f, 0.15f), 0.62f)(p);
        float jaw = Box(new Vector2(0f, -0.5f), new Vector2(0.36f, 0.25f))(p);
        float s = Mathf.Min(head, jaw);
        float eyeL = Circle(new Vector2(-0.24f, 0.08f), 0.17f)(p);
        float eyeR = Circle(new Vector2(0.24f, 0.08f), 0.17f)(p);
        float nose = Triangle(new Vector2(-0.09f, -0.3f), new Vector2(0.09f, -0.3f), new Vector2(0f, -0.1f))(p);
        // Khe răng: các vạch dọc ở phần hàm
        float gaps = Mathf.Max(Box(new Vector2(0f, -0.62f), new Vector2(0.3f, 0.13f))(p), Mathf.Abs(Mathf.Repeat(p.x + 0.1f, 0.2f) - 0.1f) - 0.025f);
        return Mathf.Max(s, -Mathf.Min(Mathf.Min(eyeL, eyeR), Mathf.Min(nose, gaps)));
    };

    static Sdf Flame() => p =>
    {
        float outer = Mathf.Min(Circle(new Vector2(0f, -0.3f), 0.55f)(p),
            Poly(new Vector2(-0.53f, -0.15f), new Vector2(-0.35f, 0.45f), new Vector2(-0.1f, 0.25f), new Vector2(0.05f, 0.95f), new Vector2(0.35f, 0.4f), new Vector2(0.53f, -0.15f))(p));
        return outer;
    };

    static Sdf HalfHeartSword() => p =>
    {
        float heart = Heart()(p * 1.15f + new Vector2(0.25f, 0f));
        heart = Mathf.Max(heart, p.x - 0.0f);
        float sword = Union(Box(new Vector2(0.35f, 0.15f), new Vector2(0.09f, 0.55f)),
                            Triangle(new Vector2(0.26f, 0.7f), new Vector2(0.44f, 0.7f), new Vector2(0.35f, 0.9f)),
                            Box(new Vector2(0.35f, -0.42f), new Vector2(0.25f, 0.06f)),
                            Box(new Vector2(0.35f, -0.62f), new Vector2(0.06f, 0.15f)))(p);
        return Mathf.Min(heart, sword);
    };

    static Sdf ThreeBullets() => Union(Bullet(new Vector2(-0.48f, -0.1f)), Bullet(new Vector2(0f, 0.08f)), Bullet(new Vector2(0.48f, -0.1f)));

    static Sdf Bullet(Vector2 o) => p =>
    {
        p -= o;
        float body = Box(new Vector2(0f, -0.25f), new Vector2(0.15f, 0.4f))(p);
        float tip = Circle(new Vector2(0f, 0.15f), 0.15f)(p);
        float nose = Triangle(new Vector2(-0.15f, 0.15f), new Vector2(0.15f, 0.15f), new Vector2(0f, 0.55f))(p);
        return Mathf.Min(body, Mathf.Min(tip, nose));
    };

    static Sdf Fist() => p =>
    {
        float palm = RoundBox(new Vector2(0f, -0.25f), new Vector2(0.48f, 0.42f), 0.15f)(p);
        float fingers = 9f;
        for (int i = 0; i < 4; i++)
            fingers = Mathf.Min(fingers, RoundBox(new Vector2(-0.36f + i * 0.24f, 0.32f), new Vector2(0.11f, 0.22f), 0.1f)(p));
        float thumb = RoundBox(new Vector2(-0.55f, -0.05f), new Vector2(0.12f, 0.28f), 0.1f)(p);
        return Mathf.Min(palm, Mathf.Min(fingers, thumb));
    };

    static Sdf Crown() => p =>
    {
        float baseBand = Box(new Vector2(0f, -0.45f), new Vector2(0.72f, 0.15f))(p);
        float body = Poly(new Vector2(-0.72f, -0.32f), new Vector2(-0.72f, 0.45f), new Vector2(-0.36f, 0.05f), new Vector2(0f, 0.6f),
                          new Vector2(0.36f, 0.05f), new Vector2(0.72f, 0.45f), new Vector2(0.72f, -0.32f))(p);
        float balls = Mathf.Min(Circle(new Vector2(-0.72f, 0.55f), 0.11f)(p), Mathf.Min(Circle(new Vector2(0f, 0.7f), 0.12f)(p), Circle(new Vector2(0.72f, 0.55f), 0.11f)(p)));
        return Mathf.Min(Mathf.Min(baseBand, body), balls);
    };

    // ---------- SDF primitives ----------

    static Sdf Circle(Vector2 c, float r) => p => Vector2.Distance(p, c) - r;

    static Sdf Box(Vector2 c, Vector2 half) => p =>
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half;
        return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0);
    };

    static Sdf RoundBox(Vector2 c, Vector2 half, float r) => p => Box(c, half - Vector2.one * r)(p) - r;

    static Sdf Segment(Vector2 a, Vector2 b, float r) => p =>
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    };

    static Sdf Triangle(Vector2 a, Vector2 b, Vector2 c) => Poly(a, b, c);

    // Đa giác lồi hoặc lõm bất kỳ (khoảng cách tới cạnh gần nhất, dấu theo even-odd)
    static Sdf Poly(params Vector2[] v) => p =>
    {
        float d = float.MaxValue;
        bool inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            Vector2 e = v[j] - v[i], w = p - v[i];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, b.sqrMagnitude);
            if ((v[i].y > p.y) != (v[j].y > p.y) && p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
                inside = !inside;
        }
        return (inside ? -1f : 1f) * Mathf.Sqrt(d);
    };

    static Sdf Union(params Sdf[] s) => p =>
    {
        float d = float.MaxValue;
        foreach (var f in s) d = Mathf.Min(d, f(p));
        return d;
    };

    static Sdf Rotate(Sdf s, float deg) => s.Rot(deg);

    static Sdf Rot(this Sdf s, float deg)
    {
        float r = -deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return p => s(new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs));
    }

    // ---------- Rasterise ----------

    // Icon: hình tô gradient + viền tối + bóng đổ, nền trong suốt
    static void Icon(string name, Color color, Sdf shape)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var px = new Color[Size * Size];
        float pixel = 2f / Size;
        Color dark = Color.Lerp(color, Color.black, 0.75f);
        Color light = Color.Lerp(color, Color.white, 0.45f);
        Vector2 shadowOffset = new Vector2(0.05f, -0.06f);

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f) * 1.12f;
            float d = shape(p);
            float sd = shape(p - shadowOffset);

            // bóng đổ
            Color c = new Color(0, 0, 0, Mathf.Clamp01(0.5f - sd / (pixel * 3f)) * 0.45f);

            // viền
            float outline = Mathf.Clamp01(0.5f - (d - 0.07f) / pixel);
            c = Over(c, new Color(dark.r, dark.g, dark.b, outline));

            // thân: gradient từ trên sáng xuống dưới tối + highlight cạnh trong
            float fill = Mathf.Clamp01(0.5f - d / pixel);
            float t = Mathf.InverseLerp(-1f, 1f, p.y);
            Color body = Color.Lerp(color * 0.8f, light, t * t);
            float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 0.035f) / 0.03f) * Mathf.Clamp01(p.y + 0.3f);
            body = Color.Lerp(body, Color.white, rim * 0.5f);
            body.a = fill;
            c = Over(c, body);

            px[y * Size + x] = c;
        }
        tex.SetPixels(px);
        Save(tex, IconDir + name + ".png");
    }

    // Khung thẻ bài: nền tối có gradient, viền màu độ hiếm, góc bo, ánh sáng phía trên, đá quý ở đỉnh
    static void CardFrame(string name, Color rim, Color bg, int gems)
    {
        const int W = 256, H = 320;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color[W * H];
        float radius = 22f;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - W / 2f, y + 0.5f - H / 2f);
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(W / 2f - radius - 2f, H / 2f - radius - 2f);
            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;

            float alpha = Mathf.Clamp01(0.5f - d);
            float ty = (float)y / H;
            Color inner = Color.Lerp(bg * 0.7f, bg * 1.5f, ty);
            inner.a = 1f;
            // vầng sáng màu độ hiếm ở phía trên
            float glow = Mathf.Exp(-((p.x * p.x) / 9000f + Mathf.Pow((H / 2f - p.y) / 70f, 2f)));
            inner = Color.Lerp(inner, rim, glow * 0.35f);

            // viền đôi
            float border = Mathf.Clamp01(1f - Mathf.Abs(d + 4f) / 3.5f);
            float innerLine = Mathf.Clamp01(1f - Mathf.Abs(d + 13f) / 1.2f) * 0.5f;
            Color c = Color.Lerp(inner, Color.Lerp(rim, Color.white, 0.25f), border);
            c = Color.Lerp(c, rim, innerLine);

            // vạch phân cách dưới tiêu đề
            float sep = Mathf.Clamp01(1f - Mathf.Abs(y - (H - 128f)) / 1.2f) * Mathf.Clamp01(1f - Mathf.Abs(p.x) / 95f);
            c = Color.Lerp(c, rim, sep * 0.8f);

            c.a = alpha;
            px[y * W + x] = c;
        }

        // đá quý trên đỉnh, số viên = độ hiếm + 1
        for (int g = 0; g <= gems; g++)
        {
            float gx = W / 2f + (g - gems / 2f) * 26f;
            float gy = H - 6f;
            for (int y = (int)gy - 16; y <= (int)gy + 16; y++)
            for (int x = (int)gx - 16; x <= (int)gx + 16; x++)
            {
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                float dd = Mathf.Abs(x - gx) + Mathf.Abs(y - gy) - 10f;
                float a = Mathf.Clamp01(0.5f - dd);
                float o = Mathf.Clamp01(0.5f - (dd - 2.5f));
                var cur = px[y * W + x];
                cur = Over(cur, new Color(0.05f, 0.03f, 0.08f, o));
                cur = Over(cur, new Color(Mathf.Lerp(rim.r, 1, 0.3f), Mathf.Lerp(rim.g, 1, 0.3f), Mathf.Lerp(rim.b, 1, 0.3f), a));
                px[y * W + x] = cur;
            }
        }

        tex.SetPixels(px);
        Save(tex, ArtDir + name + ".png");
    }

    // Ô vuông bo góc chứa icon trên thanh skill
    static void IconSlot()
    {
        const int S = 96;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - S / 2f, y + 0.5f - S / 2f);
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (S / 2f - 14f);
            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 12f;
            Color c = new Color(0.06f, 0.05f, 0.1f, 0.75f);
            float border = Mathf.Clamp01(1f - Mathf.Abs(d + 2.5f) / 2f);
            c = Color.Lerp(c, new Color(1f, 1f, 1f, 0.9f), border * 0.6f);
            c.a *= Mathf.Clamp01(0.5f - d);
            px[y * S + x] = c;
        }
        tex.SetPixels(px);
        Save(tex, ArtDir + "icon_slot.png");
    }

    // Texture vòng lửa cho hào quang: vòng tròn có nhiễu, alpha mềm
    static void FlameRing()
    {
        const int S = 256;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - S / 2f, y + 0.5f - S / 2f) / (S / 2f);
            float r = p.magnitude;
            float ang = Mathf.Atan2(p.y, p.x);
            float noise = Mathf.PerlinNoise(Mathf.Cos(ang) * 3f + 10f, Mathf.Sin(ang) * 3f + 10f) * 0.5f
                        + Mathf.PerlinNoise(Mathf.Cos(ang) * 9f + 30f, Mathf.Sin(ang) * 9f + 30f) * 0.25f;
            float ring = Mathf.Exp(-Mathf.Pow((r - 0.86f) / (0.06f + noise * 0.05f), 2f));
            float inner = Mathf.Clamp01(1f - r) * 0.18f * Mathf.Clamp01(r * 1.5f);
            float a = Mathf.Clamp01(ring + inner) * Mathf.Clamp01((1f - r) * 20f);
            Color c = Color.Lerp(new Color(1f, 0.25f, 0.05f), new Color(1f, 0.9f, 0.4f), ring * (0.5f + noise));
            c.a = a;
            px[y * S + x] = c;
        }
        tex.SetPixels(px);
        Save(tex, ArtDir + "aura_ring.png");
    }

    static Color Over(Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0f) return new Color(0, 0, 0, 0);
        Color c = (src * src.a + dst * dst.a * (1f - src.a)) / a;
        c.a = a;
        return c;
    }

    static void Save(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static void ConfigureImporters()
    {
        foreach (var file in Directory.GetFiles(ArtDir, "*.png", SearchOption.AllDirectories))
        {
            var path = file.Replace('\\', '/');
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("No importer for " + path); continue; }
            bool isAura = path.EndsWith("aura_ring.png");
            imp.textureType = isAura ? TextureImporterType.Default : TextureImporterType.Sprite;
            if (!isAura) imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = isAura;
            imp.wrapMode = TextureWrapMode.Clamp;
            if (path.Contains("card_"))
            {
                imp.spriteBorder = new Vector4(40, 40, 40, 140); // 9-slice: giữ góc + phần đầu thẻ
            }
            if (path.EndsWith("icon_slot.png")) imp.spriteBorder = new Vector4(26, 26, 26, 26);
            imp.SaveAndReimport();
        }
        AssetDatabase.Refresh();
    }
}
