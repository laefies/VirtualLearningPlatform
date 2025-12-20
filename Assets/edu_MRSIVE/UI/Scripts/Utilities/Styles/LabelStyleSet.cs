using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LabelStyleSet", menuName = "UI/Label Style Set")]
public class LabelStyleSet : ScriptableObject
{
    public LabelStyle[] styles;
    private Dictionary<string, LabelStyle> styleCache;

    private void OnEnable() {
        if (styleCache == null) {
            styleCache = new Dictionary<string, LabelStyle>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var style in styles) {
                if (!string.IsNullOrEmpty(style.styleName))
                    styleCache[style.styleName] = style;
            }
        }
    }

    public LabelStyle GetStyle(string name) {
        return styleCache.TryGetValue(name, out var style) ? style : null;
    }
}