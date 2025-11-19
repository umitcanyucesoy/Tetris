using System.Collections.Generic;
using Game.Scripts.Game.Core;
using Game.Scripts.Game.Managers;
using UnityEngine;

namespace Game.Scripts.Game.Database
{
    public class ShapeDatabase : MonoSingleton<ShapeDatabase>
    {
        [Header("Shape Move Settings")]
        public float cellX = 1f;
        public float cellY = 1f;
        public float dropInterval = .3f;
        public float fastDropInterval = .1f;
        public float deadzonePixels = 5f;
        public float horizStepInterval = 0.06f;

        private int[] GetFrontCornerIndices(int rotationIndex)
        {
            switch (Mod4(rotationIndex))
            {
                case 0: return new[] { 0, 1 };
                case 1: return new[] { 1, 3 };
                case 2: return new[] { 2, 3 };
                default: return new[] { 0, 2 };
            }
        }

        private bool IsCellSolid(Vector2Int cell)
        {
            return !GridManager.Instance.IsCellEmpty(cell.x, cell.y);
        }

        public bool IsTSpin(Shape shape, out bool isMini)
        {
            isMini = false;
            if (!shape ||shape.shapeType != Shape.ShapeType.T) return false;
            if (!shape.lastActionWasRotate) return false;

            var pivot = shape.PivotGridCoord;
            Vector2Int[] corners =
            {
                new(pivot.x - 1, pivot.y + 1),
                new(pivot.x + 1, pivot.y + 1),
                new(pivot.x - 1, pivot.y - 1),
                new(pivot.x + 1, pivot.y - 1),
            };
            
            int solid = 0; for (int i = 0; i < 4; i++) if (IsCellSolid(corners[i])) solid++;
            if (solid < 3) return false; 

            int[] front = GetFrontCornerIndices(shape._rotationIndex);
            int frontSolid = 0; foreach (int idx in front) if (IsCellSolid(corners[idx])) frontSolid++;

            bool usedKick = (shape.lastKickOffset != Vector2Int.zero);

            isMini = (frontSolid == 0) || (usedKick && frontSolid == 1);
            return true;
        }
        
        private readonly Vector2Int[][] JLSTZ_KICKS =
        {
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,+1), new Vector2Int(0,-2), new Vector2Int(-1,-2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(+1,-1), new Vector2Int(0,+2), new Vector2Int(+1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(+1,-1), new Vector2Int(0,+2), new Vector2Int(+1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,+1), new Vector2Int(0,-2), new Vector2Int(-1,-2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(+1,+1), new Vector2Int(0,-2), new Vector2Int(+1,-2) },
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,-1), new Vector2Int(0,+2), new Vector2Int(-1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,-1), new Vector2Int(0,+2), new Vector2Int(-1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(+1,+1), new Vector2Int(0,-2), new Vector2Int(+1,-2) },
        };

        private readonly Vector2Int[][] I_KICKS = 
        {
            new [] { new Vector2Int(0,0), new Vector2Int(-2,0), new Vector2Int(+1,0), new Vector2Int(-2,-1), new Vector2Int(+1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+2,0), new Vector2Int(-1,0), new Vector2Int(+2,+1), new Vector2Int(-1,-2) },
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(+2,0), new Vector2Int(-1,+2), new Vector2Int(+2,-1) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(-2,0), new Vector2Int(+1,-2), new Vector2Int(-2,+1) },
            new [] { new Vector2Int(0,0), new Vector2Int(+2,0), new Vector2Int(-1,0), new Vector2Int(+2,+1), new Vector2Int(-1,-2) },
            new [] { new Vector2Int(0,0), new Vector2Int(-2,0), new Vector2Int(+1,0), new Vector2Int(-2,-1), new Vector2Int(+1,+2) },
            new [] { new Vector2Int(0,0), new Vector2Int(+1,0), new Vector2Int(-2,0), new Vector2Int(+1,-2), new Vector2Int(-2,+1) },
            new [] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(+2,0), new Vector2Int(-1,+2), new Vector2Int(+2,-1) },
        };
        
        private int MapIdx(int from, int to)
        {
            if (from == 0 && to == 1) return 0;
            if (from == 1 && to == 0) return 1;
            if (from == 1 && to == 2) return 2;
            if (from == 2 && to == 1) return 3;
            if (from == 2 && to == 3) return 4;
            if (from == 3 && to == 2) return 5;
            if (from == 3 && to == 0) return 6;
            return 7;
        }
        
        public int Mod4(int v) => (v % 4 + 4) % 4;
        
        public Vector2Int[] GetKickTests(Shape.ShapeType type, int fromIdx, int toIdx)
        {
            int map = MapIdx(fromIdx, toIdx);
            if (type == Shape.ShapeType.I) return I_KICKS[map];
            return JLSTZ_KICKS[map];
        }
    }
}