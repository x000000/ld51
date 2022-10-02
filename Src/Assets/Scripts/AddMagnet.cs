using System.Threading.Tasks;
using UnityEngine;

namespace x0.ld51
{
    public class AddMagnet : IEffect
    {
        public Task<bool> Apply(Stage stage)
        {
            while (true) {
                var index = Random.Range(0, stage.Shape.Length);
                var block = stage.Blocks[index];
                if (block.GetComponentInChildren<IPlacementAware>() != null) {
                    continue;
                }

                Object.Instantiate(stage.MagnetTemplate, block);
                return Task.FromResult(true);
            }
        }
    }
}