using UnityEngine;

public class LoadAssets : MonoBehaviour
{
    // Assign these in the Inspector
    public GameObject redObj;
    [SerializeField] private GameObject blueObj;

    private GameObject redInstance;
    private GameObject blueInstance;

    private void Awake()
    {
        if (redObj != null)
        {
            redInstance = Instantiate(redObj, new Vector3(2.2f, 0f, 0f), Quaternion.identity);
        }

        if (blueObj != null)
        {
            blueInstance = Instantiate(blueObj, new Vector3(-2.2f, 0f, 0f), Quaternion.identity);
        }
    }
}

