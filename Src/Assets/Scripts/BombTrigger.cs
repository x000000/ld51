using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static x0.ld51.Stage;

namespace x0.ld51
{
    public class BombTrigger : MonoBehaviour, IPlacementAware
    {
        private static readonly int ActivatedProp = Animator.StringToHash("Activated");

        private readonly TaskCompletionSource<bool> _tcs = new();
        private readonly List<GameObject> _blocks = new();
        private BlockContext[] _blockContexts;

        public Task OnPlacement(Stage stage, Vector2Int cell)
        {
            var ri = 2;
            var rf = 2.4f;
            for (int y = cell.y - ri, i = 0; i < ri * 2 + 1; i++, y++) {
                for (int x = cell.x - ri, j = 0; j < ri * 2 + 1; j++, x++) {
                    if (x is >= 0 and < StageWidth && y is >= 0 and < StageHeight) {
                        if (new Vector2(x - cell.x, y - cell.y).magnitude <= rf) {
                            var tf = stage.Grid[x, y];
                            if (tf != null) {
                                stage.Grid[x, y] = null;
                                _blocks.Add(tf.gameObject);
                            }
                        }
                    }
                }
            }

            GetComponent<Animator>().SetBool(ActivatedProp, true);
            return _tcs.Task;
        }

        public void TriggerExplode()
        {
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<ParticleSystem>().Play();

            _blockContexts = new BlockContext[_blocks.Count];
            for (var i = 0; i < _blocks.Count; i++) {
                _blockContexts[i] = new BlockContext(_blocks[i].GetComponentInChildren<SpriteRenderer>(false));
            }

            StartCoroutine(HeatBlocks());
        }

        private void OnParticleSystemStopped()
        {
            foreach (var ctx in _blockContexts) {
                ctx.Renderer.enabled = false;
                ctx.Renderer.GetComponent<ParticleSystem>().Play();
            }
            Destroy(gameObject);
            _tcs.SetResult(true);
        }

        private IEnumerator HeatBlocks()
        {
            for (int f = 0, frames = 20; f < frames; f++) {
                var t = f / (float) frames;

                foreach (var ctx in _blockContexts) {
                    ctx.Renderer.color = Color.Lerp(ctx.InitColor, Color.white, t);
                }

                yield return new WaitForSeconds(.2f / frames);
            }
        }

        private class BlockContext
        {
            public readonly SpriteRenderer Renderer;
            public readonly Color InitColor;

            public BlockContext(SpriteRenderer renderer)
            {
                Renderer  = renderer;
                InitColor = renderer.color;
            }
        }
    }
}