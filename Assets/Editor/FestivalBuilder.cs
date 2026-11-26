using UnityEngine;
using UnityEditor;
using TMPro;

// Dekoracje festynu wokół strzelnicy: diabelski młyn, budki z jedzeniem, girlandy, latarnie, balony i drzewa.
// Wywoływane z SceneBuilder – wszystko trafia pod "Gra/Festyn". Dekoracje nie mają kolizji.
public static class FestivalBuilder
{
    static Color[] colors = { new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.8f, 0.1f), new Color(0.2f, 0.5f, 1f),
                              new Color(0.25f, 0.8f, 0.3f), new Color(0.85f, 0.3f, 0.8f), new Color(1f, 0.5f, 0.1f) };
    static Material[] paints; // błyszcząca farba w kolorach z tablicy colors
    static Material[] glows;  // świecące żarówki (Unlit)
    static Material white, steel, cord;

    public static void Build(Transform gameRoot)
    {
        Transform f = new GameObject("Festyn").transform;
        f.SetParent(gameRoot, false);
        paints = new Material[colors.Length];
        glows = new Material[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            paints[i] = SceneBuilder.Mat(colors[i], 0.6f);
            glows[i] = Unlit(Color.Lerp(colors[i], Color.white, 0.4f));
        }
        white = SceneBuilder.Mat(new Color(0.95f, 0.95f, 0.95f), 0.5f);
        steel = SceneBuilder.Mat(new Color(0.75f, 0.78f, 0.82f), 0.7f, 0.6f);
        cord = Unlit(new Color(0.1f, 0.1f, 0.1f));

        // szyld "STRZELNICA" na tylnej ścianie (widać go ponad tablicą)
        Prop(PrimitiveType.Cube, "Szyld", f, new Vector3(0f, 4.85f, 8.62f), new Vector3(4f, 0.9f, 0.1f), paints[0]);
        Label(f, "STRZELNICA", new Vector3(0f, 4.85f, 8.55f), new Vector2(3.8f, 0.8f), colors[1]);
        for (int i = 0; i < 9; i++)
        {
            Prop(PrimitiveType.Sphere, "Zarowka", f, new Vector3(-1.9f + i * 0.475f, 5.33f, 8.6f), Vector3.one * 0.12f, glows[1]);
            Prop(PrimitiveType.Sphere, "Zarowka", f, new Vector3(-1.9f + i * 0.475f, 4.37f, 8.6f), Vector3.one * 0.12f, glows[1]);
        }

        // półki z nagrodami (misie) na ścianach bocznych strzelnicy
        Material shelf = SceneBuilder.TexMat(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Deski.png"), new Vector2(3f, 0.2f), 0.3f);
        for (int side = -1; side <= 1; side += 2)
            for (int level = 0; level < 2; level++)
            {
                float y = 2.2f + level * 0.9f;
                Prop(PrimitiveType.Cube, "Polka", f, new Vector3(side * 4.42f, y, 4.75f), new Vector3(0.35f, 0.05f, 6.5f), shelf);
                for (int k = 0; k < 6; k++)
                    Teddy(f, new Vector3(side * 4.42f, y + 0.025f, 2.1f + k * 1.05f), side, k + level * 2 + (side > 0 ? 1 : 0));
            }

        // cennik na ladzie
        Prop(PrimitiveType.Cube, "Cennik", f, new Vector3(3.6f, 1.3f, 0.85f), new Vector3(0.9f, 0.55f, 0.04f), white);
        Label(f, "3 PIŁKI = 5 ZŁ\nNAGRODY!", new Vector3(3.6f, 1.3f, 0.82f), new Vector2(0.8f, 0.45f), colors[0]);

        FerrisWheel(f, new Vector3(-9f, 12f, 30f), 9f);

        // budki z jedzeniem wokół gracza
        string[] names = { "LODY", "WATA CUKROWA", "HOT DOG", "LOSY", "GOFRY" };
        Vector3[] spots = { new Vector3(-9f, 0f, 0f), new Vector3(9f, 0f, 0f), new Vector3(-7f, 0f, -7f), new Vector3(7f, 0f, -7f), new Vector3(0f, 0f, -10f) };
        for (int i = 0; i < names.Length; i++) Stall(f, spots[i], names[i], i);

        // girlandy chorągiewek nad strzelnicą
        Garland(f, new Vector3(-4.6f, 4.1f, 2.5f), new Vector3(4.6f, 4.1f, 2.5f), 0.4f);
        Garland(f, new Vector3(-4.6f, 4.1f, 4.8f), new Vector3(4.6f, 4.1f, 4.8f), 0.4f);

        // latarnie dookoła placu połączone girlandami (z przerwą na strzelnicę)
        Vector3 prevTop = Vector3.zero;
        for (int i = 0; i < 8; i++)
        {
            float angle = (120f + i * 40f) * Mathf.Deg2Rad;
            Vector3 top = Lamp(f, new Vector3(Mathf.Cos(angle) * 13f, 0f, Mathf.Sin(angle) * 13f));
            if (i > 0) Garland(f, prevTop, top, 0.8f);
            prevTop = top;
        }

        // balony przy rogach strzelnicy
        Balloons(f, new Vector3(-4.7f, 4.2f, 0.8f), 0);
        Balloons(f, new Vector3(4.7f, 4.2f, 0.8f), 3);
        Balloons(f, new Vector3(-4.7f, 4f, 8.6f), 1);
        Balloons(f, new Vector3(4.7f, 4f, 8.6f), 4);

        // drzewa dookoła (stałe ziarno losowania = zawsze ten sam układ)
        Random.InitState(7);
        Material bark = SceneBuilder.Mat(new Color(0.35f, 0.22f, 0.12f), 0.1f);
        Material leaves = SceneBuilder.Mat(new Color(0.3f, 0.65f, 0.22f), 0.1f);
        for (int i = 0; i < 22; i++)
        {
            float angle = i * 360f / 22f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(19f, 26f);
            if (Vector3.Distance(pos, new Vector3(-9f, 0f, 30f)) < 9f) continue; // nie w diabelskim młynie
            float h = Random.Range(3f, 5f);
            Prop(PrimitiveType.Cylinder, "Pien", f, pos + Vector3.up * h * 0.5f, new Vector3(0.4f, h * 0.5f, 0.4f), bark);
            Prop(PrimitiveType.Sphere, "Korona", f, pos + Vector3.up * (h + 0.8f), Vector3.one * Random.Range(3f, 4.5f), leaves);
        }

        // nieruchome dekoracje oznaczamy jako statyczne – Unity narysuje je szybciej
        foreach (Transform t in f.GetComponentsInChildren<Transform>())
            if (t.GetComponentInParent<Animacja>() == null)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // Diabelski młyn: stojące nogi + obracające się koło z wagonikami, które zawsze wiszą prosto.
    static void FerrisWheel(Transform f, Vector3 center, float radius)
    {
        Transform w = new GameObject("Diabelski mlyn").transform;
        w.SetParent(f, false);
        w.localPosition = center;
        Prop(PrimitiveType.Cube, "Podest", w, new Vector3(0f, 0.25f - center.y, 0f), new Vector3(radius * 1.4f, 0.5f, 4f), paints[0]);
        for (int side = -1; side <= 1; side += 2)
            for (int z = -1; z <= 1; z += 2)
                Beam(w, new Vector3(side * radius * 0.55f, -center.y, z * 1.6f), new Vector3(0f, 0f, z * 0.7f), 0.35f, steel);

        Transform wheel = new GameObject("Kolo").transform;
        wheel.SetParent(w, false);
        wheel.gameObject.AddComponent<Animacja>().rotationSpeed = new Vector3(0f, 0f, 6f);
        Prop(PrimitiveType.Sphere, "Piasta", wheel, Vector3.zero, Vector3.one * 1.3f, white);
        int n = 24;
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius;
            Vector3 p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
            for (int z = -1; z <= 1; z += 2) Beam(wheel, p0 + Vector3.forward * z * 0.5f, p1 + Vector3.forward * z * 0.5f, 0.2f, steel);
            Prop(PrimitiveType.Sphere, "Zarowka", wheel, p0 + Vector3.back * 0.65f, Vector3.one * 0.35f, glows[i % glows.Length]);
            if (i % 2 != 0) continue;
            Beam(wheel, Vector3.zero, p0, 0.15f, white); // szprycha
            // zawieszenie wagonika obraca się w przeciwną stronę niż koło, więc wagonik wisi prosto
            Transform hanger = new GameObject("Zawieszenie").transform;
            hanger.SetParent(wheel, false);
            hanger.localPosition = p0;
            hanger.gameObject.AddComponent<Animacja>().rotationSpeed = new Vector3(0f, 0f, -6f);
            Prop(PrimitiveType.Cube, "Wagonik", hanger, new Vector3(0f, -0.9f, 0f), new Vector3(1.1f, 1.1f, 1.1f), paints[(i / 2) % paints.Length]);
            Prop(PrimitiveType.Cube, "Daszek", hanger, new Vector3(0f, -0.25f, 0f), new Vector3(1.3f, 0.1f, 1.3f), white);
        }
    }

    // Pluszowy miś na półce, przodem do środka strzelnicy (side = -1 lewa ściana, 1 prawa).
    static void Teddy(Transform f, Vector3 pos, int side, int colorIndex)
    {
        Material fur = SceneBuilder.Mat(Color.Lerp(colors[colorIndex % colors.Length], Color.white, 0.35f), 0.1f);
        Material dark = cord;
        Vector3 face = Vector3.left * side; // kierunek, w którą stronę patrzy miś
        Vector3 head = pos + Vector3.up * 0.48f;
        Prop(PrimitiveType.Sphere, "Mis", f, pos + Vector3.up * 0.18f, new Vector3(0.3f, 0.36f, 0.3f), fur);
        Prop(PrimitiveType.Sphere, "Glowa", f, head, Vector3.one * 0.24f, fur);
        Prop(PrimitiveType.Sphere, "Pyszczek", f, head + face * 0.1f + Vector3.down * 0.03f, Vector3.one * 0.09f, white);
        for (int e = -1; e <= 1; e += 2)
        {
            Prop(PrimitiveType.Sphere, "Ucho", f, head + Vector3.up * 0.1f + Vector3.forward * e * 0.09f, Vector3.one * 0.09f, fur);
            Prop(PrimitiveType.Sphere, "Oko", f, head + face * 0.105f + Vector3.up * 0.03f + Vector3.forward * e * 0.05f, Vector3.one * 0.035f, dark);
        }
    }

    // Budka z jedzeniem, ustawiona przodem do środka placu.
    static void Stall(Transform f, Vector3 pos, string name, int colorIndex)
    {
        Material paint = paints[colorIndex % paints.Length];
        Transform s = new GameObject("Budka " + name).transform;
        s.SetParent(f, false);
        s.localPosition = pos;
        s.localRotation = Quaternion.LookRotation(new Vector3(pos.x, 0f, pos.z).normalized); // przód (-Z) patrzy na środek
        Prop(PrimitiveType.Cube, "Lada", s, new Vector3(0f, 0.55f, 0f), new Vector3(3f, 1.1f, 1.2f), paint);
        Prop(PrimitiveType.Cube, "Blat", s, new Vector3(0f, 1.13f, -0.05f), new Vector3(3.2f, 0.06f, 1.4f), white);
        Prop(PrimitiveType.Cube, "Tyl", s, new Vector3(0f, 1.6f, 0.6f), new Vector3(3f, 3.2f, 0.1f), paint);
        for (int side = -1; side <= 1; side += 2)
            Prop(PrimitiveType.Cube, "Slupek", s, new Vector3(side * 1.45f, 1.6f, -0.55f), new Vector3(0.1f, 3.2f, 0.1f), white);
        for (int i = 0; i < 6; i++)
        {
            GameObject stripe = Prop(PrimitiveType.Cube, "Dach", s, new Vector3(-1.375f + i * 0.55f, 3.3f, 0f),
                                     new Vector3(0.55f, 0.05f, 1.6f), i % 2 == 0 ? paint : white);
            stripe.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Prop(PrimitiveType.Sphere, "Zarowka", s, new Vector3(-1.375f + i * 0.55f, 3.05f, -0.8f), Vector3.one * 0.12f, glows[(i + colorIndex) % glows.Length]);
        }
        for (int i = 0; i < 3; i++) // towar na ladzie
            Prop(PrimitiveType.Sphere, "Towar", s, new Vector3(-0.8f + i * 0.8f, 1.3f, -0.2f), Vector3.one * 0.3f, paints[(colorIndex + i + 1) % paints.Length]);
        Prop(PrimitiveType.Cube, "Szyld", s, new Vector3(0f, 3.75f, -0.85f), new Vector3(2.6f, 0.6f, 0.05f), white);
        Label(s, name, new Vector3(0f, 3.75f, -0.88f), new Vector2(2.4f, 0.5f), Color.Lerp(colors[colorIndex % colors.Length], Color.black, 0.25f));
    }

    // Girlanda kolorowych chorągiewek zwisająca między dwoma punktami.
    static void Garland(Transform f, Vector3 a, Vector3 b, float sag)
    {
        int count = Mathf.Max(4, Mathf.RoundToInt(Vector3.Distance(a, b) / 0.45f));
        LineRenderer line = new GameObject("Girlanda").AddComponent<LineRenderer>();
        line.transform.SetParent(f, false);
        line.useWorldSpace = false;
        line.widthMultiplier = 0.025f;
        line.sharedMaterial = cord;
        line.positionCount = count + 1;
        Quaternion facing = Quaternion.LookRotation(Vector3.Cross(Vector3.up, b - a)) * Quaternion.Euler(0f, 0f, 45f);
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
            line.SetPosition(i, p);
            if (i == 0 || i == count) continue;
            GameObject flag = Prop(PrimitiveType.Cube, "Choragiewka", f, p + Vector3.down * 0.13f, new Vector3(0.18f, 0.18f, 0.01f), paints[i % paints.Length]);
            flag.transform.localRotation = facing;
        }
    }

    // Latarnia; zwraca pozycję lampy na górze (do zaczepienia girlandy).
    static Vector3 Lamp(Transform f, Vector3 pos)
    {
        Prop(PrimitiveType.Cylinder, "Latarnia", f, pos + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), cord);
        Prop(PrimitiveType.Sphere, "Lampa", f, pos + Vector3.up * 4.1f, Vector3.one * 0.4f, Unlit(new Color(1f, 0.9f, 0.6f)));
        return pos + Vector3.up * 3.9f;
    }

    // Trzy balony na sznurkach, lekko się bujają.
    static void Balloons(Transform f, Vector3 anchor, int colorIndex)
    {
        Vector3[] offsets = { new Vector3(-0.3f, 1f, 0f), new Vector3(0.3f, 1.2f, 0.1f), new Vector3(0f, 1.5f, -0.1f) };
        for (int k = 0; k < 3; k++)
        {
            Vector3 p = anchor + offsets[k];
            GameObject balloon = Prop(PrimitiveType.Sphere, "Balon", f, p, new Vector3(0.45f, 0.55f, 0.45f), paints[(colorIndex + k) % paints.Length]);
            Animacja anim = balloon.AddComponent<Animacja>();
            anim.bobHeight = 0.08f;
            anim.bobSpeed = 1.2f + k * 0.4f;
            Beam(f, anchor, p + Vector3.down * 0.27f, 0.01f, white); // sznurek
        }
    }

    // Prymityw bez kolizji (dekoracja).
    static GameObject Prop(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = SceneBuilder.Make(type, name, parent, pos, scale, mat);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Belka (wydłużony prostopadłościan) od punktu a do punktu b.
    static void Beam(Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
    {
        GameObject beam = Prop(PrimitiveType.Cube, "Belka", parent, (a + b) * 0.5f, new Vector3(thickness, Vector3.Distance(a, b), thickness), mat);
        beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    static Material Unlit(Color color)
    {
        return new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
    }

    // Napis 3D (TextMeshPro) czytelny od strony -Z.
    static void Label(Transform parent, string text, Vector3 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Napis " + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.color = color;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = size;
        t.textWrappingMode = TextWrappingModes.NoWrap; // za długi napis zmniejszy się zamiast łamać
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.5f;
        t.fontSizeMax = 20f;
    }
}
