using UnityEngine;

namespace Game.Scripts.Game.Core
{
    public class Tile : MonoBehaviour
    {
        public Vector2Int coord;
        public bool isOccupied;
        public SpriteRenderer tetrominoBlock;

        public void Init(Vector2Int coordinate)
        {
            coord = coordinate;
            isOccupied = false;
            tetrominoBlock = null;
        }
    }
}