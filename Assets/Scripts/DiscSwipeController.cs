using System;
using System.Collections;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

public class DiscSwipeController : MonoBehaviour
{
    public float moveSpeed = 5f; 
    private Vector2 swipeStart;
    private bool isSwiping = false;
    private GameObject selectedDisc = null; 
    [SerializeField] private GameObject puff;
    [SerializeField] private Camera mainCamera;
    [SerializeField] Canvas gameCanvas;
    [SerializeField] private AudioSource jumpSound;
    [SerializeField] AudioSource popSound;

    private int totalDiscs; 
    private int remainingDiscs;
    
    public UIManager uiManager;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        totalDiscs = GameObject.FindGameObjectsWithTag("Disc").Length;
        remainingDiscs = totalDiscs;
    }

    private void Update()
    {
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;

            // Check if the starting position is a disc
            Ray ray = Camera.main.ScreenPointToRay(swipeStart);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Disc"))
                {
                    selectedDisc = hit.collider.gameObject;
                    Debug.Log($"Disc selected at start: {selectedDisc.name}");
                }
                else
                {
                    Debug.Log("No disc detected at start.");
                    isSwiping = false; 
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 swipeEnd = Input.mousePosition;
            Vector2 swipeDirection = swipeEnd - swipeStart;

            if (swipeDirection.magnitude > 0.1f && selectedDisc != null) 
            {
                swipeDirection.Normalize();
                DetermineMoveDirection(swipeDirection);
            }
            else
            {
                Debug.Log("Swipe too short or no disc selected, not registering.");
            }

            isSwiping = false;
            selectedDisc = null; // Reset selectedDisc after the swipe
        }
    }

    private void DetermineMoveDirection(Vector2 direction)
    {
        Vector3 moveDirection;

        // Determine cardinal direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            moveDirection = direction.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            moveDirection = direction.y > 0 ? Vector3.forward : Vector3.back;
        }

        Debug.Log($"Swipe detected. Moving direction: {moveDirection}");

        if (selectedDisc != null)
        {
            StartCoroutine(MoveDisc(selectedDisc, moveDirection));
        }
        else
        {
            Debug.Log("No disc selected to move.");
        }
    }

    private System.Collections.IEnumerator MoveDisc(GameObject disc, Vector3 direction)
    {
        Vector3 startScale = disc.transform.localScale;
        Vector3 enlargedScale = startScale * 1.2f;

        while (true)
        {
            Vector3 nextPosition = disc.transform.position + direction;
            Collider[] colliders = Physics.OverlapSphere(nextPosition, 0.5f);
            bool canMove = false;
            bool isHole = false;
            GameObject targetBlock = null;
            GameObject targetHole = null;

            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Block") && collider.transform.childCount == 0)
                {
                    canMove = true;
                    targetBlock = collider.gameObject;
                    break;
                }
                else if (collider.CompareTag("Block") && collider.transform.childCount > 0)
                {
                    Debug.Log("Obstacle detected: Stopping movement.");
                    //CreatePuff(); // Create puff when blocked
                }
                else if (collider.CompareTag("Hole"))
                {
                    isHole = true;
                    targetHole = collider.gameObject;
                    break;
                }
            }

            // Handle holes
            if (isHole)
            {
                string discName = disc.name;
                string holeName = targetHole.name;

                if (!discName.Equals(holeName.Replace("Hole", "Disc")))
                {
                    disc.transform.position = targetHole.transform.position + new Vector3(0, 0.5f, 0) - direction;
                    //CreatePuff();
                    Debug.Log($"Disc {disc.name} stopped behind hole: {targetHole.name}");
                    yield break;
                }
                else
                {
                    Debug.Log($"Disc {disc.name} moving to hole: {targetHole.name}");

                    // Smoothly move to the hole
                    float moveDuration = 0.3f; // Duration for movement
                    float elapsedTime = 0f;

                    Vector3 initialPosition = disc.transform.position;
                    Vector3 holePosition = targetHole.transform.position + new Vector3(0, 0.5f, 0);

                    while (elapsedTime < moveDuration)
                    {
                        float t = elapsedTime / moveDuration;
                        t = t * t * (3f - 2f * t); // Smoothstep easing
                        disc.transform.position = Vector3.Lerp(initialPosition, holePosition, t);

                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    disc.transform.position = holePosition;
                    Debug.Log($"Disc {disc.name} destroyed in hole: {targetHole.name}");
                    Destroy(disc);
                    popSound.Play();
                    CreatePuff(targetHole);

                    remainingDiscs--;

                    if (remainingDiscs == 0)
                    {
                        uiManager.TriggerGameWon();
                    }

                    yield break;
                }
            }

            if (!canMove)
            {
                Debug.Log("No valid block to move to. Stopping.");
                yield break;
            }
            CreateTrailEffect(disc);
            float moveTime = 0.2f; // Duration for each block movement
            float elapsedBlockTime = 0f;

            Vector3 startPosition = disc.transform.position;

            // Scale up for juiciness
            disc.transform.localScale = enlargedScale;

            while (elapsedBlockTime < moveTime)
            {
                float t = elapsedBlockTime / moveTime;
                t = t * t * (3f - 2f * t); // Smoothstep easing
                disc.transform.position = Vector3.Lerp(startPosition, nextPosition, t);

                elapsedBlockTime += Time.deltaTime;
                yield return null;
            }

            disc.transform.position = nextPosition;
            disc.transform.localScale = startScale;
            jumpSound.Play();

            if (targetBlock != null)
            {
                disc.transform.SetParent(targetBlock.transform);
                Vector3 customPosition = targetBlock.transform.position + new Vector3(0, 0.5f, 0);
                disc.transform.position = customPosition;

                Debug.Log($"Disc {disc.name} positioned at custom position: {disc.transform.position}");
            }
        }
    }

    private void CreateTrailEffect(GameObject disc)
    {
        TrailRenderer trail = disc.GetComponent<TrailRenderer>();
        if (!trail)
        {
            trail = disc.AddComponent<TrailRenderer>();
            trail.time = 1f; // Duration of the trail
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);
        }
    }

    private void CreatePuff(GameObject holeObject)
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(holeObject.transform.position);
        GameObject puffObject = Instantiate(puff, gameCanvas.transform);
        RectTransform rectTransform = puffObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Convert screen position to Canvas space
            rectTransform.anchoredPosition = ScreenToCanvasPosition(screenPosition, gameCanvas);
            rectTransform.localScale = Vector3.one; // Ensure correct scale
        }

        Destroy(puffObject, 1f);
    }
    private Vector2 ScreenToCanvasPosition(Vector3 screenPosition, Canvas canvas)
    {
        Vector2 canvasPosition;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Convert screen position to Canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPosition, 
            canvas.worldCamera, 
            out canvasPosition
        );

        return canvasPosition;
    }
}