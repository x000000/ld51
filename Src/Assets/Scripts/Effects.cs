using System;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace x0.ld51
{
    public interface IPlacementAware
    {
        Task OnPlacement(Stage stage, Vector2Int cell);
    }

    public interface IEffect
    {
        public Task<bool> Apply(Stage stage);
    }

    public static class Effects
    {
        public static IEffect GetRandom() => Random.Range(0, 3) switch {
            0 => new ShiftRows(),
            1 => new AddBomb(),
            2 => new AddMagnet(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}