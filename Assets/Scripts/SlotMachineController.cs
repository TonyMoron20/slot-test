using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct WinInfo
{
    public int patternIndex;
    public Symbol symbol;
    public int count;
    public int reward;

    public WinInfo(int patternIndex, Symbol symbol, int count, int reward)
    {
        this.patternIndex = patternIndex;
        this.symbol = symbol;
        this.count = count;
        this.reward = reward;
    }
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

    private readonly List<WinInfo> lastWins = new List<WinInfo>();
    private bool isSpinning = false;

    void Start()
    {
        UpdateUI();
        if (spinButton != null)
            spinButton.onClick.AddListener(OnSpinButton);
    }

    void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(OnSpinButton);
    }

    void OnSpinButton()
    {
        if (isSpinning) return;
        if (credits < betPerSpin)
        {
            Debug.Log("No hay créditos suficientes");
            return;
        }

        credits -= betPerSpin;
        UpdateUI();
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        if (reels == null || reels.Length == 0)
            yield break;

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

        lastWinText.text = $"Ganaste: {totalWin} créditos";

        UpdateUI();
        isSpinning = false;
    }

    private void UpdateUI()
    {
        if (creditsText != null)
            creditsText.text = $"Créditos: {credits}";
    }

    private int EvaluatePatterns()
    {
        lastWins.Clear();
        if (linePatterns == null || linePatterns.Length == 0) return 0;
        if (reels == null || reels.Length == 0) return 0;

        int totalWin = 0;

        for (int p = 0; p < linePatterns.Length; p++)
        {
            if (!enableExtraPatterns && p >= 3)
                continue;

            var pattern = linePatterns[p];
            if (pattern == null || pattern.rowByReel == null) continue;
            if (pattern.rowByReel.Length != reels.Length) continue;

            WinInfo? maybeWin = EvaluateSinglePattern(p, pattern);
            if (maybeWin.HasValue)
            {
                WinInfo win = maybeWin.Value;
                totalWin += win.reward;
                lastWins.Add(win);
            }
        }

        return totalWin;
    }

    private WinInfo? EvaluateSinglePattern(int patternIndex, LinePattern pattern)
    {
        int reelCount = reels.Length;
        Symbol[] lineSymbols = new Symbol[reelCount];

        for (int reelIndex = 0; reelIndex < reelCount; reelIndex++)
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

        for (int i = 0; i < paytableEntries.Length; i++)
        {
            var entry = paytableEntries[i];
            if (entry.symbol == firstSymbol && count >= entry.minCount)
            {
                if (entry.rewardCredits > bestReward)
                    bestReward = entry.rewardCredits;
            }
        }

        if (bestReward <= 0)
            return null;

        return new WinInfo(patternIndex, firstSymbol, count, bestReward);
    }

    private void ShowWinLog()
    {
        if (logText == null) return;

        if (lastWins.Count == 0)
        {
            logText.text = "Sin líneas ganadoras.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Líneas ganadoras:");

        for (int i = 0; i < lastWins.Count; i++)
        {
            var win = lastWins[i];
            sb.AppendLine(
                $"Línea {win.patternIndex}: {win.symbol} x{win.count} -> {win.reward} créditos"
            );
        }

        logText.text = sb.ToString();
    }

    private void PlayHighlightFX()
    {
        if (reels == null) return;

        foreach (var reel in reels)
        {
            reel.ResetAllHighlights();
        }

        for (int i = 0; i < lastWins.Count; i++)
        {
            var win = lastWins[i];
            LinePattern pattern = linePatterns[win.patternIndex];

            for (int reelIndex = 0; reelIndex < win.count; reelIndex++)
            {
                int row = pattern.rowByReel[reelIndex];
                reels[reelIndex].HighlightRow(row);
            }
        }
    }
}
