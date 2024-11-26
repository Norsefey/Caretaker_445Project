using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Camera Settings")]
    public Camera gameCamera;
    public float moveSpeed = 20f;
    public float zoomSpeed = 500f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("Boundaries")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Currency")]
    public int energyOrbs = 100;
    public TMP_Text energyOrbText;

    [Header("Active Elementals")]
    public int fireElementals;
    public TMP_Text fireElementalsText;
    public int waterElementals;
    public TMP_Text waterElementalsText;
    public int natureElementals;
    public TMP_Text natureElementalsText;

    [Header("Building System")]
    public LayerMask placementLayer;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Placement Validation")]
    public LayerMask obstaclesLayer;      // Layer mask for obstacles
    public float overlapCheckRadius = 0.5f;// Radius for overlap check
    public float groundCheckOffset = 0.1f; // Distance above ground for overlap check
    public int groundCheckRays = 4;        // Number of raycasts for ground check

    [Header("Orb Collection")]
    public LayerMask energyOrbLayer;
    private bool cusorInMenu = false;

    private PlaceableItem selectedItem;
    private GameObject previewObject;
    private bool canPlace;
    private Vector3 lastValidPosition;

    private void Awake()
    {
        Instance = this;

        energyOrbText.text = energyOrbs.ToString();
        natureElementalsText.text = natureElementals.ToString();
        fireElementalsText.text = fireElementals.ToString();
        waterElementalsText.text = waterElementals.ToString();
    }
    void Update()
    {
        // do not try to collect while cursor is in menu or placing something to prevent confusion
        if (!cusorInMenu && previewObject == null && Input.GetMouseButtonDown(0))
        {
            HandleOrbCollection();
        }

        HandleCameraMovement();
        HandleZoom();
        HandleObjectPlacement();
        UpdatePreview();
    }
    void HandleOrbCollection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.Log("Trying to collect");
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, energyOrbLayer))
        {
            Debug.Log("Collecting");

            CollectEnergyOrb(2);
            Destroy(hit.collider.gameObject);
        }
    }
    void HandleCameraMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

        transform.position = newPosition;
    }
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float newSize = gameCamera.orthographicSize - scroll * zoomSpeed * Time.deltaTime;
        gameCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
    }
    void UpdatePreview()
    {
        if (previewObject != null && selectedItem != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayer))
            {
                previewObject.SetActive(true);
                Vector3 position = hit.point;
                previewObject.transform.position = position;

                // Check if placement is valid
                canPlace = IsValidPlacement(position);

                // Update preview material
                UpdatePreviewMaterial(canPlace);

                if (canPlace)
                {
                    lastValidPosition = position;
                }
            }
            else
            {
                previewObject.SetActive(false);
                canPlace = false;
            }
        }
    }
    void HandleObjectPlacement()
    {
        if (selectedItem != null && previewObject != null && Input.GetMouseButtonDown(0))
        {
            if (canPlace && CanAfford(selectedItem.cost))
            {
                // Place the object
                GameObject placedObject = Instantiate(selectedItem.prefab, lastValidPosition, Quaternion.identity);
                // activate the interaction script, cant be active before due to infinite feed bug
                placedObject.transform.GetChild(0).gameObject.SetActive(true);
                SpendEnergyOrbs(selectedItem.cost);

                //Deselect after placing
                DeselectObject();
            }
        }

        // Right click to cancel placement
        if (Input.GetMouseButtonDown(1))
        {
            DeselectObject();
        }
    }
    public void CursorOnUI(bool toggle)
    {
        cusorInMenu = toggle;
    }
    public void SelectObject(PlaceableItem item)
    {
        // Clean up previous preview if it exists
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        selectedItem = item;

        // Create new preview
        if (item != null)
        {
            previewObject = Instantiate(item.prefab);
            // Make preview semi-transparent
            SetPreviewTransparency(previewObject);
            previewObject.SetActive(false);
        }
    }
    public void DeselectObject()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        selectedItem = null;
    }
    void SetPreviewTransparency(GameObject obj)
    {
        // Make all renderers semi-transparent
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material previewMaterial = new Material(materials[i]);
                previewMaterial.color = new Color(
                    previewMaterial.color.r,
                    previewMaterial.color.g,
                    previewMaterial.color.b,
                    0.5f
                );
                materials[i] = previewMaterial;
            }
            renderer.materials = materials;
        }
    }
    void UpdatePreviewMaterial(bool isValid)
    {
        if (previewObject != null)
        {
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            Material materialToUse = isValid ? validPlacementMaterial : invalidPlacementMaterial;

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].mainTexture = null;
                    materials[i].color = new Color(
                        materialToUse.color.r,
                        materialToUse.color.g,
                        materialToUse.color.b,
                        0.5f
                    );
                }
                renderer.materials = materials;
            }
        }
    }
    bool IsValidPlacement(Vector3 position)
    {
        if (selectedItem == null) return false;

        // Check if position is within game boundaries
        if (position.x < minX - 40 || position.x > maxX + 40 ||
            position.z < minZ - 40 || position.z > maxZ + 40)
        {
            return false;
        }

        // Check for overlapping obstacles
        Collider[] colliders = Physics.OverlapSphere(
            position + Vector3.up * groundCheckOffset,
            overlapCheckRadius,
            obstaclesLayer
        );

        if (colliders.Length > 0)
        {
            return false; // Overlapping with obstacles
        }

        if (selectedItem.requiresFlatSurface)
        {
            if (!IsGroundFlat(position, selectedItem.maxSlopeAngle))
                return false;
        }

        return true;
    }
    private bool IsGroundFlat(Vector3 position, float maxSlopeAngle)
    {
        Vector3 center = position + Vector3.up * 1f;
        float checkRadius = overlapCheckRadius * 0.8f;

        // Check multiple points in a circle for consistent ground height
        List<float> heights = new List<float>();

        for (int i = 0; i < groundCheckRays; i++)
        {
            float angle = i * (360f / groundCheckRays);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 checkPoint = center + direction * checkRadius;

            Ray ray = new Ray(checkPoint, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 2f, placementLayer))
            {
                heights.Add(hit.point.y);

                // Check slope angle
                if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                {
                    return false; // Too steep
                }
            }
            else
            {
                return false; // No ground detected
            }
        }

        // Check if height difference is within tolerance
        if (heights.Count > 0)
        {
            float maxHeight = Mathf.Max(heights.ToArray());
            float minHeight = Mathf.Min(heights.ToArray());

            // Allow for small height variations (adjust this value as needed)
            float maxHeightDifference = 0.1f;

            return (maxHeight - minHeight) <= maxHeightDifference;
        }

        return false;
    }
    public bool CanAfford(int cost)
    {
        return energyOrbs >= cost;
    }
    public void CollectEnergyOrb(int amount)
    {
        energyOrbs += amount;
        UpdateEnergyOrbDisplay();
    }
    public bool SpendEnergyOrbs(int amount)
    {
        if (CanAfford(amount))
        {
            energyOrbs -= amount;
            UpdateEnergyOrbDisplay();
            return true;
        }
        return false;
    }
    void UpdateEnergyOrbDisplay()
    {
        if (energyOrbText != null)
        {
            energyOrbText.text = $"{energyOrbs}";
        }
    }
    public void UpdateElementalSpiritCount(SpiritStats spirit)
    {
        if (spirit.spiritData.spiritName.Contains("Nature"))
        {
            natureElementals++;
            natureElementalsText.text = natureElementals.ToString();
        }else if (spirit.spiritData.spiritName.Contains("Fire"))
        {
            fireElementals++;
            fireElementalsText.text = fireElementals.ToString();
        }
        else if (spirit.spiritData.spiritName.Contains("Water"))
        {
            waterElementals++;
            waterElementalsText.text = waterElementals.ToString();
        }
    }
}
