using Pool.Domain;
using UnityEngine;

namespace Effects.Application
{
    public sealed class PooledVfxHandle : IPoolable
    {
        private struct ParticlePresentationBaseline
        {
            internal ParticleSystem System;
            internal ParticleSystem.MinMaxGradient StartColor;
            internal ParticleSystem.MinMaxCurve EmissionRateOverTime;
        }

        private readonly Transform _transform;
        private ParticleSystem[] _particleSystems;
        private ParticlePresentationBaseline[] _presentationBaselines;
        private bool _particlesCached;
        private bool _presentationBaselinesCaptured;
        private Vector3 _baselineLocalScale;

        public PooledVfxHandle(Transform transform)
        {
            _transform = transform;
        }

        public GameObject GameObject => _transform.gameObject;
        public string Address { get; private set; }

        public void Bind(string address)
        {
            Address = address;
            _particlesCached = false;
            _presentationBaselinesCaptured = false;
            CapturePresentationBaselines();
        }

        public void Reset()
        {
            RestorePresentationBaselines();
            StopAndClearParticles();
            GameObject.SetActive(false);
        }

        public void Activate(Vector3 worldPosition, Quaternion worldRotation, Transform parent)
        {
            if (parent != null)
            {
                _transform.SetParent(parent, false);
                _transform.localPosition = worldPosition;
                _transform.localRotation = worldRotation;
            }
            else
            {
                _transform.SetParent(null, false);
                _transform.SetPositionAndRotation(worldPosition, worldRotation);
            }

            GameObject.SetActive(true);
            PlayParticles();
        }

        private void CacheParticles()
        {
            if (_particlesCached)
                return;

            _particleSystems = GameObject.GetComponentsInChildren<ParticleSystem>(true);
            _particlesCached = true;
        }

        private void PlayParticles()
        {
            CacheParticles();
            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopAndClearParticles()
        {
            CacheParticles();
            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void CapturePresentationBaselines()
        {
            if (_presentationBaselinesCaptured)
                return;

            _baselineLocalScale = _transform.localScale;
            CacheParticles();
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                _presentationBaselinesCaptured = true;
                return;
            }

            _presentationBaselines = new ParticlePresentationBaseline[_particleSystems.Length];
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ParticleSystem.EmissionModule emission = ps.emission;
                _presentationBaselines[i] = new ParticlePresentationBaseline
                {
                    System = ps,
                    StartColor = ps.main.startColor,
                    EmissionRateOverTime = emission.rateOverTime
                };
            }

            _presentationBaselinesCaptured = true;
        }

        private void RestorePresentationBaselines()
        {
            if (!_presentationBaselinesCaptured)
                return;

            _transform.localScale = _baselineLocalScale;
            if (_presentationBaselines == null)
                return;

            for (int i = 0; i < _presentationBaselines.Length; i++)
            {
                ParticlePresentationBaseline baseline = _presentationBaselines[i];
                if (baseline.System == null)
                    continue;

                ParticleSystem.MainModule main = baseline.System.main;
                main.startColor = baseline.StartColor;

                ParticleSystem.EmissionModule emission = baseline.System.emission;
                emission.rateOverTime = baseline.EmissionRateOverTime;
            }
        }
    }
}
