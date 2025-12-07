using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class LabelStyle
{
    public string styleName;
    public Color backgroundColor = Color.gray;
    public Color textColor = Color.white;
}

public class Label : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image background;
    [SerializeField] private LabelStyleSet styleSet;

    public bool ApplyStyle(string styleName) {
        LabelStyle style = styleSet?.GetStyle(styleName);

        if (style != null) {
            if (labelText != null)
            {
                labelText.text  = styleName;                
                labelText.color = style.textColor;
            }
            if (background != null) background.color = style.backgroundColor;
            return true;
        }

        return false;
    }
}