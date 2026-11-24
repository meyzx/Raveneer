using System;

public enum AchievementMetric { Nights, Items, Album }

public class AchievementDef
{
    public string id;
    public string name;
    public string description;
    public AchievementMetric metric;
    public int target;
}

public static class Achievements
{
    public static readonly AchievementDef[] All =
    {
        new AchievementDef { id = "night_1", name = "Primera noche", description = "Sobrevive 1 noche", metric = AchievementMetric.Nights, target = 1 },
        new AchievementDef { id = "night_3", name = "Ojos de búho", description = "Sobrevive 3 noches", metric = AchievementMetric.Nights, target = 3 },
        new AchievementDef { id = "night_7", name = "Guardián nocturno", description = "Sobrevive 7 noches", metric = AchievementMetric.Nights, target = 7 },
        new AchievementDef { id = "items_10", name = "Pico curioso", description = "Recolecta 10 objetos", metric = AchievementMetric.Items, target = 10 },
        new AchievementDef { id = "items_50", name = "Recolector", description = "Recolecta 50 objetos", metric = AchievementMetric.Items, target = 50 },
        new AchievementDef { id = "items_100", name = "Gran acaparador", description = "Recolecta 100 objetos", metric = AchievementMetric.Items, target = 100 },
        new AchievementDef { id = "album_1", name = "Primera foto", description = "Fotografía 1 objeto precioso", metric = AchievementMetric.Album, target = 1 },
        new AchievementDef { id = "album_4", name = "Coleccionista", description = "Fotografía 4 objetos preciosos", metric = AchievementMetric.Album, target = 4 },
        new AchievementDef { id = "album_8", name = "Álbum completo", description = "Fotografía los 8 objetos preciosos", metric = AchievementMetric.Album, target = 8 }
    };

    public static event Action<AchievementDef> OnUnlocked;

    public static int Value(AchievementMetric metric)
    {
        var persistence = DataPersistence.Instance;
        if (persistence == null) return 0;

        switch (metric)
        {
            case AchievementMetric.Nights: return persistence.Data.nightsSurvived;
            case AchievementMetric.Items: return persistence.Data.itemsCollected;
            default: return persistence.Data.album.Count;
        }
    }

    public static bool IsUnlocked(AchievementDef def)
    {
        return DataPersistence.Instance != null && DataPersistence.Instance.Data.achievements.Contains(def.id);
    }

    public static void Evaluate()
    {
        var persistence = DataPersistence.Instance;
        if (persistence == null) return;

        foreach (var def in All)
        {
            if (persistence.Data.achievements.Contains(def.id)) continue;
            if (Value(def.metric) < def.target) continue;

            persistence.Data.achievements.Add(def.id);
            persistence.Save();
            if (OnUnlocked != null) OnUnlocked(def);
        }
    }
}
