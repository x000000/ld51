using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static x0.ld51.Stage;

namespace x0.ld51
{
    using V2i = Vector2Int;

    public class MagnetTrigger : MonoBehaviour, IPlacementAware
    {
        private const int Distance = 3;

        private readonly TaskCompletionSource<bool> _tcs = new();
        private Stage _stage;
        private List<BlockContext> _blocks = new();

        public Task OnPlacement(Stage stage, V2i cell)
        {
            _stage = stage;
            Transform block = null;

            int n, blocked;
            var x = cell.x;
            var y = cell.y + 1;
            for (n = 0, blocked = 0; n < Distance; n++, y++)
                if (TryBlock(x, y, ref block)) AddBlock(block, x, y, new V2i(0, blocked++ - n));

            for (n = Distance - blocked, y = cell.y + blocked + 1; n > 0 && y < StageHeight; n--, y++) stage.Grid[x, y] = null;

            y = cell.y - 1;
            for (n = 0, blocked = 0; n < Distance; n++, y--)
                if (TryBlock(x, y, ref block)) AddBlock(block, x, y, new V2i(0, n - blocked++));

            for (n = Distance - blocked, y = cell.y - blocked - 1; n > 0 && y >= 0; n--, y--) stage.Grid[x, y] = null;

            y = cell.y;
            x = cell.x + 1;
            for (n = 0, blocked = 0; n < Distance; n++, x++)
                if (TryBlock(x, y, ref block)) AddBlock(block, x, y, new V2i(blocked++ - n, 0));

            for (n = Distance - blocked, x = cell.x + blocked + 1; n > 0 && x < StageWidth; n--, x++) stage.Grid[x, y] = null;

            x = cell.x - 1;
            for (n = 0, blocked = 0; n < Distance; n++, x--)
                if (TryBlock(x, y, ref block)) AddBlock(block, x, y, new V2i(n - blocked++, 0));

            for (n = Distance - blocked, x = cell.x - blocked - 1; n > 0 && x >= 0; n--, x--) stage.Grid[x, y] = null;

            StartCoroutine(MoveBlocks());

            return _tcs.Task;
        }

        private bool TryBlock(int x, int y, ref Transform tf)
        {
            if (x is >= 0 and < StageWidth && y is >= 0 and < StageHeight) {
                tf = _stage.Grid[x, y];
                return tf != null;
            }
            return false;
        }

        private void AddBlock(Transform block, int x, int y, V2i dir)
        {
            // Debug.Log($"> Add {x}, {y} -> {dir}");
            if (dir != Vector2Int.zero) {
                _stage.Grid[x + dir.x, y + dir.y] = block;
                _blocks.Add(new BlockContext(block, x, y, dir));
            }
        }

        private IEnumerator MoveBlocks()
        {
            for (int f = 0, frames = 20; f < frames; f++) {
                var t = f / (float) frames;

                foreach (var ctx in _blocks) {
                    ctx.Block.localPosition = Vector2.Lerp(ctx.Origin, ctx.Dest, t);
                }

                yield return new WaitForSeconds(.15f / frames);
            }

            foreach (var ctx in _blocks) {
                ctx.Block.localPosition = (Vector2) ctx.Dest;
            }

            Destroy(gameObject);
            _tcs.SetResult(true);
        }


        private class BlockContext
        {
            public readonly Transform Block;
            public readonly V2i Origin;
            public readonly V2i Dest;

            public BlockContext(Transform block, int x, int y, V2i distance)
            {
                Block  = block;
                Origin = new Vector2Int(x, y) * BlockSize;
                Dest   = Origin + distance * BlockSize;
            }
        }
    }
}