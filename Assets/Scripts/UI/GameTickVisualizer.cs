using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameTickVisualizer : MonoBehaviour
{
    [SerializeField] private RectTransform m_tickOuter;
    [SerializeField] private RectTransform m_tickInner;
    [SerializeField] private Image m_tickInnerImage;
    [SerializeField] private AnimationCurve m_movementCurve;

    Vector2 m_tickStartSize, m_tickEndSize;

    private void Awake()
    {
        m_tickInnerImage.color = Colors.Orange;
        m_tickStartSize = Vector2.zero;
        m_tickEndSize = m_tickOuter.sizeDelta;
    }

    private void Start()
    {
        m_tickInner.sizeDelta = m_tickStartSize;

        GameManager.Instance.OnTickTimePasses += GameManager_TickTimePasses;
        GameManager.Instance.OnDisableActionsToBePlayed += GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnEnableActionsToBePlayed += GameManager_EnableActionsToBePlayed;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnTickTimePasses -= GameManager_TickTimePasses;
        GameManager.Instance.OnDisableActionsToBePlayed -= GameManager_DisableActionsToBePlayed;
        GameManager.Instance.OnEnableActionsToBePlayed -= GameManager_EnableActionsToBePlayed;
    }

    private void GameManager_TickTimePasses(float time, int ticksPlayed)
    {
        float step = time / GameManager.s_TimePerTick;
        float curveStep = m_movementCurve.Evaluate(step);
        m_tickInner.sizeDelta = Vector2.Lerp(m_tickStartSize, m_tickEndSize, curveStep);
    }

    private void GameManager_EnableActionsToBePlayed()
    {
        m_tickInnerImage.color = Colors.Turquoise;
    }

    private void GameManager_DisableActionsToBePlayed()
    {
        m_tickInnerImage.color = Colors.Orange;
    }

    //private IEnumerator TickCoroutine()
    //{
    //    float time = 0;
    //    while (true)
    //    {
    //        time += Time.deltaTime;
    //        float step = time / GameManager.s_TimePerTick;
    //        float curveStep = m_movementCurve.Evaluate(step);
    //        m_tickInner.sizeDelta = Vector2.Lerp(m_tickStartSize, m_tickEndSize, curveStep);

    //        if (time >= GameManager.s_TimePerTick)
    //        {
    //            m_tickInner.sizeDelta = Vector2.zero;
    //            time = 0;
    //        }

    //        yield return null;
    //    }
    //}
}
