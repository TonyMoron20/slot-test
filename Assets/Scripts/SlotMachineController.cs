using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WinInfo
{
    public int patternIndex;
    public Symbol symbol;
    public int count;
    public int reward;
}

public class SlotMachineController : MonoBehaviour
{
    [Header("Reels")]
    public ReelController[] reels;

    [Header("UI")]
    public Button spinButton;
    public Text creditsText;
    public Text lastWinText;

    [Header("Game Config")]
    public int credits = 1000;
    public int betPerSpin = 10;

    [Header("Patterns & Paytable")]
    public LinePattern[] linePatterns;
    public PaytableEntry[] paytableEntries;

    [Header("FX & Log")]
    public Text logText;
    public bool enableExtraPatterns = true;

    List<WinInfo> lastWins = new List<WinInfo>();


    private bool isSpinning = false;

    void Start()
    {
        UpdateUI();
        spinButton.onClick.AddListener(OnSpinButton);
    }

    void OnSpinButton()
    {
        if (isSpinning) return;
        if (credits < betPerSpin)
        {
            Debug.Log("No hay creditos suficientes");
            return;
        }

        credits -= betPerSpin;
        UpdateUI();
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        lastWinText.text = "";

        float delayBetweenReels = 0.2f;

        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].StartSpin();
            yield return new WaitForSeconds(delayBetweenReels);
        }

        for (int i = 0; i < reels.Length; i++)
        {
            float stopDelay = Random.Range(2f, 4f);
            yield return StartCoroutine(reels[i].StopSpin(stopDelay));
        }

        int totalWin = EvaluatePatterns();
        credits += totalWin;

        ShowWinLog();
        PlayHighlightFX();

        lastWinText.text = "Ganaste: " + totalWin + " creditos";

        UpdateUI();
        isSpinning = false;
    }

    private void UpdateUI()
    {
        creditsText.text = "Creditos: " + credits;
    }

    private int EvaluatePatterns()
    {
        int totalWin = 0;
        lastWins.Clear();

        for (int p = 0; p < linePatterns.Length; p++)
        {
            if (!enableExtraPatterns && p >= 3)
                continue;

            var pattern = linePatterns[p];
            if (pattern == null || pattern.rowByReel == null || pattern.rowByReel.Length == 0)
                continue;

            Symbol[] lineSymbols = new Symbol[reels.Length];

            for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
            {
                int row = pattern.rowByReel[reelIndex];
                lineSymbols[reelIndex] = reels[reelIndex].GetSymbolAtRow(row);
            }

            Symbol firstSymbol = lineSymbols[0];
            int count = 1;

            for (int i = 1; i < lineSymbols.Length; i++)
            {
                if (lineSymbols[i] == firstSymbol)
                    count++;
                else
                    break;
            }

            int bestReward = 0;

            foreach (var entry in paytableEntries)
            {
                if (entry.symbol == firstSymbol && count >= entry.minCount)
                {
                    if (entry.rewardCredits > bestReward)
                        bestReward = entry.rewardCredits;
                }
            }

            if (bestReward > 0)
            {
                totalWin += bestReward;

                WinInfo info = new WinInfo
                {
                    patternIndex = p,
                    symbol = firstSymbol,
                    count = count,
                    reward = bestReward
                };
                lastWins.Add(info);
            }
        }

        return totalWin;
    }

    void ShowWinLog()
    {
        if (logText == null) return;

        if (lastWins.Count == 0)
        {
            logText.text = "Sin lineas ganadoras.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Lineas ganadoras:");

        foreach (var win in lastWins)
        {
            sb.AppendLine(
                $"Linea {win.patternIndex}: {win.symbol} x{win.count} -> {win.reward} creditos"
            );
        }

        logText.text = sb.ToString();
    }

    void PlayHighlightFX()
    {
        foreach (var reel in reels)
        {
            reel.ResetAllHighlights();
        }

        foreach (var win in lastWins)
        {
            LinePattern pattern = linePatterns[win.patternIndex];

            for (int reelIndex = 0; reelIndex < win.count; reelIndex++)
            {
                int row = pattern.rowByReel[reelIndex];
                reels[reelIndex].HighlightRow(row);
            }
        }
    }
}
