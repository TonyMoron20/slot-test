using UnityEngine;

// Enum que lista todas las frutas posibles del slot
// El orden debe coincidir con el orden de los sprites en los arreglos
public enum Symbol
{
    Bell,
    Watermelon,
    Grapes,
    Plum,
    Orange,
    Lemon,
    Cherry
}

// Representa un patrón de línea para cada carrete, indica qué fila se usa (0=arriba,1=centro,2=abajo)
[System.Serializable]
public class LinePattern
{
    // Este arreglo tiene 5 posiciones para los 5 carretes de la maquina
    // Cada posición indica la fila que se toma en ese carrete
    public int[] rowByReel = new int[5];
}


// Representa los creditos que se ganaran por una combinación de frutas
[System.Serializable]
public class PaytableEntry
{
    // Fruta
    public Symbol symbol;
    // Cantidad mínima de frutas seguidas en la línea desde el 1er carrete
    public int minCount;
    // Creditos que se ganan si se cumple la condicion
    public int rewardCredits;
}