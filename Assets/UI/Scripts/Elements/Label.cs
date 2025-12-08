using UnityEngine;
using Nova;

[System.Serializable]
public class LabelStyle
{
    public string styleName;
    public Color mainColor = Color.white;
    public Color textColor = Color.white;
    public float bodyAlpha = 0.1f;
}

public class Label : MonoBehaviour
{
    [SerializeField] private LabelStyleSet styleSet;
    [SerializeField] private UIBlock2D labelBody;
    [SerializeField] private TextBlock labelText;

    public bool ApplyStyle(string styleName) 
    {
        if (styleSet == null || labelBody == null || labelText == null) 
            return false;

        LabelStyle style = styleSet.GetStyle(styleName);
        if (style == null) return false;

        labelText.Text  = style.styleName;
        labelText.Color = style.textColor;
        
        Color bodyColor = style.mainColor;
        bodyColor.a     = style.bodyAlpha;
        labelBody.Color = bodyColor;
    
        labelBody.Border.Color = style.mainColor;

        return true;
    }
}