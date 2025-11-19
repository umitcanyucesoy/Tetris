using System;
using System.Collections.Generic;
using Game.Scripts.Game.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Scripts.Game.Managers
{
    public class SpawnManager : MonoSingleton<SpawnManager>
    {
        [Header("Elements")] 
        public List<Shape> shapes = new();
        public Transform spawnPoint;

        [Header("Round Robin")]
        [SerializeField] private float roundWeight = 1f;
        [SerializeField] private float robinBonus = .6f;
        public List<int> roundRobinShapes;
        
        public Shape SpawnShape()
        {
            if (GameManager.Instance.currentGameState != GameState.Playing) return null;
            var shape = Instantiate(GetRandomShape(), spawnPoint.position, Quaternion.identity, spawnPoint);
            shape.OnSpawned();
            shape.SetColor();
            return shape;
        }
        
        private Shape GetRandomShape()
        {
            EnsureRoundRobin();

            float total = 0f;
            var weights = new float [shapes.Count];
            for (int i = 0; i < shapes.Count; i++)
            {
                float w = roundWeight + roundRobinShapes[i] * robinBonus;
                weights[i] = w;
                total += w;
            }
            
            float r =  Random.Range(0, total);
            int chosen = 0;
            float acc = 0f;
            for (int i = 0; i < shapes.Count; i++)
            {
                acc += weights[i];
                if (r <= acc) { chosen = i; break; }
            }

            for (int i = 0; i < roundRobinShapes.Count; i++)
                roundRobinShapes[i] = (i == chosen) ? 0 : roundRobinShapes[i] + 1;

            return shapes[chosen];
        }

        public Shape SpawnPreview(Transform slotParent)
        {
            var shape = Instantiate(GetRandomShape(), slotParent.position, Quaternion.identity, slotParent.transform);
            shape.SetColor();
            shape.SetPreviewMode(true);
            shape.enabled = false;
            shape.transform.localPosition = shape.queueOffset;
            return shape;
        }

        private void EnsureRoundRobin()
        {
            if (roundRobinShapes == null || roundRobinShapes.Count != shapes.Count)
            {
                roundRobinShapes = new List<int>(shapes.Count);
                for (int i = 0; i < shapes.Count; i++) roundRobinShapes.Add(0);
            }
        }

        public void ClearRoundRobinAndSpawnParent()
        {
            for (int i = spawnPoint.childCount - 1; i >= 0; i--)
                Destroy(spawnPoint.GetChild(i).gameObject);
            
            for (int i = 0; i < roundRobinShapes.Count; i++)
                roundRobinShapes[i] = 0;
        }
    }
}