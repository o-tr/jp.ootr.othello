using System;
using jp.ootr.common;
using UdonSharp;
using UnityEngine;

namespace jp.ootr.othello
{
    // Board and player types
    public enum Cell
    {
        Empty = 0,
        Black = 1,
        White = 2
    }

    public enum Player
    {
        Black = 1,
        White = 2
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Othello : BaseClass
    {
        [SerializeField] [UdonSynced] private Cell[] _board;

        [SerializeField] private OthelloCell[] cells;
        [SerializeField] private OthelloUI ui;

        private readonly int[] _directions = { -8, -7, 1, 9, 8, 7, -1, -9 };
        [UdonSynced] private Player _currentPlayer;

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            _board = new Cell[64];

            _board[27] = Cell.White;
            _board[28] = Cell.Black;
            _board[35] = Cell.Black;
            _board[36] = Cell.White;

            _currentPlayer = Player.Black;

            for (var i = 0; i < cells.Length; i++)
                cells[i].Init(this);
            ui.Init(this);

            UpdateCells();
        }

        private int PosToIndex(int row, int col)
        {
            return row * 8 + col;
        }

        private int GetRow(int index)
        {
            return index / 8;
        }

        private int GetCol(int index)
        {
            return index % 8;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < 64;
        }

        private bool WouldCrossEdge(int index, int direction)
        {
            var row = GetRow(index);
            var col = GetCol(index);

            return (row == 0 && (direction == -8 || direction == -7 || direction == -9)) ||
                   (row == 7 && (direction == 8 || direction == 7 || direction == 9)) ||
                   (col == 0 && (direction == -9 || direction == -1 || direction == 7)) ||
                   (col == 7 && (direction == -7 || direction == 1 || direction == 9));
        }

        public bool IsValidMoveIndex(int index, Player player)
        {
            if (_board[index] != Cell.Empty) return false;

            var opponent = player == Player.Black ? Cell.White : Cell.Black;

            foreach (var direction in _directions)
            {
                if (WouldCrossEdge(index, direction)) continue;

                var currIndex = index + direction;
                var foundOpponent = false;

                // 最初に隣のマスが有効かつ相手の石かチェック
                if (!IsValidIndex(currIndex) || WouldCrossEdge(index, direction) || _board[currIndex] != opponent)
                    continue;

                foundOpponent = true;
                currIndex += direction;

                // 連続する相手の石をスキップし、自分の石を見つけたらvalidとする
                while (IsValidIndex(currIndex))
                {
                    // 現在のインデックスから次のインデックスへの移動がエッジを超えるかチェック
                    var currRow = GetRow(currIndex - direction);
                    var currCol = GetCol(currIndex - direction);
                    var nextRow = GetRow(currIndex);
                    var nextCol = GetCol(currIndex);

                    // 行または列が不連続になるようなら（エッジを超える）、ループを抜ける
                    if ((direction == 1 || direction == -1) && currRow != nextRow)
                        break;
                    if ((direction == 8 || direction == -8) && currCol != nextCol)
                        break;
                    if ((direction == 9 || direction == -9 || direction == 7 || direction == -7) &&
                        (Math.Abs(currRow - nextRow) != 1 || Math.Abs(currCol - nextCol) != 1))
                        break;

                    if (_board[currIndex] == opponent)
                        currIndex += direction;
                    else if (_board[currIndex] == (Cell)player)
                        return true; // 挟んでいる自分の石を見つけた
                    else
                        break; // 空きマスがあればここで終了
                }
            }

            return false;
        }

        public int[] GetValidMoves()
        {
            var tempValidMoves = new int[64];
            var validMoveCount = 0;
            var potentialMoves = new bool[64];

            for (var index = 0; index < 64; index++)
                if (_board[index] != Cell.Empty)
                    foreach (var direction in _directions)
                    {
                        var adjacentIndex = index + direction;

                        if (IsValidIndex(adjacentIndex) &&
                            !WouldCrossEdge(index, direction) &&
                            _board[adjacentIndex] == Cell.Empty)
                            potentialMoves[adjacentIndex] = true;
                    }


            for (var index = 0; index < 64; index++)
                if (potentialMoves[index] && IsValidMoveIndex(index, _currentPlayer))
                {
                    tempValidMoves[validMoveCount] = index;
                    validMoveCount++;
                }

            var validMoves = new int[validMoveCount];
            for (var i = 0; i < validMoveCount; i++) validMoves[i] = tempValidMoves[i];

            return validMoves;
        }

        private void MakeMoveIndex(int index, Player player, out int[] flippedStones, out int flippedCount)
        {
            _board[index] = (Cell)player;
            var opponent = player == Player.Black ? Cell.White : Cell.Black;

            var allFlippedStones = new int[63];
            flippedCount = 0;

            // Check all 8 directions
            foreach (var direction in _directions)
            {
                if (WouldCrossEdge(index, direction)) continue;

                var currIndex = index + direction;
                var stonesToFlip = new int[64];
                var flipCount = 0;

                // 最初のマスが有効かつ対戦相手の石かチェック
                if (!IsValidIndex(currIndex) || _board[currIndex] != opponent)
                    continue;

                stonesToFlip[flipCount] = currIndex;
                flipCount++;
                currIndex += direction;

                // 連続する石を調べる
                while (IsValidIndex(currIndex))
                {
                    // エッジをまたぐかチェック
                    var prevRow = GetRow(currIndex - direction);
                    var prevCol = GetCol(currIndex - direction);
                    var currRow = GetRow(currIndex);
                    var currCol = GetCol(currIndex);

                    // 行または列が不連続ならループを終了
                    if ((direction == 1 || direction == -1) && prevRow != currRow)
                        break;
                    if ((direction == 8 || direction == -8) && prevCol != currCol)
                        break;
                    if ((direction == 9 || direction == -9 || direction == 7 || direction == -7) &&
                        (Math.Abs(prevRow - currRow) != 1 || Math.Abs(prevCol - currCol) != 1))
                        break;

                    if (_board[currIndex] == opponent)
                    {
                        stonesToFlip[flipCount] = currIndex;
                        flipCount++;
                        currIndex += direction;
                    }
                    else if (_board[currIndex] == (Cell)player)
                    {
                        // 自分の石が見つかった場合、間の石をひっくり返す
                        if (flipCount > 0)
                            for (var i = 0; i < flipCount; i++)
                            {
                                var flipIndex = stonesToFlip[i];
                                _board[flipIndex] = (Cell)player;

                                allFlippedStones[flippedCount] = flipIndex;
                                flippedCount++;
                            }

                        break;
                    }
                    else
                    {
                        break; // 空きマスの場合終了
                    }
                }
            }

            // 返された石の配列を返す
            flippedStones = allFlippedStones;
        }

        public bool PutStone(int row, int col)
        {
            return PutStone(row, col, out var _1, out var _2);
        }

        public bool PutStone(int row, int col, out int[] flippedIndices, out int flippedCount)
        {
            flippedCount = 0;
            flippedIndices = new int[0];
            var index = PosToIndex(row, col);

            if (!IsValidMoveIndex(index, _currentPlayer)) return false;

            MakeMoveIndex(index, _currentPlayer, out flippedIndices, out flippedCount);

            for (var i = 0; i < flippedCount; i++) cells[flippedIndices[i]].SetCell((CellType)_currentPlayer);

            SwitchPlayer();

            Sync();

            return true;
        }

        public override void _OnDeserialization()
        {
            base._OnDeserialization();
            UpdateCells();
            if (IsGameOver()) ui.OnGameOver(GetWinner() == "Black");
        }

        public int CountStones(Player player)
        {
            var count = 0;
            for (var i = 0; i < _board.Length; i++)
                if (_board[i] == (Cell)player)
                    count++;

            return count;
        }

        private void SwitchPlayer()
        {
            _currentPlayer = _currentPlayer == Player.Black ? Player.White : Player.Black;
        }

        public bool IsGameOver()
        {
            var originalPlayer = _currentPlayer;

            _currentPlayer = Player.Black;
            var blackHasMove = GetValidMoves().Length > 0;

            _currentPlayer = Player.White;
            var whiteHasMove = GetValidMoves().Length > 0;

            _currentPlayer = originalPlayer;

            return !blackHasMove && !whiteHasMove;
        }

        public string GetWinner()
        {
            var blackCount = CountStones(Player.Black);
            var whiteCount = CountStones(Player.White);

            if (blackCount > whiteCount) return "Black";
            if (whiteCount > blackCount) return "White";
            return "Draw";
        }

        public Player GetCurrentPlayer()
        {
            return _currentPlayer;
        }

        public Cell[] GetBoard()
        {
            return _board;
        }

        public void UpdateCells()
        {
            var board = GetBoard();
            for (var i = 0; i < board.Length; i++)
                cells[i].SetCell((CellType)board[i]);
            var validMoves = GetValidMoves();

            if (validMoves.Length == 0)
            {
                SwitchPlayer();
                var otherValidMoves = GetValidMoves();
                if (otherValidMoves.Length == 0)
                {
                    Debug.Log("Game over");
                    return;
                }

                validMoves = otherValidMoves;
            }

            for (var i = 0; i < validMoves.Length; i++)
                cells[validMoves[i]].SetCell((CellType)((int)GetCurrentPlayer() + 2));

            ui.UpdateIngameUI(CountStones(Player.Black), CountStones(Player.White), GetCurrentPlayer() == Player.Black);
        }

        private bool Contains(int[] array, int value)
        {
            for (var i = 0; i < array.Length; i++)
                if (array[i] == value)
                    return true;
            return false;
        }
    }
}
