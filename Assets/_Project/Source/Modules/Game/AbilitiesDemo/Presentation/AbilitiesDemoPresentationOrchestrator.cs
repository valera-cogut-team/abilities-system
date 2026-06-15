using System;
using System.Collections.Generic;
using Addressables.Facade;
using Audio.Facade;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Facade;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Effects.Facade;
using Logger.Facade;
using Pool.Facade;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>Bootstraps demo arena, entities, presentation bridge, and runtime camera.</summary>
    public sealed class AbilitiesDemoPresentationOrchestrator : IDisposable
    {
        private readonly AbilitiesDemoBootstrapService _bootstrap;
        private readonly TargetingService _targeting;
        private readonly PlayerInputRouter _inputRouter;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly DemoEntityRegistry _registry;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly DemoGameplaySession _session;
        private readonly IAbilitiesDemoFacade _demoFacade;
        private readonly ILoggerFacade _logger;
        private readonly AbilitiesDemoTickHandler _tickHandler;
        private readonly CombatFeedbackPresenter _combatFeedback;
        private readonly EntityDespawnService _despawnService;
        private readonly IAbilitiesFacade _abilities;
        private readonly IAudioFacade _audio;
        private readonly IEffectsFacade _effects;
        private readonly IAddressablesFacade _addressables;
        private readonly IPoolFacade _poolFacade;
        private readonly GradualCombatApplier _gradualApplier;
        private readonly EntityAttachedVfxRegistry _entityVfxRegistry;

        private readonly Transform _worldRoot;
        private TargetSelectionIndicator _targetIndicator;
        private AbilityPresentationBridge _presentationBridge;
        private DemoWorldUiCanvas _worldUi;
        private Camera _camera;

        private bool _bootstrapComplete;
        private bool _pendingWorldVisible;
        private bool _pendingGameplayActive;
        private bool _disposed;

        private readonly List<CombatEntityFeedbackWire> _combatWires = new List<CombatEntityFeedbackWire>(8);

        public AbilitiesDemoPresentationOrchestrator(
            Transform worldRoot,
            AbilitiesDemoBootstrapService bootstrap,
            TargetingService targeting,
            PlayerInputRouter inputRouter,
            IAbilitiesFacade abilities,
            DemoEntityRegistry registry,
            DemoCombatRegistry combatRegistry,
            DemoGameplaySession session,
            EntityStateRegistry entityStateRegistry,
            IAbilitiesDemoFacade demoFacade,
            AbilitiesDemoTickHandler tickHandler,
            CombatFeedbackPresenter combatFeedback,
            IAudioFacade audio,
            IEffectsFacade effects,
            IAddressablesFacade addressables,
            IPoolFacade poolFacade,
            ILoggerFacade logger,
            GradualCombatApplier gradualApplier,
            EntityDespawnService despawnService,
            EntityAttachedVfxRegistry entityVfxRegistry,
            DemoAddressableCatalog addressCatalog)
        {
            _worldRoot = worldRoot;
            _bootstrap = bootstrap;
            _targeting = targeting;
            _inputRouter = inputRouter;
            _abilities = abilities;
            _registry = registry;
            _combatRegistry = combatRegistry;
            _session = session;
            _entityStateRegistry = entityStateRegistry;
            _demoFacade = demoFacade;
            _tickHandler = tickHandler;
            _combatFeedback = combatFeedback;
            _audio = audio;
            _effects = effects;
            _addressables = addressables;
            _poolFacade = poolFacade;
            _logger = logger;
            _gradualApplier = gradualApplier;
            _despawnService = despawnService;
            _entityVfxRegistry = entityVfxRegistry;

            DOTween.defaultRecyclable = true;

            var worldUiGo = new GameObject("DemoWorldUiCanvas");
            worldUiGo.transform.SetParent(_worldRoot, false);
            _worldUi = new DemoWorldUiCanvas(worldUiGo.transform);
            _despawnService?.BindWorldUi(_worldUi);

            var indicatorGo = new GameObject("TargetSelectionIndicator");
            indicatorGo.transform.SetParent(_worldRoot, false);
            _targetIndicator = new TargetSelectionIndicator(indicatorGo.transform, logger);
            _tickHandler.ConfigurePresentation(_targetIndicator, _worldUi);

            _presentationBridge = new AbilityPresentationBridge(
                abilities,
                registry,
                combatRegistry,
                entityStateRegistry,
                combatFeedback,
                audio,
                effects,
                addressables,
                addressCatalog,
                logger,
                gradualApplier,
                entityVfxRegistry);

            _demoFacade.SelectedTargetsChanged += OnSelectedTargetsChanged;
            _demoFacade.WorldVisibleChanged += OnWorldVisibleChanged;
            _demoFacade.GameplayActiveChanged += OnGameplayActiveChanged;

            _pendingWorldVisible = _demoFacade.IsWorldVisible;
            _pendingGameplayActive = _demoFacade.IsGameplayActive;
            BuildWorldAsync().Forget();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_demoFacade != null)
            {
                _demoFacade.SelectedTargetsChanged -= OnSelectedTargetsChanged;
                _demoFacade.WorldVisibleChanged -= OnWorldVisibleChanged;
                _demoFacade.GameplayActiveChanged -= OnGameplayActiveChanged;
            }

            _tickHandler?.SetPresentationReady(false);
            _tickHandler?.ConfigurePresentation(null, null);
            _despawnService?.BindWorldUi(null);

            DisposeAllHealthBars();

            for (int i = 0; i < _combatWires.Count; i++)
                _combatWires[i]?.Dispose();
            _combatWires.Clear();

            _presentationBridge?.Dispose();
            _presentationBridge = null;
            _targetIndicator?.Hide();
        }

        private void OnSelectedTargetsChanged()
        {
            if (!_demoFacade.IsGameplayActive)
            {
                _targetIndicator.Hide();
                return;
            }

            IReadOnlyList<EntityId> selected = _demoFacade.SelectedTargets;
            if (selected == null || selected.Count == 0)
            {
                _targetIndicator.Hide();
                return;
            }

            var views = new List<EntityView>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                if (_registry.TryGetView(selected[i], out EntityView view))
                    views.Add(view);
            }

            if (views.Count == 0)
            {
                _targetIndicator.Hide();
                return;
            }

            _targetIndicator.SetTargets(views);
            _inputRouter?.FaceTarget(selected[0]);
        }

        private void OnWorldVisibleChanged(bool visible) => ApplyWorldVisibility(visible);

        private void OnGameplayActiveChanged(bool active) => ApplyGameplayPresentation(active);

        private void ApplyWorldVisibility(bool visible)
        {
            if (!_bootstrapComplete)
            {
                _pendingWorldVisible = visible;
                return;
            }

            if (_worldRoot.gameObject.activeSelf != visible)
                _worldRoot.gameObject.SetActive(visible);

            if (_camera != null)
                _camera.enabled = visible;
        }

        private void ApplyGameplayPresentation(bool active)
        {
            if (!_bootstrapComplete)
            {
                _pendingGameplayActive = active;
                return;
            }

            _worldUi?.SetGameplayUiVisible(active);

            if (!active)
            {
                _targetIndicator?.Hide();
                return;
            }

            OnSelectedTargetsChanged();
        }

        private async UniTaskVoid BuildWorldAsync()
        {
            if (_bootstrap == null)
            {
                _logger?.LogError("[AbilitiesDemo] Gameplay root: missing bootstrap service.");
                return;
            }

            EnsureDefaultWorldLighting();
            _camera = CreateRuntimeCamera();
            _camera.transform.SetParent(_worldRoot, false);
            _camera.transform.position = DemoConstants.Camera.DefaultPosition;
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.zero - _camera.transform.position, Vector3.up);

            _targeting?.AttachCamera(_camera);
            _inputRouter?.AttachCamera(_camera);

            try
            {
                await _bootstrap.BootstrapAsync(_worldRoot);
                if (_disposed)
                    return;

                _targetIndicator?.SetGroundSurfaceY(_session != null ? _session.GroundSurfaceY : _bootstrap.GroundSurfaceY);
                _targetIndicator?.Prewarm();
                await _presentationBridge.PreloadPresentationAssetsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[AbilitiesDemo] Bootstrap failed: {ex.Message}", ex);
            }

            if (_disposed)
                return;

            try
            {
                _worldUi?.Initialize(_camera);
                ApplyWorldVisibility(_pendingWorldVisible);
                _combatFeedback?.Initialize(_worldUi, _camera, _poolFacade);
                _combatFeedback?.PrewarmPool(DemoConstants.Presentation.CombatFloatPrewarmCount);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[AbilitiesDemo] Combat feedback unavailable: {ex.Message}");
            }

            if (_disposed)
                return;

            AttachHealthBars();
            if (_disposed)
                return;

            WireCombatFeedback();
            if (_disposed)
                return;

            _bootstrapComplete = true;
            _tickHandler?.SetPresentationReady(true);
            ApplyGameplayPresentation(_demoFacade.IsGameplayActive);
            ApplyWorldVisibility(_demoFacade.IsWorldVisible);
        }

        private void AttachHealthBars()
        {
            if (_disposed || _worldUi == null)
                return;

            foreach (EntityCombatState state in _combatRegistry.All)
            {
                if (!_registry.TryGetView(state.EntityId, out EntityView view) || view == null)
                    continue;

                if (view.transform == null)
                    continue;

                if (view.HealthBar != null)
                {
                    if (view.HealthBar.IsAlive)
                        continue;

                    ReleaseViewHealthBar(view);
                }

                var barGo = new GameObject($"HealthBar_{state.DisplayName}", typeof(RectTransform));
                var bar = new EntityHealthBar(barGo.transform, view.transform, _logger);
                bar.Initialize(_worldUi);
                if (!bar.HasVisual)
                {
                    bar.Dispose();
                    continue;
                }

                view.AttachHealthBar(bar);
                bar.SetFill(state.CurrentHealth, state.MaxHealth);
            }
        }

        private void DisposeAllHealthBars()
        {
            foreach (EntityCombatState state in _combatRegistry.All)
            {
                if (_registry.TryGetView(state.EntityId, out EntityView view))
                    ReleaseViewHealthBar(view);
            }

            if (_registry.TryGetView(_registry.PlayerId, out EntityView playerView))
                ReleaseViewHealthBar(playerView);

            _worldUi?.Clear();
        }

        private static void ReleaseViewHealthBar(EntityView view)
        {
            if (view == null || view.HealthBar == null)
                return;

            view.HealthBar.Dispose();
            view.AttachHealthBar(null);
        }

        private void WireCombatFeedback()
        {
            foreach (EntityCombatState state in _combatRegistry.All)
            {
                var wire = new CombatEntityFeedbackWire(
                    state,
                    _registry,
                    _entityStateRegistry,
                    _combatFeedback,
                    _despawnService);
                _despawnService?.RegisterFeedbackWire(state.EntityId, wire);
                _combatWires.Add(wire);
            }
        }

        private void EnsureDefaultWorldLighting()
        {
            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length > 0)
                return;

            var lightGo = new GameObject("AbilitiesDemoSun");
            lightGo.transform.SetParent(_worldRoot, false);
            lightGo.transform.rotation = Quaternion.Euler(DemoConstants.Lighting.DefaultSunRotation);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = DemoConstants.Lighting.DefaultSunIntensity;
            light.shadows = LightShadows.None;

            if (lightGo.GetComponent<UniversalAdditionalLightData>() == null)
                lightGo.AddComponent<UniversalAdditionalLightData>();
        }

        private static Camera CreateRuntimeCamera()
        {
            var camGo = new GameObject("AbilitiesDemoCamera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = DemoConstants.Camera.FallbackBackgroundColor;
            cam.nearClipPlane = DemoConstants.Camera.NearClipPlane;
            cam.farClipPlane = DemoConstants.Camera.FarClipPlane;
            cam.depth = 0;
            cam.allowMSAA = false;

            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urpData = camGo.GetComponent<UniversalAdditionalCameraData>();
            if (urpData == null)
                urpData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;
            urpData.renderPostProcessing = false;

            return cam;
        }
    }
}
