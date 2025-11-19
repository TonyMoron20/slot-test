using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Este script se encarga de controlar un carrete de la máquina
public class ReelController : MonoBehaviour
{
    [Header("Config")]
    // Secuencia de frutas que componen el carrete
    public Symbol[] reelSequence;
    // Velocidad con que se mueve el carrete
    public float spinSpeed = 20f;
    // Cantidad de filas visibles que tiene el carrete en la maquina
    public int visibleRows = 3;

    [Header("UI")]
    // Referencias a las imagenes de las filas que se muestran en la pantalla (arriba, centro, abajo)
    public Image[] visibleSymbolImages;
    // Sprites que representan cada fruta en el carrete
    public Sprite[] symbolSprites;

    [Header("FX")]
    // Color que se usará para resaltar una fila ganadora
    public Color highlightColor = Color.yellow;

    // Indica si el carrete esta girando o no
    private bool isSpinning = false;
    // Indice actual dentro de reelSequence que representa la fila superior
    private int currentStartIndex = 0;
    // Colores originales de cada imagen, para poder restaurarlos despues de que se resalten
    private Color originalColor = Color.white;

    // Comienza el giro del carrete
    public void StartSpin()
    {
        // Si ya esta girando, se ignora la llamada a la corutina
        if (isSpinning) return;
        isSpinning = true;
        // Se inicia la corrutina que va moviendo el carrete
        StartCoroutine(SpinRoutine());
    }

    // Detiene el carrete despues del tiempo especificado
    public IEnumerator StopSpin(float afterSeconds)
    {
        // Espera la cantidad de segundos indicada
        yield return new WaitForSeconds(afterSeconds);
        // Se pone en falso y con esto el carrete ya no seguira girando
        isSpinning = false;

        // Actualiza las imagenes visibles en la maquina
        UpdateVisibleSymbols();
    }

    // Corrutina encargada del giro, si isSpinning es true, va avanzando el indice y refrescando las imagenes
    private IEnumerator SpinRoutine()
    {
        // Si el carrete no tiene frutas agregadas, sale de la corutina
        if (reelSequence == null || reelSequence.Length == 0)
        {
            yield break;
        }

        // Intervalo de tiempo entre cada paso del carrete
        float stepDelay = 1f / spinSpeed;

        while (isSpinning)
        {
            // Avanza una fruta en la cinta, el movimiento es hacia abajo
            currentStartIndex = (currentStartIndex + 1) % reelSequence.Length;
            // Actualiza las filas visibles
            UpdateVisibleSymbols();
            // Espera antes de dar el siguiente paso de giro
            yield return new WaitForSeconds(stepDelay);
        }
    }

    // Actualiza las imagenes visibles para que coincidan con la posición actual del carrete
    private void UpdateVisibleSymbols()
    {
        // Si no hay secuencia, no se hace nada
        if (reelSequence == null || reelSequence.Length == 0) return;

        int rowsToUpdate = Mathf.Min(visibleRows, visibleSymbolImages.Length);

        for (int row = 0; row < rowsToUpdate; row++)
        {
            // Calcula que fruta corresponde a esta fila visible
            int index = (currentStartIndex + row) % reelSequence.Length;
            Symbol s = reelSequence[index];

            Image img = visibleSymbolImages[row];
            // Si existe la imagen y se tienen sprites suficientes, asignamos el sprite de la fruta correcto
            if (img != null && symbolSprites != null && (int)s < symbolSprites.Length)
            {
                img.sprite = symbolSprites[(int)s];
            }
        }
    }

    // Devuelve qué fruta hay en una de las filas visibles en la maquina (0=arriba, 1=centro, 2=abajo)
    // Esto lo utiliza el controlador de la maquina para evaluar si la línea tiene premio
    public Symbol GetSymbolAtRow(int row)
    {
        // Si no hay secuencia, se devuelve un valor por defecto
        if (reelSequence == null || reelSequence.Length == 0)
            return Symbol.Bell;

        int index = (currentStartIndex + row) % reelSequence.Length;
        return reelSequence[index];
    }

    // Resalta una fila especifica (se usa cuando la fila forma parte de una linea ganadora)
    public void HighlightRow(int row, float duration = 0.5f, int flashes = 3)
    {
        // Si el índice de fila no es valido, sale de la función
        if (row < 0 || row >= visibleSymbolImages.Length) return;

        // Inicia la corrutina que hace parpadear la fila
        StartCoroutine(HighlightRoutine(row, duration, flashes));
    }

    // Corrutina que hace parpadear una fila cambiando su color varias veces
    private IEnumerator HighlightRoutine(int row, float duration, int flashes)
    {
        Image img = visibleSymbolImages[row];
        if (img == null) yield break;

        // Pone el color base original de esa fila
        Color baseColor = originalColor;
        float halfDuration = duration * 0.5f;

        for (int i = 0; i < flashes; i++)
        {
            // Cambia al color de highlight
            img.color = highlightColor;
            yield return new WaitForSeconds(halfDuration);

            // Vuelve al color original
            img.color = baseColor;
            yield return new WaitForSeconds(halfDuration);
        }

        // Hace que se quede con el color normal
        img.color = baseColor;
    }

    // Restaura el color original de TODAS las filas
    public void ResetAllHighlights()
    {
        for (int i = 0; i < visibleSymbolImages.Length; i++)
        {
            if (visibleSymbolImages[i] != null)
                visibleSymbolImages[i].color = originalColor;
        }
    }
}
