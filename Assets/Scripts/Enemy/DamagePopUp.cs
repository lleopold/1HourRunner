using TMPro;
using UnityEngine;

public class DamagePopUp : MonoBehaviour
{
    public static DamagePopUp Create(Vector3 position, int damageAmount)
    {
        GameObject damagePopupprefab = Resources.Load<GameObject>("pfDamagePopUp");
        Transform damagePopupTransform = Instantiate(damagePopupprefab.transform, position, Quaternion.identity);
        DamagePopUp popUp = damagePopupTransform.GetComponent<DamagePopUp>();

        popUp.Setup(damageAmount, false);
        return (popUp);
    }
    private static int sortingOrder;
    private const float DISSAPEAR_TIME_MAX = 0.8f;
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    void Setup(int damageAmount, bool isCritical)
    {
        textMesh.SetText(damageAmount.ToString());
        if (!isCritical)
        {
            textMesh.fontSize = 36;
            textColor = GetColorFromString("FFC500");
        }
        else
        {
            textMesh.fontSize = 45;
            textColor = GetColorFromString("FF2B00");
        }
        textMesh.color = textColor;
        disappearTimer = DISSAPEAR_TIME_MAX;
        sortingOrder++;
        textMesh.sortingOrder = sortingOrder;
        moveVector = new Vector3(.7f, 1, 1) * 10f;
    }

    private Color GetColorFromString(string colorHex)
    {
        // Remove any '#' characters from the colorHex string
        colorHex = colorHex.Replace("#", "");

        // Parse the hexadecimal color string into separate R, G, and B components
        byte r = byte.Parse(colorHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(colorHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(colorHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        // Create and return the Color object
        return new Color32(r, g, b, 255); // Alpha channel is set to maximum (255)
    }
    void Update()
    {
        float moveYSpeed = 5f;

        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * moveYSpeed * Time.deltaTime;
        disappearTimer -= Time.deltaTime;
        if (disappearTimer > DISSAPEAR_TIME_MAX * .5f)
        {
            float increaseScaleAmount = 1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            float decreaseScaleAmount = 1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        // Make the text always face the camera
        transform.LookAt(transform.position + Camera.main.transform.forward, Camera.main.transform.up);
        if (disappearTimer < 0)
        {
            // Start disappearing
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }

}
