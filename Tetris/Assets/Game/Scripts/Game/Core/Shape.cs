using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.Game.Database;
using Game.Scripts.Game.Managers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Scripts.Game.Core
{
    public class Shape : MonoBehaviour
    {
        public enum ShapeType { J, L, S, T, Z, I, O }

        [Header("References")] 
        [SerializeField] private Material shapeMaterial;
        [SerializeField] private SpriteRenderer ghostBlockPrefab;
        public ShapeType shapeType;
        public List<SpriteRenderer> shapeRenderers = new();
        public int srsOrientationOffset = 0;
        public Vector3 queueOffset;
        [HideInInspector] public int _rotationIndex = 0;
        [HideInInspector] public bool lastActionWasRotate = false;
        [HideInInspector] public Vector2Int lastKickOffset = Vector2Int.zero;

        private readonly List<SpriteRenderer> _ghostRenderers = new();
        private ShapeDatabase _shapeDatabase;
        

        private void Start()
        {
            _shapeDatabase = ShapeDatabase.Instance; 
            BuildGhostShape();
            UpdateGhostShape();
        }

        public void OnSpawned()
        {
            lastActionWasRotate = false;
            lastKickOffset = Vector2Int.zero;
        }
        
        public Vector2Int PivotGridCoord => Vector2Int.RoundToInt(transform.position);

        public void SetColor()
        {
            foreach (var rend in shapeRenderers)
                rend.sharedMaterial = shapeMaterial;
        }

        public IEnumerator MoveVertical(float interval, float cellY)
        {
            var wait = new WaitForSeconds(interval);

            while (true)
            {
                yield return wait;                         
                transform.position += Vector3.down * cellY;

                if (!IsValidPosition())
                {
                    transform.position += Vector3.up * cellY;
                    HideGhost();
                    GameManager.Instance.ShapeCoroutineManagement();
                    yield break;
                }
                
                UpdateGhostShape();
            }
        }

        public void MoveHorizontal(float dx)
        {
            var pos = transform;
            Vector3 delta = new Vector3(dx, 0f, 0f);
            pos.position += delta;
            
            if (!IsValidPosition())
                pos.position -= delta;

            lastActionWasRotate = false;
            UpdateGhostShape();
        }
        
        public void HardDrop(float cellY)
        {
            var t = transform;
            while (true)
            {
                t.position += Vector3.down * cellY;
                if (!IsValidPosition())
                {
                    t.position -= Vector3.down * cellY;
                    break;
                }
            }
            
            lastActionWasRotate = false;
            HideGhost();
        }

        public void MoveRotate()
        {
            TryRotateSRS(-90f);
            UpdateGhostShape();
        }

        private void TryRotateSRS(float degrees)
        {
            if (shapeType == ShapeType.O) return;

            var t = transform;
            var prevRotQ = t.rotation;
            var prevPos  = t.position;

            int fromIndex = _rotationIndex;
            int step = (degrees < 0f) ? +1 : -1;  
            int toIndex   = _shapeDatabase.Mod4(_rotationIndex + step);

            int srsFrom = _shapeDatabase.Mod4(fromIndex + srsOrientationOffset);
            int srsTo   = _shapeDatabase.Mod4(toIndex   + srsOrientationOffset);

            t.Rotate(0f, 0f, degrees, Space.Self);

            var kicks = _shapeDatabase.GetKickTests(shapeType, srsFrom, srsTo);
            for (int i = 0; i < kicks.Length; i++)
            {
                var offset = kicks[i];
                transform.position = prevPos + new Vector3(offset.x * _shapeDatabase.cellX, offset.y * _shapeDatabase.cellY, 0f);
                if (IsValidPosition())
                {
                    _rotationIndex = toIndex;
                    lastActionWasRotate = true;
                    lastKickOffset = offset;
                    return;
                }
                
                lastActionWasRotate = false;
                lastKickOffset = Vector2Int.zero;
            }

            transform.rotation = prevRotQ;
            transform.position = prevPos;
        }
        
        private bool IsValidPosition()
        {
            foreach (var sr in shapeRenderers)
            {
                Vector2Int pos = Vector2Int.RoundToInt(sr.transform.position);
              
                if (pos.x < 0 || pos.x >= GridManager.Instance.width) return false;
                if (pos.y < 0) return false;
                if (pos.y >= GridManager.Instance.height) continue;
                if (!GridManager.Instance.IsCellEmpty(pos.x, pos.y)) return false;
            }
            return true;
        }

        private void BuildGhostShape()
        {
            foreach (var t in _ghostRenderers)
                Destroy(t.gameObject);
            
            _ghostRenderers.Clear();

            for (int i = 0; i < shapeRenderers.Count; i++)
            {
                var ghostBlock = Instantiate(ghostBlockPrefab, transform);
                ghostBlock.enabled = false;
                _ghostRenderers.Add(ghostBlock);
            }
        }

        private void UpdateGhostShape()
        {
            if (_ghostRenderers.Count == 0) return;

            if (IsAnyBlockOutOfGrid())
            {
                HideGhost();
                return;
            }

            int steps = ComputeDropSteps();
            if (steps <= 0)
            {
                HideGhost(); 
                return;
            }

            int n = Mathf.Min(_ghostRenderers.Count, shapeRenderers.Count);
            for (int i = 0; i < n; i++)
            {
                var src = shapeRenderers[i];
                var g   = _ghostRenderers[i];

                g.enabled = true;
                g.sprite  = src.sprite;
                g.transform.position = src.transform.position + Vector3.down * (steps * _shapeDatabase.cellY);
            }
            for (int i = n; i < _ghostRenderers.Count; i++)
                _ghostRenderers[i].enabled = false;
        }

        private int ComputeDropSteps()
        {
            int steps = 0;
            Vector3 save = transform.position;

            while (true)
            {
                transform.position += Vector3.down * _shapeDatabase.cellY;
                if (!IsValidPosition())
                {
                    transform.position = save;
                    return steps;
                }
                
                steps++;
            }
        }
        
        private bool IsAnyBlockOutOfGrid()
        {
            int w = GridManager.Instance.width;
            int h = GridManager.Instance.height;

            foreach (var sr in shapeRenderers)
            {
                var pos = Vector2Int.RoundToInt(sr.transform.position);
                if (pos.x < 0 || pos.x >= w) return true;
                if (pos.y < 0) return true;
            }
            return false;
        }
        
        private void HideGhost()
        {
            foreach (var renderer in _ghostRenderers)
                renderer.enabled = false;
        }

        public void SetPreviewMode(bool isPreview)
        {
            if(isPreview) 
                HideGhost();
        }
    }
}