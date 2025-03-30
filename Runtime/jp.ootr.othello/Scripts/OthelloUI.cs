using TMPro;
using UdonSharp;
using UnityEngine;

namespace jp.ootr.othello
{
    public class OthelloUI : UdonSharpBehaviour
    {
        [SerializeField] private TextMeshProUGUI _blackStoneCount;
        [SerializeField] private TextMeshProUGUI _whiteStoneCount;
        [SerializeField] private TextMeshProUGUI _turnText;
        [SerializeField] private TextMeshProUGUI _gameOverText;

        [SerializeField] private GameObject _inGameUI;
        [SerializeField] private GameObject _gameOverUI;
        private Othello _othelloCore;

        public void Init(Othello othelloCore)
        {
            _othelloCore = othelloCore;
            SetActiveUI(true);
        }

        public void OnResetClick()
        {
            _othelloCore.InitializeGame();
        }

        public void UpdateIngameUI(int black, int white, bool isBlackTurn)
        {
            _blackStoneCount.text = black.ToString();
            _whiteStoneCount.text = white.ToString();
            _turnText.text = isBlackTurn ? "Black Turn" : "White Turn";
        }

        public void OnGameOver(bool isBlackWin)
        {
            _gameOverText.text = isBlackWin ? "Black Win!" : "White Win!";
            SetActiveUI(false);
        }

        private void SetActiveUI(bool inGame)
        {
            _inGameUI.SetActive(inGame);
            _gameOverUI.SetActive(!inGame);
        }
    }
}
