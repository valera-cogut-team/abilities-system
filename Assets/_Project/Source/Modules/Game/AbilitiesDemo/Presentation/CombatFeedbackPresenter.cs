using System;
using System.Collections.Generic;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using DG.Tweening;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using Logger.Facade;
using Pool.Application;
using Pool.Facade;
using TMPro;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class CombatFeedbackPresenter
    {
        private const int CachedTextMax = 999;

        private static readonly string[] DamageTexts = new string[CachedTextMax + 1];
        private static readonly string[] HealTexts = new string[CachedTextMax + 1];

        private static bool _loggedMissingFont;

        private readonly float _floatHeight;
        private readonly float _duration;
        private readonly float _riseDistance;

        private IObjectPool<PooledCombatLabel> _pool;
        private Transform _labelRoot;
        private Camera _camera;
        private readonly ILoggerFacade _logger;
        private readonly Dictionary<EntityId, List<PooledCombatLabel>> _activeByEntity =
            new Dictionary<EntityId, List<PooledCombatLabel>>();

        static CombatFeedbackPresenter()
        {
            for (int i = 0; i <= CachedTextMax; i++)
            {
                DamageTexts[i] = $"-{i}";
                HealTexts[i] = $"+{i}";
            }
        }

        public CombatFeedbackPresenter(
            ILoggerFacade logger = null,
            float floatHeight = DemoConstants.CombatFeedback.FloatHeight,
            float duration = DemoConstants.CombatFeedback.DurationSeconds,
            float riseDistance = DemoConstants.CombatFeedback.RiseDistance)
        {
            _logger = logger;
            _floatHeight = floatHeight;
            _duration = duration;
            _riseDistance = riseDistance;
        }

        public void Initialize(DemoWorldUiCanvas worldUi, Camera camera, IPoolFacade poolFacade)
        {
            _camera = camera;
            _labelRoot = worldUi != null ? worldUi.FloatingNumbersWorldRoot : null;
            EnsureRoot();

            if (poolFacade == null || _pool != null)
                return;

            _pool = EnsureCombatFloatPool(poolFacade);
        }

        private IObjectPool<PooledCombatLabel> EnsureCombatFloatPool(IPoolFacade poolFacade)
        {
            string poolId = DemoConstants.Presentation.CombatFloatPoolId;
            IObjectPool<PooledCombatLabel> pool = poolFacade.GetPool<PooledCombatLabel>(poolId);
            if (pool != null)
                return pool;

            try
            {
                return poolFacade.CreatePool(
                    poolId,
                    CreatePooledLabel,
                    initialSize: 0,
                    maxSize: DemoConstants.Presentation.CombatFloatMaxPoolSize);
            }
            catch (System.InvalidOperationException)
            {
                poolFacade.RemovePool(poolId);
                return poolFacade.CreatePool(
                    poolId,
                    CreatePooledLabel,
                    initialSize: 0,
                    maxSize: DemoConstants.Presentation.CombatFloatMaxPoolSize);
            }
        }

        public void PrewarmPool(int count)
        {
            EnsureRoot();
            if (_pool == null || !EnsureFont())
                return;

            while (_pool.InactiveCount < count)
            {
                PooledCombatLabel label = CreatePooledLabel();
                if (label == null)
                    break;

                _pool.Return(label);
            }
        }

        public void ShowNumber(EntityView view, float value, CombatNumberType type)
        {
            if (view == null || _camera == null || _pool == null)
                return;

            EnsureRoot();
            if (!EnsureFont())
                return;

            PooledCombatLabel pooled = _pool.Get();
            if (pooled?.Label == null)
                return;

            EntityId entityId = view.EntityId;
            pooled.AttachedEntityId = entityId;
            TrackLabel(entityId, pooled);

            DemoTmpFontProvider.ApplyTo(pooled.Label);
            pooled.GameObject.SetActive(true);
            TextMeshPro label = pooled.Label;
            int labelId = pooled.TweenId;

            label.text = FormatNumber(value, type);
            label.color = type switch
            {
                CombatNumberType.Heal => DemoConstants.CombatFeedback.HealColor,
                CombatNumberType.Dot => DemoConstants.CombatFeedback.DotColor,
                _ => DemoConstants.CombatFeedback.DamageColor
            };

            float driftX = UnityEngine.Random.Range(
                DemoConstants.CombatFeedback.DriftMinX,
                DemoConstants.CombatFeedback.DriftMaxX);

            Transform anchor = view.GetFloatingNumbersAnchor();
            label.transform.SetParent(anchor, false);

            Vector3 startLocal = new Vector3(0f, _floatHeight, 0f);
            Vector3 endLocal = startLocal + new Vector3(driftX, _riseDistance, 0f);
            label.transform.localPosition = startLocal;
            label.alpha = 1f;
            label.transform.localScale = Vector3.one * DemoConstants.CombatFeedback.LabelWorldScale;
            BillboardLabel(label.transform, label.transform.position);

            DemoTween.Kill(labelId);
            const float startScale = DemoConstants.CombatFeedback.LabelWorldScale;
            const float peakScale = startScale * DemoConstants.CombatFeedback.PeakScaleMultiplier;
            const float popInFraction = DemoConstants.CombatFeedback.PopInDurationFraction;
            DemoTween.ToFloat(labelId, 0f, 1f, _duration, t =>
            {
                if (label == null)
                    return;

                float moveT = DOVirtual.EasedValue(0f, 1f, t, Ease.OutQuad);
                label.transform.localPosition = Vector3.Lerp(startLocal, endLocal, moveT);
                label.alpha = 1f - DOVirtual.EasedValue(0f, 1f, t, Ease.InQuad);
                BillboardLabel(label.transform, label.transform.position);

                float scale = t < popInFraction / _duration
                    ? Mathf.Lerp(startScale, peakScale, t / (popInFraction / _duration))
                    : Mathf.Lerp(peakScale, startScale, (t - popInFraction / _duration) / (1f - popInFraction / _duration));
                label.transform.localScale = Vector3.one * scale;
            },
            Ease.Linear,
            pooled.GameObject)
            .OnComplete(() =>
            {
                if (label == null)
                    return;

                pooled.CompleteTween();
            });
        }

        public void CancelLabelsForEntity(EntityId entityId)
        {
            if (entityId == default || !_activeByEntity.TryGetValue(entityId, out List<PooledCombatLabel> active))
                return;

            var snapshot = active.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                PooledCombatLabel pooled = snapshot[i];
                if (pooled == null)
                    continue;

                DemoTween.Kill(pooled.TweenId);
                ReturnLabel(pooled);
            }
        }

        private void TrackLabel(EntityId entityId, PooledCombatLabel pooled)
        {
            if (entityId == default || pooled == null)
                return;

            if (!_activeByEntity.TryGetValue(entityId, out List<PooledCombatLabel> list))
            {
                list = new List<PooledCombatLabel>(2);
                _activeByEntity[entityId] = list;
            }

            list.Add(pooled);
        }

        private void UntrackLabel(EntityId entityId, PooledCombatLabel pooled)
        {
            if (entityId == default || pooled == null)
                return;

            if (!_activeByEntity.TryGetValue(entityId, out List<PooledCombatLabel> list))
                return;

            list.Remove(pooled);
            if (list.Count == 0)
                _activeByEntity.Remove(entityId);
        }

        private void BillboardLabel(Transform labelTransform, Vector3 worldPosition)
        {
            if (_camera == null)
                return;

            labelTransform.rotation = Quaternion.LookRotation(
                worldPosition - _camera.transform.position,
                _camera.transform.up);
        }

        private static string FormatNumber(float value, CombatNumberType type)
        {
            int amount = Mathf.Clamp(Mathf.RoundToInt(value), 0, CachedTextMax);
            return type == CombatNumberType.Heal ? HealTexts[amount] : DamageTexts[amount];
        }

        private bool EnsureFont()
        {
            if (DemoTmpFontProvider.TryGetFont(out _))
                return true;

            if (!_loggedMissingFont)
            {
                _loggedMissingFont = true;
                _logger?.LogWarning("[AbilitiesDemo] TMP font unavailable for floating combat text.");
            }

            return false;
        }

        private void EnsureRoot()
        {
            if (_labelRoot == null)
                _logger?.LogWarning("[AbilitiesDemo] Combat feedback label root is missing.");
        }

        private PooledCombatLabel CreatePooledLabel()
        {
            if (!DemoTmpFontProvider.TryGetFont(out _))
                return null;

            var go = new GameObject("FloatingCombatText");
            go.transform.SetParent(_labelRoot, false);
            go.transform.localScale = Vector3.one * DemoConstants.CombatFeedback.LabelWorldScale;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            ConfigureLabel(tmp);
            var pooled = new PooledCombatLabel(go, tmp, this);
            go.SetActive(false);
            return pooled;
        }

        private static void ConfigureLabel(TextMeshPro tmp)
        {
            DemoTmpFontProvider.ApplyTo(tmp);
            if (tmp.font == null)
                return;

            tmp.fontSize = DemoConstants.CombatFeedback.FontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = DemoConstants.CombatFeedback.LabelRectSize;
            tmp.richText = false;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        internal void ReturnLabel(PooledCombatLabel pooled)
        {
            if (pooled == null || _pool == null)
                return;

            UntrackLabel(pooled.AttachedEntityId, pooled);
            pooled.AttachedEntityId = default;

            if (pooled.GameObject != null && _labelRoot != null)
            {
                pooled.GameObject.transform.SetParent(_labelRoot, false);
                pooled.GameObject.transform.localPosition = Vector3.zero;
            }

            _pool.Return(pooled);
        }
    }
}
