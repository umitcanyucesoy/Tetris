using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Game.Managers
{
    public class UIManager : MonoSingleton<UIManager>
    {
        [Header("Panels")] 
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        [Header("Queue Preview")] 
        public Transform[] queueSlots = new Transform[3];
        
        [Header("Hold")]
        public Transform holdSlot;

        [Header("Level Elements")] 
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;

        private CanvasGroup _mainCG, _gameCG, _settingsCG;
        private Vector2 _mainMenuDefaultPos;
        private Vector2 _gamePanelDefaultPos;
        private Coroutine _tSpinFlashCoro;

        private void Start()
        {
            GameManager.OnGameStateChange += Apply;
            
            _mainCG = mainMenuPanel.GetComponent<CanvasGroup>();
            _gameCG = gamePanel.GetComponent<CanvasGroup>();
            _settingsCG = settingsPanel ? (settingsPanel.GetComponent<CanvasGroup>()) : null;
            
            _mainMenuDefaultPos = mainMenuPanel.GetComponent<RectTransform>().anchoredPosition;
            _gamePanelDefaultPos = gamePanel.GetComponent<RectTransform>().anchoredPosition;
        }

        private void Apply(GameState state)
        {
            mainMenuPanel.SetActive(state == GameState.MainMenu);
            gamePanel.SetActive(state == GameState.Playing);
            if (winPanel) winPanel.SetActive(state == GameState.Won);
            if (winPanel) losePanel.SetActive(state == GameState.GameOver);
            infoPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        public void SetLevel(int level) => levelText.text = $"Level: {level}";
        public void SetScore(int score) => scoreText.text = $"Score: {score}";
        public void SetTargetScore(int goal) => targetScoreText.text = $"Target: {goal}";
        public void ShowGamePanel(bool on) => gamePanel.SetActive(on); 
        public void ShowWinPanel(bool on) => winPanel.SetActive(on); 
        public void OnClickPlaySlide() => PlayGameSlideEffect();
        public void OnClickSettings() => settingsPanel.SetActive(true);
        public void OnClickBackFromSettings() => settingsPanel.SetActive(false);
        
        public void OnClickQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        
        public void OnClickInfo()
        {
            infoPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
        }

        public void OnClickBackFromInfo()
        {
            infoPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }

        public void OnClickRetry()
        {
            settingsPanel.SetActive(false);
            gamePanel.SetActive(true);
            
            GameManager.Instance.RestartGame();
        }

        public void OnClickBackToMainMenu()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            
            var mRt = mainMenuPanel.GetComponent<RectTransform>();
            var gRt = gamePanel.GetComponent<RectTransform>();
            DOTween.Kill(mRt);
            DOTween.Kill(gRt);
            DOTween.Kill(_mainCG);
            DOTween.Kill(_gameCG);

            gamePanel.SetActive(false);
            _gameCG.alpha = 0f;
            gRt.anchoredPosition = _gamePanelDefaultPos;

            mainMenuPanel.SetActive(true);
            _mainCG.alpha = 1f;
            _mainCG.interactable = true;
            _mainCG.blocksRaycasts = true;
            mRt.anchoredPosition = _mainMenuDefaultPos;
            
            GameManager.Instance.BackToMainMenu();
        }
        
        private void PlayGameSlideEffect()
        {
            var mRt = mainMenuPanel.GetComponent<RectTransform>();
            var gRt = gamePanel.GetComponent<RectTransform>();

            var mCg = mainMenuPanel.GetComponent<CanvasGroup>();
            var gCg = gamePanel.GetComponent<CanvasGroup>();

            mainMenuPanel.SetActive(true);
            gamePanel.SetActive(true);    

            mCg.interactable = false; mCg.blocksRaycasts = false;
            gCg.interactable = false; gCg.blocksRaycasts = false;

            Vector2 mStart = mRt.anchoredPosition;             
            Vector2 mOff   = mStart + new Vector2(-200f, 0f);    
            Vector2 gEnd   = gRt.anchoredPosition;               
            Vector2 gStart = gEnd + new Vector2(0f, -800f);      

            gRt.anchoredPosition = gStart;
            gCg.alpha = 0f;

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(mRt.DOAnchorPos(mOff, 0.25f).SetEase(Ease.InSine))
                .Join(mCg.DOFade(0f, 0.20f))
                .Insert(0.10f, gRt.DOAnchorPos(gEnd, 0.35f).SetEase(Ease.OutCubic))
                .Insert(0.10f, gCg.DOFade(1f, 0.30f))
                .OnComplete(() =>
                {
                    mCg.alpha = 0f; mCg.interactable = false; mCg.blocksRaycasts = false;
                    gCg.interactable = true; gCg.blocksRaycasts = true;

                    GameManager.Instance.StartGame();
                });
        }
    }
}