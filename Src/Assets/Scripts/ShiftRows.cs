using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace x0.ld51
{
    public class ShiftRows : IEffect
    {
        public Task<bool> Apply(Stage stage)
        {
            var ceiling = stage.Ceiling;
            if (ceiling == 0) return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();

            var rows = Random.Range(1, ceiling);
            var offset = Random.Range(0, ceiling - rows);
            var shiftLeft = Random.Range(0, 2) < 1;

            for (int i = 0, y = offset; i < rows; i++, y++) {
                var blocks = new Transform[Stage.StageWidth + 1];
                var dir = shiftLeft ? -1 : 1;

                foreach (var pos in stage.Shape) {
                    if (pos.y == y) {
                        var x = pos.x - dir;
                        if (x >= Stage.StageWidth) {
                            x = 0;
                        } else if (x < 0) {
                            x = Stage.StageWidth - 1;
                        }

                        if (stage.Grid[x, y] != null) {
                            dir = 0;
                            shiftLeft = !shiftLeft;
                            break;
                        }
                    }
                }

                if (dir == 0) {
                    continue;
                }

                var first = shiftLeft ? stage.Grid[0, y] : stage.Grid[Stage.StageWidth - 1, y];
                if (first != null) {
                    var last = Object.Instantiate(first.gameObject, first.parent, false).transform;
                    last.localPosition = first.localPosition;
                    first.localPosition = new Vector3(shiftLeft ? Stage.StageWidth : -1, y) * Stage.BlockSize;
                    blocks[^1] = last;
                }

                for (var x = 0; x < Stage.StageWidth; x++) {
                    blocks[x] = stage.Grid[x, y];
                }

                var arrow = Object.Instantiate(stage.PushArrowTemplate, stage.transform).transform;
                arrow.localScale = shiftLeft ? new Vector3(-1, 1, 1) : Vector3.one;
                arrow.localPosition = new Vector3(shiftLeft ? Stage.StageWidth * Stage.BlockSize - Stage.BlockSize * .5f : - Stage.BlockSize * .5f, y * Stage.BlockSize);
                arrow.gameObject.SetActive(true);

                stage.StartCoroutine(Nudge(y, new Transition(dir, blocks)));
                shiftLeft = !shiftLeft;
            }

            IEnumerator Nudge(int y, Transition ctx)
            {
                for (int f = 0, frames = 20; f < frames; f++) {
                    for (int i = 0; i < ctx.Origins.Length; i++) {
                        if (ctx.Blocks[i] != null) {
                            var pos = ctx.Origins[i];
                            pos.x += ctx.Direction * Stage.BlockSize * f / (float) frames;
                            ctx.Blocks[i].localPosition = pos;
                        }
                    }

                    yield return new WaitForSeconds(.3f / frames);
                }

                for (var i = 0; i < ctx.Origins.Length - 1; i++) {
                    if (ctx.Blocks[i] != null) {
                        var pos = ctx.Origins[i];
                        pos.x += ctx.Direction * Stage.BlockSize;
                        ctx.Blocks[i].localPosition = pos;
                    }
                }

                for (var i = 0; i < Stage.StageWidth; i++) {
                    var x = i + ctx.Direction;
                    if (x < 0) {
                        x = Stage.StageWidth - 1;
                    } else if (x > Stage.StageWidth - 1) {
                        x = 0;
                    }

                    stage.Grid[x, y] = ctx.Blocks[i];
                }

                if (ctx.Blocks[^1] != null) {
                    Object.Destroy(ctx.Blocks[^1].gameObject);
                }

                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }

        private class Transition
        {
            public readonly int Direction;
            public readonly Transform[] Blocks;
            public readonly Vector3[] Origins;

            public Transition(int direction, Transform[] blocks)
            {
                Direction = direction;
                Blocks = blocks;

                Origins = new Vector3[blocks.Length];
                for (int i = 0; i < blocks.Length; i++) {
                    if (blocks[i] != null) {
                        Origins[i] = blocks[i].localPosition;
                    }
                }
            }
        }
    }
}