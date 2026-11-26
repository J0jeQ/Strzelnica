using UnityEngine;

// Tarcza. Po trafieniu znika na 1 s i pojawia się (z efektem „wyskoczenia”) w losowym miejscu obok.
public class Target : MonoBehaviour
{
    public int points = 10;           // punkty za trafienie
    public float moveAmplitude = 0f;  // ruch na boki w metrach (0 = tarcza stoi)
    public float moveSpeed = 1.5f;    // szybkość ruchu na boki
    public float respawnRange = 0.8f; // jak daleko od startu może się pojawić
    public float hideTime = 1f;       // ile sekund tarcza jest niewidoczna

    Vector3 homePos;     // pozycja startowa
    Vector3 basePos;     // aktualny środek (wokół niego tarcza się porusza)
    Vector3 baseScale;   // normalny rozmiar tarczy
    float hiddenTimer;   // > 0 oznacza, że tarcza jest schowana
    Renderer[] rends;    // tarcza + jej pierścienie
    Collider col;

    void Start()
    {
        homePos = transform.localPosition;
        basePos = homePos;
        baseScale = transform.localScale;
        rends = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        if (hiddenTimer > 0f)
        {
            hiddenTimer -= Time.deltaTime;
            if (hiddenTimer <= 0f) Show();
            return;
        }
        // tarcza po pojawieniu się szybko rośnie do normalnego rozmiaru
        transform.localScale = Vector3.MoveTowards(transform.localScale, baseScale, Time.deltaTime * 3f);
        // ruch sinusoidalny na boki (w lokalnej osi X rodzica)
        if (moveAmplitude > 0f)
            transform.localPosition = basePos + Vector3.right * Mathf.Sin(Time.time * moveSpeed) * moveAmplitude;
    }

    void OnCollisionEnter(Collision collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();
        if (ball != null && ball.TryResolve())
        {
            GameManager.Instance.RegisterHit(points);
            foreach (Renderer r in rends) r.enabled = false; // schowaj tarczę
            col.enabled = false;
            hiddenTimer = hideTime;
        }
    }

    // Pokazuje tarczę w nowym, losowym miejscu w pobliżu pozycji startowej.
    void Show()
    {
        float x = Random.Range(-respawnRange, respawnRange);
        float y = Random.Range(-respawnRange, respawnRange) * 0.5f;
        basePos = homePos + new Vector3(x, y, 0f);
        transform.localPosition = basePos;
        transform.localScale = baseScale * 0.2f; // zaczyna mała i „wyskakuje”
        foreach (Renderer r in rends) r.enabled = true;
        col.enabled = true;
    }
}
