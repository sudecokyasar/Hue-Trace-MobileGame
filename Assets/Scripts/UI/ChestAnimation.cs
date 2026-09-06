using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChestAnimation : MonoBehaviour
{
    [Header("UI & G�rseller")]
    [SerializeField] private Image chestImage;
    [SerializeField] private Sprite closedChestSprite;
    [SerializeField] private Sprite openChestSprite;
    [SerializeField] private GameObject glowEffect; 

    [Header("Animasyon Ayarlar�")]
    [SerializeField] private float shakeDuration = 0.6f;
    [SerializeField] private float shakeMagnitude = 8f;

    private RectTransform rectTransform;
    private Vector3 initialScale;
    private Vector2 initialPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialScale = rectTransform.localScale;
        initialPos = rectTransform.anchoredPosition;
    }

    public void PlayOpenAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateChestOpen());
    }

    private IEnumerator AnimateChestOpen()
    {
        chestImage.sprite = closedChestSprite;
        rectTransform.localScale = initialScale;
        rectTransform.anchoredPosition = initialPos;
        if (glowEffect != null) glowEffect.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
            float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
            rectTransform.anchoredPosition = initialPos + new Vector2(offsetX, offsetY);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = initialPos;

        rectTransform.localScale = new Vector3(initialScale.x * 1.15f, initialScale.y * 0.85f, initialScale.z);
        yield return new WaitForSeconds(0.08f);

        chestImage.sprite = openChestSprite;
        if (glowEffect != null) glowEffect.SetActive(true);

        float popElapsed = 0f;
        float popDuration = 0.25f;
        Vector3 peakScale = initialScale * 1.25f;

        while (popElapsed < popDuration)
        {
            rectTransform.localScale = Vector3.Lerp(initialScale, peakScale, popElapsed / popDuration);
            popElapsed += Time.deltaTime;
            yield return null;
        }

        popElapsed = 0f;
        while (popElapsed < 0.15f)
        {
            rectTransform.localScale = Vector3.Lerp(peakScale, initialScale, popElapsed / 0.15f);
            popElapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = initialScale;
    }
}