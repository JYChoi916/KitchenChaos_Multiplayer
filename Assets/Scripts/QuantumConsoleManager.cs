using QFSW.QC;
using UnityEngine;

public class QuantumConsoleManager : MonoBehaviour
{
    private static QuantumConsoleManager instance;
    public static QuantumConsoleManager Instance {
        get
        {
            return instance;
        }
    }

    [SerializeField] private GameObject quantumConsole;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            quantumConsole.SetActive(!quantumConsole.activeInHierarchy);
        }        
    }
}
