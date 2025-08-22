using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class SceneFader
{
    private static Transform GetOverlayCanvas()
    {
        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvas.transform;
            }
        }
        Canvas tmpCanvas = new GameObject("OverlayCanvas").AddComponent<Canvas>();
        tmpCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return tmpCanvas.transform;
    }
    public static GameObject GetFadingImage()
    {
        GameObject go = new GameObject("SceneFade");
        go.transform.SetParent(GetOverlayCanvas());
        Image img = go.AddComponent<Image>();
        RectTransform rt = img.rectTransform;
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        rt.anchoredPosition = Vector2.zero;

        return go;
    }
    public static void FadeToScene(string name)
    {
        SceneFade fade = GetFadingImage().AddComponent<SceneFade>();
        fade.StartCoroutine(fade.FadeIn(name));
    }
    public static void FadeOut()
    {
        SceneFade fade = GetFadingImage().AddComponent<SceneFade>();
        fade.StartCoroutine(fade.FadeOut());
    }
}
public class SceneFade : MonoBehaviour
{
    public Image image;
    private float fadeSpeed = 1.5f;
    private float fadeoutDealy = 0.5f;

    public void Awake()
    {
        image = GetComponent<Image>();
    }
    public IEnumerator FadeIn(string name)
    {
        Color tmpColor = Color.black;
        tmpColor.a = 0;
        image.color = tmpColor;

        while (image.color.a < 1)
        {
            tmpColor = image.color;
            tmpColor.a += Time.deltaTime * fadeSpeed;
            image.color = tmpColor;

            yield return new WaitForEndOfFrame();
        }
        SceneManager.LoadScene(name);

    }
    public IEnumerator FadeOut()
    {
        Color tmpColor = Color.black;
        tmpColor.a -= Time.deltaTime * fadeSpeed;
        image.color = tmpColor;
        //yield return new WaitForseconds(fadeoutDealy);

        while (image.color.a > 0)
        {
            tmpColor = image.color;
            tmpColor.a -= Time.deltaTime * fadeSpeed;
            image.color = tmpColor;

            yield return new WaitForEndOfFrame();
        }
        Destroy(image.gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
