using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace x0.ld51
{
    public class Shape : IEnumerable<Vector2Int>
    {
        private static readonly Shape[] Shapes = {
            // I-shape
            new(new Vector2Int[] { new(0, 1), new(0, 0), new(0, 2), new(0, 3) }),
            // S-shape
            new(new Vector2Int[] { new(0, 0), new(1, 0), new(0, 1), new(-1, 1) }),
            new(new Vector2Int[] { new(0, 0), new(-1, 0), new(0, 1), new(1, 1) }),
            // L-shape
            new(new Vector2Int[] { new(0, 0), new(0, 1), new(0, 2), new(1, 0) }, new(0, 1), new(1, 0)),
            new(new Vector2Int[] { new(0, 0), new(0, 1), new(0, 2), new(-1, 0) }, new(-1, 0), new(0, 1)),
            // Cube
            new(new Vector2Int[] { new(0, 0), new(0, 1), new(1, 0), new(1, 1) }, new(0, 1), new(1, 0)),
            // T-shape
            new(new Vector2Int[] { new(1, 0), new(0, 0), new(2, 0), new(1, 1) }),
        };

        private static readonly Quaternion CwRotation = Quaternion.AngleAxis(-90, Vector3.forward);
        private static readonly Quaternion CcwRotation = Quaternion.AngleAxis(90, Vector3.forward);

        public static Shape GetRandom() => new(Shapes[Random.Range(0, Shapes.Length)]);

        public Vector2Int Origin { get; set; }

        public int Length => _positions.Length;

        private readonly Vector2Int[] _positions;
        private Vector2Int _nudgeCw;
        private Vector2Int _nudgeCcw;

        private Shape(Vector2Int[] positions, Vector2Int nudgeCw = default, Vector2Int nudgeCcw = default)
        {
            _nudgeCw   = nudgeCw;
            _nudgeCcw  = nudgeCcw;
            _positions = positions;
        }

        private Shape(Shape shape)
        {
            _nudgeCw   = shape._nudgeCw;
            _nudgeCcw  = shape._nudgeCcw;
            _positions = new Vector2Int[shape._positions.Length];
            Array.Copy(shape._positions, _positions, _positions.Length);
        }

        public Vector2Int this[int value] => _positions[value];

        public IEnumerator<Vector2Int> GetEnumerator()
        {
            foreach (var pos in _positions) {
                yield return pos + Origin;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Shape RotateCw() => Rotate(CwRotation, _nudgeCw);

        public Shape RotateCcw() => Rotate(CcwRotation, _nudgeCcw);

        private Shape Rotate(Quaternion q, Vector2Int nudge)
        {
            var origin = _positions[0];
            for (var i = 1; i < _positions.Length; i++) {
                var diff = q * (Vector2) (_positions[i] - origin);
                _positions[i] = new Vector2Int(
                    origin.x + Mathf.RoundToInt(diff.x) + nudge.x,
                    origin.y + Mathf.RoundToInt(diff.y) + nudge.y
                );
            }
            _positions[0] += nudge;

            _nudgeCw  = RotateNudge(q, _nudgeCw);
            _nudgeCcw = RotateNudge(q, _nudgeCcw);

            return this;
        }

        private Vector2Int RotateNudge(Quaternion q, Vector2Int nudge)
        {
            var n = q * (Vector2) nudge;
            return new Vector2Int(Mathf.RoundToInt(n.x), Mathf.RoundToInt(n.y));
        }
    }
}