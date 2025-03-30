using UdonSharp;
using UnityEngine;

namespace jp.ootr.othello
{
    public enum CellType
    {
        Empty,
        Black,
        White,
        PlaceableBlack,
        PlaceableWhite
    }

    public class OthelloCell : UdonSharpBehaviour
    {
        [SerializeField] private Collider _collider;
        [SerializeField] private GameObject stone;

        private CellType _cell = CellType.Empty;
        private int _col;
        private int _index;

        private Othello _othelloCore;
        private int _row;

        public void Start()
        {
            var parentName = transform.parent.name;
            _row = parentName[parentName.Length - 1] - '0';
            _col = name[name.Length - 1] - '0';
            _index = _row * 8 + _col;
        }

        public void Init(Othello othelloCore)
        {
            _othelloCore = othelloCore;
        }

        public void SetCell(CellType cell)
        {
            _cell = cell;
            stone.SetActive(cell != CellType.Empty);

            var placeable = cell == CellType.PlaceableBlack || cell == CellType.PlaceableWhite;

            stone.transform.rotation = new Quaternion(90, 0, 0,
                cell == CellType.Black || cell == CellType.PlaceableBlack ? -90 : 90);
            stone.transform.localScale = placeable ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(1, 1, 1);

            _collider.enabled = placeable;
        }

        public override void Interact()
        {
            base.Interact();
            _othelloCore.PutStone(_row, _col);
        }
    }
}
