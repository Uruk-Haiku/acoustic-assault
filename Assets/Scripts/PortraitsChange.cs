using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PortraitsChange : MonoBehaviour
{
    public GameObject pinkBird;
    public GameObject pinkBird_hit;
    public GameObject greenBird;
    public GameObject greenBird_hit;
    public float changeDuration = 1f; // how long to show hit portraits

    // Shake parameters
    public float shakeDuration = 0.5f; // how long to shake
    public float shakeIntensity = 10f; // how strong the shake

    // Call this method to start the coroutine
    public void ChangePortraits()
    {
        StartCoroutine(SwapImages());
    }

    private IEnumerator SwapImages()
    {
        // Switch: Hit images shown
        pinkBird.SetActive(false);
        greenBird.SetActive(false);
        pinkBird_hit.SetActive(true);
        greenBird_hit.SetActive(true);

        // Start shaking
        RectTransform pinkRect = pinkBird_hit.GetComponent<RectTransform>();
        RectTransform greenRect = greenBird_hit.GetComponent<RectTransform>();

        Vector3 originalPos_p = pinkRect.anchoredPosition;
        Vector3 originalPos_g = greenRect.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            pinkRect.anchoredPosition = originalPos_p + new Vector3(offsetX, offsetY, 0);
            greenRect.anchoredPosition = originalPos_g + new Vector3(-offsetX, -offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset position
        pinkRect.anchoredPosition = originalPos_p;
        greenRect.anchoredPosition = originalPos_g;


        // Wait for changeDuration second
        yield return new WaitForSeconds(changeDuration);

        // Switch back
        pinkBird.SetActive(true);
        greenBird.SetActive(true);
        pinkBird_hit.SetActive(false);
        greenBird_hit.SetActive(false);
    }
}