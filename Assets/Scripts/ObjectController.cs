using UnityEngine;
using System.Collections;

public class ObjectController : MonoBehaviour
{
    [Header("Touch Settings")]
    [Tooltip("How fast the object rotates with touch")]
    public float rotationSpeed = 0.3f;
    [Tooltip("How fast the object scales when pinching")]
    public float zoomSpeed = 0.02f;
    [Tooltip("How fast the object moves when panning")]
    public float moveSpeed = 0.02f;

    [Header("Feature Toggles")]
    public bool enableRotation = true;
    public bool enableZoom = true;
    public bool enablePan = true;
    public bool enableDoubleTapReset = true;
    public bool enableModelSwapping = true;

    [Header("Scale Limits")]
    [Tooltip("Minimum scale allowed")]
    public float minScale = 0.1f;
    [Tooltip("Maximum scale allowed")]
    public float maxScale = 3.0f;

    [Header("Model Swapping")]
    [Tooltip("The primary model (default visible)")]
    public GameObject primaryModel;
    [Tooltip("The secondary model (shown when holding)")]
    public GameObject secondaryModel;
    public float holdThreshold = 0.8f;

    // Private variables for state tracking
    private Vector3 initialLocalScale;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Transform cachedTransform;

    // Touch tracking
    private float lastTapTime;
    private float touchStartTime;
    private bool isTouchHolding = false;
    private bool isShowingSecondaryModel = false;
    private const float doubleTapTimeThreshold = 0.3f;

    // Transition animation
    private Coroutine transitionCoroutine;

    void Awake()
    {
        cachedTransform = transform;

        // Make sure model references are valid
        if (primaryModel == null)
            primaryModel = gameObject;

        if (secondaryModel != null)
            secondaryModel.SetActive(false);
    }

    void Start()
    {
        // Store initial transform values for reset functionality
        SaveInitialState();
    }

    private void SaveInitialState()
    {
        initialLocalScale = cachedTransform.localScale;
        initialLocalPosition = cachedTransform.localPosition;
        initialLocalRotation = cachedTransform.localRotation;
    }

    void Update()
    {
        // Handle touch input
        HandleTouchInput();
    }
    private void HandleTouchInput()
    {
        // No touches, end hold state if active
        if (Input.touchCount == 0)
        {
            if (isTouchHolding)
            {
                isTouchHolding = false;

                // If we're showing secondary model and touch was released, switch back
                if (isShowingSecondaryModel && enableModelSwapping)
                {
                    SwapToModel(true); // Switch back to primary
                }
            }
            return;
        }

        // Single touch - Rotation, Double-tap Reset, or Hold for model swap
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Touch began - potential start of hold or double tap
            if (touch.phase == TouchPhase.Began)
            {
                touchStartTime = Time.time;

                // Check for double tap reset
                if (enableDoubleTapReset && Time.time - lastTapTime < doubleTapTimeThreshold)
                {
                    ResetObject();
                    lastTapTime = 0; // Prevent triple-tap issues
                    return;
                }

                lastTapTime = Time.time;
                isTouchHolding = true;
            }
            // Check for hold gesture to swap models
            else if (touch.phase == TouchPhase.Stationary && enableModelSwapping)
            {
                if (isTouchHolding && !isShowingSecondaryModel &&
                    Time.time - touchStartTime >= holdThreshold)
                {
                    // Held long enough, swap to secondary model
                    SwapToModel(false);
                }
            }
            // Touch moving - handle rotation
            else if (touch.phase == TouchPhase.Moved)
            {
                // If moving significantly, cancel hold for model swap
                if (touch.deltaPosition.magnitude > 5f)
                {
                    isTouchHolding = false;
                }

                // Handle rotation
                if (enableRotation)
                {
                    float rotX = touch.deltaPosition.x * rotationSpeed * Time.deltaTime * 50f;
                    float rotY = touch.deltaPosition.y * rotationSpeed * 0.5f * Time.deltaTime * 50f;

                    cachedTransform.Rotate(Vector3.up, -rotX, Space.World);
                    cachedTransform.Rotate(Vector3.right, rotY, Space.World);
                }
            }
            // Touch ended
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouchHolding = false;

                // If we're showing secondary model and touch ended, switch back
                if (isShowingSecondaryModel && enableModelSwapping)
                {
                    SwapToModel(true); // Switch back to primary
                }
            }
        }
        // Two finger touch - Zoom and Pan
        else if (Input.touchCount == 2)
        {
            isTouchHolding = false; // Cancel hold when second finger is down

            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Previous touch positions
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Zoom handling
            if (enableZoom)
            {
                // Find the magnitude (distance) between touches in each frame
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Calculate the difference in distances
                float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

                // Scale more naturally based on current scale
                float scaleFactor = deltaMagnitudeDiff * zoomSpeed * Time.deltaTime * 50f;
                Vector3 newScale = cachedTransform.localScale * (1 + scaleFactor);

                // Check if any dimension would exceed limits
                float maxDimension = Mathf.Max(newScale.x, newScale.y, newScale.z);
                float minDimension = Mathf.Min(newScale.x, newScale.y, newScale.z);

                if (minDimension >= minScale && maxDimension <= maxScale)
                {
                    cachedTransform.localScale = newScale;
                }
                else
                {
                    // Apply clamping while preserving proportions
                    if (maxDimension > maxScale)
                    {
                        float ratio = maxScale / maxDimension;
                        cachedTransform.localScale = newScale * ratio;
                    }
                    else if (minDimension < minScale)
                    {
                        float ratio = minScale / minDimension;
                        cachedTransform.localScale = newScale * ratio;
                    }
                }
            }

            // Pan handling
            if (enablePan && (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved))
            {
                // Find the midpoint of the two touches
                Vector2 curMidPoint = (touchZero.position + touchOne.position) / 2;
                Vector2 prevMidPoint = (touchZeroPrevPos + touchOnePrevPos) / 2;

                // Calculate the movement delta
                Vector2 moveDelta = curMidPoint - prevMidPoint;

                // Try to use camera for better pan direction
                Camera cam = Camera.main;
                if (cam != null)
                {
                    // Apply movement considering camera orientation
                    Vector3 moveVector = (cam.transform.right * moveDelta.x + cam.transform.up * moveDelta.y)
                        * moveSpeed * Time.deltaTime * 50f;
                    cachedTransform.position += moveVector;
                }
                else
                {
                    // Fallback to standard top-down style movement
                    Vector3 move = new Vector3(moveDelta.x * moveSpeed, 0, moveDelta.y * moveSpeed)
                        * Time.deltaTime * 50f;
                    cachedTransform.Translate(move, Space.World);
                }
            }
        }
    }
    private void SwapToModel(bool showPrimary)
    {
        // Validate we have both models set up
        if (primaryModel == null || secondaryModel == null)
            return;

        // Stop any ongoing transition
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        // Start new transition
        transitionCoroutine = StartCoroutine(TransitionBetweenModels(showPrimary));

        // Update state tracking
        isShowingSecondaryModel = !showPrimary;
    }

    private IEnumerator TransitionBetweenModels(bool showPrimary)
    {
        // Swap models
        primaryModel.SetActive(showPrimary);
        secondaryModel.SetActive(!showPrimary);

        yield return new WaitForSeconds(0.1f); // Short delay for effect

        transitionCoroutine = null;
    }
    public void ResetObject()
    {
        // Reset transform
        cachedTransform.localPosition = initialLocalPosition;
        cachedTransform.localRotation = initialLocalRotation;
        cachedTransform.localScale = initialLocalScale;

        // If showing secondary model, switch back to primary
        if (isShowingSecondaryModel)
        {
            SwapToModel(true);
        }
        Debug.Log("Object reset to initial state");
    }


    /// Public method to update the saved initial state
    public void UpdateInitialState()
    {
        SaveInitialState();
    }
}