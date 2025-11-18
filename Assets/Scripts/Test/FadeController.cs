using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeController : MonoBehaviour
{
    [Header("Objects To Fade")]
    public List<GameObject> objectsToFade;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("Toggle")]
    public bool fadeIn = true;

    private bool lastState;

    // Stores original alpha per object
    private Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();

    void Start()
    {
        CacheOriginalValues();
        lastState = fadeIn;
        TriggerFade(fadeIn);
    }

    void Update()
    {
        if (fadeIn != lastState)
        {
            lastState = fadeIn;
            TriggerFade(fadeIn);
        }
    }

    private void CacheOriginalValues()
    {
        foreach (var obj in objectsToFade)
        {
            if (obj == null) continue;

            // Check CanvasGroup first
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                originalAlphas[obj] = cg.alpha;
                continue;
            }

            // Check renderer for 3D objects
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                originalAlphas[obj] = r.material.color.a;
            }
        }
    }

    private void TriggerFade(bool state)
    {
        foreach (var obj in objectsToFade)
        {
            StartCoroutine(FadeObject(obj, state));
        }
    }

    private IEnumerator FadeObject(GameObject obj, bool fadeIn)
    {
        if (obj == null) yield break;

        float originalA = originalAlphas.ContainsKey(obj) ? originalAlphas[obj] : 1f;

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float start = cg.alpha;
            float end = fadeIn ? originalA : 0f;

            float t = 0f;
            if (fadeIn) obj.SetActive(true);

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, end, t / fadeDuration);
                yield return null;
            }

            cg.alpha = end;
            if (!fadeIn) obj.SetActive(false);
            yield break;
        }

        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = r.material;
            float startA = mat.color.a;
            float endA = fadeIn ? originalA : 0f;

            float t = 0f;
            if (fadeIn) obj.SetActive(true);

            while (t < fadeDuration)
            {
                t += Time.deltaTime;

                Color c = mat.color;
                c.a = Mathf.Lerp(startA, endA, t / fadeDuration);
                mat.color = c;

                yield return null;
            }

            if (!fadeIn) obj.SetActive(false);
        }
    }
}
