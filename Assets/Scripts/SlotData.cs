using UnityEngine;

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

[System.Serializable]
public class LinePattern
{
    public int[] rowByReel = new int[5];
}

[System.Serializable]
public class PaytableEntry
{
    public Symbol symbol;
    public int minCount;
    public int rewardCredits;
}