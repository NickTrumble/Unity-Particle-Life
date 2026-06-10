using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleConfig))]
public class ParticleConfigEditor : Editor
{
    private SerializedProperty width;
    private SerializedProperty particleCount;
    private SerializedProperty particleTypeCount;
    private SerializedProperty particleSize;
    private SerializedProperty particleMass;
    private SerializedProperty forceRadius;
    private SerializedProperty maxAttraction;
    private SerializedProperty maxSpeed;
    private SerializedProperty damping;
    private SerializedProperty timeStep;
    private SerializedProperty typeColours;

    private bool showAdvanced;

    private void OnEnable()
    {
        width = serializedObject.FindProperty("width");
        particleCount = serializedObject.FindProperty("particleCount");
        particleTypeCount = serializedObject.FindProperty("particleTypeCount");
        particleSize = serializedObject.FindProperty("particleSize");
        particleMass = serializedObject.FindProperty("particleMass");
        forceRadius = serializedObject.FindProperty("forceRadius");
        maxAttraction = serializedObject.FindProperty("maxAttraction");
        maxSpeed = serializedObject.FindProperty("maxSpeed");
        damping = serializedObject.FindProperty("damping");
        timeStep = serializedObject.FindProperty("timeStep");
        typeColours = serializedObject.FindProperty("TypeColours");
    }

    public override void OnInspectorGUI()
    {
        ParticleConfig config = (ParticleConfig)target;

        serializedObject.Update();

        DrawActions(config);
        EditorGUILayout.Space(8);
        DrawSimulationSettings();
        EditorGUILayout.Space(8);
        DrawDisplaySettings();
        EditorGUILayout.Space(8);
        DrawAdvancedSettings();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);
        DrawWeightMatrix(config);
    }

    private void DrawActions(ParticleConfig config)
    {
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            string text = config.isRunning() ? "Stop Simulation" : "Start Simulation";
            if (GUILayout.Button(text))
                config.StartStopSim();

            if (GUILayout.Button("Recommended"))
            {
                Undo.RecordObject(config, "Apply Recommended Particle Settings");
                config.ApplyRecommendedSettings();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset Particles"))
                config.ResetParticles();

            if (GUILayout.Button("Randomise Positions"))
                config.RandomiseParticlePositions();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Randomise Weights"))
            {
                Undo.RecordObject(config, "Randomise Particle Weights");
                config.RandomiseWeightMatrix();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Randomise Colours"))
            {
                Undo.RecordObject(config, "Randomise Particle Colours");
                config.RandomiseTypeColours();
                EditorUtility.SetDirty(config);
            }
        }
    }

    private void DrawSimulationSettings()
    {
        EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
        EditorGUILayout.IntSlider(particleCount, 0, 100000, "Particle Count");
        EditorGUILayout.IntSlider(particleTypeCount, 1, 8, "Types");
        EditorGUILayout.IntSlider(forceRadius, 5, 160, "Force Radius");
        EditorGUILayout.IntSlider(maxAttraction, 0, 10, "Max Attraction");
        EditorGUILayout.Slider(maxSpeed, 1f, 100f, "Max Speed");
        EditorGUILayout.Slider(damping, 0.75f, 0.99f, "Damping");
        EditorGUILayout.Slider(timeStep, 0.05f, 1f, "Time Step");
        EditorGUILayout.IntSlider(particleMass, 1, 50, "Mass");
    }

    private void DrawDisplaySettings()
    {
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        EditorGUILayout.IntSlider(width, 100, 3200, "Texture Width");

        int previewHeight = Mathf.Max(100, 9 * width.intValue / 16);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Texture Height", previewHeight);
        }

        EditorGUILayout.IntSlider(particleSize, 3, 31, "Particle Size");
        EditorGUILayout.PropertyField(typeColours, new GUIContent("Type Colours"));
    }

    private void DrawWeightMatrix(ParticleConfig config)
    {
        EditorGUILayout.LabelField("Weights", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Rows are the particle being affected. Columns are the nearby particle applying the influence. Positive attracts, negative repels.", MessageType.None);

        int typeCount = Mathf.Clamp(config.particleTypeCount, 1, 8);
        const float labelWidth = 56f;
        float cellWidth = Mathf.Max(52f, (EditorGUIUtility.currentViewWidth - labelWidth - 38f) / typeCount);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(labelWidth);
            for (int column = 0; column < typeCount; column++)
                GUILayout.Label($"T{column}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(cellWidth));
        }

        for (int row = 0; row < typeCount; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"T{row}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(labelWidth));
                for (int column = 0; column < typeCount; column++)
                {
                    float oldValue = config.GetWeight(row, column);
                    float newValue = EditorGUILayout.FloatField(oldValue, GUILayout.Width(cellWidth));

                    if (!Mathf.Approximately(oldValue, newValue))
                    {
                        Undo.RecordObject(config, "Edit Particle Weight");
                        config.SetWeight(row, column, newValue);
                        EditorUtility.SetDirty(config);
                    }
                }
            }
        }
    }

    private void DrawAdvancedSettings()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
        if (!showAdvanced)
            return;

        EditorGUILayout.HelpBox("The simulation uses these cells to limit force checks to nearby particles. Cell size should usually be at least as large as the force radius.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gridX"), new GUIContent("Grid Columns"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gridY"), new GUIContent("Grid Rows"));
    }
}
