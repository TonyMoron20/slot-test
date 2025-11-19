using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ReelController : MonoBehaviour
{
    [Header("Config")]
    public Symbol[] reelSequence;
    public float spinSpeed = 20f;
    public int visibleRows = 3;

    [Header("UI")]
    public Image[] visibleSymbolImages;
    public Sprite[] symbolSprites;

    [Header("FX")]
    public Color highlightColor = Color.yellow;

    private bool isSpinning = false;
    private int currentStartIndex = 0;
    private Color[] originalColors;

    void Awake()
    {
        originalColors = new Color[visibleSymbolImages.Length];
        for (int i = 0; i < visibleSymbolImages.Length; i++)
        {
            if (visibleSymbolImages[i] != null)
                originalColors[i] = visibleSymbolImages[i].color;
            else
                originalColors[i] = Color.white;
        }
    }

    public void StartSpin()
    {
        if (isSpinning) return;
        isSpinning = true;
        StartCoroutine(SpinRoutine());
    }

    public IEnumerator StopSpin(float afterSeconds)
    {
        yield return new WaitForSeconds(afterSeconds);
        isSpinning = false;

        UpdateVisibleSymbols();
    }

    private IEnumerator SpinRoutine()
    {
        if (reelSequence == null || reelSequence.Length == 0)
        {
            yield break;
        }

        float stepDelay = 1f / spinSpeed;

        while (isSpinning)
        {
            currentStartIndex = (currentStartIndex + 1) % reelSequence.Length;
            UpdateVisibleSymbols();
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private void UpdateVisibleSymbols()
    {
        if (reelSequence == null || reelSequence.Length == 0) return;

        int rowsToUpdate = Mathf.Min(visibleRows, visibleSymbolImages.Length);

        for (int row = 0; row < rowsToUpdate; row++)
        {
            int index = (currentStartIndex + row) % reelSequence.Length;
            Symbol s = reelSequence[index];

            Image img = visibleSymbolImages[row];
            if (img != null && symbolSprites != null && (int)s < symbolSprites.Length)
            {
                img.sprite = symbolSprites[(int)s];
            }
        }
    }

    public Symbol GetSymbolAtRow(int row)
    {
        if (reelSequence == null || reelSequence.Length == 0)
            return Symbol.Bell;

        int index = (currentStartIndex + row) % reelSequence.Length;
        return reelSequence[index];
    }

    public void HighlightRow(int row, float duration = 0.5f, int flashes = 3)
    {
        if (row < 0 || row >= visibleSymbolImages.Length) return;
        StartCoroutine(HighlightRoutine(row, duration, flashes));
    }

    private IEnumerator HighlightRoutine(int row, float duration, int flashes)
    {
        Image img = visibleSymbolImages[row];
        if (img == null) yield break;

        Color baseColor = originalColors[row];
        float halfDuration = duration * 0.5f;

        for (int i = 0; i < flashes; i++)
        {
            img.color = highlightColor;
            yield return new WaitForSeconds(halfDuration);
            img.color = baseColor;
            yield return new WaitForSeconds(halfDuration);
        }

        img.color = baseColor;
    }

    public void ResetAllHighlights()
    {
        for (int i = 0; i < visibleSymbolImages.Length; i++)
        {
            if (visibleSymbolImages[i] != null)
                visibleSymbolImages[i].color = originalColors[i];
        }
    }
}
