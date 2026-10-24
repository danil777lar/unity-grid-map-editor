using System;
using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor
{
    public sealed class TilePrefab : MonoBehaviour
    {
        [field: SerializeField] public Transform From { get; private set; }
        [field: SerializeField] public Transform To { get; private set; }

        [Space]
        [SerializeField] private List<ModelGroup> _modelGroups = new List<ModelGroup>();

        public void Initialize()
        {
            foreach (ModelGroup group in _modelGroups)
            {
                InitializeGroup(group);
            }
        }

        private void InitializeGroup(ModelGroup group)
        {
            if (group.Models.Count == 0)
            {
                return;
            }

            if (UnityEngine.Random.Range(0f, 1f) > group.SpawnChance)
            {
                foreach (ModelVariant model in group.Models)
                {
                    model.Model.SetActive(false);
                }

                return;
            }

            float totalWeight = 0f;
            foreach (ModelVariant model in group.Models)
            {
                totalWeight += model.Weight;
            }

            ModelVariant selectedModel = null;
            float random = UnityEngine.Random.Range(0f, totalWeight);
            foreach (ModelVariant model in group.Models)
            {
                random -= model.Weight;
                if (random <= 0f)
                {
                    selectedModel = model;
                    break;
                }
            }

            foreach (ModelVariant model in group.Models)
            {
                model.Model.SetActive(model == selectedModel);
            }
        }

        private void OnValidate()
        {
            foreach (ModelGroup group in _modelGroups)
            {
                foreach (ModelVariant model in group.Models)
                {
                    model.Validate();
                }
            }
        }

        [Serializable]
        private sealed class ModelGroup
        {
            [field: SerializeField, Range(0f, 1f)] public float SpawnChance { get; private set; } = 1f;
            [field: SerializeField] public List<ModelVariant> Models { get; private set; } = new List<ModelVariant>();
        }

        [Serializable]
        private sealed class ModelVariant
        {
            [HideInInspector, SerializeField] private string _inspectorName;

            [field: SerializeField] public GameObject Model { get; private set; }
            [field: SerializeField] public float Weight { get; private set; }

            public void Validate()
            {
                _inspectorName = Model != null ? Model.name : "None";
            }
        }
    }
}
