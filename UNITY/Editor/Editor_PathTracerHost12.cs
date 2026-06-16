using UnityEngine;
using UnityEditor;

// 1. CHANGE ONLY THIS LINE WHEN RENAMING YOUR HOST SCRIPT
[CustomEditor(typeof(PathTracerHost12))]
public class PathTracerHost9Editor : Editor
{
    // A simple shortcut so the code below automatically matches your target type
    private System.Type TargetType => target.GetType();

    public override void OnInspectorGUI()
    {
        // 2. This line dynamically handles casting without knowing the hardcoded name!
        var host = target; 

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dynamic Sphere Light Interface", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        // 3. We use SerializedProperties instead of direct casts. 
        // This is the industry standard way because it natively supports Undo/Redo and prefab overrides!
        SerializedProperty lightModeProp = serializedObject.FindProperty("lightMode");
        SerializedProperty lightCenterProp = serializedObject.FindProperty("lightCenter");
        SerializedProperty lightRadiusProp = serializedObject.FindProperty("lightRadius");
        SerializedProperty lightIntensityProp = serializedObject.FindProperty("lightIntensity");
        SerializedProperty lightDiscreteProp = serializedObject.FindProperty("lightDiscreteWavelength");
        SerializedProperty lightColorProp = serializedObject.FindProperty("lightColorPicker");

        // Sync the internal state representations
        serializedObject.Update();

        // Draw properties elegantly without needing to type-cast the 'host' object
        EditorGUILayout.PropertyField(lightModeProp, new GUIContent("Light Emission Mode"));
        EditorGUILayout.PropertyField(lightCenterProp, new GUIContent("Light Center Point"));
        EditorGUILayout.PropertyField(lightRadiusProp, new GUIContent("Light Radius"));
        EditorGUILayout.PropertyField(lightIntensityProp, new GUIContent("Light Intensity Multiplier"));

        // Check enum integer values safely
        if (lightModeProp.enumValueIndex == 0) // MonochromaticLaser
        {
            EditorGUILayout.PropertyField(lightDiscreteProp, new GUIContent("Discrete Wavelength (nm)"));
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(lightColorProp, new GUIContent("Spectrum Color Picker (Disabled)"));
            EditorGUI.EndDisabledGroup();
        }
        else // PolychromaticSpectrum
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(lightDiscreteProp, new GUIContent("Discrete Wavelength (Disabled)"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(lightColorProp, new GUIContent("Spectrum Color Picker"));
        }

        // Apply changes cleanly to the serialization pipeline
        if (serializedObject.ApplyModifiedProperties())
        {
            // This replaces EditorUtility.SetDirty automatically and cleans up cache tracking flawlessly
        }
    }
}