using AvantajPrim.AbilitiesDemo.Domain;
using DG.Tweening;
using Logger.Facade;
using UnityEngine;
using UnityEngine.UI;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class EntityHealthBar
    {
        private readonly Transform _root;
        private Image _fill;
        private Image _background;
        private readonly Transform _followTarget;
        private readonly ILoggerFacade _logger;
        private DemoWorldUiCanvas _worldUi;
        private float _displayedFill = 1f;
        private readonly int _tweenId;
        private Vector3 _lastFollowPosition;
        private bool _hasFollowSnapshot;
        private bool _disposed;
        private TweenCallback _restoreFillColorCallback;

        public GameObject GameObject => _root != null ? _root.gameObject : null;
        public Transform Transform => _root;
        public bool IsAlive => !_disposed && _root != null && _followTarget != null;
        public bool HasVisual => _fill != null;

        public EntityHealthBar(Transform root, Transform followTarget, ILoggerFacade logger = null)
        {
            _root = root;
            _followTarget = followTarget;
            _logger = logger;
            _tweenId = DemoTween.Id(root.gameObject);
            _restoreFillColorCallback = RestoreFillColor;
        }

        public void Initialize(DemoWorldUiCanvas worldUi)
        {
            if (!IsAlive)
                return;

            _worldUi = worldUi;
            EnsureBar(worldUi);
            if (_fill == null)
                return;

            SyncWorldPosition();
            if (worldUi?.Camera != null)
                Billboard(worldUi.Camera);

            worldUi?.Register(this);
        }

        public void SetFill(float current, float max, float tweenDuration = DemoConstants.HealthBar.DefaultTweenSeconds)
        {
            if (!IsAlive || _fill == null)
                return;

            float target = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            float delta = Mathf.Abs(target - _displayedFill);
            bool isDamage = target < _displayedFill - DemoConstants.HealthBar.FillDeltaEpsilon;
            bool isHeal = target > _displayedFill + DemoConstants.HealthBar.FillDeltaEpsilon;
            bool isSmallTick = tweenDuration < DemoConstants.HealthBar.SmallTickTweenThresholdSeconds &&
                               delta < DemoConstants.HealthBar.SmallTickMaxDelta;

            if (isSmallTick)
            {
                DemoTween.Kill(_tweenId);
                _displayedFill = target;
                _fill.fillAmount = target;
                return;
            }

            float duration = tweenDuration > 0f ? tweenDuration : DemoConstants.HealthBar.DefaultTweenSeconds;

            DemoTween.Kill(_tweenId);
            DemoTween.ToFloat(_tweenId, _displayedFill, target, duration, v =>
                {
                    _displayedFill = v;
                    _fill.fillAmount = v;
                }, isHeal ? Ease.OutQuad : Ease.OutCubic);

            if (!isDamage && !isHeal)
                return;

            Color flashColor = isHeal ? DemoConstants.HealthBar.HealFlashColor : DemoConstants.HealthBar.DamageFlashColor;
            _fill.DOColor(flashColor, DemoConstants.HealthBar.FlashInSeconds).SetRecyclable(true).SetId(_tweenId)
                .OnComplete(_restoreFillColorCallback);
        }

        private void RestoreFillColor()
        {
            if (_fill == null)
                return;

            _fill.DOColor(DemoConstants.HealthBar.DefaultFillColor, DemoConstants.HealthBar.FlashOutSeconds)
                .SetRecyclable(true).SetId(_tweenId);
        }

        public bool NeedsBillboard()
        {
            if (!IsAlive)
                return false;

            if (!_hasFollowSnapshot)
                return true;

            return (_followTarget.position - _lastFollowPosition).sqrMagnitude >
                   DemoConstants.HealthBar.FollowMoveEpsilonSqr;
        }

        public void Billboard(Camera camera)
        {
            if (!IsAlive)
                return;

            SyncWorldPosition();

            if (camera == null)
                return;

            _root.rotation = Quaternion.LookRotation(
                _root.position - camera.transform.position,
                camera.transform.up);
        }

        private void SyncWorldPosition()
        {
            if (!IsAlive)
                return;

            Vector3 pos = _followTarget.position + Vector3.up * DemoConstants.HealthBar.HeightOffset;
            _root.position = pos;
            _lastFollowPosition = _followTarget.position;
            _hasFollowSnapshot = true;
        }

        private void EnsureBar(DemoWorldUiCanvas worldUi)
        {
            if (!IsAlive || _fill != null)
                return;

            if (worldUi == null)
                return;

            Transform parent = worldUi.HealthBarsRoot;
            if (parent == null)
                return;

            if (_root.parent != parent)
                _root.SetParent(parent, false);

            if (!IsAlive)
                return;

            EnsureDedicatedCanvas(worldUi);

            if (!IsAlive)
                return;

            _root.localScale = Vector3.one * DemoConstants.HealthBar.CanvasScale;

            var rootGo = new GameObject("HealthBarRoot", typeof(RectTransform));
            rootGo.transform.SetParent(_root, false);
            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.sizeDelta = DemoConstants.HealthBar.BarSize;

            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(rootGo.transform, false);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            _background = bgGo.GetComponent<Image>();
            _background.sprite = DemoUiSprites.White;
            _background.color = DemoConstants.HealthBar.BackgroundColor;
            _background.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bgGo.transform, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _fill = fillGo.GetComponent<Image>();
            _fill.sprite = DemoUiSprites.White;
            _fill.color = DemoConstants.HealthBar.DefaultFillColor;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = _displayedFill;
            _fill.raycastTarget = false;
        }

        private void EnsureDedicatedCanvas(DemoWorldUiCanvas worldUi)
        {
            Canvas canvas = _root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = _root.gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldUi != null ? worldUi.Camera : null;
            canvas.overrideSorting = true;
            canvas.sortingOrder = DemoConstants.HealthBar.SortingOrder;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            DemoTween.Kill(_tweenId);
            _worldUi?.Unregister(this);
            _worldUi = null;

            if (_root != null)
                Object.Destroy(_root.gameObject);
        }
    }
}
