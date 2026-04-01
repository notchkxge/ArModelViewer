using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TapToPlace : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject cubePrefab;              // assign your cube prefab here
    public ARModelManager modelManager;        // assign the object with ARModelManager

    void Update()
    {
        // Handle input: mouse click in Editor, touch on device
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // Get the input position
            Vector2 inputPosition;
            if (Input.touchCount > 0)
                inputPosition = Input.GetTouch(0).position;
            else
                inputPosition = Input.mousePosition;

            // Ignore if the input is over a UI element
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // Perform AR raycast to find a plane
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                SpawnCubeAt(hitPose.position, hitPose.rotation);
            }
            else
            {
                // Optional: fallback for Editor when no planes are detected
                // Spawn in front of camera (for testing without AR planes)
                Vector3 fallbackPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                SpawnCubeAt(fallbackPos, Quaternion.identity);
            }
        }
    }

    void SpawnCubeAt(Vector3 position, Quaternion rotation)
    {
        GameObject newCube = Instantiate(cubePrefab, position, rotation);
        newCube.AddComponent<ARAnchor>();

        // Update the model manager
        if (modelManager != null)
        {
            if (modelManager.currentModel != null)
                Destroy(modelManager.currentModel);
            modelManager.currentModel = newCube;
            modelManager.currentModelName = "Cube";   // must match database name
            Debug.Log("Cube spawned at " + position);
        }
    }
}