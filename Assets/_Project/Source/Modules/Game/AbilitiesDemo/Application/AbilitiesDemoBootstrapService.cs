using System;
using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using Effects.Facade;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using Logger.Facade;
using StateMachine.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class AbilitiesDemoBootstrapService
    {
        private readonly IAddressablesFacade _addressables;
        private readonly IAbilitiesFacade _abilities;
        private readonly AbilityCatalog _catalog;
        private readonly DemoEntityRegistry _registry;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly IStateMachineFacade _stateMachineFacade;
        private readonly IAbilityPresentationPort _presentation;
        private readonly IAbilityCastLifecycle _castLifecycle;
        private readonly IEffectsFacade _effects;
        private readonly DemoGameplaySession _session;
        private readonly ILoggerFacade _logger;
        private readonly EntityAttachedVfxRegistry _entityVfxRegistry;
        private readonly DemoAddressableCatalog _addressCatalog;

        private float _groundSurfaceY;
        private Transform _arenaRoot;

        public float GroundSurfaceY => _groundSurfaceY;

        public AbilitiesDemoBootstrapService(
            IAddressablesFacade addressables,
            IAbilitiesFacade abilities,
            AbilityCatalog catalog,
            DemoEntityRegistry registry,
            DemoCombatRegistry combatRegistry,
            EntityStateRegistry entityStateRegistry,
            IStateMachineFacade stateMachineFacade,
            IAbilityPresentationPort presentation,
            IAbilityCastLifecycle castLifecycle,
            IEffectsFacade effects,
            DemoGameplaySession session,
            ILoggerFacade logger,
            EntityAttachedVfxRegistry entityVfxRegistry,
            DemoAddressableCatalog addressCatalog)
        {
            _addressables = addressables;
            _abilities = abilities;
            _catalog = catalog;
            _registry = registry;
            _combatRegistry = combatRegistry;
            _entityStateRegistry = entityStateRegistry;
            _stateMachineFacade = stateMachineFacade;
            _presentation = presentation;
            _castLifecycle = castLifecycle;
            _effects = effects;
            _session = session;
            _logger = logger;
            _entityVfxRegistry = entityVfxRegistry;
            _addressCatalog = addressCatalog;
        }

        public async UniTask BootstrapAsync(Transform root, CancellationToken cancellationToken = default)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            await _addressables.EnsureInitializedAsync();

            _registry.Clear();
            _combatRegistry.Clear();
            _entityStateRegistry.Clear();

            DemoAddressableCatalog.DecorEntry[] placements = _addressCatalog?.ExtraDecor ?? System.Array.Empty<DemoAddressableCatalog.DecorEntry>();
            var decorLoadTasks = new UniTask<GameObject>[placements.Length];
            for (int i = 0; i < placements.Length; i++)
                decorLoadTasks[i] = TryLoadDemoPrefabAsync(placements[i].Prefab, cancellationToken);

            UniTask<GameObject> arenaLoadTask = TryLoadDemoPrefabAsync(_addressCatalog?.Arena, cancellationToken);
            UniTask<GameObject> playerLoadTask = TryLoadDemoPrefabAsync(_addressCatalog?.Player, cancellationToken);
            UniTask<GameObject> enemyLoadTask = TryLoadDemoPrefabAsync(_addressCatalog?.Enemy, cancellationToken);
            var prefabsTask = UniTask.WhenAll(
                UniTask.WhenAll(decorLoadTasks),
                arenaLoadTask,
                UniTask.WhenAll(playerLoadTask, enemyLoadTask));
            UniTask abilitiesTask = LoadAbilitiesAsync(cancellationToken);

            (GameObject[] decorPrefabs, GameObject arenaPrefab, (GameObject, GameObject) entityPrefabs) = await prefabsTask;
            await abilitiesTask;

            (GameObject playerPrefab, GameObject enemyPrefab) = entityPrefabs;

            InstantiateArena(root, arenaPrefab);
            SpawnExtraArenaDecor(_arenaRoot, placements, decorPrefabs);
            SpawnEntities(root, playerPrefab, enemyPrefab);

            _logger?.LogInfo(
                $"[AbilitiesDemo] Bootstrap complete. Player + {_registry.EnemyIds.Count} enemies spawned.");
        }

        private async UniTask<GameObject> TryLoadDemoPrefabAsync(UnityEngine.AddressableAssets.AssetReferenceGameObject reference, CancellationToken cancellationToken)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
                return null;

            try
            {
                return await _addressables.TryLoadPrefabAsync(reference)
                    .AttachExternalCancellation(cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(DemoConstants.Bootstrap.PrefabLoadTimeoutSeconds));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[AbilitiesDemo] Prefab '{reference.TryGetRuntimeKey()}' unavailable: {ex.Message}");
                return null;
            }
        }

        private static GameObject InstantiateUnder(Transform root, GameObject prefab, Vector3 localPosition)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, root);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        private void InstantiateArena(Transform root, GameObject arenaPrefab)
        {
            GameObject arena;
            if (arenaPrefab == null)
            {
                arena = CreateFallbackArena(root);
                _logger?.LogInfo("[AbilitiesDemo] Using fallback arena primitive.");
            }
            else
            {
                arena = InstantiateUnder(root, arenaPrefab, Vector3.zero);
                arena.transform.localScale = DemoConstants.Spawn.ArenaScale;
            }

            EnsureGroundLayer(arena);
            DisableDecorColliders(arena);
            _arenaRoot = arena.transform;
            _groundSurfaceY = DemoGroundAlignment.ResolveGroundSurfaceY(arena);
            _session?.SetGroundSurfaceY(_groundSurfaceY);
        }

        private static void SpawnExtraArenaDecor(
            Transform arenaRoot,
            DemoAddressableCatalog.DecorEntry[] placements,
            GameObject[] prefabs)
        {
            if (arenaRoot == null || placements.Length == 0)
                return;

            var decorRoot = new GameObject("ArenaDecor");
            decorRoot.transform.SetParent(arenaRoot, false);

            for (int i = 0; i < placements.Length; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null)
                    continue;

                DemoAddressableCatalog.DecorEntry placement = placements[i];
                GameObject instance = InstantiateUnder(decorRoot.transform, prefab, placement.LocalPosition);
                instance.transform.localRotation = Quaternion.Euler(0f, placement.RotationY, 0f);
                if (Mathf.Abs(placement.Scale - 1f) > DemoConstants.Physics.ScaleComparisonEpsilon)
                    instance.transform.localScale *= placement.Scale;
            }

            DisableDecorColliders(decorRoot);
        }

        private void SpawnEntities(Transform root, GameObject playerPrefab, GameObject enemyPrefab)
        {
            GameObject playerGo;
            if (playerPrefab == null)
            {
                playerGo = CreateFallbackEntity(root, "DemoPlayer", DemoConstants.Spawn.PlayerPosition, Color.cyan);
                _logger?.LogInfo("[AbilitiesDemo] Using fallback player primitive.");
            }
            else
            {
                playerGo = InstantiateUnder(root, playerPrefab, DemoConstants.Spawn.PlayerPosition);
            }

            DemoGroundAlignment.SnapFeetToGround(playerGo, _groundSurfaceY);

            var playerId = new EntityId(1);
            EntityView playerView = EnsureEntityView(playerGo, playerId, isPlayer: true, DemoConstants.Entity.PlayerDisplayName);
            _registry.SetPlayer(playerId, playerView);
            _abilities.RegisterEntity(playerView.EntityModel);
            RegisterCombat(playerId, DemoConstants.Entity.PlayerDisplayName);
            EnsureTargetingLayer(playerGo);

            var enemyGos = new List<GameObject>(DemoConstants.Entity.EnemyCount);

            for (int i = 0; i < DemoConstants.Entity.EnemyCount; i++)
            {
                GameObject enemyGo;
                if (enemyPrefab != null)
                {
                    enemyGo = InstantiateUnder(root, enemyPrefab, DemoConstants.Spawn.EnemyPositions[i]);
                    TintEntity(enemyGo, DemoConstants.Entity.EnemyTintColors[i]);
                }
                else
                {
                    enemyGo = CreateFallbackEntity(
                        root,
                        DemoConstants.Entity.EnemyDisplayNames[i],
                        DemoConstants.Spawn.EnemyPositions[i],
                        DemoConstants.Entity.EnemyTintColors[i]);
                }

                DemoGroundAlignment.SnapFeetToGround(enemyGo, _groundSurfaceY);
                enemyGos.Add(enemyGo);
                var enemyId = new EntityId(i + 2);
                EntityView enemyView = EnsureEntityView(enemyGo, enemyId, isPlayer: false, DemoConstants.Entity.EnemyDisplayNames[i]);
                _registry.AddEnemy(enemyId, enemyView);
                _abilities.RegisterEntity(enemyView.EntityModel);
                RegisterCombat(enemyId, DemoConstants.Entity.EnemyDisplayNames[i]);
                EnsureTargetingLayer(enemyGo);
            }

            ApplyInitialFacing(playerGo, enemyGos);
        }

        private async UniTask LoadAbilitiesAsync(CancellationToken cancellationToken)
        {
            int count = await AbilityAddressableDiscovery.LoadAllIntoCatalogAsync(
                _addressables, _catalog, cancellationToken);

            if (count == 0)
                _logger?.LogWarning("[AbilitiesDemo] No ability configs loaded from Addressables.");
        }

        private void RegisterCombat(EntityId id, string name)
        {
            var state = new EntityCombatState(id, name, DemoConstants.Entity.DefaultMaxHealth);
            _combatRegistry.Register(state);
        }

        private EntityView EnsureEntityView(GameObject go, EntityId id, bool isPlayer, string displayName)
        {
            EnsureCapsuleCollider(go);
            EnsureVfxSpawnPoint(go);

            EntityView view = go.GetComponent<EntityView>();
            if (view == null)
                view = go.AddComponent<EntityView>();
            view.Configure(id, isPlayer, displayName);

            var controller = new EntityStateMachineController(
                id,
                view,
                isPlayer,
                _stateMachineFacade,
                _entityStateRegistry,
                _combatRegistry,
                _presentation,
                _castLifecycle,
                _effects,
                _addressables,
                _entityVfxRegistry);

            return view;
        }

        private static void EnsureCapsuleCollider(GameObject go)
        {
            if (go.GetComponentInChildren<CapsuleCollider>() != null)
                return;

            CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
            collider.height = DemoConstants.Entity.CapsuleHeight;
            collider.radius = DemoConstants.Entity.CapsuleRadius;
            collider.center = DemoConstants.Entity.CapsuleCenter;
        }

        private static void EnsureVfxSpawnPoint(GameObject go)
        {
            if (go.transform.Find(DemoConstants.ObjectNames.VfxSpawnPoint) != null)
                return;

            var spawn = new GameObject(DemoConstants.ObjectNames.VfxSpawnPoint);
            spawn.transform.SetParent(go.transform, false);
            spawn.transform.localPosition = DemoConstants.Entity.VfxSpawnLocalPosition;
        }

        private static void EnsureTargetingLayer(GameObject go)
        {
            int layer = LayerMask.NameToLayer(DemoConstants.Layers.Targeting);
            if (layer < 0)
                return;

            SetLayerRecursive(go, layer);
        }

        private static void EnsureGroundLayer(GameObject go)
        {
            int layer = LayerMask.NameToLayer(DemoConstants.Layers.Ground);
            if (layer < 0 || go == null)
                return;

            // Only the arena floor root carries Ground — not trees/rocks (their collider bounds.max.y would skew surface Y).
            go.layer = layer;
        }

        private static void DisableDecorColliders(GameObject arenaRoot)
        {
            if (arenaRoot == null)
                return;

            Collider[] colliders = arenaRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.gameObject == arenaRoot)
                    continue;

                collider.enabled = false;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private static void ApplyInitialFacing(GameObject player, IReadOnlyList<GameObject> enemies)
        {
            if (player == null || enemies == null || enemies.Count == 0)
                return;

            Vector3 enemyCenter = Vector3.zero;
            for (int i = 0; i < enemies.Count; i++)
                enemyCenter += enemies[i].transform.position;
            enemyCenter /= enemies.Count;

            FaceHorizontal(player.transform, enemyCenter);
            for (int i = 0; i < enemies.Count; i++)
                FaceHorizontal(enemies[i].transform, player.transform.position);
        }

        private static void FaceHorizontal(Transform from, Vector3 worldTarget)
        {
            Vector3 direction = worldTarget - from.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < DemoConstants.Physics.DirectionEpsilonSqr)
                return;

            from.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void TintEntity(GameObject go, Color color)
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial == null)
                    continue;

                renderer.GetPropertyBlock(block);
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", color);
                else if (renderer.sharedMaterial.HasProperty("_Color"))
                    block.SetColor("_Color", color);

                renderer.SetPropertyBlock(block);
            }
        }

        private static GameObject CreateFallbackArena(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = DemoConstants.ObjectNames.FallbackArena;
            ground.transform.SetParent(root, false);
            ground.transform.localScale = DemoConstants.Spawn.ArenaScale;
            return ground;
        }

        private static GameObject CreateFallbackEntity(Transform root, string name, Vector3 localPosition, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPosition;

            TintEntity(go, color);
            return go;
        }
    }
}
