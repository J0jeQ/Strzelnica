using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Piłka do rzucania. Pamięta swoje miejsce na ladzie i wraca tam po rzucie.
[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class Ball : MonoBehaviour
{
    public float returnDelay = 1.5f; // po ilu sekundach od rzutu piłka wraca na ladę

    Rigidbody rb;
    XRGrabInteractable grab;
    Vector3 startPos;
    Quaternion startRot;
    bool thrown;       // piłka została rzucona i jeszcze nie wróciła
    bool resolved;     // ten rzut został już policzony (trafienie albo pudło)
    float returnTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        startPos = transform.position;
        startRot = transform.rotation;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        thrown = false;
        GameManager.Instance.StartRound(); // nic nie robi, jeśli runda już trwa
    }

    void OnRelease(SelectExitEventArgs args)
    {
        thrown = true;
        resolved = false;
        returnTimer = returnDelay;
    }

    // Rzut bez gogli (używa go DesktopThrow) – działa jak puszczenie piłki z ręki.
    public void ThrowFrom(Vector3 position, Vector3 velocity)
    {
        GameManager.Instance.StartRound();
        rb.position = position; // fizyka – od razu
        transform.position = position;
        rb.linearVelocity = velocity;
        OnRelease(null);
    }

    // Zwraca true tylko raz na rzut – dzięki temu rzut to albo trafienie, albo pudło.
    public bool TryResolve()
    {
        if (!thrown || resolved) return false;
        resolved = true;
        return true;
    }

    void Update()
    {
        if (thrown) returnTimer -= Time.deltaTime;
        // po czasie albo gdy spadła pod mapę – wraca na ladę (chyba że ktoś ją trzyma)
        if (((thrown && returnTimer <= 0f) || transform.position.y < -5f) && !grab.isSelected)
        {
            thrown = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = startPos;
            rb.rotation = startRot;
            transform.SetPositionAndRotation(startPos, startRot);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // dotknięcie podłogi bez trafienia w tarczę = pudło
        if (collision.gameObject.CompareTag("Floor") && TryResolve())
            GameManager.Instance.RegisterMiss();
    }
}
