using System;
using System.Collections.Generic;
using AvantajPrim.AbilitiesDemo.Domain;
using DG.Tweening;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>Maps entity FSM enter/exit signals to Animator and tweens only.</summary>
    public sealed class EntityAnimationPresenter
    {
        private static readonly int IsWalkingHash = Animator.StringToHash(DemoConstants.Animation.IsWalking);
        private static readonly int IdleHash = Animator.StringToHash(DemoConstants.Animation.Idle);
        private static readonly int WalkForwardHash = Animator.StringToHash(DemoConstants.Animation.WalkForward);
        private static readonly int DeadHash = Animator.StringToHash(DemoConstants.Animation.Dead);
        private static readonly int GetHitHash = Animator.StringToHash(DemoConstants.Animation.GetHit);

        private static readonly Dictionary<string, string> CastTriggerToState = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { DemoConstants.Animation.DefencedAttack, DemoConstants.Animation.DefendHitState },
            { DemoConstants.Animation.Heal, DemoConstants.Animation.PotionDrinkState },
            { DemoConstants.Animation.Healing, DemoConstants.Animation.PotionDrinkState }
        };

        private readonly Transform _transform;
        private readonly Animator _animator;
        private readonly int _tweenId;
        private readonly HashSet<int> _triggerHashes = new HashSet<int>();
        private Vector3 _baseLocalScale = Vector3.one;
        private bool _hasIsWalking;
        private bool _hasGetHit;
        private bool _parametersCached;

        public EntityAnimationPresenter(EntityView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            _transform = view.transform;
            _animator = view.Animator;
            _tweenId = view.gameObject.GetInstanceID();
            _baseLocalScale = _transform.localScale;
        }

        private void EnsureAnimatorParametersCached()
        {
            if (_parametersCached || !HasAnimatorController)
                return;

            _parametersCached = true;
            _triggerHashes.Clear();
            _hasIsWalking = false;
            _hasGetHit = false;

            foreach (AnimatorControllerParameter p in _animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger)
                    _triggerHashes.Add(p.nameHash);
                else if (p.name == DemoConstants.Animation.IsWalking && p.type == AnimatorControllerParameterType.Bool)
                    _hasIsWalking = true;
                else if (p.name == DemoConstants.Animation.GetHit && p.type == AnimatorControllerParameterType.Trigger)
                    _hasGetHit = true;
            }
        }

        public void SetWalking(bool walking)
        {
            if (!HasAnimatorController)
                return;

            EnsureAnimatorParametersCached();

            if (_hasIsWalking)
                _animator.SetBool(IsWalkingHash, walking);

            if (!walking)
            {
                if (_animator.HasState(0, IdleHash))
                    _animator.CrossFade(DemoConstants.Animation.Idle, DemoConstants.Animation.IdleBlendSeconds);
                return;
            }

            if (_animator.HasState(0, WalkForwardHash))
                _animator.CrossFade(DemoConstants.Animation.WalkForward, DemoConstants.Animation.WalkBlendSeconds);
        }

        private bool HasAnimatorController =>
            _animator != null &&
            _animator.isActiveAndEnabled &&
            _animator.runtimeAnimatorController != null &&
            _animator.isInitialized;

        public void ReturnToIdle() => SetWalking(false);

        public void PlayCastTrigger(string triggerName)
        {
            if (!HasAnimatorController || string.IsNullOrEmpty(triggerName))
                return;

            EnsureAnimatorParametersCached();

            int hash = Animator.StringToHash(triggerName);
            string stateName = ResolveCastStateName(triggerName);
            int stateHash = Animator.StringToHash(stateName);

            SetWalking(false);

            if (_animator.HasState(0, stateHash))
                _animator.CrossFade(stateName, DemoConstants.Animation.CastBlendSeconds);

            if (_triggerHashes.Contains(hash))
            {
                _animator.ResetTrigger(hash);
                _animator.SetTrigger(hash);
            }
        }

        public bool TickCastAnimationWait(string animationName, ref float elapsed, ref int phase, float deltaTime)
        {
            if (!HasAnimatorController || string.IsNullOrEmpty(animationName))
                return true;

            EnsureAnimatorParametersCached();

            int stateHash = Animator.StringToHash(ResolveCastStateName(animationName));

            if (phase == 0)
            {
                elapsed += deltaTime;
                if (IsInState(stateHash))
                {
                    phase = 1;
                    elapsed = 0f;
                }
                else if (elapsed >= DemoConstants.Animation.CastWaitTimeoutSeconds)
                {
                    return true;
                }

                return false;
            }

            elapsed += deltaTime;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == stateHash &&
                stateInfo.normalizedTime >= DemoConstants.Animation.CastNormalizedTimeComplete)
                return true;

            float clipLength = GetCurrentClipLength(1f);
            return elapsed >= Mathf.Min(
                DemoConstants.Animation.CastWaitTimeoutSeconds,
                clipLength + DemoConstants.Animation.CastClipLengthBufferSeconds);
        }

        private static string ResolveCastStateName(string triggerOrStateName)
        {
            if (CastTriggerToState.TryGetValue(triggerOrStateName, out string mapped))
                return mapped;
            return triggerOrStateName;
        }

        private bool IsInState(int stateHash) =>
            _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash;

        private float GetCurrentClipLength(float fallback)
        {
            AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(0);
            if (clips == null || clips.Length == 0)
                return fallback;

            return clips[0].clip != null ? clips[0].clip.length : fallback;
        }

        public void PlayHitReact()
        {
            DemoTween.Kill(_tweenId, complete: false);
            ResetLocalScale();

            EnsureAnimatorParametersCached();
            if (HasAnimatorController && _hasGetHit)
                _animator.SetTrigger(GetHitHash);

            DemoTween.PunchScale(
                    _transform,
                    Vector3.one * DemoConstants.Animation.HitReactPunchScale,
                    DemoConstants.Animation.HitReactPunchDurationSeconds,
                    _tweenId)
                .SetUpdate(true)
                .OnKill(ResetLocalScale)
                .OnComplete(ResetLocalScale);
        }

        public void PlayDead()
        {
            DemoTween.Kill(_tweenId, complete: false);
            ResetLocalScale();
            SetWalking(false);

            EnsureAnimatorParametersCached();
            if (HasAnimatorController && _animator.HasState(0, DeadHash))
                _animator.CrossFade(DemoConstants.Animation.Dead, DemoConstants.Animation.IdleBlendSeconds);
        }

        public void ApplyDisplacement(Vector3 worldOffset, float duration, Action onComplete)
        {
            DemoTween.Kill(_tweenId);
            ResetLocalScale();
            SetWalking(false);

            Vector3 offset = _transform.TransformDirection(worldOffset);
            Vector3 target = _transform.position + offset;
            float tweenDuration = Mathf.Max(DemoConstants.Animation.MinDisplacementTweenSeconds, duration);

            DemoTween.Move(_transform, target, tweenDuration, _tweenId, Ease.OutQuad)
                .SetRecyclable(true)
                .SetLink(_transform.gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Dispose() => DemoTween.Kill(_tweenId);

        private void ResetLocalScale() => _transform.localScale = _baseLocalScale;
    }
}
