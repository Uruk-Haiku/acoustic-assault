using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum EmotionScore
{
    Great,
    Good,
    Bad
}
public class PortraitsChange : MonoBehaviour
{
    public GameObject pinkBird;
    public GameObject pinkBird_hit;
    public GameObject greenBird;
    public GameObject greenBird_hit;

    public GameObject popupBubble;

    public GameObject greenBirdGreatPopup;
    public GameObject greenBirdGoodPopup;
    public GameObject greenBirdBadPopup;

    public GameObject pinkBirdGreatPopup;
    public GameObject pinkBirdGoodPopup;
    public GameObject pinkBirdBadPopup;

    public float changeDuration = 1f; // how long to show hit portraits

    // Shake parameters
    public float shakeDuration = 0.5f; // how long to shake
    public float shakeIntensity = 10f; // how strong the shake

    private bool isPopupAnimating = false; // Track if a popup is currently showing

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

    public void ShowPopupForPlayer(int player, EmotionScore score)
    {
        // Don't show a new popup if one is already animating
        if (isPopupAnimating)
        {
            return;
        }

        GameObject popupToShow = null;

        if (player == 0)
        {
            if (score == EmotionScore.Great)
            {
                popupToShow = pinkBirdGreatPopup;
            }
            else if (score == EmotionScore.Good)
            {
                popupToShow = pinkBirdGoodPopup;
            }
            else if (score == EmotionScore.Bad)
            {
                popupToShow = pinkBirdBadPopup;
            }
        }
        else if (player == 1)
        {
            if (score == EmotionScore.Great)
            {
                popupToShow = greenBirdGreatPopup;
            }
            else if (score == EmotionScore.Good)
            {
                popupToShow = greenBirdGoodPopup;
            }
            else if (score == EmotionScore.Bad) {
                popupToShow = greenBirdBadPopup;
            }
        }

        if (popupToShow != null)
        {
            StartCoroutine(ShowEmotionPopup(popupToShow));
        }
    }

    public IEnumerator ShowEmotionPopup(GameObject popupObject)
    {
        // Mark that popup animation is in progress
        isPopupAnimating = true;

        // Play SFX
        // SongManager.Instance.Play();

        popupBubble.SetActive(true);
        popupObject.SetActive(true);

        RectTransform bubbleRect = popupBubble.GetComponent<RectTransform>();
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        
        Vector3 originalBubbleScale = bubbleRect.localScale;
        Quaternion originalBubbleRotation = bubbleRect.localRotation;
        Vector3 originalPopupPos = popupRect.anchoredPosition;
        
        // Animation timings
        float scaleDuration = 0.3f;
        float rockDuration = 0.4f;
        float bounceDuration = 0.5f;
        float displayDuration = 1.5f;
        
        float rockAngle = 15f;
        float bounceHeight = 20f;
        
        // Start both animations simultaneously
        bubbleRect.localScale = Vector3.zero;
        float totalAnimDuration = Mathf.Max(scaleDuration + rockDuration, bounceDuration);
        float elapsed = 0f;
        
        while (elapsed < totalAnimDuration)
        {
            float t = elapsed;
            
            // Bubble scale animation (0 to scaleDuration)
            if (t < scaleDuration)
            {
                float scaleT = t / scaleDuration;
                bubbleRect.localScale = Vector3.Lerp(Vector3.zero, originalBubbleScale, scaleT);
            }
            else
            {
                bubbleRect.localScale = originalBubbleScale;
            }
            
            // Bubble rock animation (after scale, from scaleDuration to scaleDuration + rockDuration)
            if (t >= scaleDuration && t < scaleDuration + rockDuration)
            {
                float rockT = (t - scaleDuration) / rockDuration;
                float angle = Mathf.Sin(rockT * Mathf.PI * 4) * rockAngle * (1 - rockT);
                bubbleRect.localRotation = originalBubbleRotation * Quaternion.Euler(0, 0, angle);
            }
            else if (t >= scaleDuration + rockDuration)
            {
                bubbleRect.localRotation = originalBubbleRotation;
            }
            
            // Popup bounce animation (runs simultaneously from start)
            if (t < bounceDuration)
            {
                float bounceT = t / bounceDuration;
                float bounce = Mathf.Sin(bounceT * Mathf.PI * 4) * bounceHeight * (1 - bounceT);
                popupRect.anchoredPosition = originalPopupPos + new Vector3(0, bounce, 0);
            }
            else
            {
                popupRect.anchoredPosition = originalPopupPos;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final positions
        bubbleRect.localScale = originalBubbleScale;
        bubbleRect.localRotation = originalBubbleRotation;
        popupRect.anchoredPosition = originalPopupPos;
        
        // Display time
        yield return new WaitForSeconds(displayDuration);
        
        // Deactivate
        popupObject.SetActive(false);
        popupBubble.SetActive(false);

        // Mark that popup animation is complete
        isPopupAnimating = false;
    }
}