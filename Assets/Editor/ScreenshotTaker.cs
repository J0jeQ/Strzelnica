using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using TMPro;

// Menu "Tools/Zrób screenshoty do dokumentacji" – 9 zrzutów sceny gry (PNG 1920x1080) bez wchodzenia w Play.
// Zapis do folderu Screenshots/ w głównym katalogu projektu (poza Assets).
// Na czas zdjęć ustawiamy przykładowe teksty na tablicy i chowamy kontrolery gracza,
// a na koniec wszystko przywracamy – scena zostaje bez zmian.
public static class ScreenshotTaker
{
    const int Width = 1920, Height = 1080;

    // Oczy gracza: XR Origin stoi w (0,0,0), oczy na 1.6 m, wzrok na tarcze, lekko w dół (widać ladę i tablicę).
    static readonly Vector3 eyes = new Vector3(0f, 1.6f, 0f);
    static readonly Vector3 eyesTarget = new Vector3(0f, 0.15f, 6.6f);

    [MenuItem("Tools/Zrób screenshoty do dokumentacji")]
    public static void TakeAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Screenshoty", "Najpierw zatrzymaj Play – zrzuty robimy w trybie edycji.", "OK");
            return;
        }
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Screenshoty", "Brak GameManagera w scenie. Najpierw użyj Tools/Zbuduj scenę gry.", "OK");
            return;
        }

        // zapamiętaj oryginalne teksty tablicy
        TMP_Text[] texts = { gm.pointsText, gm.timeText, gm.streakText, gm.messageText };
        string[] oldTexts = new string[texts.Length];
        for (int i = 0; i < texts.Length; i++) oldTexts[i] = texts[i].text;

        // schowaj kontrolery, dłonie i promienie gracza oraz symulator (zasłaniałyby widok)
        List<Renderer> hidden = new List<Renderer>();
        XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin != null) Hide(origin.gameObject, hidden);
        GameObject simulator = GameObject.Find("XR Interaction Simulator");
        if (simulator != null) Hide(simulator, hidden);

        Camera cam = new GameObject("Kamera do screenshotow").AddComponent<Camera>();
        cam.gameObject.hideFlags = HideFlags.HideAndDontSave; // nie trafi do sceny
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(folder);

        try
        {
            // Teksty w tym samym formacie, co w GameManager. Przykładowa runda zgodna z zasadami punktacji:
            // czerwona 10, złota 30, mnożnik x2 od serii 3, x3 od 6, premia +50 co 5 trafień z rzędu.
            //  - 10, 10, złota 30*2 = 60 → 80 pkt, pudło
            //  - nowa seria: 10, 10, 20, 20 → 140 pkt, seria 4 (x2)            → zrzut 05
            //  - 5. trafienie: 20 + premia 50 → 210 pkt                        → zrzut 07
            //  - 6. trafienie w złotą: 30*3 = 90 → 300 pkt                     → zrzut 06
            //  - 7. trafienie: 10*3 = 30 → 330 pkt, potem pudło                → zrzut 08
            //  - 10, 10 → 350 pkt, pudło, koniec czasu: 12 trafień / 15 rzutów → zrzut 09
            int oldRecord = PlayerPrefs.GetInt("Rekord", 0); // tylko odczyt – rekord się nie zmienia
            string start = "Złap piłkę, aby zacząć!\nRekord: " + oldRecord;

            SetBoard(texts, 0, 0, 0, start);
            Shot(cam, folder, "01_scena_ogolna.png", new Vector3(3.8f, 3.7f, -3.2f), new Vector3(-0.3f, 1.4f, 4.5f)); // pod markizą, żeby było widać piłki
            Shot(cam, folder, "02_widok_gracza.png", eyes, eyesTarget);
            Shot(cam, folder, "03_lada_pilki.png", new Vector3(0.35f, 1.45f, -0.15f), new Vector3(0f, 1.05f, 0.7f));
            Shot(cam, folder, "04_tarcze.png", new Vector3(0f, 1.9f, 2.6f), new Vector3(0f, 1.6f, 6.6f));

            SetBoard(texts, 140, 37, 4, "");
            Shot(cam, folder, "05_tablica_w_grze.png", eyes, eyesTarget);
            SetBoard(texts, 300, 33, 6, "+90 pkt");
            Shot(cam, folder, "06_combo.png", eyes, eyesTarget);
            SetBoard(texts, 210, 35, 5, "+20 pkt\nPremia za serię +50!");
            Shot(cam, folder, "07_seria_premia.png", eyes, eyesTarget);
            SetBoard(texts, 330, 30, 0, "Pudło!");
            Shot(cam, folder, "08_pudlo.png", eyes, eyesTarget);

            int score = 350, hits = 12, throws = 15, bestStreak = 7;
            int record = Mathf.Max(score, oldRecord);
            int accuracy = hits * 100 / throws;
            SetBoard(texts, score, 0, 0, "Koniec rundy!\nWynik: " + score + "   Celność: " + accuracy + "%\nNajdłuższa seria: " +
                                         bestStreak + "   Rekord: " + record + "\nZłap piłkę, aby zagrać ponownie");
            Shot(cam, folder, "09_koniec_rundy.png", eyes, eyesTarget);
        }
        finally
        {
            // sprzątanie: usuń kamerę, przywróć teksty i widoczność
            Object.DestroyImmediate(cam.gameObject);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].text = oldTexts[i];
                texts[i].ForceMeshUpdate();
            }
            foreach (Renderer r in hidden) r.enabled = true;
        }
        Debug.Log("Zapisano 9 screenshotów w " + folder);
    }

    // Wyłącza widoczne renderery pod obiektem i zapisuje je na liście, żeby potem je włączyć.
    static void Hide(GameObject root, List<Renderer> hidden)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            if (r.enabled)
            {
                r.enabled = false;
                hidden.Add(r);
            }
    }

    // Tablica jak w GameManager.UpdateTexts (mnożnik jak w GameManager.Multiplier).
    static void SetBoard(TMP_Text[] texts, int score, int time, int streak, string message)
    {
        int multiplier = streak >= 10 ? 4 : streak >= 6 ? 3 : streak >= 3 ? 2 : 1;
        texts[0].text = "Punkty: " + score;
        texts[1].text = "Czas: " + time;
        texts[2].text = "Seria: " + streak + " (x" + multiplier + ")";
        texts[3].text = message;
        foreach (TMP_Text t in texts) t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
    }

    // Ustawia kamerę, renderuje do RenderTexture i zapisuje PNG.
    static void Shot(Camera cam, string folder, string fileName, Vector3 position, Vector3 lookAt)
    {
        cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4; // gładsze krawędzie
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        File.WriteAllBytes(Path.Combine(folder, fileName), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }
}
