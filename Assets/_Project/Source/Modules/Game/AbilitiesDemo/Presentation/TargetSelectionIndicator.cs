using System.Collections.Generic;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using Logger.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class TargetSelectionIndicator
    {
        private readonly Transform _host;
        private readonly float _radius;
        private readonly float _heightOffset;
        private readonly Color _color;
        private readonly ILoggerFacade _logger;
        private readonly List<RingSlot> _rings = new List<RingSlot>(4);
        private float _groundSurfaceY;

        public EntityId? CurrentTarget => _rings.Count > 0 ? _rings[0].TargetId : null;

        public TargetSelectionIndicator(
            Transform host,
            ILoggerFacade logger,
            float radius = DemoConstants.Targeting.RingRadius,
            float heightOffset = DemoConstants.Targeting.HeightOffset,
            Color? color = null)
        {
            _host = host;
            _logger = logger;
            _radius = radius;
            _heightOffset = heightOffset;
            _color = color ?? DemoConstants.Targeting.RingColor;
        }

        public void SetGroundSurfaceY(float groundSurfaceY) => _groundSurfaceY = groundSurfaceY;

        public void Prewarm()
        {
            EnsureRingCount(1);
            for (int i = 0; i < _rings.Count; i++)
                _rings[i].Renderer.enabled = false;
        }

        public void SetTarget(EntityView view)
        {
            if (view == null)
            {
                Hide();
                return;
            }

            SetTargets(new[] { view });
        }

        public void SetTargets(IReadOnlyList<EntityView> views)
        {
            if (views == null || views.Count == 0)
            {
                Hide();
                return;
            }

            EnsureRingCount(views.Count);
            _host.gameObject.SetActive(true);

            for (int i = 0; i < _rings.Count; i++)
            {
                RingSlot ring = _rings[i];
                if (i >= views.Count)
                {
                    ring.TargetId = null;
                    ring.FollowTarget = null;
                    ring.Renderer.enabled = false;
                    continue;
                }

                EntityView view = views[i];
                ring.TargetId = view.EntityId;
                ring.FollowTarget = view.transform;
                ring.Renderer.enabled = true;
                SyncRing(ring);
            }
        }

        public void Hide()
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                _rings[i].TargetId = null;
                _rings[i].FollowTarget = null;
                _rings[i].Renderer.enabled = false;
            }
        }

        public void LateFollow()
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                RingSlot ring = _rings[i];
                if (!ring.Renderer.enabled || ring.FollowTarget == null)
                    continue;

                SyncRing(ring);
            }
        }

        private void EnsureRingCount(int count)
        {
            while (_rings.Count < count)
            {
                var ringGo = new GameObject($"TargetRingSprite_{_rings.Count}");
                ringGo.transform.SetParent(_host, false);
                ApplyIgnoreRaycastLayer(ringGo);

                SpriteRenderer renderer = ringGo.AddComponent<SpriteRenderer>();
                renderer.sprite = DemoTargetRingSprite.Ring;
                renderer.color = _color;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = DemoConstants.Targeting.RingSortingOrder;
                renderer.enabled = false;

                _rings.Add(new RingSlot
                {
                    Root = ringGo.transform,
                    Renderer = renderer
                });
            }
        }

        private void SyncRing(RingSlot ring)
        {
            if (ring.Root == null || ring.FollowTarget == null || ring.Renderer == null)
                return;

            Vector3 center = DemoGroundAlignment.GetRingPosition(ring.FollowTarget, _groundSurfaceY, _heightOffset);
            ring.Root.SetPositionAndRotation(
                center,
                Quaternion.Euler(DemoConstants.Targeting.RingRotationX, 0f, 0f));

            float diameter = _radius * 2f;
            ring.Root.localScale = new Vector3(diameter, diameter, 1f);
            ring.Renderer.color = _color;
        }

        private static void ApplyIgnoreRaycastLayer(GameObject go)
        {
            int ignoreLayer = LayerMask.NameToLayer(DemoConstants.Layers.IgnoreRaycast);
            if (ignoreLayer >= 0)
                go.layer = ignoreLayer;
        }

        private sealed class RingSlot
        {
            internal Transform Root;
            internal SpriteRenderer Renderer;
            internal Transform FollowTarget;
            internal EntityId? TargetId;
        }
    }
}
