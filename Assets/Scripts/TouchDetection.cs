using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameActions;

public class TouchDetection : MonoBehaviour
{
    public const float k_trailBuffer = 0.01f;
    public const int k_tracePositionBuffer = 5;
    public const float k_directionSimilarityThreshold = 0.95f;
    public const float k_enclosingThreshold = 1;
    public const float k_circleRadiusVarianceThreshold = 900;

    private List<Vector2> m_touchPosList;
    private Vector2 m_initialDirection;
    private int m_directionChanges;
    private bool m_isTracingSameInitialDirection;
    private bool m_isInitiallyTracingUpwards;
    private bool m_hasTracedUpwards;
    private bool m_hasTracedDownwards;
    private bool m_hasTracedLeftwards;
    private bool m_hasTracedRightwards;
    private bool m_hasTracedDown;
    private bool m_hasTracedUp;
    private bool m_hasTracedLeft;
    private bool m_hasTracedRight;

    private bool m_isPollingTouch = false;

    [SerializeField] private GameObject m_trail;
    private TrailRenderer m_trailRenderer;

    private Coroutine m_trailDrawingCoroutine;

    private void Awake()
    {
        m_trailRenderer = m_trail.GetComponent<TrailRenderer>();
        m_trailRenderer.emitting = false;
    }

    private void Start()
    {
        m_touchPosList = new List<Vector2>();

        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        InputManager.Instance.OnStartTouch += InputManager_StartTouch;
        InputManager.Instance.OnEndTouch += InputManager_EndTouch;

        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        InputManager.Instance.OnStartTouch -= InputManager_StartTouch;
        InputManager.Instance.OnEndTouch -= InputManager_EndTouch;

        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Disable();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        foreach(UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                DetectTouch();
            }
        }
    }

    private void GameManager_DisableActionsToBePlayed()
    {
        Debug.LogFormat("Actions done, must figure out trace {0}", m_trailDrawingCoroutine != null);
        if (m_trailDrawingCoroutine == null) return;

        StopCoroutine(m_trailDrawingCoroutine);
        m_trailDrawingCoroutine = null;
        m_trailRenderer.emitting = false;
        m_trailRenderer.Clear();

        DetectTouch();
    }

    private void InputManager_StartTouch(Vector2 position)
    {
        m_touchPosList.Clear();
        //Debug.LogFormat("Start {0}", position);
        //m_touchPosList.Add(position);
        m_initialDirection = Vector2.zero;
        m_directionChanges = 0;
        m_isTracingSameInitialDirection = true;
        m_isInitiallyTracingUpwards = true;
        m_hasTracedUpwards = false;
        m_hasTracedDownwards = false;
        m_hasTracedLeftwards = false;
        m_hasTracedRightwards = false;
        m_hasTracedDown = false;
        m_hasTracedUp = false;
        m_hasTracedLeft = false;
        m_hasTracedRight = false;

        if (m_trailDrawingCoroutine != null)
        {
            StopCoroutine(m_trailDrawingCoroutine);
            m_trailDrawingCoroutine = null;
        }
        m_trailDrawingCoroutine = StartCoroutine(DrawTrail());
    }

    private void InputManager_EndTouch(Vector2 position)
    {
        if (m_trailDrawingCoroutine == null) return;

        StopCoroutine(m_trailDrawingCoroutine);
        m_trailDrawingCoroutine = null;
        m_trailRenderer.emitting = false;
        m_trailRenderer.Clear();

        //m_endPos = position;
        //m_touchPosList.Add(position);
        DetectTouch();
    }

    private void DetectTouch()
    {
        //Debug.LogFormat("is same direction: {0}, is initially up: {1}, is down at some point: {2}", m_isTracingSameInitialDirection, m_isInitiallyTracingUpwards, m_isTracedDownwards);
        if (m_touchPosList.Count < 2)
        {
            return;
        }

        Vector2 traceDirection = m_touchPosList.Last() - m_touchPosList.First();

        bool isEnclosed = traceDirection.magnitude <= k_enclosingThreshold;
        bool hasTracedAllDirections = m_hasTracedUp && m_hasTracedDown && m_hasTracedLeft && m_hasTracedRight;
        bool hasTracedTriangle = (m_hasTracedLeft ^ m_hasTracedRight) && m_hasTracedUpwards && m_hasTracedDownwards;

        if (m_isTracingSameInitialDirection)
        {
            Vector2 traceDirectionNorm = traceDirection.normalized;
            float horizontalSimilarity = Mathf.Abs(Vector2.Dot(Vector2.right, traceDirectionNorm));
            //Debug.LogFormat("horizontal {0}", horizontalSimilarity);
            if (horizontalSimilarity >= k_directionSimilarityThreshold)
            {
                Debug.Log("Horizontal trace");
                ActionManager.Instance.EnqueueAction(GameAction.Water);
            }
        }
        else if (hasTracedAllDirections && isEnclosed)
        {
            Debug.Log("Circular trace");
            ActionManager.Instance.EnqueueAction(GameAction.Egg);
        }
        else if (hasTracedTriangle && isEnclosed)
        {
            Debug.Log("Triangular trace");
            ActionManager.Instance.EnqueueAction(GameAction.Reflect);
        }
        else if (m_isInitiallyTracingUpwards && m_hasTracedDownwards)
        {
            Debug.Log("Up-down trace");
            ActionManager.Instance.EnqueueAction(GameAction.Fire);
        }
    }

    private IEnumerator DrawTrail()
    {
        float time = 0;
        while (true)
        {
            if (time > k_trailBuffer && !m_trailRenderer.emitting)
            {
                m_trailRenderer.Clear();
                m_trailRenderer.emitting = true;
            }
            else
            {
                time += Time.deltaTime;
                yield return null;
            }

            Vector2 touchPos = InputManager.Instance.GetPrimaryPosition();
            m_trail.transform.position = touchPos;
            m_touchPosList.Add(touchPos);

            Vector2 currentDirection = Vector2.zero;

            if (m_touchPosList.Count >= k_tracePositionBuffer)
            {
                if (m_initialDirection == Vector2.zero)
                {
                    m_initialDirection = (m_touchPosList.Last() - m_touchPosList.First()).normalized;
                    if (m_initialDirection != Vector2.zero)
                    {
                        m_isInitiallyTracingUpwards = IsTracingUpwards(m_initialDirection);
                    }
                }
                else
                {
                    currentDirection = touchPos - m_touchPosList[^k_tracePositionBuffer];
                }
            }

            if (currentDirection != Vector2.zero)
            {
                if (m_isTracingSameInitialDirection)
                {
                    bool sameDirection = IsSameDirection(m_initialDirection, currentDirection, k_directionSimilarityThreshold);
                    if (!sameDirection)
                    {
                        m_directionChanges++;
                    }
                    m_isTracingSameInitialDirection = sameDirection;
                }

                if (!m_hasTracedUpwards)
                {
                    m_hasTracedUpwards = IsTracingUpwards(currentDirection);
                    if (m_hasTracedUpwards)
                    {
                        Debug.Log("traced upwards");
                    }
                }

                if (!m_hasTracedDownwards)
                {
                    m_hasTracedDownwards = IsTracingDownwards(currentDirection);
                    if (m_hasTracedDownwards)
                    {
                        Debug.Log("traced downwards");
                    }
                }

                if (!m_hasTracedLeftwards)
                {
                    m_hasTracedLeftwards = IsTracingLeftwards(currentDirection);
                    if (m_hasTracedLeftwards)
                    {
                        Debug.Log("traced leftwards");
                    }
                }

                if (!m_hasTracedRightwards)
                {
                    m_hasTracedRightwards = IsTracingRightwards(currentDirection);
                    if (m_hasTracedRightwards)
                    {
                        Debug.Log("traced rightwards");
                    }
                }

                if (!m_hasTracedUp)
                {
                    m_hasTracedUp = IsTracingUp(currentDirection);
                    if (m_hasTracedUp)
                    {
                        Debug.Log("traced up");
                    }
                }

                if (!m_hasTracedDown)
                {
                    m_hasTracedDown = IsTracingDown(currentDirection);
                    if (m_hasTracedDown)
                    {
                        Debug.Log("traced down");
                    }
                }

                if (!m_hasTracedLeft)
                {
                    m_hasTracedLeft = IsTracingLeft(currentDirection);
                    if (m_hasTracedLeft)
                    {
                        Debug.Log("traced left");
                    }
                }

                if (!m_hasTracedRight)
                {
                    m_hasTracedRight = IsTracingRight(currentDirection);
                    if (m_hasTracedRight)
                    {
                        Debug.Log("traced right");
                    }
                }
            }

            yield return null;
        }
    }

    private bool IsTracingUpwards(Vector2 path)
    {
        return IsSameDirection(Vector2.up, path, 0);
    }

    private bool IsTracingDownwards(Vector2 path)
    {
        return IsSameDirection(Vector2.down, path, 0);
    }
    private bool IsTracingLeftwards(Vector2 path)
    {
        return IsSameDirection(Vector2.left, path, 0);
    }

    private bool IsTracingRightwards(Vector2 path)
    {
        return IsSameDirection(Vector2.right, path, 0);
    }

    private bool IsTracingUp(Vector2 path)
    {
        return IsSameDirection(Vector2.up, path, k_directionSimilarityThreshold);
    }

    private bool IsTracingDown(Vector2 path)
    {
        return IsSameDirection(Vector2.down, path, k_directionSimilarityThreshold);
    }

    private bool IsTracingLeft(Vector2 path)
    {
        return IsSameDirection(Vector2.left, path, k_directionSimilarityThreshold);
    }

    private bool IsTracingRight(Vector2 path)
    {
        return IsSameDirection(Vector2.right, path, k_directionSimilarityThreshold);
    }

    private bool IsSameDirection(Vector2 direction, Vector2 otherDirection, float threshold)
    {
        if (direction == Vector2.zero || otherDirection == Vector2.zero) { return false; }

        float similarity = Vector2.Dot(direction.normalized, otherDirection.normalized);
        return similarity > threshold;
    }

    //private bool IsCircleDrawn()
    //{
    //    Vector2 firstPos = m_touchPosList.First();
    //    Vector2 lastPos = m_touchPosList.Last();
    //    Vector2 firstLastDiff = firstPos - lastPos;
    //    if (firstLastDiff.magnitude > k_enclosingThreshold)
    //    {
    //        return false;
    //    }

    //    float xCenter = m_touchPosList.Select(pos => pos.x).Average();
    //    float yCenter = m_touchPosList.Select(pos => pos.y).Average();
    //    Vector2 centre = new(xCenter, yCenter);
    //    List<Vector2> radii = new();
    //    foreach (Vector2 pos in m_touchPosList)
    //    {
    //        radii.Add(pos - centre);
    //    }
    //    IEnumerable<float> magnitudes = radii.Select(pos => pos.magnitude);
    //    float avgMagnitude = magnitudes.Average();
    //    float variance = (magnitudes.Average(magnitude => Mathf.Pow(magnitude - avgMagnitude, 2)));
    //    Debug.LogFormat("magnitude: {0}\nvariance: {1}", avgMagnitude, variance);

    //    return variance <= k_circleRadiusVarianceThreshold;
    //}
}
