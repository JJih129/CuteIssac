using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    [DisallowMultipleComponent]
    public sealed class PooledEffectAutoReturn : MonoBehaviour, IPooledObjectLifecycle
    {
        private readonly List<ParticleSystem> _particleSystems = new(4);
        private float _remainingLifetime;
        private bool _isArmed;
        private bool _hasCachedParticles;

        private void Update()
        {
            if (!_isArmed)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime > 0f)
            {
                return;
            }

            _isArmed = false;
            PrefabPoolService.Return(gameObject);
        }

        public void Arm(float fallbackLifetime)
        {
            CacheParticleSystems();
            RestartParticles();
            _remainingLifetime = ResolveLifetime(fallbackLifetime);
            _isArmed = true;
        }

        public void OnPoolSpawned()
        {
        }

        public void OnPoolDespawned()
        {
            _isArmed = false;
            StopParticles();
        }

        private void CacheParticleSystems()
        {
            if (_hasCachedParticles)
            {
                return;
            }

            GetComponentsInChildren(true, _particleSystems);
            _hasCachedParticles = true;
        }

        private void RestartParticles()
        {
            for (int i = 0; i < _particleSystems.Count; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void StopParticles()
        {
            for (int i = 0; i < _particleSystems.Count; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem != null)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private float ResolveLifetime(float fallbackLifetime)
        {
            float lifetime = Mathf.Max(0.05f, fallbackLifetime);

            for (int i = 0; i < _particleSystems.Count; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                float particleLifetime = main.startLifetime.constantMax;
                lifetime = Mathf.Max(lifetime, main.duration + particleLifetime);
            }

            return lifetime;
        }
    }
}
