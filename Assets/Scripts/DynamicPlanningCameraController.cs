using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DynamicPlanningCameraController : MonoBehaviour, ILevelPhaseListener
{
    private const string RuntimeObjectName = "DynamicPlanningCameraController";

    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private PlayerDuckController playerDuck;
    [SerializeField] private PlaceableInventoryPanel inventoryPanel;
    [SerializeField] private BuildModePlacementController placementController;
    [SerializeField] private float planningOrthographicScale = 0.6f;
    [SerializeField] private float edgeBandPixels = 64f;
    [SerializeField] private float panViewportHeightsPerSecond = 0.5f;
    [SerializeField] private float executionZoomOutDurationSeconds = 0.6f;
    [SerializeField] private float inventoryPlanningInsetPixels = 64f;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private Vector3 fullCameraPosition;
    private float fullOrthographicSize;
    private bool configured;
    private LevelPhase currentPhase = LevelPhase.Planning;
    private Coroutine executionZoomCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForLoadedScene()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreateForActiveScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForActiveScene();
    }

    private static void TryCreateForActiveScene()
    {
        GameStateManager manager = FindFirstObjectByType<GameStateManager>();

        if (manager == null
            || manager.CurrentLevelDefinition == null
            || !manager.CurrentLevelDefinition.UseDynamicPlanningCamera)
        {
            return;
        }

        DynamicPlanningCameraController existing = FindFirstObjectByType<DynamicPlanningCameraController>();

        if (existing != null)
        {
            existing.Initialize(manager);
            return;
        }

        GameObject controllerObject = new GameObject(RuntimeObjectName);
        DynamicPlanningCameraController controller = controllerObject.AddComponent<DynamicPlanningCameraController>();
        controller.Initialize(manager);
    }

    public void Initialize(GameStateManager manager)
    {
        gameStateManager = manager;
    }

    private void Start()
    {
        if (!EnsureConfigured())
        {
            enabled = false;
            return;
        }

        OnLevelPhaseChanged(gameStateManager.CurrentPhase);
    }

    private void Update()
    {
        if (currentPhase != LevelPhase.Planning || !EnsureConfigured() || IsPlacementInteractionActive())
        {
            return;
        }

        Vector2 inputDirection = ResolveInputDirection();

        if (inputDirection == Vector2.zero)
        {
            return;
        }

        float panDistance = DynamicPlanningCameraMath.CalculateViewportRelativePanDistance(
            worldCamera.orthographicSize,
            panViewportHeightsPerSecond,
            Time.deltaTime);
        Vector2 desiredCenter = (Vector2)worldCamera.transform.position + inputDirection * panDistance;
        SetCameraCenter(ClampToFullView(desiredCenter, worldCamera.orthographicSize));
    }

    private void OnDisable()
    {
        SetInventoryPlanningInset(false);
    }

    private void OnDestroy()
    {
        SetInventoryPlanningInset(false);
    }

    public void OnLevelPhaseChanged(LevelPhase phase)
    {
        currentPhase = phase;

        if (!EnsureConfigured())
        {
            return;
        }

        if (phase == LevelPhase.Planning)
        {
            EnterPlanningView();
            return;
        }

        if (phase == LevelPhase.Execution)
        {
            EnterExecutionView();
        }
    }

    private bool EnsureConfigured()
    {
        if (configured)
        {
            return true;
        }

        ResolveSceneReferences();

        if (worldCamera == null
            || !worldCamera.orthographic
            || gameStateManager == null
            || gameStateManager.CurrentLevelDefinition == null
            || !gameStateManager.CurrentLevelDefinition.UseDynamicPlanningCamera)
        {
            return false;
        }

        CaptureFullCameraPose();
        configured = true;
        return true;
    }

    private void ResolveSceneReferences()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance != null
                ? GameStateManager.Instance
                : FindFirstObjectByType<GameStateManager>();
        }

        if (playerDuck == null)
        {
            playerDuck = FindFirstObjectByType<PlayerDuckController>();
        }

        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<PlaceableInventoryPanel>();
        }

        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }
    }

    private void CaptureFullCameraPose()
    {
        fullCameraPosition = worldCamera.transform.position;
        fullOrthographicSize = worldCamera.orthographicSize;
    }

    private void EnterPlanningView()
    {
        StopExecutionZoom();
        ResolveSceneReferences();

        worldCamera.orthographicSize = DynamicPlanningCameraMath.CalculatePlanningOrthographicSize(
            fullOrthographicSize,
            planningOrthographicScale);

        Vector2 targetCenter = playerDuck != null
            ? playerDuck.transform.position
            : fullCameraPosition;

        SetCameraCenter(ClampToFullView(targetCenter, worldCamera.orthographicSize));
        SetInventoryPlanningInset(true);
    }

    private void EnterExecutionView()
    {
        SetInventoryPlanningInset(false);
        StopExecutionZoom();

        if (executionZoomOutDurationSeconds <= 0f)
        {
            ApplyFullView();
            return;
        }

        executionZoomCoroutine = StartCoroutine(AnimateToFullView());
    }

    private IEnumerator AnimateToFullView()
    {
        Vector3 startPosition = worldCamera.transform.position;
        float startSize = worldCamera.orthographicSize;
        float elapsedSeconds = 0f;

        while (elapsedSeconds < executionZoomOutDurationSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedSeconds / executionZoomOutDurationSeconds);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            worldCamera.transform.position = Vector3.Lerp(startPosition, fullCameraPosition, easedProgress);
            worldCamera.orthographicSize = Mathf.Lerp(startSize, fullOrthographicSize, easedProgress);
            yield return null;
        }

        ApplyFullView();
        executionZoomCoroutine = null;
    }

    private void StopExecutionZoom()
    {
        if (executionZoomCoroutine == null)
        {
            return;
        }

        StopCoroutine(executionZoomCoroutine);
        executionZoomCoroutine = null;
    }

    private void ApplyFullView()
    {
        worldCamera.transform.position = fullCameraPosition;
        worldCamera.orthographicSize = fullOrthographicSize;
    }

    private Vector2 ResolveInputDirection()
    {
        Vector2 direction = ResolveKeyboardDirection();
        Vector2 mouseDirection = ResolveMouseEdgeDirection();
        return DynamicPlanningCameraMath.NormalizeDirection(direction + mouseDirection);
    }

    private Vector2 ResolveKeyboardDirection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        bool down = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        bool up = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        return DynamicPlanningCameraMath.ResolveKeyboardDirection(left, right, down, up);
    }

    private Vector2 ResolveMouseEdgeDirection()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (IsPointerOverButton(screenPosition))
        {
            return Vector2.zero;
        }

        return DynamicPlanningCameraMath.ResolveEdgeScrollDirection(
            screenPosition,
            new Vector2(Screen.width, Screen.height),
            edgeBandPixels);
    }

    private bool IsPointerOverButton(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            Button button = uiRaycastResults[i].gameObject.GetComponentInParent<Button>();

            if (button != null && button.isActiveAndEnabled)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlacementInteractionActive()
    {
        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }

        return placementController != null && placementController.HasActivePlacementInteraction;
    }

    private Vector2 ClampToFullView(Vector2 desiredCenter, float orthographicSize)
    {
        Vector2 fullViewExtents = DynamicPlanningCameraMath.CalculateViewExtents(
            fullOrthographicSize,
            worldCamera.aspect);
        Vector2 planningViewExtents = DynamicPlanningCameraMath.CalculateViewExtents(
            orthographicSize,
            worldCamera.aspect);

        return DynamicPlanningCameraMath.ClampCameraCenter(
            desiredCenter,
            fullCameraPosition,
            fullViewExtents,
            planningViewExtents);
    }

    private void SetCameraCenter(Vector2 center)
    {
        worldCamera.transform.position = new Vector3(
            center.x,
            center.y,
            worldCamera.transform.position.z);
    }

    private void SetInventoryPlanningInset(bool active)
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<PlaceableInventoryPanel>();
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetDynamicPlanningCameraInset(active, inventoryPlanningInsetPixels);
        }
    }
}
