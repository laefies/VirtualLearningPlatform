using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(VisualStatePropertyAttribute))]
public class VisualStatePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        VisualStatePropertyAttribute attr = (VisualStatePropertyAttribute)attribute;
        
        EditorGUI.BeginProperty(position, label, property);
        
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = new Color(0.3f, 0.6f, 1f);
        
        EditorGUI.PropertyField(position, property, new GUIContent("   " + attr.Label, property.tooltip), true);
        
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHide = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHide, property);

        if (enabled)
        {
            position.x += 15;
            position.width -= 15;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalHideAttribute condHide = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHide, property);

        if (enabled)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        else
        {
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }

    private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHide, SerializedProperty property)
    {
        bool enabled = true;
        string propertyPath = property.propertyPath;
        string conditionPath = propertyPath.Replace(property.name, condHide.ConditionalFieldName);
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

        if (sourcePropertyValue != null)
        {
            enabled = sourcePropertyValue.boolValue;
        }
        else
        {
            Debug.LogWarning("Conditional field not found: " + condHide.ConditionalFieldName);
        }

        return enabled;
    }
}

[CustomEditor(typeof(InteractionFeedback))]
public class InteractionFeedbackEditor : Editor
{
    private SerializedProperty targetBlock;
    private SerializedProperty targetLabel;
    private SerializedProperty enableHoverEffects;
    private SerializedProperty hoverState;
    private SerializedProperty enablePressedEffects;
    private SerializedProperty pressedState;
    private SerializedProperty enableDisabledState;
    private SerializedProperty disabledState;
    private SerializedProperty transitionSpeed;
    private SerializedProperty useSmoothTransitions;
    private SerializedProperty easingCurve;
    private SerializedProperty hoverDelay;
    private SerializedProperty hoverSound;
    private SerializedProperty pressSound;
    private SerializedProperty releaseSound;
    private SerializedProperty audioVolume;

    private bool showHoverState = true;
    private bool showPressedState = true;
    private bool showDisabledState = true;
    private bool showAudio = false;

    private void OnEnable()
    {
        targetBlock = serializedObject.FindProperty("targetBlock");
        targetLabel = serializedObject.FindProperty("targetLabel");
        enableHoverEffects = serializedObject.FindProperty("enableHoverEffects");
        hoverDelay = serializedObject.FindProperty("hoverDelay");
        hoverState = serializedObject.FindProperty("hoverState");
        enablePressedEffects = serializedObject.FindProperty("enablePressedEffects");
        pressedState = serializedObject.FindProperty("pressedState");
        enableDisabledState = serializedObject.FindProperty("enableDisabledState");
        disabledState = serializedObject.FindProperty("disabledState");
        transitionSpeed = serializedObject.FindProperty("transitionSpeed");
        useSmoothTransitions = serializedObject.FindProperty("useSmoothTransitions");
        easingCurve = serializedObject.FindProperty("easingCurve");
        hoverSound = serializedObject.FindProperty("hoverSound");
        pressSound = serializedObject.FindProperty("pressSound");
        releaseSound = serializedObject.FindProperty("releaseSound");
        audioVolume = serializedObject.FindProperty("audioVolume");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        
        // Target Elements
        DrawSectionHeader("Target Elements");
        EditorGUILayout.PropertyField(targetBlock);
        EditorGUILayout.PropertyField(targetLabel);

        EditorGUILayout.Space(10);
        
        // Hover State
        showHoverState = DrawStateSection(
            "Hover State", 
            showHoverState, 
            enableHoverEffects
        );
        
        if (showHoverState && enableHoverEffects.boolValue)
        {
            DrawStateContent(() => {
                EditorGUILayout.PropertyField(hoverDelay, new GUIContent("Hover Delay"));
                if (hoverDelay.floatValue > 0)
                {
                    EditorGUILayout.HelpBox($"Effects apply after {hoverDelay.floatValue:F2}s delay", MessageType.None);
                }
                EditorGUILayout.PropertyField(hoverState, GUIContent.none, true);
            });
        }

        EditorGUILayout.Space(8);
        
        // Pressed State
        showPressedState = DrawStateSection(
            "Pressed State", 
            showPressedState, 
            enablePressedEffects
        );
        
        if (showPressedState && enablePressedEffects.boolValue)
        {
            DrawStateContent(() => {
                EditorGUILayout.PropertyField(pressedState, GUIContent.none, true);
            });
        }

        EditorGUILayout.Space(8);
        
        // Disabled State
        showDisabledState = DrawStateSection(
            "Disabled State", 
            showDisabledState, 
            enableDisabledState
        );
        
        if (showDisabledState && enableDisabledState.boolValue)
        {
            DrawStateContent(() => {
                EditorGUILayout.PropertyField(disabledState, GUIContent.none, true);
            });
        }

        EditorGUILayout.Space(10);
        
        // Animation Settings
        DrawSectionHeader("Animation Settings");
        EditorGUILayout.PropertyField(useSmoothTransitions, new GUIContent("Smooth Transitions"));
        
        if (useSmoothTransitions.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(transitionSpeed);
            EditorGUILayout.PropertyField(easingCurve);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        
        // Audio Feedback
        showAudio = EditorGUILayout.Foldout(showAudio, "Audio Feedback", true, EditorStyles.foldoutHeader);
        if (showAudio)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hoverSound);
            EditorGUILayout.PropertyField(pressSound);
            EditorGUILayout.PropertyField(releaseSound);
            EditorGUILayout.PropertyField(audioVolume);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        
        // Reset button
        if (GUILayout.Button("Reset to Default State", GUILayout.Height(28)))
        {
            if (Application.isPlaying)
            {
                ((InteractionFeedback)target).ResetToDefault();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSectionHeader(string title)
    {
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 11;
        EditorGUILayout.LabelField(title, style);
        DrawDivider(new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    private bool DrawStateSection(string title, bool foldout, SerializedProperty enableProp)
    {
        var rect = EditorGUILayout.GetControlRect(false, 24);
        
        // Draw subtle background when enabled
        if (enableProp.boolValue)
        {
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));
        }
        
        // Foldout area
        var foldoutRect = new Rect(rect.x, rect.y, rect.width - 70, rect.height);
        var style = new GUIStyle(EditorStyles.foldout);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 11;
        
        foldout = EditorGUI.Foldout(foldoutRect, foldout, title, true, style);
        
        // Toggle on the right
        var toggleRect = new Rect(rect.x + rect.width - 60, rect.y + 4, 60, 16);
        
        EditorGUI.BeginChangeCheck();
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.fontSize = 10;
        
        var checkRect = new Rect(toggleRect.x, toggleRect.y, 14, 14);
        var labelRect = new Rect(toggleRect.x + 18, toggleRect.y, 42, 14);
        
        enableProp.boolValue = EditorGUI.Toggle(checkRect, enableProp.boolValue);
        EditorGUI.LabelField(labelRect, "Enabled", labelStyle);
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        
        return foldout;
    }

    private void DrawStateContent(System.Action drawContent)
    {
        var bgRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, bgRect.width, bgRect.height + 4), new Color(0, 0, 0, 0.1f));
        
        EditorGUILayout.Space(4);
        EditorGUI.indentLevel++;
        drawContent();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
        
        EditorGUILayout.EndVertical();
    }

    private void DrawDivider(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
        EditorGUILayout.Space(2);
    }
}