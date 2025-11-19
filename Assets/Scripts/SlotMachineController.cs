using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Estructura que guarda la informacion de una línea ganadora al finalizar un giro
[System.Serializable]
public struct WinInfo
{
    // Indice del patron dentro del arreglo linePatterns
    public int patternIndex;
    // Fruta que formo la combinación ganadora
    public Symbol symbol;
    // Número de simbolos iguales seguidos
    public int count;
    // Creditos ganados
    public int reward;

    public WinInfo(int patternIndex, Symbol symbol, int count, int reward)
    {
        this.patternIndex = patternIndex;
        this.symbol = symbol;
        this.count = count;
        this.reward = reward;
    }
}

// Este script controla toda la logica de la máquina:
// - Maneja los carretes
// - Controla los creditos que se gastan y ganan
// - Verifica las lineas ganadoras
// - Muestra los resultados en pantalla
public class SlotMachineController : MonoBehaviour
{
    [Header("Reels")]
    // Arreglo que referencia a los carretes que forman la máquina
    public ReelController[] reels;

    [Header("UI")]
    // Botón para iniciar el giro de la máquina
    public Button spinButton;
    // Texto que muestra los créditos actuales del jugador
    public Text creditsText;
    // Texto que muestra cuántos creditos se ganaron en el último giro
    public Text lastWinText;

    [Header("Game Config")]
    // Creditos actuales del jugador
    public int credits = 1000;
    // Costo de cada giro
    public int betPerSpin = 10;

    [Header("Patterns & Paytable")]
    // Lista de patrones de línea, se agregaron los 9 del documento.
    public LinePattern[] linePatterns;
    // Tabla de pago, fruta, cantidad mínima y creditos que paga
    public PaytableEntry[] paytableEntries;

    [Header("FX & Log")]
    // Texto de prueba donde se muestra que líneas han ganado y el total de creditos ganados
    public Text logText;

    // Lista con la informacion de las líneas ganadoras en el ultimo giro
    private readonly List<WinInfo> lastWins = new List<WinInfo>();

    // Indica si la maquina se encuentra girando
    private bool isSpinning = false;

    void Start()
    {
        // Actualiza la UI con los creditos iniciales
        UpdateUI();

        // Se conecta el botón de spin con la funcion que maneja el click
        if (spinButton != null)
            spinButton.onClick.AddListener(OnSpinButton);
    }

    void OnDestroy()
    {
        // Desconecta el listener al destruir el objeto
        if (spinButton != null)
            spinButton.onClick.RemoveListener(OnSpinButton);
    }

    // Se llama cuando se hace click en el botón spin
    void OnSpinButton()
    {
        // Si ya está girando la maquina, se ignora el click
        if (isSpinning) return;

        // Valida si hay creditos suficientes para pagar jugar
        if (credits < betPerSpin)
        {
            Debug.Log("No hay créditos suficientes");
            return;
        }

        // Descuenta el costo del giro
        credits -= betPerSpin;
        UpdateUI();

        // Inicia la corrutina que hace que gire la maquina
        StartCoroutine(SpinRoutine());
    }

    // Controla el flujo completo del giro:
    // - Empieza a girar cada carrete
    // - Los detiene con delays aleatorios
    // - Evalua las líneas y muestra el resultado
    private IEnumerator SpinRoutine()
    {
        // Si no hay carretes configurados, no se hace nada
        if (reels == null || reels.Length == 0)
            yield break;

        isSpinning = true;
        // Borra el texto de la ultima ganancia mientras se realiza el giro
        if (lastWinText != null)
            lastWinText.text = "";

        // Pequeño retraso entre el inicio del giro de cada carrete
        float delayBetweenReels = 0.2f;

        // 1) Inicia el giro de cada carrete, uno tras otro
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].StartSpin();
            yield return new WaitForSeconds(delayBetweenReels);
        }

        // 2) Detiene cada carrete con un tiempo aleatorio entre 2 y 4 segundos
        for (int i = 0; i < reels.Length; i++)
        {
            float stopDelay = Random.Range(2f, 4f);
            yield return StartCoroutine(reels[i].StopSpin(stopDelay));
        }

        // 3) Cuando se detiene la maquina, evalua las lineas resultantes y se calculan las ganancias totales
        int totalWin = EvaluatePatterns();
        credits += totalWin;

        // Se muestra el detalle en el log y se resaltan las filas ganadoras
        ShowWinLog();
        PlayHighlightFX();

        // Muestra la ganancia total del giro en el texto correspondiente
        if (lastWinText != null)
            lastWinText.text = $"Ganaste: {totalWin} créditos";

        // Actualiza los creditos en la pantalla
        UpdateUI();

        isSpinning = false;
    }

    // Actualiza el texto que muestra los creditos
    private void UpdateUI()
    {
        if (creditsText != null)
            creditsText.text = $"Créditos: {credits}";
    }

    // Evalua todas las líneas que se definieron en linePatterns, y se usa la paytable para calcular cuanto se gana.
    private int EvaluatePatterns()
    {
        // Limpia los resultados anteriores
        lastWins.Clear();

        // Si no hay patrones o carretes, no evalua nada
        if (linePatterns == null || linePatterns.Length == 0) return 0;
        if (reels == null || reels.Length == 0) return 0;

        int totalWin = 0;

        // Recorre todos los patrones de línea
        for (int p = 0; p < linePatterns.Length; p++)
        {
            var pattern = linePatterns[p];
            // Validaciones basicas, patrón no nulo, array de filas válido y del mismo tamaño que la cantidad de carretes
            if (pattern == null || pattern.rowByReel == null) continue;
            if (pattern.rowByReel.Length != reels.Length) continue;

            // Se evalua una sola línea con este patrón
            WinInfo? maybeWin = EvaluateSinglePattern(p, pattern);
            if (maybeWin.HasValue)
            {
                WinInfo win = maybeWin.Value;
                // Suma el premio de esta línea al total
                totalWin += win.reward;
                // Guarda el detalle para el log y los efectos visuales
                lastWins.Add(win);
            }
        }

        return totalWin;
    }

    // Evalua un solo patrón de línea:
    // Revisa que frutas salieron en esa línea
    // Cuenta cuantos iguales hay desde el primer carrete
    // Busca el o los premios que le corresponden en la paytable
    // Si no hay premio, devuelve null; si sí hay, devuelve un WinInfo
    private WinInfo? EvaluateSinglePattern(int patternIndex, LinePattern pattern)
    {
        int reelCount = reels.Length;
        Symbol[] lineSymbols = new Symbol[reelCount];

        // Construye el arreglo de frutas que aparecen en esta línea
        for (int reelIndex = 0; reelIndex < reelCount; reelIndex++)
        {
            int row = pattern.rowByReel[reelIndex];
            lineSymbols[reelIndex] = reels[reelIndex].GetSymbolAtRow(row);
        }

        // Tomamos la fruta del primer carrete como referencia para la combinacion
        Symbol firstSymbol = lineSymbols[0];
        int count = 1;

        // Se cuenta la cantidad de carretes seguidos que tienen esa misma fruta
        for (int i = 1; i < lineSymbols.Length; i++)
        {
            if (lineSymbols[i] == firstSymbol)
                count++;
            else
                break;
        }

        int bestReward = 0;

        // se busca en la paytable el premio aplicable para esa fruta y la cantidad
        for (int i = 0; i < paytableEntries.Length; i++)
        {
            var entry = paytableEntries[i];
            // Si la fruta coincide y se cumple el minimo de casillas
            if (entry.symbol == firstSymbol && count >= entry.minCount)
            {
                // Se obtiene el valor mas alto que se encuentre
                if (entry.rewardCredits > bestReward)
                    bestReward = entry.rewardCredits;
            }
        }

        // Si no hay premio, regresa null
        if (bestReward <= 0)
            return null;

        // Si hay premio, se crea una estructura con toda la info de esta línea ganadora
        return new WinInfo(patternIndex, firstSymbol, count, bestReward);
    }

    // Muestra en pantalla todas las líneas ganadoras del giro, estas solo se ven cuando el juego esta en prueba
    private void ShowWinLog()
    {
        if (logText == null) return;

        // Si no hubo líneas ganadoras, se muestra un mensaje simple
        if (lastWins.Count == 0)
        {
            logText.text = "Sin líneas ganadoras.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Líneas ganadoras:");

        // Se recorre cada WinInfo y se agrega una linea de texto con el detalle
        for (int i = 0; i < lastWins.Count; i++)
        {
            var win = lastWins[i];
            sb.AppendLine(
                $"Línea {win.patternIndex}: {win.symbol} x{win.count} -> {win.reward} créditos"
            );
        }

        logText.text = sb.ToString();
    }

    // Se resaltan las casillas que forman parte de las lineas ganadoras
    private void PlayHighlightFX()
    {
        if (reels == null) return;

        // Se limpian las casillas resaltadas de la ronda anterior
        foreach (var reel in reels)
        {
            reel.ResetAllHighlights();
        }

        // Por cada línea ganadora, se resaltan las filas correspondientes
        for (int i = 0; i < lastWins.Count; i++)
        {
            var win = lastWins[i];
            LinePattern pattern = linePatterns[win.patternIndex];

            // Solo se resaltan la cantidad de carretes que forman la combinación ganadora
            for (int reelIndex = 0; reelIndex < win.count; reelIndex++)
            {
                int row = pattern.rowByReel[reelIndex];
                reels[reelIndex].HighlightRow(row);
            }
        }
    }
}