using UnityEngine;

public enum TreasureShape { Gem, Coin, Pearl, Ruby, Emerald, Key, Feather, Crystal }

public class TreasureKind
{
    public string id;
    public string name;
    public TreasureShape shape;
    public Color main;
    public Color accent;
}

public static class TreasureCatalog
{
    public static readonly TreasureKind[] All =
    {
        new TreasureKind { id = "gem", name = "Gema azul", shape = TreasureShape.Gem, main = new Color(0.25f, 0.65f, 0.95f), accent = new Color(0.75f, 0.92f, 1f) },
        new TreasureKind { id = "coin", name = "Moneda de oro", shape = TreasureShape.Coin, main = new Color(0.98f, 0.78f, 0.25f), accent = new Color(0.78f, 0.55f, 0.12f) },
        new TreasureKind { id = "pearl", name = "Perla", shape = TreasureShape.Pearl, main = new Color(0.96f, 0.92f, 0.95f), accent = Color.white },
        new TreasureKind { id = "ruby", name = "Rubí", shape = TreasureShape.Ruby, main = new Color(0.86f, 0.18f, 0.3f), accent = new Color(1f, 0.6f, 0.65f) },
        new TreasureKind { id = "emerald", name = "Esmeralda", shape = TreasureShape.Emerald, main = new Color(0.2f, 0.7f, 0.42f), accent = new Color(0.65f, 0.95f, 0.75f) },
        new TreasureKind { id = "key", name = "Llave antigua", shape = TreasureShape.Key, main = new Color(0.72f, 0.5f, 0.25f), accent = new Color(0.95f, 0.78f, 0.5f) },
        new TreasureKind { id = "feather", name = "Pluma de plata", shape = TreasureShape.Feather, main = new Color(0.78f, 0.82f, 0.9f), accent = new Color(0.45f, 0.5f, 0.62f) },
        new TreasureKind { id = "crystal", name = "Cristal rosa", shape = TreasureShape.Crystal, main = new Color(0.9f, 0.5f, 0.82f), accent = new Color(1f, 0.82f, 0.95f) }
    };

    public static int IndexOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return i;
        return -1;
    }
}
