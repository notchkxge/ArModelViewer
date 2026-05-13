using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MetadataCacheManager : MonoBehaviour
{
    // Singleton
    public static MetadataCacheManager Instance { get; private set; }

    [SerializeField] private string baseUrl = "http://192.168.0.125:5000/api/CreateAsset";
    private string cacheFilePath;
    private Dictionary<string, CachedEntry> cache = new Dictionary<string, CachedEntry>();

    [Serializable]
    private class CachedEntry
    {
        public string name;
        public string description;
        public string modelPath;
        public string cachedAt;
    }

    [Serializable]
    private class CacheFile
    {
        public List<CachedEntry> entries = new List<CachedEntry>();
    }

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // survives scene loads if you have multiple scenes

        cacheFilePath = Path.Combine(Application.persistentDataPath, "metadata_cache.json");
        LoadCacheFromDisk();
    }

    // Public method – unchanged, but now accessible via Instance
    public void GetDescription(string modelName, Action<string> onResult)
    {
        StartCoroutine(FetchDescriptionCoroutine(modelName, onResult));
    }

    private IEnumerator FetchDescriptionCoroutine(string modelName, Action<string> callback)
    {
        // 1. Cache hit
        if (cache.TryGetValue(modelName, out CachedEntry cached))
        {
            callback?.Invoke(cached.description ?? "No description");
            yield break;
        }

        // 2. Offline and miss
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callback?.Invoke("Offline – no cached data");
            yield break;
        }

        // 3. Online fetch
        string url = $"{baseUrl}/byname/{UnityWebRequest.EscapeURL(modelName)}";
        Debug.Log($"Fetching from: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log($"API response: {json}");

                try
                {
                    ApiResponse record = JsonUtility.FromJson<ApiResponse>(json);
                    if (record != null && !string.IsNullOrEmpty(record.name))
                    {
                        CachedEntry entry = new CachedEntry
                        {
                            name = record.name,
                            description = record.description,
                            modelPath = record.modelPath,
                            cachedAt = DateTime.UtcNow.ToString("o")
                        };
                        cache[record.name] = entry;
                        SaveCacheToDisk();
                        callback?.Invoke(record.description ?? "No description");
                    }
                    else
                    {
                        callback?.Invoke("Invalid data from server");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("JSON parse error: " + e.Message);
                    callback?.Invoke("Parse error");
                }
            }
            else
            {
                Debug.LogError($"Network error: {req.error}");
                callback?.Invoke("Network error: " + req.error);
            }
        }
    }

    private void LoadCacheFromDisk()
    {
        if (!File.Exists(cacheFilePath)) return;
        string json = File.ReadAllText(cacheFilePath);
        CacheFile data = JsonUtility.FromJson<CacheFile>(json);
        if (data?.entries != null)
        {
            cache.Clear();
            foreach (var entry in data.entries)
            {
                if (!string.IsNullOrEmpty(entry.name))
                    cache[entry.name] = entry;
            }
            Debug.Log($"Loaded {cache.Count} cached entries from disk.");
        }
    }

    private void SaveCacheToDisk()
    {
        CacheFile data = new CacheFile();
        data.entries.AddRange(cache.Values);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(cacheFilePath, json);
    }

    [Serializable]
    private class ApiResponse
    {
        public string name;
        public string description;
        public string modelPath;
    }
}