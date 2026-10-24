using UnityEngine;

namespace Larje.MapEditor
{
    [CreateAssetMenu(fileName = "Subwall Config", menuName = "Larje/Map Editor/Subwall Config")]
    public sealed class SubwallConfig : ScriptableObject
    {
        [field: Header("Main")]
        [field: SerializeField, Range(0f, 1f)] public float AnchorBottom { get; private set; } = 0f;
        [field: SerializeField, Range(0f, 1f)] public float AnchorTop { get; private set; } = 1f;
        [field: SerializeField] public float OffsetBottom { get; private set; } = 0f;
        [field: SerializeField] public float OffsetTop { get; private set; } = 0f;

        [field: Header("Steps")]
        [field: SerializeField] public bool SmoothSteps { get; private set; } = true;
        [field: SerializeField, Min(1)] public int Steps { get; private set; } = 1;
        [field: SerializeField] public AnimationCurve WidthCurve { get; private set; } = AnimationCurve.Linear(0, 0, 1, 0);

        [field: Header("Visuals")]
        [field: SerializeField] public float UVScale { get; private set; } = 1f;
        [field: SerializeField] public Gradient VertexColorGradient { get; private set; } = new Gradient();
        [field: SerializeField] public Material Material { get; private set; }

        [field: Header("Caps")]
        [field: SerializeField] public bool DrawTop { get; private set; } = true;
        [field: SerializeField] public Color TopColor { get; private set; } = Color.white;
        [field: SerializeField] public bool DrawBottom { get; private set; } = true;
        [field: SerializeField] public Color BottomColor { get; private set; } = Color.white;
    }
}
