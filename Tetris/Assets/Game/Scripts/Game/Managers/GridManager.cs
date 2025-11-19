using System.Collections.Generic;
using Game.Scripts.Game.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Game.Managers
{
    public class GridManager : MonoSingleton<GridManager>
    {
        [Header("Elements")] 
        public Tile tilePrefab;

        [Header("Grid Settings")] 
        public int height;
        public int width;

        public event UnityAction<int> OnRowsCleared; 

        private Tile[,] _grid;
        
        public void Init()
        {
            _grid = new Tile[width, height];
            GenerateGrid();
        }

        private void GenerateGrid()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3(x, y, 0);
                    Tile tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                    tile.name = $"Tile {x},{y}";
                    tile.Init(new Vector2Int(x, y));
                    _grid[x, y] = tile;
                }
            }
        }

        public void ClearGrid()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = _grid[x, y];
                    if (!tile) continue;

                    if (tile.isOccupied && tile.tetrominoBlock)
                        Destroy(tile.tetrominoBlock.gameObject);
                    
                    tile.isOccupied = false;
                    tile.tetrominoBlock = null;
                }
            }
        }
        
        public bool SettleShape(Shape shape)
        {
            bool overflow = false;
            
            ParticleManager.Instance.SettleFX(shape, "glowingSquare");
            
            foreach (var sr in shape.shapeRenderers)
            {
                Vector2Int pos = Vector2Int.RoundToInt(sr.transform.position);
                if (pos.y >= height) overflow = true;
                if (!IsWithinBoards(pos.x, pos.y)) continue;

                var tile = _grid[pos.x, pos.y];
                tile.isOccupied = true;
                tile.tetrominoBlock = sr;             
                sr.transform.SetParent(transform); 
            }

            return overflow;
        }

        public void ClearFullRows()
        {
            var fullRows = new List<int>();
            for (int y = 0; y < height; y++)
             if(IsRowFull(y))  fullRows.Add(y);

            if (fullRows.Count == 0) return;
            
            OnRowsCleared?.Invoke(fullRows.Count);

            for (int i = 0; i < fullRows.Count; i++)
            {
                int y = fullRows[i];
                for (int x = 0; x < width; x++)
                {
                    var tile = _grid[x, y];
                    ParticleManager.Instance.Play("glowingSquare", tile.transform.position);
                }
            }

            fullRows.Sort();

            for (int i = 0; i < fullRows.Count; i++)
            {
                int y = fullRows[i];
                for (int x = 0; x < width; x++)
                {
                    var tile =  _grid[x, y];
                    if(tile.isOccupied && tile.tetrominoBlock)
                        Destroy(tile.tetrominoBlock.gameObject);
                    
                    tile.isOccupied = false;
                    tile.tetrominoBlock = null;
                }
                
                ShiftRows(y);
                
                for (int j = i + 1; j < fullRows.Count; j++)
                    fullRows[j]--;
            }
        }

        private bool IsRowFull(int y)
        {
            for (int x = 0; x < width; x++)
                if (!_grid[x, y].isOccupied) return false;

            return true;
        }

        private void ShiftRows(int clearedY)
        {
            for (int y = clearedY + 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var from = _grid[x, y];
                    if (!from.isOccupied) continue;
                    
                    var to = _grid[x, y - 1];
                    if (to.isOccupied) continue;

                    to.isOccupied = true;
                    to.tetrominoBlock = from.tetrominoBlock;
                    to.tetrominoBlock.transform.position = new Vector3(x, y - 1, 0);
                    
                    from.isOccupied = false;
                    from.tetrominoBlock = null;
                }
            }
        }
        
        private bool IsWithinBoards(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height);

        public bool IsCellEmpty(int x, int y)
        {
            if (!IsWithinBoards(x, y)) return false;
            return !_grid[x, y].isOccupied;
        }
    }
}
