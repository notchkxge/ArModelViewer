using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TapToPlace : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager; // ADDED: Needed to check if planes exist
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject capsulePrefab;
    public ARModelManager modelManager;

    private string selectedModel = "Cube";
    private bool isARReady = false; // ADDED: Prevents placement until planes detected

    void Start()
    {
        // Optional: wait for first plane
        // You can also hook into planeManager's planesChanged event
    }

    void Update()
    {
        // 1. Check if AR is actually ready (at least one plane detected)
        if (planeManager != null && planeManager.trackables.count > 0)
        {
            isARReady = true;
        }
        else
        {
            isARReady = false; // No planes yet
            return; // Exit – can't place objects
        }

        // 2. If AR is ready, check for touch/click input
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector2 inputPosition;
            if (Input.touchCount > 0)
                inputPosition = Input.GetTouch(0).position;
            else
                inputPosition = Input.mousePosition;

            // Prevent placing when clicking on UI
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // 3. Raycast only against detected planes
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                hitPose.position += new Vector3(0.1f, 0.1f, 0);
                SpawnSelectedModel(hitPose.position, hitPose.rotation);
            }
            // NO FALLBACK: Don't place anywhere if no plane is hit
            // This ensures placement only happens on real AR detection
        }
    }

    void SpawnSelectedModel(Vector3 position, Quaternion rotation)
    {
        GameObject newModel = null;

        switch (selectedModel)
        {
            case "Cube":
                newModel = Instantiate(cubePrefab, position, rotation);
                break;
            case "Sphere":
                newModel = Instantiate(spherePrefab, position, rotation);
                break;
            case "Capsule":
                newModel = Instantiate(capsulePrefab, position, rotation);
                break;
            default:
                newModel = Instantiate(cubePrefab, position, rotation);
                break;
        }

        if (newModel != null)
        {
            newModel.AddComponent<ARAnchor>();

            if (modelManager != null)
            {
                if (modelManager.currentModel != null)
                    Destroy(modelManager.currentModel);
                modelManager.currentModel = newModel;
                modelManager.currentModelName = selectedModel;
                Debug.Log($"{selectedModel} spawned at {position}");
            }
        }
    }

    // Called by the dropdown's OnValueChanged event
    public void SetSelectedModel(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 0:
                selectedModel = "Cube";
                break;
            case 1:
                selectedModel = "Sphere";
                break;
            case 2:
                selectedModel = "Capsule";
                break;
            default:
                selectedModel = "Cube";
                break;
        }
        Debug.Log($"Selected model changed to: {selectedModel}");
    }
}