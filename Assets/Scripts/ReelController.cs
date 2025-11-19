using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ReelController : MonoBehaviour
{
    [Header("Config")]
    public Symbol[] reelSequence;
    public float spinSpeed = 20f;
    public int visibleRows = 3;

    private bool isSpinning = false;
    private int currentStartIndex = 0;

   
    public Image[] visibleSymbolImages;
    public Sprite[] symbolSprites;

    public Color normalColor = Color.white;

    public void StartSpin()
    {
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
        while (isSpinning)
        {
            currentStartIndex = (currentStartIndex + 1) % reelSequence.Length;
            UpdateVisibleSymbols();
            yield return new WaitForSeconds(1f / spinSpeed); 
        }
    }

    private void UpdateVisibleSymbols()
    {
        for (int row = 0; row < visibleRows; row++)
        {
            int index = (currentStartIndex + row) % reelSequence.Length;
            Symbol s = reelSequence[index];

            if (row < visibleSymbolImages.Length)
            {
                visibleSymbolImages[row].sprite = symbolSprites[(int)s];
            }
        }
    }

    public Symbol GetSymbolAtRow(int row)
    {
        int index = (currentStartIndex + row) % reelSequence.Length;
        return reelSequence[index];
    }

    public void HighlightRow(int row, float duration = 0.5f, int flashes = 3)
    {
        if (row < 0 || row >= visibleSymbolImages.Length) return;
        StartCoroutine(HighlightRoutine(row, duration, flashes));
    }

    IEnumerator HighlightRoutine(int row, float duration, int flashes)
    {
        Image img = visibleSymbolImages[row];
        if (img == null) yield break;

        normalColor = img.color;

        for (int i = 0; i < flashes; i++)
        {
            img.color = Color.yellow;
            yield return new WaitForSeconds(duration * 0.5f);
            img.color = normalColor;
            yield return new WaitForSeconds(duration * 0.5f);
        }

        img.color = normalColor;
    }

    public void ResetAllHighlights()
    {
        for (int i = 0; i < visibleSymbolImages.Length; i++)
        {
            if (visibleSymbolImages[i] != null)
                visibleSymbolImages[i].color = normalColor;
        }
    }
}
