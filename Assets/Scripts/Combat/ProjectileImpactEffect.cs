using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Lightweight one-shot visual used by projectile hit and destroy effect prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileImpactEffect : MonoBehaviour
    {
        [SerializeField] [Min(0.01f)] private float lifetime = 0.18f;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool deriveLifetimeFromParticles = true;
        [SerializeField] [Min(0f)] private float lifetimePadding = 0.04f;

        private ParticleSystem[] _particleSystems;
        private TrailRenderer[] _trailRenderers;
        private Animator[] _animators;
        private float _remainingLifetime;

        private void Awake()
        {
            CacheEffectComponents();
        }

        private void OnEnable()
        {
            CacheEffectComponents();
            ResetPooledEffectState();
            _remainingLifetime = ResolveLifetime();
        }

        private void Update()
        {
            _remainingLifetime -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        private void OnDisable()
        {
            ClearTrails();
        }

        private void CacheEffectComponents()
        {
            _particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
            _trailRenderers ??= GetComponentsInChildren<TrailRenderer>(true);
            _animators ??= GetComponentsInChildren<Animator>(true);
        }

        private void ResetPooledEffectState()
        {
            ClearTrails();
            RestartParticles();
            RestartAnimators();
        }

        private float ResolveLifetime()
        {
            float resolvedLifetime = Mathf.Max(0.01f, lifetime);

            if (!deriveLifetimeFromParticles || _particleSystems == null)
            {
                return resolvedLifetime;
            }

            for (int index = 0; index < _particleSystems.Length; index++)
            {
                ParticleSystem particleSystem = _particleSystems[index];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                if (main.loop)
                {
                    continue;
                }

                float particleLifetime = main.duration + main.startLifetime.constantMax + lifetimePadding;
                resolvedLifetime = Mathf.Max(resolvedLifetime, particleLifetime);
            }

            return resolvedLifetime;
        }

        private void RestartParticles()
        {
            if (_particleSystems == null)
            {
                return;
            }

            for (int index = 0; index < _particleSystems.Length; index++)
            {
                ParticleSystem particleSystem = _particleSystems[index];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }
        }

        private void RestartAnimators()
        {
            if (_animators == null)
            {
                return;
            }

            for (int index = 0; index < _animators.Length; index++)
            {
                Animator animator = _animators[index];
                if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void ClearTrails()
        {
            if (_trailRenderers == null)
            {
                return;
            }

            for (int index = 0; index < _trailRenderers.Length; index++)
            {
                TrailRenderer trailRenderer = _trailRenderers[index];
                if (trailRenderer == null)
                {
                    continue;
                }

                trailRenderer.Clear();
            }
        }
    }
}
