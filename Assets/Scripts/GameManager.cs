using UnityEngine;
using TMPro;

// Zarządza rundą: czas, punkty, seria trafień, mnożnik, rekord i teksty na tablicy.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // dostęp z innych skryptów: GameManager.Instance

    public TextMeshProUGUI pointsText, timeText, streakText, messageText; // 4 teksty na tablicy
    public float roundTime = 60f;  // długość rundy w sekundach
    public bool roundActive;       // czy runda trwa

    float timeLeft, messageTimer;  // messageTimer = 0 oznacza, że komunikat zostaje na stałe
    int score, streak, bestStreak, hits, throws;

    void Awake() { Instance = this; }

    void Start() { UpdateTexts(); ShowMessage("Złap piłkę, aby zacząć!\nRekord: " + PlayerPrefs.GetInt("Rekord", 0), 0f); }

    void Update()
    {
        if (roundActive)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) EndRound();
            UpdateTexts();
        }
        if (messageTimer > 0f) // chowanie komunikatu po czasie
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }

    // Wywoływane przy złapaniu piłki. Jeśli runda już trwa – nic nie robi.
    public void StartRound()
    {
        if (roundActive) return;
        roundActive = true; timeLeft = roundTime;
        score = streak = bestStreak = hits = throws = 0;
        ShowMessage("Start! Rzucaj w tarcze!", 1.2f);
    }

    // Mnożnik zależy od liczby trafień pod rząd: 3+ = x2, 6+ = x3, 10+ = x4.
    int Multiplier() { return streak >= 10 ? 4 : streak >= 6 ? 3 : streak >= 3 ? 2 : 1; }

    // Wywoływane przez tarczę po trafieniu.
    public void RegisterHit(int points)
    {
        if (!roundActive) return;
        hits++; throws++; streak++;
        bestStreak = Mathf.Max(bestStreak, streak);
        int gained = points * Multiplier();
        score += gained;
        string msg = "+" + gained + " pkt";
        if (streak % 5 == 0) { score += 50; msg += "\nPremia za serię +50!"; } // co 5 trafień z rzędu
        ShowMessage(msg, 1.2f);
    }

    // Wywoływane przez piłkę, gdy spadnie na podłogę bez trafienia.
    public void RegisterMiss()
    {
        if (!roundActive) return;
        throws++; streak = 0;
        ShowMessage("Pudło!", 1.2f);
    }

    void EndRound()
    {
        roundActive = false; timeLeft = 0f;
        int record = Mathf.Max(score, PlayerPrefs.GetInt("Rekord", 0));
        PlayerPrefs.SetInt("Rekord", record);
        PlayerPrefs.Save(); // zapis od razu na dysk (np. gdy gogle się wyłączą)
        int accuracy = throws > 0 ? hits * 100 / throws : 0;
        ShowMessage("Koniec rundy!\nWynik: " + score + "   Celność: " + accuracy + "%\nNajdłuższa seria: " +
                    bestStreak + "   Rekord: " + record + "\nZłap piłkę, aby zagrać ponownie", 0f);
    }

    void ShowMessage(string text, float duration) { messageText.text = text; messageTimer = duration; }

    void UpdateTexts()
    {
        pointsText.text = "Punkty: " + score;
        timeText.text = "Czas: " + Mathf.CeilToInt(timeLeft);
        streakText.text = "Seria: " + streak + " (x" + Multiplier() + ")";
    }
}
