using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using TMPro;

public class ARModelManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Base URL of your API, e.g., http://localhost:5000/api/CreateAsset/byname/")]
    public string apiBaseUrl = "";   // ← Set this in the Inspector

    [Header("UI References")]
    public TextMeshProUGUI descriptionText;     // UI Text to show description

    [Header("AR Placement")]
    public ARRaycastManager raycastManager;
    public GameObject placementIndicator;   // optional

    // Public fields so other scripts (like TapToPlace) can access them
    public GameObject currentModel;   // currently spawned model
    public string currentModelName;   // name of the last spawned model

    // Called when you spawn a model (e.g., by a button or tap on plane)
    public void SpawnModel(string modelName)
    {
        // Remove previous model
        if (currentModel != null) Destroy(currentModel);

        // Load prefab from Resources (folder: Resources/prefab/ModelName.prefab)
        GameObject prefab = Resources.Load<GameObject>($"prefab/{modelName}");
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found: {modelName}");
            if (descriptionText != null)
                descriptionText.text = $"Prefab '{modelName}' not found.";
            return;
        }

        // Place in front of camera
        Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.0f;
        currentModel = Instantiate(prefab, spawnPos, Quaternion.identity);
        currentModel.AddComponent<ARAnchor>();  // to keep it anchored

        currentModelName = modelName;

        Debug.Log($"Spawned model: {modelName}");
    }

    // Auto-fetch after delay (optional)
    IEnumerator AutoFetchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FetchDescription();
    }

    // Called by a UI button (or auto after delay)
    public void FetchDescription()
    {
        Debug.Log($"FetchDescription called. CurrentModelName = {currentModelName}");

        // Check if we have a model spawned
        if (string.IsNullOrEmpty(currentModelName))
        {
            Debug.LogWarning("No model spawned yet.");
            if (descriptionText != null)
                descriptionText.text = "Spawn a model first.";
            return;
        }

        // Check API base URL
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            Debug.LogError("API Base URL is not set in the Inspector!");
            if (descriptionText != null)
                descriptionText.text = "API URL missing.";
            return;
        }

        // Check UI text reference
        if (descriptionText == null)
        {
            Debug.LogError("Description Text UI is not assigned!");
            return;
        }

        StartCoroutine(GetDescriptionFromAPI(currentModelName));
    }

    IEnumerator GetDescriptionFromAPI(string modelName)
    {
        string url = apiBaseUrl + modelName;
        Debug.Log($"Fetching from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                try
                {
                    AssetData data = JsonUtility.FromJson<AssetData>(json);
                    descriptionText.text = data.description;
                    Debug.Log($"Description for {modelName}: {data.description}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse JSON: {e.Message}\nJSON: {json}");
                    descriptionText.text = "Error parsing description.";
                }
            }
            else
            {
                string errorMsg = $"API error: {request.responseCode} - {request.error}";
                Debug.LogError($"Failed to fetch {modelName}: {errorMsg}");
                descriptionText.text = errorMsg;
            }
        }
    }

    [System.Serializable]
    public class AssetData
    {
        public string name;
        public string description;
        public string modelPath;   // you could use this if needed
    }
}