using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchCountdown : MonoBehaviour
{
    [SerializeField] private GameObject m_countdownGameObject;
    private TextMeshProUGUI m_countdownText;

    private float m_startingVerticalOffset = 25;
    private float m_startingOpacity = 0.2f;
    private float m_fadeInTime = 0.1f;
    private float m_moveDownTime = 0.2f;
    private float m_constantDisplayTime = 0.15f;
    private float m_fadeOutTime = 0.15f;
    private float m_endingScale = 1.5f;
    private float m_endingOpacity = 0.2f;

    private float m_animationTime;
    private float m_secondPartStartTime;

    private Coroutine m_countdownAnimation;

    public void Awake()
    {
        m_countdownText = m_countdownGameObject.GetComponent<TextMeshProUGUI>();
        m_countdownGameObject.SetActive(false);

        m_animationTime = m_moveDownTime + m_constantDisplayTime + m_fadeOutTime;
        m_secondPartStartTime = m_moveDownTime + m_constantDisplayTime;
    }

    public void Start()
    {
        GameManager.Instance.OnMatchCountdown += GameManager_MatchCountdown;
    }

    public void OnDisable()
    {
        GameManager.Instance.OnMatchCountdown -= GameManager_MatchCountdown;
    }

    private void GameManager_MatchCountdown(int countdown)
    {
        m_countdownText.text = countdown.ToString();

        if (m_countdownAnimation != null)
        {
            StopCoroutine(m_countdownAnimation);
            m_countdownAnimation = null;
        }
        m_countdownAnimation = StartCoroutine(CountdownAnimation());
    }

    private IEnumerator CountdownAnimation()
    {
        m_countdownText.color = new Color(1, 1, 1, m_startingOpacity);

        Vector3 originalPos = m_countdownGameObject.transform.localPosition;
        Vector3 countdownStartPos = originalPos;
        countdownStartPos.y += m_startingVerticalOffset;
        m_countdownGameObject.transform.SetLocalPositionAndRotation(countdownStartPos, m_countdownGameObject.transform.rotation);
        m_countdownGameObject.transform.localScale = Vector3.one;
        m_countdownGameObject.SetActive(true);

        float time = 0;
        while (time < m_animationTime)
        {
            time += Time.deltaTime;
            if (time <= m_moveDownTime)
            {
                float fadeInStep = time / m_fadeInTime;
                if (fadeInStep <= 1)
                {
                    float opacity = Mathf.Lerp(m_startingOpacity, 1, fadeInStep);
                    m_countdownText.color = new Color(1, 1, 1, opacity);
                }
                else if (m_countdownText.color.a != 1f)
                {
                    m_countdownText.color = new Color(1, 1, 1);
                }

                float moveDownStep = time / m_moveDownTime;
                Vector3 pos = Vector3.Lerp(countdownStartPos, originalPos, moveDownStep);
                m_countdownGameObject.transform.localPosition = pos;
            }
            else if (m_countdownGameObject.transform.localPosition != originalPos)
            {
                m_countdownGameObject.transform.localPosition = originalPos;
            }
            else if (time >= m_secondPartStartTime)
            {
                float endStep = (time - m_secondPartStartTime) / m_fadeOutTime;
                float opacity = Mathf.Lerp(1, m_endingOpacity, endStep);
                m_countdownText.color = new Color(1, 1, 1, opacity);

                float scale = Mathf.Lerp(1, m_endingScale, endStep);
                m_countdownGameObject.transform.localScale = new Vector3(scale, scale, 1);
            }

            yield return null;
        }
        m_countdownGameObject.SetActive(false);
        m_countdownAnimation = null;
    }
}
