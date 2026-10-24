using System;
using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public abstract class VertexColorer
    {
        public abstract void ColorMesh(Mesh mesh);

        public abstract List<Action> GetEditorGUIProperties();
    }
}
