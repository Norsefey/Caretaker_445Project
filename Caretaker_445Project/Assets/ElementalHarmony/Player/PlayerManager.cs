using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.Burst.CompilerServices;
using UnityEngine.AI;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    private PlaceableUI UI;

    [SerializeField] private float doomsDayTimer = 60;
    private bool startedDoomTimer = false;

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
    public int fireElementals = 0;
    public TMP_Text fireElementalsText;
    public int waterElementals = 0;
    public TMP_Text waterElementalsText;
    public int natureElementals = 0;
    public TMP_Text natureElementalsText;

    private SpiritStats currentFollowedSpirit;
    private bool isFollowingSpirit = false;

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
        UI = GetComponent<PlaceableUI>();
        energyOrbText.text = energyOrbs.ToString();

        waterElementalsText.text = waterElementals.ToString();
        fireElementalsText.text = fireElementals.ToString();
        natureElementalsText.text = natureElementals.ToString();

    }
    void Update()
    {
        // do not try to collect while cursor is in menu or placing something to prevent confusion
        if (!cusorInMenu && previewObject == null && Input.GetMouseButtonDown(0))
        {
            HandleOrbCollection();
        }

        if (startedDoomTimer)
        {
            DoomsDayCountDown();
        }

        HandleCameraMovement();
        HandleZoom();
        HandleObjectPlacement();
        UpdatePreview();

        HandleSpiritFollowing();
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

        // Calculate dynamic movement speed based on zoom level
        float currentZoomFactor = gameCamera.orthographicSize / maxZoom;
        float dynamicMoveSpeed = moveSpeed * (1f + (1f - currentZoomFactor) * 2f); // Increase speed when zoomed in

        Vector3 movement = new Vector3(horizontalInput, 0, verticalInput) * dynamicMoveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;

        // Calculate dynamic boundaries based on zoom level
        float zoomBoundaryFactor = 1f + (1f - currentZoomFactor) * 0.5f; // Expand boundaries when zoomed in
        float dynamicMinX = minX * zoomBoundaryFactor;
        float dynamicMaxX = maxX * zoomBoundaryFactor;
        float dynamicMinZ = minZ * zoomBoundaryFactor;
        float dynamicMaxZ = maxZ * zoomBoundaryFactor;

        newPosition.x = Mathf.Clamp(newPosition.x, dynamicMinX, dynamicMaxX);
        newPosition.z = Mathf.Clamp(newPosition.z, dynamicMinZ, dynamicMaxZ);

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
                position.y = 15.5f;
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
        UI.SetDescriptionText(selectedItem);
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
        UI.ClearDescriptionText();
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

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(position, out hit, 5, NavMesh.AllAreas))
        {
            Debug.Log("Not on NavMesh");
            return false;
        }

        return true;
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
    public void UpdateElementalSpiritCount(SpiritStats spirit, int count)
    {
        Debug.Log($"Updating Spirit Count {count} {spirit.spiritData.spiritName}");

        if (spirit.spiritData.spiritName.Contains("Nature"))
        {
            natureElementals += count;
            natureElementalsText.text = natureElementals.ToString();
        }
        else if (spirit.spiritData.spiritName.Contains("Fire"))
        {
            fireElementals += count;
            fireElementalsText.text = fireElementals.ToString();
        }
        else if (spirit.spiritData.spiritName.Contains("Water"))
        {
            waterElementals += count;
            waterElementalsText.text = waterElementals.ToString();
        }

        if(!startedDoomTimer)
        {
            if(natureElementals <= 0 || fireElementals <= 0 || waterElementals <= 0)
            {
                Debug.Log("Starting Dooms Day");
                startedDoomTimer = true;
                UI.ToggleDoomBanner(true);
            }
        }
        else if (startedDoomTimer)
        {
            if(natureElementals > 0 && fireElementals > 0 && waterElementals > 0)
            {
                startedDoomTimer = false;
                UI.ToggleDoomBanner(false);

                doomsDayTimer = 60;
            }
        }
    }
    private void DoomsDayCountDown()
    {
        doomsDayTimer -= Time.deltaTime;
        UI.ShowDoomCountDown(doomsDayTimer);
        // Load Fail Scene

        if (doomsDayTimer <= 0)
            SceneManager.LoadScene(3);
    }
    public void FindSpiritOfType(string type)
    {
        // Find all SpiritStats components in the scene
        SpiritStats[] spirits = FindObjectsOfType<SpiritStats>();

        // Filter spirits by the specified type
        List<SpiritStats> matchingSpirits = spirits.Where(spirit =>
            spirit.spiritData.spiritName.Contains(type)).ToList();

        // If no spirits of the specified type are found, exit the method
        if (matchingSpirits.Count == 0)
        {
            Debug.Log($"No {type} spirits found in the scene.");
            return;
        }

        // Select a random spirit from the matching spirits
        SpiritStats randomSpirit = matchingSpirits[Random.Range(0, matchingSpirits.Count)];

        // Calculate dynamic boundaries based on zoom level
        float currentZoomFactor = gameCamera.orthographicSize / maxZoom;
        float zoomBoundaryFactor = 1f + (1f - currentZoomFactor) * 0.5f;

        // Calculate dynamic boundaries
        float dynamicMinX = minX * zoomBoundaryFactor;
        float dynamicMaxX = maxX * zoomBoundaryFactor;
        float dynamicMinZ = minZ * zoomBoundaryFactor;
        float dynamicMaxZ = maxZ * zoomBoundaryFactor;

        if (randomSpirit != null)
        {
            // Reset zoom to max when finding a new spirit
            gameCamera.orthographicSize = 10;

            // Adjust for isometric offset - you may need to fine-tune these values
            float xOffset = -2f;  // Adjust this to move left/right
            float zOffset = 2f;   // Adjust this to move forward/back

            // Move camera to spirit's position with offsets
            Vector3 newPosition = new Vector3(
                Mathf.Clamp(randomSpirit.transform.position.x + xOffset, dynamicMinX, dynamicMaxX),
                transform.position.y,
                Mathf.Clamp(randomSpirit.transform.position.z + zOffset, dynamicMinZ, dynamicMaxZ)
            );

            transform.position = newPosition;

            // Set spirit following mode
            currentFollowedSpirit = randomSpirit;
            isFollowingSpirit = true;

            Debug.Log($"Found and focused on a {type} spirit!");
        }
    }
    void HandleSpiritFollowing()
    {
        // Check if we're following a spirit
        if (isFollowingSpirit && currentFollowedSpirit != null)
        {
            // Check for player input that should stop following
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // If player provides any movement input, stop following
            if (Mathf.Abs(horizontalInput) > 0.1f ||
                Mathf.Abs(verticalInput) > 0.1f )
            {
                isFollowingSpirit = false;
                currentFollowedSpirit = null;
                return;
            }

            // Calculate dynamic boundaries based on zoom level
            float currentZoomFactor = gameCamera.orthographicSize / maxZoom;
            float zoomBoundaryFactor = 1f + (1f - currentZoomFactor) * 0.5f;

            // Calculate dynamic boundaries
            float dynamicMinX = minX * zoomBoundaryFactor;
            float dynamicMaxX = maxX * zoomBoundaryFactor;
            float dynamicMinZ = minZ * zoomBoundaryFactor;
            float dynamicMaxZ = maxZ * zoomBoundaryFactor;

            // Adjust for isometric offset - you may need to fine-tune these values
            float xOffset = 20f;  // Adjust this to move left/right
            float zOffset = 2f;   // Adjust this to move forward/back

            // If still following, update camera position to center the spirit
            Vector3 spiritPosition = currentFollowedSpirit.transform.position;
            Vector3 newPosition = new Vector3(
                Mathf.Clamp(spiritPosition.x + xOffset, dynamicMinX, dynamicMaxX),
                transform.position.y,
                Mathf.Clamp(spiritPosition.z + zOffset, dynamicMinZ, dynamicMaxZ)
            );

            transform.position = newPosition;
        }
    }

}