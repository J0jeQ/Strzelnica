using System.IO;
using UnityEngine;
using UnityEditor;

// Tekstury stanowiska rysowane w kodzie (bez plików z internetu).
// Każda tekstura zapisuje się jako PNG w Assets/Textures i powtarza się bez szwów.
public static class TextureMaker
{
    const string Folder = "Assets/Textures";
    const int Size = 512;

    // Deski poziome z usłojeniem (podłoga, blat, słupki).
    public static Texture2D Planks()
    {
        return Save("Deski", (x, y) =>
        {
            int plank = y / (Size / 8);                          // 8 desek na teksturę
            int joint = (int)(Hash(plank, 0, 1) * Size);          // gdzie deska się kończy
            float shade = 0.85f + Hash(plank, 0, 2) * 0.3f;       // każda deska trochę inna
            float t = x * Mathf.PI * 2f / Size;
            float grain = 0.9f + 0.06f * Mathf.Sin(t * 3f + y * 0.35f + plank) + 0.04f * Mathf.Sin(t * 11f + y * 0.9f);
            Color c = new Color(0.62f, 0.42f, 0.25f) * shade * grain * (0.95f + Hash(x, y, 3) * 0.1f);
            if (y % (Size / 8) < 3 || Mathf.Abs(x - joint) < 2) c = new Color(0.22f, 0.14f, 0.08f); // szczeliny
            return c;
        });
    }

    // Przód lady: pionowe deski pomalowane na czerwono, złote listwy u góry i na dole.
    public static Texture2D CounterPanel()
    {
        return Save("Lada", (x, y) =>
        {
            float v = y / (float)Size;
            if (v > 0.86f || v < 0.1f) return (v > 0.97f || v < 0.02f) ? new Color(0.3f, 0.2f, 0.05f) : new Color(0.95f, 0.75f, 0.2f);
            int board = x / (Size / 4);
            Color c = new Color(0.72f, 0.1f, 0.12f) * (0.92f + Hash(board, 0, 4) * 0.12f) * (0.94f + Hash(x, y, 5) * 0.08f);
            if (x % (Size / 4) < 4) c *= 0.5f; // szczeliny między deskami
            return c;
        });
    }

    // Pasy jak w namiocie cyrkowym (ściany boczne).
    public static Texture2D Stripes()
    {
        return Save("Paski", (x, y) =>
        {
            float u = x / (float)Size;
            Color c = u < 0.5f ? new Color(0.75f, 0.12f, 0.15f) : new Color(0.96f, 0.9f, 0.78f);
            float edge = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)); // lekkie cieniowanie fałd materiału
            return c * (0.82f + 0.18f * edge) * (0.97f + Hash(x, y, 6) * 0.06f);
        });
    }

    // Granatowa ściana ze złotymi gwiazdami (za tarczami).
    public static Texture2D Stars()
    {
        int cells = 4; // 4 x 4 gwiazdy na teksturę
        int cell = Size / cells;
        return Save("Gwiazdy", (x, y) =>
        {
            Color c = new Color(0.08f, 0.1f, 0.26f) * (0.92f + Hash(x, y, 7) * 0.1f);
            int cx = x / cell, cy = y / cell;
            // środek gwiazdy losowo przesunięty w swojej komórce, przesunięcie co drugi rząd
            float sx = (cx + 0.3f + Hash(cx, cy, 8) * 0.4f) * cell, sy = (cy + 0.3f + Hash(cx, cy, 9) * 0.4f) * cell;
            float dx = x - sx, dy = y - sy;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / (cell * 0.22f);
            float sector = Mathf.Repeat(Mathf.Atan2(dy, dx) + Mathf.PI / 2f, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f);
            float starEdge = Mathf.Lerp(1f, 0.45f, Mathf.Abs(sector - 0.5f) * -2f + 1f); // ramiona gwiazdy
            if (r < starEdge) return new Color(1f, 0.8f, 0.25f);
            if (Hash(x, y, 10) > 0.9985f) return new Color(0.9f, 0.85f, 0.6f); // drobne iskierki
            return c;
        });
    }

    // Trawa z przebarwieniami.
    public static Texture2D Grass()
    {
        return Save("Trawa", (x, y) =>
        {
            float t = Mathf.PI * 2f / Size;
            float blobs = 0.5f + 0.25f * Mathf.Sin(x * t * 2f + 1f) * Mathf.Sin(y * t * 3f) + 0.25f * Mathf.Sin((x + y) * t * 5f);
            Color c = Color.Lerp(new Color(0.29f, 0.5f, 0.2f), new Color(0.34f, 0.55f, 0.23f), blobs);
            return c * (0.88f + Hash(x, y, 11) * 0.24f);
        });
    }

    // Rysuje teksturę piksel po pikselu, zapisuje jako PNG i zwraca zaimportowany plik.
    static Texture2D Save(string name, System.Func<int, int, Color> pixel)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = pixel(x, y);
        tex.SetPixels(pixels);
        tex.Apply();
        Directory.CreateDirectory(Folder);
        string path = Folder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Pseudolosowa liczba 0..1 zależna od (x, y, seed) – zawsze taka sama dla tych samych danych.
    static float Hash(int x, int y, int seed)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
    }
}
