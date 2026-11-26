using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

// Tryb testowy bez gogli: celuj kursorem myszy i naciśnij F, żeby rzucić piłkę.
// Na goglach nic nie robi (brak klawiatury), więc można go zostawić.
public class DesktopThrow : MonoBehaviour
{
    public float throwSpeed = 15f; // prędkość rzutu
    public float lift = 2f;        // lekko w górę, żeby piłka nie opadała za szybko

    Ball[] balls;
    int next; // która piłka poleci następna

    void Start()
    {
        balls = GetComponentsInChildren<Ball>();

        XROrigin origin = FindAnyObjectByType<XROrigin>();
        if (UnityEngine.XR.XRSettings.isDeviceActive || origin == null) return;
        // Bez gogli nikt nie ustawia wysokości oczu – podnosimy kamerę na 1.6 m,
        // pochylamy ją lekko w dół i poszerzamy widok, żeby było widać ladę z piłkami.
        origin.CameraFloorOffsetObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        origin.CameraFloorOffsetObject.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
        origin.Camera.fieldOfView = 75f;
        // modele kontrolerów zasłaniały piłki (promienie zostają)
        foreach (MeshRenderer m in origin.GetComponentsInChildren<MeshRenderer>(true)) m.enabled = false;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null || Camera.main == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame || balls.Length == 0) return;

        // promień z kamery przez kursor myszy
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 start = ray.origin + ray.direction * 0.5f;
        if (start.y < 1.2f) start.y = 1.2f; // żeby piłka nie wystartowała w ladzie

        balls[next].ThrowFrom(start, ray.direction * throwSpeed + Vector3.up * lift);
        next = (next + 1) % balls.Length;
    }
}
