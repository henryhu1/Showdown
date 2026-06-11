using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameActions;

public class TouchDetection : MonoBehaviour
{
    public const float k_trailBuffer = 0.01f;
    public const float k_trailLerpT = 0.5f;
    public const float k_trailInterpolationSpacing = 0.03f;
    public const int k_trailInterpolationStepsMax = 24;

    private const int k_minSamplesForShape = 8;
    private const float k_dedupeEpsilonScale = 0.008f;
    private const float k_closePathScale = 0.22f;
    private const float k_swipeStraightness = 0.84f;
    private const float k_swipeMinLengthScale = 0.14f;
    private const float k_swipeAxisDominance = 2.0f;
    private const float k_swipeBboxAspect = 1.65f;
    private const float k_rdpEpsilonScale = 0.045f;
    private const float k_iqEggMin = 0.68f;
    private const float k_iqTriangleMax = 0.76f;
    private const float k_iqTriangleMin = 0.34f;
    private const int k_triangleSimplifiedVertsMax = 6;

    private List<Vector2> m_touchPosList;

    [SerializeField] private GameObject m_trail;
    private TrailRenderer m_trailRenderer;

    private Coroutine m_trailDrawingCoroutine;

    private Vector2 m_trailSmoothedPosition;
    private Vector2 m_lastTrailDrawPosition;
    private bool m_trailFollowInitialized;

    private void Awake()
    {
        m_trailRenderer = m_trail.GetComponent<TrailRenderer>();
        m_trailRenderer.emitting = false;
    }

    private void Start()
    {
        m_touchPosList = new List<Vector2>();

        GameManager.Instance.OnBeforeActionSubmit += GameManager_BeforeActionSubmit;
        InputManager.Instance.OnStartTouch += InputManager_StartTouch;
        InputManager.Instance.OnEndTouch += InputManager_EndTouch;

        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnBeforeActionSubmit -= GameManager_BeforeActionSubmit;
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

    private void GameManager_BeforeActionSubmit()
    {
        Debug.LogFormat("Going to submit action, must figure out trace {0}", m_trailDrawingCoroutine != null);
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

        m_trailFollowInitialized = false;

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

        if (m_touchPosList.Count > 0)
            m_touchPosList.Add(position);

        DetectTouch();
    }

    private void DetectTouch()
    {
        if (m_touchPosList.Count < k_minSamplesForShape)
            return;

        List<Vector2> pts = DedupeConsecutive(m_touchPosList, k_dedupeEpsilonScale);
        if (pts.Count < k_minSamplesForShape)
            return;

        float scale = Mathf.Max(GetBoundingSize(pts), 1e-4f);
        bool isClosed = Vector2.Distance(pts[0], pts[pts.Count - 1]) <= k_closePathScale * scale;

        if (TryClassifyHorizontalSwipe(pts, scale, isClosed))
        {
            ActionManager.Instance.EnqueueAction(GameAction.Water);
            return;
        }

        if (TryClassifyVerticalSwipe(pts, scale, isClosed))
        {
            ActionManager.Instance.EnqueueAction(GameAction.Fire);
            return;
        }

        if (isClosed && TryClassifyCircleOrOval(pts, scale))
        {
            ActionManager.Instance.EnqueueAction(GameAction.Egg);
            return;
        }

        if (isClosed && TryClassifyRoughTriangle(pts, scale))
        {
            ActionManager.Instance.EnqueueAction(GameAction.Reflect);
            return;
        }
    }

    private static bool TryClassifyHorizontalSwipe(List<Vector2> pts, float scale, bool isClosed)
    {
        if (isClosed)
            return false;

        float pathLen = PolylineLength(pts, false);
        if (pathLen < k_swipeMinLengthScale * scale)
            return false;

        Vector2 net = pts[pts.Count - 1] - pts[0];
        float netLen = net.magnitude;
        if (netLen < k_swipeMinLengthScale * scale)
            return false;

        if (netLen / pathLen < k_swipeStraightness)
            return false;

        if (Mathf.Abs(net.x) < k_swipeAxisDominance * Mathf.Abs(net.y))
            return false;

        GetBoundingBox(pts, out Vector2 min, out Vector2 max);
        float w = max.x - min.x;
        float h = max.y - min.y;
        if (h < 1e-4f || w / h < k_swipeBboxAspect)
            return false;

        if (!IsMostlyColinearHorizontal(pts, min.y, max.y, scale))
            return false;

        return true;
    }

    private static bool TryClassifyVerticalSwipe(List<Vector2> pts, float scale, bool isClosed)
    {
        if (isClosed)
            return false;

        float pathLen = PolylineLength(pts, false);
        if (pathLen < k_swipeMinLengthScale * scale)
            return false;

        Vector2 net = pts[pts.Count - 1] - pts[0];
        float netLen = net.magnitude;
        if (netLen < k_swipeMinLengthScale * scale)
            return false;

        if (netLen / pathLen < k_swipeStraightness)
            return false;

        if (Mathf.Abs(net.y) < k_swipeAxisDominance * Mathf.Abs(net.x))
            return false;

        GetBoundingBox(pts, out Vector2 min, out Vector2 max);
        float w = max.x - min.x;
        float h = max.y - min.y;
        if (w < 1e-4f || h / w < k_swipeBboxAspect)
            return false;

        if (!IsMostlyColinearVertical(pts, min.x, max.x, scale))
            return false;

        return true;
    }

    private static bool TryClassifyCircleOrOval(List<Vector2> pts, float scale)
    {
        float perimeter = PolylineLength(pts, true);
        if (perimeter < 2.2f * scale)
            return false;

        float area = Mathf.Abs(ShoelaceAreaClosed(pts));
        float iq = IsoperimetricQuotient(area, perimeter);
        if (iq < k_iqEggMin)
            return false;

        float rdpEps = k_rdpEpsilonScale * scale;
        List<Vector2> simp = RamerDouglasPeucker(pts, rdpEps);
        if (simp.Count < 3)
            return false;

        if (simp.Count <= k_triangleSimplifiedVertsMax && iq < k_iqTriangleMax + 0.04f)
            return false;

        float axisRatio = PrincipalAxisRatio(pts);
        if (axisRatio > 14f)
            return false;

        return true;
    }

    private static bool TryClassifyRoughTriangle(List<Vector2> pts, float scale)
    {
        float perimeter = PolylineLength(pts, true);
        if (perimeter < 1.8f * scale)
            return false;

        float area = Mathf.Abs(ShoelaceAreaClosed(pts));
        float iq = IsoperimetricQuotient(area, perimeter);
        if (iq < k_iqTriangleMin || iq > k_iqTriangleMax)
            return false;

        float rdpEps = k_rdpEpsilonScale * scale;
        List<Vector2> simp = RamerDouglasPeucker(pts, rdpEps);
        if (simp.Count < 3 || simp.Count > k_triangleSimplifiedVertsMax)
            return false;

        return true;
    }

    private static List<Vector2> DedupeConsecutive(List<Vector2> src, float epsilonScale)
    {
        if (src.Count == 0)
            return new List<Vector2>();

        float eps = Mathf.Max(epsilonScale * GetBoundingSize(src), 1e-5f);
        var dst = new List<Vector2>(src.Count) { src[0] };
        for (int i = 1; i < src.Count; i++)
        {
            if (Vector2.Distance(src[i], dst[dst.Count - 1]) >= eps)
                dst.Add(src[i]);
        }
        return dst;
    }

    private static float GetBoundingSize(List<Vector2> pts)
    {
        GetBoundingBox(pts, out Vector2 min, out Vector2 max);
        return Mathf.Max(max.x - min.x, max.y - min.y);
    }

    private static void GetBoundingBox(List<Vector2> pts, out Vector2 min, out Vector2 max)
    {
        min = max = pts[0];
        for (int i = 1; i < pts.Count; i++)
        {
            Vector2 p = pts[i];
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
    }

    private static float PolylineLength(List<Vector2> pts, bool closeLoop)
    {
        float len = 0f;
        for (int i = 0; i < pts.Count - 1; i++)
            len += Vector2.Distance(pts[i], pts[i + 1]);
        if (closeLoop && pts.Count >= 2)
            len += Vector2.Distance(pts[pts.Count - 1], pts[0]);
        return len;
    }

    private static float ShoelaceAreaClosed(List<Vector2> pts)
    {
        double sum = 0.0;
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            sum += (double)pts[i].x * pts[j].y - (double)pts[j].x * pts[i].y;
        }
        return (float)(sum * 0.5);
    }

    private static float IsoperimetricQuotient(float area, float perimeter)
    {
        if (perimeter < 1e-5f || area < 1e-8f)
            return 0f;
        return (4f * Mathf.PI * area) / (perimeter * perimeter);
    }

    private static bool IsMostlyColinearHorizontal(List<Vector2> pts, float yMin, float yMax, float scale)
    {
        float tol = Mathf.Max((yMax - yMin) * 0.35f, scale * 0.04f);
        float yMid = 0.5f * (yMin + yMax);
        for (int i = 0; i < pts.Count; i++)
        {
            if (Mathf.Abs(pts[i].y - yMid) > tol)
                return false;
        }
        return true;
    }

    private static bool IsMostlyColinearVertical(List<Vector2> pts, float xMin, float xMax, float scale)
    {
        float tol = Mathf.Max((xMax - xMin) * 0.35f, scale * 0.04f);
        float xMid = 0.5f * (xMin + xMax);
        for (int i = 0; i < pts.Count; i++)
        {
            if (Mathf.Abs(pts[i].x - xMid) > tol)
                return false;
        }
        return true;
    }

    private static float PrincipalAxisRatio(List<Vector2> pts)
    {
        Vector2 c = Centroid(pts);
        float cxx = 0f, cyy = 0f, cxy = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            float dx = pts[i].x - c.x;
            float dy = pts[i].y - c.y;
            cxx += dx * dx;
            cyy += dy * dy;
            cxy += dx * dy;
        }
        int n = Mathf.Max(pts.Count, 1);
        cxx /= n;
        cyy /= n;
        cxy /= n;

        float trace = cxx + cyy;
        float det = cxx * cyy - cxy * cxy;
        float disc = Mathf.Sqrt(Mathf.Max(0f, trace * trace * 0.25f - det));
        float l1 = trace * 0.5f + disc;
        float l2 = trace * 0.5f - disc;
        if (l2 < 1e-8f)
            return l1 < 1e-8f ? 1f : 100f;
        return Mathf.Sqrt(l1 / l2);
    }

    private static Vector2 Centroid(List<Vector2> pts)
    {
        Vector2 s = Vector2.zero;
        for (int i = 0; i < pts.Count; i++)
            s += pts[i];
        return s / Mathf.Max(pts.Count, 1);
    }

    private static List<Vector2> RamerDouglasPeucker(List<Vector2> pts, float epsilon)
    {
        if (pts.Count < 3)
            return new List<Vector2>(pts);

        float dmax = 0f;
        int index = 0;
        Vector2 a = pts[0];
        Vector2 b = pts[pts.Count - 1];
        for (int i = 1; i < pts.Count - 1; i++)
        {
            float d = PerpendicularDistance(pts[i], a, b);
            if (d > dmax)
            {
                index = i;
                dmax = d;
            }
        }

        if (dmax > epsilon)
        {
            List<Vector2> left = RamerDouglasPeucker(pts.GetRange(0, index + 1), epsilon);
            List<Vector2> right = RamerDouglasPeucker(pts.GetRange(index, pts.Count - index), epsilon);
            var res = new List<Vector2>(left.Count + right.Count);
            for (int i = 0; i < left.Count - 1; i++)
                res.Add(left[i]);
            for (int i = 0; i < right.Count; i++)
                res.Add(right[i]);
            return res;
        }

        return new List<Vector2> { pts[0], pts[pts.Count - 1] };
    }

    private static float PerpendicularDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float mag = ab.magnitude;
        if (mag < 1e-6f)
            return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / (mag * mag));
        Vector2 proj = a + ab * t;
        return Vector2.Distance(p, proj);
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
                m_lastTrailDrawPosition = m_trailSmoothedPosition;
            }
            else
            {
                time += Time.deltaTime;
                yield return null;
            }

            Vector2 touchPos = InputManager.Instance.GetPrimaryPosition();
            m_touchPosList.Add(touchPos);

            if (!m_trailFollowInitialized)
            {
                m_trailSmoothedPosition = touchPos;
                m_lastTrailDrawPosition = touchPos;
                m_trailFollowInitialized = true;
            }
            else
            {
                m_trailSmoothedPosition = Vector2.Lerp(m_trailSmoothedPosition, touchPos, k_trailLerpT);
            }

            if (m_trailRenderer.emitting)
            {
                float segmentLength = Vector2.Distance(m_lastTrailDrawPosition, m_trailSmoothedPosition);
                int steps = Mathf.Clamp(Mathf.CeilToInt(segmentLength / k_trailInterpolationSpacing), 1, k_trailInterpolationStepsMax);
                for (int i = 1; i <= steps; i++)
                {
                    Vector2 p = Vector2.Lerp(m_lastTrailDrawPosition, m_trailSmoothedPosition, (float)i / steps);
                    m_trail.transform.position = p;
                }
                m_lastTrailDrawPosition = m_trailSmoothedPosition;
            }
            else
            {
                m_trail.transform.position = m_trailSmoothedPosition;
            }

            yield return null;
        }
    }
}
