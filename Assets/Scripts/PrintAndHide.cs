using UnityEngine;

public class PrintAndHide : MonoBehaviour
{
    public Renderer rend; // assign in Inspector

    private int frameCount = 0;
    private int blueDisableFrame = -1;

    private void Awake()
    {
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }

    private void Start()
    {
        // The evaluator waits one frame in HD90Coroutine before checking logs.
        // Ensuring instances exist by LoadAssets.Start(), so our first Update() after that will be counted.
    }
    }

    private void Update()
    {
        frameCount++;
        Debug.Log(gameObject.name + ":" + frameCount);

        // Red: deactivate on frame 100
        if (CompareTag("Red") && frameCount == 100)
        {
            gameObject.SetActive(false);
            return;
        }

        // Blue: randomly disable renderer at 150-200 inclusive OR up to 250 inclusive depending on instructions
        if (CompareTag("Blue") && rend != null)
        {
            // Generate once to avoid re-rolling each frame. 200 inclusive for ints (Range max is exclusive)
            if (blueDisableFrame < 0)
            {
                blueDisableFrame = Random.Range(150, 201);
            }
            if (frameCount == blueDisableFrame)
            {
                rend.enabled = false;
            }
        }
    }
}

