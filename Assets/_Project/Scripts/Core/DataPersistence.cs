using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class AlbumEntry
{
    public string kindId;
    public string title;
    public string date;
}

[Serializable]
public class SaveData
{
    public List<string> nestItems = new List<string>();
    public List<string> keysCollected = new List<string>();
    public float bestSurvivalTime;
    public int itemsCollected;
    public int nightsSurvived;
    public List<AlbumEntry> album = new List<AlbumEntry>();
    public List<string> achievements = new List<string>();
}

public class DataPersistence : MonoBehaviour
{
    public const int TOTAL_KEYS = 5;

    public static DataPersistence Instance { get; private set; }
    public SaveData Data { get; private set; } = new SaveData();

    private string FilePath => Path.Combine(Application.persistentDataPath, "raveneer_save.json");

    void Awake()
    {
        if (!Application.isPlaying) return;

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    void OnApplicationQuit()
    {
        Save();
    }

    public void AddToNest(string itemId, bool isKey)
    {
        if (isKey)
        {
            if (!Data.keysCollected.Contains(itemId) && Data.keysCollected.Count < TOTAL_KEYS)
                Data.keysCollected.Add(itemId);
        }
        else
        {
            Data.nestItems.Add(itemId);
        }

        Save();
    }

    public static string AlbumFolder => Path.Combine(Application.persistentDataPath, "album");

    public AlbumEntry FindAlbum(string kindId)
    {
        return Data.album.Find(entry => entry.kindId == kindId);
    }

    public Texture2D LoadPhoto(string kindId)
    {
        try
        {
            string path = Path.Combine(AlbumFolder, kindId + ".jpg");
            if (!File.Exists(path)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError("Error al leer la foto: " + e.Message);
            return null;
        }
    }

    public void SaveAlbumEntry(string kindId, string title, byte[] photo)
    {
        try
        {
            Directory.CreateDirectory(AlbumFolder);
            File.WriteAllBytes(Path.Combine(AlbumFolder, kindId + ".jpg"), photo);
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar la foto: " + e.Message);
        }

        var entry = FindAlbum(kindId);
        if (entry == null)
        {
            entry = new AlbumEntry { kindId = kindId };
            Data.album.Add(entry);
        }

        entry.title = title;
        entry.date = DateTime.Now.ToString("dd/MM/yyyy");
        Save();
        Achievements.Evaluate();
    }

    public void AddItemCollected()
    {
        Data.itemsCollected++;
        Achievements.Evaluate();
    }

    public void AddNightSurvived()
    {
        Data.nightsSurvived++;
        Save();
        Achievements.Evaluate();
    }

    public bool TrySetRecord(float seconds)
    {
        if (seconds <= Data.bestSurvivalTime) return false;

        Data.bestSurvivalTime = seconds;
        Save();
        return true;
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar: " + e.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (loaded != null) Data = loaded;
        }
        catch (Exception e)
        {
            Debug.LogError("Error al cargar: " + e.Message);
            Data = new SaveData();
        }
    }
}
