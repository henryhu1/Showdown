using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BestOfThree : MonoBehaviour
{
    public static BestOfThree Instance { get; private set; }

    private List<MatchImage> m_matchImages;
    [SerializeField] private MatchImage m_matchImagePrefab;
    [SerializeField] private Sprite m_unplayed;
    [SerializeField] private Sprite m_win;
    [SerializeField] private Sprite m_loss;
    [SerializeField] private float m_growShrinkTime = 0.25f;
    [SerializeField] private float m_growToSize = 2;
    [SerializeField] private AnimationCurve m_movementCurve;

    private Coroutine m_growShrinkCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    private void Start()
    {
        m_matchImages = new List<MatchImage>();
        for (int i = 0; i < GameManager.s_BestOf; i++)
        {
            MatchImage matchPlayedImage = Instantiate(m_matchImagePrefab);
            matchPlayedImage.transform.SetParent(transform, false);
            m_matchImages.Add(matchPlayedImage);
        }
        SetAllUnplayed();

        GameManager.Instance.OnMatchWon += GameManager_MatchWon;
        GameManager.Instance.OnMatchLost += GameManager_MatchLost;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnMatchWon -= GameManager_MatchWon;
        GameManager.Instance.OnMatchLost -= GameManager_MatchLost;
    }

    private IEnumerator GrowShrink(MatchImage matchImage)
    {
        float time = 0;
        Vector3 atScale = matchImage.transform.localScale;
        Vector3 targetScale = matchImage.transform.localScale * m_growToSize;
        bool isGrowing = true;
        while (time < 2 * m_growShrinkTime)
        {
            if (isGrowing && time > m_growShrinkTime)
            {
                isGrowing = false;
                Vector3 temp = atScale;
                atScale = targetScale;
                targetScale = temp;
                matchImage.transform.localScale = atScale;
            }
            time += Time.deltaTime;
            float step = time % m_growShrinkTime / m_growShrinkTime;
            float curveStep = m_movementCurve.Evaluate(step);
            matchImage.transform.localScale = Vector3.Lerp(atScale, targetScale, curveStep);
            yield return null;
        }

        matchImage.transform.localScale = targetScale;
        m_growShrinkCoroutine = null;
    }

    private void AnimateMatchImage(int atMatch)
    {
        if (atMatch >= m_matchImages.Count)
        {
            return;
        }

        if (m_growShrinkCoroutine != null)
        {
            StopCoroutine(m_growShrinkCoroutine);
            m_growShrinkCoroutine = null;
        }
        m_growShrinkCoroutine = StartCoroutine(GrowShrink(m_matchImages[atMatch]));
    }

    private void GameManager_MatchWon()
    {
        int atMatch = GameManager.Instance.m_AtMatch.Value - 1;
        m_matchImages[atMatch].SetImage(m_win);
        AnimateMatchImage(atMatch);
    }

    private void GameManager_MatchLost()
    {
        int atMatch = GameManager.Instance.m_AtMatch.Value - 1;
        m_matchImages[atMatch].SetImage(m_loss);
        AnimateMatchImage(atMatch);
    }

    private void SetAllUnplayed()
    {
        foreach (MatchImage matchImage in m_matchImages)
        {
            matchImage.SetImage(m_unplayed);
        }
    }
}
