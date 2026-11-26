using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Zrzut ekranu w trakcie gry: klawisz F12 zapisuje Screenshots/gra_<data_godzina>.png.
// W edytorze folder Screenshots jest w głównym katalogu projektu (obok Assets).
public class ScreenshotHotkey : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f12Key.wasPressedThisFrame) return;

        // W edytorze ścieżka względna liczy się od katalogu projektu,
        // a na goglach Unity sam dokleja z przodu Application.persistentDataPath.
        string folder = Application.isEditor ? "Screenshots" : Path.Combine(Application.persistentDataPath, "Screenshots");
        Directory.CreateDirectory(folder);

        string file = "Screenshots/gra_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("Zapisano zrzut ekranu: " + file);
    }
}
