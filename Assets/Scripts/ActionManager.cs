using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
    }
}
