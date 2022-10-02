using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace x0.ld51
{
    public class Stage : MonoBehaviour
    {
        public const int BlockSize = 48;
        public const int StageWidth = 11;
        public const int StageHeight = 13;

        private static readonly int GlowProp = Shader.PropertyToID("_Glow");

        public GameObject BlockTemplate;
        public GameObject BombTemplate;
        public GameObject MagnetTemplate;
        public GameObject PushArrowTemplate;
        public Transform NextShapeContainer;
        public TMP_Text TimerLabel;
        public TMP_Text ScoreLabel;
        public GameObject StartButton;
        public GameObject RestartButton;
        public VolumeControl VolumeControl;

        public Transform[,] Grid { get; private set; } = new Transform[0, 0];
        public Shape Shape { get; private set; }
        public IReadOnlyList<Transform> Blocks => _blocks;

        public int Ceiling {
            get {
                var height = 0;
                for (int y = 0, n = 0; y < StageHeight; y++, n = 0) {
                    for (var x = 0; x < StageWidth; x++) {
                        if (Grid[x, y] != null) {
                            n = 1;
                            break;
                        }
                    }
                    if (n == 0) {
                        continue;
                    }
                    height++;
                }
                return height;
            }
        }

        private bool _freeze;
        private bool _dirty;
        private bool _drop;
        private float _nudgeTime;
        private Shape _nextShape;
        private Vector2 _nextShapeContainerSz;
        private readonly List<Transform> _nextShapeBlocks = new(4);
        private readonly List<Transform> _blocks = new(4);
        private float _gravityInterval;
        private int _ticks;
        private int _score;
        private int _speedPoint;

        /*
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.worldToLocalMatrix;
            for (int y = 0; y < StageHeight; y++) {
                for (int x = 0; x < StageWidth; x++) {
                    if (Grid[x, y] != null) {
                        Gizmos.DrawSphere(new Vector3(x, y, 10) * BlockSize, 20);
                    }
                }
            }
        }
        */

        public void Launch()
        {
            StartButton.SetActive(false);
            RestartButton.SetActive(false);

            if (Grid.Length == 0) VolumeControl.Play();

            foreach (var block in Grid) if (block != null) Destroy(block.gameObject);
            foreach (var block in _blocks) Destroy(block.gameObject);
            foreach (var block in _nextShapeBlocks) Destroy(block.gameObject);
            _blocks.Clear();
            _nextShapeBlocks.Clear();
            _nextShape = null;

            Grid = new Transform[StageWidth, StageHeight];
            Shape = null;

            _freeze = false;
            _dirty = true;
            _gravityInterval = 1f;
            _ticks = 0;
            _score = 0;
            _speedPoint = 60;

            PullNextShape();
            PullNextShape();
            StartCoroutine(StartGravity());
            StartCoroutine(StartCountdown());

            VolumeControl.Source.GetComponent<AudioHighPassFilter>().enabled = false;
        }

        private void Awake()
        {
            _nextShapeContainerSz = NextShapeContainer.GetComponent<SpriteRenderer>().size - Vector2.one * 16;
            var go = new GameObject();
            go.transform.SetParent(NextShapeContainer.parent);
            NextShapeContainer = go.transform;
        }

        private void Update()
        {
            if (_gravityInterval <= 0 || _freeze) {
                return;
            }

            if (!TryDrop()) {
                var cw  = Input.GetKeyUp(KeyCode.X);
                var ccw = Input.GetKeyUp(KeyCode.Z);
                if (cw ^ ccw) {
                    _ = cw ? Shape.RotateCw() : Shape.RotateCcw();

                    var reason = AssertShape();
                    if (reason != InvalidReason.None) {
                        var offset = cw ? Vector2Int.right : Vector2Int.left;
                        Shape.Origin += offset;

                        reason = AssertShape();
                        if (reason != InvalidReason.None) {
                            Shape.Origin -= offset * 2;

                            reason = AssertShape();
                            if (reason != InvalidReason.None) {
                                Shape.Origin += offset;
                            }
                        }
                    }

                    if (reason == InvalidReason.None) {
                        _dirty = true;
                    }
                }

                var time = Time.time;
                if (time - _nudgeTime > .1f) {
                    var left  = Input.GetKey(KeyCode.LeftArrow);
                    var right = Input.GetKey(KeyCode.RightArrow);
                    if (left ^ right) {
                        var dirty = true;

                        Shape.Origin += left ? Vector2Int.left : Vector2Int.right;
                        foreach (var pos in Shape) {
                            if (AssertSpace(pos) != InvalidReason.None) {
                                Shape.Origin -= left ? Vector2Int.left : Vector2Int.right;
                                dirty = false;
                                break;
                            }
                        }

                        if (dirty) {
                            _nudgeTime = time;
                            _dirty = true;
                        }
                    }
                }
            }

            if (_dirty) {
                _dirty = false;

                var index = 0;
                foreach (var pos in Shape) {
                    _blocks[index++].localPosition = (Vector2) (pos * BlockSize);
                }
            }
        }

        private InvalidReason AssertShape(bool checkCeiling = false)
        {
            var result = InvalidReason.None;
            foreach (var pos in Shape) {
                var reason = AssertSpace(pos, checkCeiling);
                if (reason == InvalidReason.Ceiling) {
                    return reason;
                }
                if (reason != InvalidReason.None) {
                    result = reason;
                }
            }
            return result;
        }

        private InvalidReason AssertSpace(Vector2Int pos, bool checkCeiling = false)
        {
            if (pos.x is < 0 or >= StageWidth) {
                return InvalidReason.Side;
            }
            if (pos.y < 0) {
                return InvalidReason.Floor;
            }
            if (pos.y >= StageHeight) {
                return checkCeiling ? InvalidReason.Ceiling : InvalidReason.None;
            }
            return Grid[pos.x, pos.y] == null ? InvalidReason.None : InvalidReason.Floor;
        }

        private IEnumerator StartCountdown()
        {
            while (_gravityInterval > 0) {
                yield return new WaitForSeconds(1f);

                while (_freeze) {
                    yield return new WaitForSeconds(.1f);
                }

                TimerLabel.text = (10 - ++_ticks % 10).ToString();
                if (_ticks % 10 == 0) {
                    _ = ApplyEffect();
                }
            }
        }

        private IEnumerator StartGravity()
        {
            while (_gravityInterval > 0) {
                yield return new WaitForSeconds(_gravityInterval);
                if (!_drop && !_freeze) _ = ApplyGravity();
            }
        }

        private IEnumerator StartDrop()
        {
            while (_drop && _gravityInterval > 0) {
                if (!_freeze) _ = ApplyGravity();
                yield return new WaitForSeconds(.05f);
            }
        }

        private bool TryDrop()
        {
            if (Input.GetKey(KeyCode.DownArrow)) {
                if (!_drop) {
                    _drop = true;
                    StartCoroutine(StartDrop());
                }
                return true;
            }
            return _drop = false;
        }

        private async Task ApplyGravity()
        {
            Shape.Origin += Vector2Int.down;

            switch (AssertShape()) {
                case InvalidReason.Floor:
                    Shape.Origin -= Vector2Int.down;

                    if (AssertShape(true) == InvalidReason.Ceiling) {
                        _gravityInterval = -1f;
                        RestartButton.SetActive(true);
                        VolumeControl.Source.GetComponent<AudioHighPassFilter>().enabled = true;
                        break;
                    }

                    var index = 0;
                    foreach (var pos in Shape) {
                        Grid[pos.x, pos.y] = _blocks[index++];
                    }

                    _freeze = true;
                    while (--index >= 0) {
                        var ipa = _blocks[index].GetComponentInChildren<IPlacementAware>();
                        if (ipa != null) {
                            await ipa.OnPlacement(this, Shape[index] + Shape.Origin);
                        }
                    }

                    var blocks = new List<SpriteRenderer>();
                    var rows = new List<int>();
                    var row = new List<Transform>(StageWidth);
                    for (int y = 0; y < StageHeight; y++) {
                        row.Clear();

                        for (int x = 0; x < StageWidth; x++) {
                            var tf = Grid[x, y];
                            if (tf == null) {
                                row.Clear();
                                break;
                            }
                            row.Add(tf);
                        }

                        if (row.Count == StageWidth) {
                            foreach (var tf in row) {
                                blocks.Add(tf.GetComponent<SpriteRenderer>());
                            }
                            rows.Add(y);
                        }
                    }

                    if (blocks.Count > 0) {
                        await ClearRows(blocks, rows);
                    }

                    _freeze = false;

                    if (_gravityInterval > 0) {
                        PullNextShape();
                    }
                    break;
                case InvalidReason.Ceiling:
                    _gravityInterval = -1f;
                    RestartButton.SetActive(true);
                    VolumeControl.Source.GetComponent<AudioHighPassFilter>().enabled = true;
                    break;
            }

            _dirty = true;
        }

        private Task ClearRows(List<SpriteRenderer> blocks, List<int> rows)
        {
            var tcs = new TaskCompletionSource<bool>();
            var time = Time.time;

            StartCoroutine(Flicker());

            IEnumerator Flicker()
            {
                float t;
                do {
                    t = Time.time - time;

                    var mpb = new MaterialPropertyBlock();
                    mpb.SetFloat(GlowProp, .5f - Mathf.Cos(t * t * t * 7));

                    foreach (var spriteRenderer in blocks) {
                        spriteRenderer.SetPropertyBlock(mpb);
                    }

                    yield return new WaitForEndOfFrame();
                } while (t < 1.2f);

                foreach (var spriteRenderer in blocks) {
                    Destroy(spriteRenderer.gameObject);
                }

                Transform block;
                for (int n = 0, y = rows[n]; y < StageHeight; y++) {
                    for (int x = 0; x < StageWidth; x++) {
                        Grid[x, y] = null;
                    }

                    var nextRow = ++n < rows.Count ? rows[n] : StageHeight;
                    for (y++; y < nextRow; y++) {
                        for (int x = 0; x < StageWidth; x++) {
                            Grid[x, y - n] = block = Grid[x, y];
                            Grid[x, y] = null;

                            if (block != null) {
                                block.localPosition = new Vector3(x * BlockSize, (y - n) * BlockSize);
                            }
                        }
                    }

                    y--;
                }

                _score += 10 * rows.Count * rows.Count;
                if (_score >= _speedPoint) {
                    _speedPoint = (int) (_speedPoint * 1.8f);
                    _gravityInterval *= .8f;
                }
                ScoreLabel.text = _score.ToString();

                tcs.SetResult(true);
            }

            return tcs.Task;
        }

        private void PullNextShape()
        {
            if (_nextShape != null) {
                SpawnNextShape();
            }

            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);

            _nextShape = Shape.GetRandom();
            for (int i = 0, n = Random.Range(0, 3), cw = Random.Range(0, 2); i < n; i++) {
                _ = cw < 1 ? _nextShape.RotateCw() : _nextShape.RotateCcw();
            }

            var color = Random.ColorHSV(0, 1, .8f, 1, .8f, 1);
            foreach (var pos in _nextShape) {
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.y > max.y) max.y = pos.y;

                var block = Instantiate(BlockTemplate, NextShapeContainer);
                block.transform.localPosition = (Vector2) (pos * BlockSize);
                block.GetComponent<SpriteRenderer>().color = color;

                _nextShapeBlocks.Add(block.transform);
                // color = Color.white;
            }

            foreach (var block in _nextShapeBlocks) {
                block.localPosition += new Vector3(.5f - min.x, .5f - min.y) * BlockSize;
                block.gameObject.SetActive(true);
            }

            var width  = (max.x - min.x + 1) * BlockSize;
            var height = (max.y - min.y + 1) * BlockSize;

            NextShapeContainer.localPosition = new Vector3(
                (_nextShapeContainerSz.x - width) * .5f - BlockSize * .5f,
                (_nextShapeContainerSz.y - height) * .5f - BlockSize * .5f
            );

            _dirty = true;
        }

        private void SpawnNextShape()
        {
            Shape = _nextShape;
            Shape.Origin = new Vector2Int(StageWidth / 2, StageHeight);

            _blocks.Clear();
            foreach (var block in _nextShapeBlocks) {
                block.SetParent(transform);
                block.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                _blocks.Add(block.transform);
            }

            _nextShapeBlocks.Clear();
        }

        private async Task ApplyEffect()
        {
            _freeze = true;

            bool result;
            do {
                result = await Effects.GetRandom().Apply(this);
            } while (!result);

            _freeze = false;
        }

        private enum InvalidReason
        {
            None, Floor, Ceiling, Side
        }
    }
}

