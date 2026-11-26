using UnityEngine;

// Prosta animacja dekoracji festynu: obracanie (np. diabelski młyn) i bujanie góra-dół (np. balony).
public class Animacja : MonoBehaviour
{
    public Vector3 rotationSpeed;  // stopnie na sekundę wokół lokalnych osi X, Y, Z
    public float bobHeight = 0f;   // jak wysoko się buja w metrach (0 = nie buja)
    public float bobSpeed = 1f;    // szybkość bujania

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        if (bobHeight > 0f)
            transform.localPosition = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }
}
