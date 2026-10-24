using System;
using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class VerticalGradientVertexColorer : VertexColorer
    {
        [SerializeField] private float _minHeight = 0f;
        [SerializeField] private float _maxHeight = 1f;
        [SerializeField] private Vector3 _direction = Vector3.up;
        [SerializeField] private Gradient _gradient = new Gradient();

        public override void ColorMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                Debug.LogError("Mesh is null, cannot apply vertex coloring.");
                return;
            }

            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                float height = Vector3.Dot(vertices[i], _direction);
                colors[i] = _gradient.Evaluate(Mathf.InverseLerp(_minHeight, _maxHeight, height));
            }

            mesh.colors = colors;
        }

        public override List<Action> GetEditorGUIProperties()
        {
            List<Action> actions = new List<Action>();
#if UNITY_EDITOR
            actions.Add(() => _minHeight = UnityEditor.EditorGUILayout.FloatField("Min Height", _minHeight));
            actions.Add(() => _maxHeight = UnityEditor.EditorGUILayout.FloatField("Max Height", _maxHeight));
            actions.Add(() => _direction = UnityEditor.EditorGUILayout.Vector3Field("Direction", _direction));
            actions.Add(() => _gradient = UnityEditor.EditorGUILayout.GradientField("Gradient", _gradient));
#endif
            return actions;
        }
    }
}
