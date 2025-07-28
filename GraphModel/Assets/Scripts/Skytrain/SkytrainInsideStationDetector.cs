using UnityEngine;

public class SkytrainInsideStationDetector : MonoBehaviour
{
    public GameObject loadingAreaPrefab; // Assign in Inspector
    public float stopThreshold = 0.001f; // Minimum movement to consider as "moving"

    private Vector3 lastPosition;
    private bool isInsideTrigger = false;
    private bool wasMoving = true;
    private Collider currentTriggerZone;

    private GameObject leftLoadingArea;
    private GameObject rightLoadingArea;

    void Start()
    {
        lastPosition = transform.position;
        if (loadingAreaPrefab == null)
        {
            Debug.LogError("Please assign a Loading Area Prefab.");
        }
    }

    void Update()
    {
        if (!isInsideTrigger || loadingAreaPrefab == null) return;

        float movement = (transform.position - lastPosition).magnitude;
        //Debug.Log("movement: " + movement); // Keeping this in case we need to debug values
        bool isCurrentlyMoving = movement > stopThreshold;

        if (!isCurrentlyMoving && wasMoving)
        {
            //Debug.Log("Skytrain has stopped inside the station."); // Keeping this in case we need to debug values
            SpawnLoadingAreas();
        }

        if (isCurrentlyMoving && !wasMoving)
        {
            //Debug.Log("Skytrain has started moving inside the station."); // Keeping this in case we need to debug values
            RemoveLoadingAreas();
        }

        wasMoving = isCurrentlyMoving;
        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object has a MeshFilter (assumed to be a cube)
        if (other.GetComponent<MeshFilter>())
        {
            currentTriggerZone = other;
            isInsideTrigger = true;
            //Debug.Log("Object entered the station area.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentTriggerZone)
        {
            //Debug.Log("Object exited the station area.");
            isInsideTrigger = false;
            currentTriggerZone = null;
        }
    }

    private void SpawnLoadingAreas()
    {
        if (leftLoadingArea != null || rightLoadingArea != null) return; // Already spawned

        Vector3 leftPos = transform.position + transform.right * -1.5f;
        Vector3 rightPos = transform.position + transform.right * 1.5f;

        leftLoadingArea = Instantiate(loadingAreaPrefab, leftPos, Quaternion.identity, transform);
        rightLoadingArea = Instantiate(loadingAreaPrefab, rightPos, Quaternion.identity, transform);
    }

    private void RemoveLoadingAreas()
    {
        if (leftLoadingArea != null) Destroy(leftLoadingArea);
        if (rightLoadingArea != null) Destroy(rightLoadingArea);

        leftLoadingArea = null;
        rightLoadingArea = null;
    }
}
