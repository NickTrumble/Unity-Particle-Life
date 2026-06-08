using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MatrixLabelsAttribute))]
public class MatrixLabelsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Extract the index from the element path (e.g., "flatWeightMatrix.Array.data[4]")
        string path = property.propertyPath;
        int elementIndex = GetElementIndex(path);

        // 2. Safely grab the target config object to check the type count
        ParticleConfig config = property.serializedObject.targetObject as ParticleConfig;

        if (config != null && elementIndex >= 0)
        {
            int typeCount = config.particleTypeCount;

            // Prevent division by zero if type count isn't set yet
            if (typeCount > 0)
            {
                // Calculate matrix rows and columns based on the 1D index
                int typeA = elementIndex / typeCount;
                int typeB = elementIndex % typeCount;

                // 3. Change the label text to your custom "A on B" format
                label.text = $"Type {typeA} on Type {typeB}";
            }
        }

        // 4. Draw the original float field, but passing in our brand new label!
        EditorGUI.PropertyField(position, property, label);
    }

    private int GetElementIndex(string path)
    {
        int startIndex = path.LastIndexOf('[');
        int endIndex = path.LastIndexOf(']');

        if (startIndex != -1 && endIndex != -1)
            if (int.TryParse(path.Substring(startIndex + 1, endIndex - startIndex - 1), out int index))
                return index;

        return -1;
    }
}