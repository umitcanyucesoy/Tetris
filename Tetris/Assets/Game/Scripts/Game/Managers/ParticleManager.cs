using System;
using System.Collections.Generic;
using Game.Scripts.Game.Core;
using UnityEngine;

namespace Game.Scripts.Game.Managers
{
    public class ParticleManager : MonoSingleton<ParticleManager>
    {
        [Serializable]
        public struct FXEntry
        {
            public string name;
            public GameObject prefab;
        }
        
        [Header("FX List")]
        public List<FXEntry> fxList = new();
        public Transform fxTransform;

        public void Play(string fxName, Vector3 pos)
        {
            foreach (var entry in fxList)
            {
                if (entry.name == fxName)
                {
                    var fx = Instantiate(entry.prefab, pos, Quaternion.identity, fxTransform);
                    var ps = fx.GetComponent<ParticleSystem>();
                    
                    if (ps.main.stopAction != ParticleSystemStopAction.Destroy)
                        Destroy(fx, ps.main.duration + ps.main.startLifetimeMultiplier + 0.1f);
                }
            }
        }

        public void SettleFX(Shape shape, string fxName)
        {
            foreach (var sr in shape.shapeRenderers)
            {
                Vector2 pos = Vector2Int.RoundToInt(sr.transform.position);
                Play(fxName, pos);
            }
        }
    }
}