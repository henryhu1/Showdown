using System.Collections.Generic;
using UnityEngine;

public class BestOfThree : MonoBehaviour
{
    public static BestOfThree Instance { get; private set; }

    private List<MatchImage> m_matchImages;
    [SerializeField] private MatchImage m_matchImagePrefab;
    [SerializeField] private Sprite m_unplayed;
    [SerializeField] private Sprite m_win;
    [SerializeField] private Sprite m_loss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        m_matchImages = new List<MatchImage>();
        for (int i = 0; i < GameManager.k_BestOf; i++)
        {
            MatchImage matchPlayedImage = Instantiate(m_matchImagePrefab);
            matchPlayedImage.transform.SetParent(transform, false);
            m_matchImages.Add(matchPlayedImage);
        }
        SetAllUnplayed();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnMatchDecided += GameManager_MatchDecided;
        GameManager.Instance.OnResetGame += GameManager_ResetGame;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnMatchDecided -= GameManager_MatchDecided;
        GameManager.Instance.OnResetGame -= GameManager_ResetGame;
    }

    private void AnimateMatchImage(int atMatch)
    {
        if (atMatch >= m_matchImages.Count)
        {
            return;
        }

        m_matchImages[atMatch].DoPunchScaleAnimation();
    }

    private void GameManager_MatchDecided(bool hasPlayerWon, MatchResult resultType)
    {
        int atMatch = GameManager.Instance.m_AtMatch.Value - 1;
        Sprite matchResultSprite = hasPlayerWon ? m_win : m_loss;
        m_matchImages[atMatch].SetImage(matchResultSprite);
        AnimateMatchImage(atMatch);
    }

    private void GameManager_ResetGame()
    {
        SetAllUnplayed();
    }

    private void SetAllUnplayed()
    {
        foreach (MatchImage matchImage in m_matchImages)
        {
            matchImage.SetImage(m_unplayed);
        }
    }
}
