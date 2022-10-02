using System;
using UnityEngine;

namespace x0.ld51
{
    public class SelfDestroy : MonoBehaviour
    {
        public void DestroySelf() => Destroy(gameObject);

        private void OnParticleSystemStopped() => DestroySelf();
    }
}