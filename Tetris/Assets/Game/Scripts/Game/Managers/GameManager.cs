using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Scripts.Game.Core;
using Game.Scripts.Game.Database;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Game.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Won,
        GameOver
    }
    
    public class GameManager : MonoSingleton<GameManager>
    {
        [Header("Elements")]
        public Shape currentShape;
        public GameState currentGameState = GameState.MainMenu;
        public static event UnityAction<GameState> OnGameStateChange;

        private readonly List<Shape> _previewQueue = new();
        private ShapeDatabase _shapeDatabase;
        private Coroutine _moveCoroutine;
        private Coroutine _inputCoroutine;
        private Coroutine _keyboardCoroutine;
        private Coroutine _loseDelayCoro;
        private bool _isWin;
        private bool _isGameOver = false;
        private bool _pendingLevelUp = false;
        private Shape _heldShape = null;
        private bool _didHoldThisTurn = false;
        private bool _tSpinPending = false;
        private bool _tSpinMiniPending = false;

        private void Start() => OnGameStateChange?.Invoke(currentGameState);

        public void StartGame()
        {
            if (currentGameState == GameState.Playing) return;
            currentGameState = GameState.Playing;
            OnGameStateChange?.Invoke(currentGameState);
            
            _tSpinPending = false;
            _tSpinMiniPending = false;

            var data = LevelManager.Instance.CurrentLevel;
            UIManager.Instance.SetLevel(data.level);
            UIManager.Instance.SetTargetScore(data.targetScore);
            ScoreManager.Instance.ResetScore();
            GridManager.Instance.Init();
            GridManager.Instance.OnRowsCleared -= HandleRowsCleared;
            GridManager.Instance.OnRowsCleared += HandleRowsCleared;
            currentShape = SpawnManager.Instance.SpawnShape();
            
            _previewQueue.Clear();
            _heldShape = null;
            _didHoldThisTurn = false;
            
            for (int i = UIManager.Instance.holdSlot.childCount - 1; i >= 0; i--)
                Destroy(UIManager.Instance.holdSlot.GetChild(i).gameObject);
            
            foreach (var slot in UIManager.Instance.queueSlots)
                _previewQueue.Add(SpawnManager.Instance.SpawnPreview(slot));

            _shapeDatabase = ShapeDatabase.Instance;
            
            SoundManager.Instance.StopLoop();               
            SoundManager.Instance.PlayLoop("BackgroundMusic", .3f);
            StartGameCoroutine();
        } 
        
        private void GameOver()
        {
            if (_isGameOver) return;
            _isGameOver = true;

            StopGameCoroutine();
            if (currentShape) currentShape.enabled = false;

            SoundManager.Instance.StopLoop();
            SoundManager.Instance.Play("Lose", 1f);

            if (_loseDelayCoro != null) StopCoroutine(_loseDelayCoro);
            _loseDelayCoro = StartCoroutine(ShowLoseAfterDelay(1f));
        }

        private IEnumerator ShowLoseAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            currentGameState = GameState.GameOver;
            OnGameStateChange?.Invoke(currentGameState);

            _loseDelayCoro = null;
        }

        public void RestartGame()
        {
            _isGameOver = false;
            _tSpinPending = false;
            _tSpinMiniPending = false;
            
            StopGameCoroutine();

            if (currentShape) { Destroy(currentShape.gameObject); currentShape = null; }
            foreach (var queue in _previewQueue)
                if (queue) Destroy(queue.gameObject);

            _previewQueue.Clear();
            _heldShape = null;
            _didHoldThisTurn = false;
            
            for (int i = UIManager.Instance.holdSlot.childCount - 1; i >= 0; i--)
                Destroy(UIManager.Instance.holdSlot.GetChild(i).gameObject);

            SpawnManager.Instance.ClearRoundRobinAndSpawnParent();
            GridManager.Instance.ClearGrid();

            currentGameState = GameState.Playing;
            OnGameStateChange?.Invoke(currentGameState);
            
            var data = LevelManager.Instance.CurrentLevel;
            UIManager.Instance.SetLevel(data.level);
            UIManager.Instance.SetTargetScore(data.targetScore);
            ScoreManager.Instance.ResetScore();

            _shapeDatabase = ShapeDatabase.Instance;

            currentShape = SpawnManager.Instance.SpawnShape();
            for (int i = 0; i < UIManager.Instance.queueSlots.Length; i++)
                _previewQueue.Add(SpawnManager.Instance.SpawnPreview(UIManager.Instance.queueSlots[i]));

            SoundManager.Instance.StopLoop();
            SoundManager.Instance.PlayLoop("BackgroundMusic", .3f);

            StartGameCoroutine();
        }

        private void StartGameCoroutine()
        {
            _inputCoroutine = StartCoroutine(InputManager.Instance.PlayerInput(_shapeDatabase.cellX, _shapeDatabase.dropInterval,
                _shapeDatabase.fastDropInterval, 
                _shapeDatabase.deadzonePixels, 
                _shapeDatabase.horizStepInterval));
            
            _keyboardCoroutine = StartCoroutine(InputManager.Instance.PlayerKeyboard(_shapeDatabase.cellX, _shapeDatabase.dropInterval,
                _shapeDatabase.fastDropInterval,
                _shapeDatabase.horizStepInterval));
            
            _moveCoroutine = StartCoroutine(currentShape.MoveVertical(
                _shapeDatabase.dropInterval, _shapeDatabase.cellY));
        }

        private void StopGameCoroutine()
        {
            if (_inputCoroutine   != null) { StopCoroutine(_inputCoroutine);   _inputCoroutine = null; }
            if (_keyboardCoroutine!= null) { StopCoroutine(_keyboardCoroutine);_keyboardCoroutine = null; }
            if (_moveCoroutine    != null) { StopCoroutine(_moveCoroutine);    _moveCoroutine = null; }
        }

        public void TargetScoreReached()
        {
            if (_isWin || currentGameState != GameState.Playing) return;
            _isWin = true;
            
            StopGameCoroutine();
            if(currentShape) currentShape.enabled = false;
            
            StartCoroutine(ShowWinAfterDelay());
        }

        private IEnumerator ShowWinAfterDelay()
        {
            SoundManager.Instance.Play("LevelUp", 1f);
            yield return new WaitForSeconds(1f);
            SoundManager.Instance.StopLoop();
            
            currentGameState = GameState.Won;
            OnGameStateChange?.Invoke(currentGameState);
            
            UIManager.Instance.ShowGamePanel(false);
            UIManager.Instance.ShowWinPanel(true);
        }

        public void NextLevel()
        {
            if (!_isWin) return;
            _isWin = false;
            _tSpinPending = false;
            _tSpinMiniPending = false;

            if (!LevelManager.Instance.NextLevelIndex())
            {
                RestartGame(); 
                return;
            }

            _isGameOver = false;
            StopGameCoroutine();

            if (currentShape) { Destroy(currentShape.gameObject); currentShape = null; }
            
            foreach (var q in _previewQueue) if (q) Destroy(q.gameObject);
            
            _previewQueue.Clear();
            _heldShape = null;
            _didHoldThisTurn = false;
            
            for (int i = UIManager.Instance.holdSlot.childCount - 1; i >= 0; i--)
                Destroy(UIManager.Instance.holdSlot.GetChild(i).gameObject);

            SpawnManager.Instance.ClearRoundRobinAndSpawnParent();
            GridManager.Instance.ClearGrid();

            currentGameState = GameState.Playing;
            OnGameStateChange?.Invoke(currentGameState);

            var data = LevelManager.Instance.CurrentLevel;
            UIManager.Instance.SetLevel(data.level);
            UIManager.Instance.SetTargetScore(data.targetScore);
            ScoreManager.Instance.ResetScore();

            _shapeDatabase = ShapeDatabase.Instance;

            currentShape = SpawnManager.Instance.SpawnShape();
            for (int i = 0; i < UIManager.Instance.queueSlots.Length; i++)
                _previewQueue.Add(SpawnManager.Instance.SpawnPreview(UIManager.Instance.queueSlots[i]));

            UIManager.Instance.ShowWinPanel(false);
            UIManager.Instance.ShowGamePanel(true);

            var p = currentShape.transform.position;
            p.y = Mathf.Min(p.y, GridManager.Instance.height - 2);
            currentShape.transform.position = p;
            currentShape.enabled = true;

            SoundManager.Instance.StopLoop();
            SoundManager.Instance.PlayLoop("BackgroundMusic", .3f);
            
            StartGameCoroutine(); 
        }

        public void BackToMainMenu()
        {
            StopGameCoroutine();

            if (currentShape) { Destroy(currentShape.gameObject); currentShape = null; }
            foreach (var queue in _previewQueue)
                if (queue) Destroy(queue.gameObject);

            _previewQueue.Clear();

            SpawnManager.Instance.ClearRoundRobinAndSpawnParent();
            GridManager.Instance.ClearGrid();
            SoundManager.Instance.StopLoop();
            _isGameOver = false;

            currentGameState = GameState.MainMenu;
            OnGameStateChange?.Invoke(currentGameState);
        }

        public void ShapeCoroutineManagement()
        {
            if (_isGameOver) return;
            if (currentGameState != GameState.Playing) return;
            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }
            
            bool tMini;
            _tSpinPending = ShapeDatabase.Instance.IsTSpin(currentShape, out tMini);
            _tSpinMiniPending = _tSpinPending && tMini;

            bool isOverflow = GridManager.Instance.SettleShape(currentShape);
            if (isOverflow) { GameOver(); return; }

            GridManager.Instance.ClearFullRows();
            if (currentGameState != GameState.Playing) return;
            
            if (_pendingLevelUp)
            {
                _pendingLevelUp = false;
                NextLevel();             
                return;
            }

            var promoted = _previewQueue[0];
            promoted.transform.SetParent(SpawnManager.Instance.spawnPoint, worldPositionStays:false);
            promoted.transform.position = SpawnManager.Instance.spawnPoint.position;
            promoted.enabled = true;               
            promoted.SetPreviewMode(false);        

            currentShape = promoted;

            for (int i = 1; i < _previewQueue.Count; i++)
            {
                var p = _previewQueue[i];
                var slot = UIManager.Instance.queueSlots[i - 1];
                p.transform.SetParent(slot, worldPositionStays:false);
                p.transform.localPosition = p.queueOffset;
            }

            _previewQueue.RemoveAt(0);
            var newPrev = SpawnManager.Instance.SpawnPreview(UIManager.Instance.queueSlots[UIManager.Instance.queueSlots.Length - 1]);
            _previewQueue.Add(newPrev);

            _moveCoroutine = StartCoroutine(currentShape.MoveVertical(_shapeDatabase.dropInterval, _shapeDatabase.cellY));
            _didHoldThisTurn = false; 
        }

        public void HardDrop()
        {
            if (_isGameOver || currentGameState != GameState.Playing) return;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            
            currentShape.HardDrop(_shapeDatabase.cellY);
            ShapeCoroutineManagement();
        }
        
        public void HoldSwap()
        {
            if (currentGameState != GameState.Playing) return;
            if (!currentShape) return;
            if (_didHoldThisTurn) return; 

            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }

            var holdSlot = UIManager.Instance.holdSlot;

            if (!_heldShape)
            {
                currentShape.enabled = false;
                currentShape.SetPreviewMode(true);
                currentShape.transform.SetParent(holdSlot, worldPositionStays:false);
                currentShape.transform.localPosition = currentShape.queueOffset;
                _heldShape = currentShape;

                var promoted = _previewQueue[0];
                promoted.transform.SetParent(SpawnManager.Instance.spawnPoint, worldPositionStays:false);
                promoted.transform.position = SpawnManager.Instance.spawnPoint.position;
                promoted.transform.rotation = Quaternion.identity;
                promoted.enabled = true;
                promoted.SetPreviewMode(false);
                currentShape = promoted;

                for (int i = 1; i < _previewQueue.Count; i++)
                {
                    var p = _previewQueue[i];
                    var slot = UIManager.Instance.queueSlots[i - 1];
                    p.transform.SetParent(slot, worldPositionStays:false);
                    p.transform.localPosition = p.queueOffset;
                }
                _previewQueue.RemoveAt(0);
                var newPrev = SpawnManager.Instance.SpawnPreview(UIManager.Instance.queueSlots[UIManager.Instance.queueSlots.Length - 1]);
                _previewQueue.Add(newPrev);

                var pPos = currentShape.transform.position;
                pPos.y = Mathf.Min(pPos.y, GridManager.Instance.height - 2);
                currentShape.transform.position = pPos;

                _moveCoroutine = StartCoroutine(currentShape.MoveVertical(ShapeDatabase.Instance.dropInterval, ShapeDatabase.Instance.cellY));
                _didHoldThisTurn = true;
                return;
            }

            {
                var oldCurrent = currentShape;
                oldCurrent.enabled = false;
                oldCurrent.SetPreviewMode(true);
                oldCurrent.transform.SetParent(holdSlot, worldPositionStays:false);
                oldCurrent.transform.localPosition = oldCurrent.queueOffset;

                var activate = _heldShape;
                _heldShape = oldCurrent;

                activate.transform.SetParent(SpawnManager.Instance.spawnPoint, worldPositionStays:false);
                activate.transform.position = SpawnManager.Instance.spawnPoint.position;
                activate.transform.rotation = Quaternion.identity;
                activate.enabled = true;
                activate.SetPreviewMode(false);

                currentShape = activate;

                var p = currentShape.transform.position;
                p.y = Mathf.Min(p.y, GridManager.Instance.height - 2);
                currentShape.transform.position = p;

                _moveCoroutine = StartCoroutine(currentShape.MoveVertical(ShapeDatabase.Instance.dropInterval, ShapeDatabase.Instance.cellY));
                _didHoldThisTurn = true;
            }
        }

        private void HandleRowsCleared(int cleared)
        {
            int level = LevelManager.Instance.CurrentLevel.level;

            if (_tSpinPending)
            {
                ScoreManager.Instance.AddTSpinScore(cleared, level);
                _tSpinPending = false;
            }
            else
            {
                ScoreManager.Instance.AddScore(cleared, level);
            }
        }

        public void SetDropInterval(float seconds)
        {
            if (currentGameState != GameState.Playing) return;
            seconds = Mathf.Max(0.001f, seconds);
            if (Mathf.Approximately(seconds, _shapeDatabase.dropInterval)) return;
            
            _shapeDatabase.dropInterval = seconds;
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(currentShape.MoveVertical(_shapeDatabase.dropInterval, _shapeDatabase.cellY));
        }
    }
}