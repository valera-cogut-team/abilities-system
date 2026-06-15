using System.Collections;
using AvantajPrim.AbilitiesDemo.Application;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.PlayMode
{
    public sealed class AbilitiesDemoGameplayTests
    {
        [UnityTest]
        public IEnumerator PlayerMovementService_ReachesDestinationAndStopsWalking()
        {
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.transform.position = Vector3.zero;
            EntityView view = playerGo.AddComponent<EntityView>();
            view.Configure(new EntityId(1), isPlayer: true);

            var registry = new DemoEntityRegistry();
            registry.SetPlayer(new EntityId(1), view);

            var entityStateRegistry = new EntityStateRegistry();
            TestPresentationFactory.CreateController(
                view,
                new EntityId(1),
                isPlayer: true,
                entityStateRegistry,
                new DemoCombatRegistry());

            var movement = new PlayerMovementService(registry, entityStateRegistry);

            movement.MoveTo(new Vector3(0f, 0f, 3f));

            float timeout = Time.realtimeSinceStartup + 2f;
            while (movement.IsMoving && Time.realtimeSinceStartup < timeout)
            {
                movement.OnUpdate(Time.deltaTime);
                yield return null;
            }

            Assert.IsFalse(movement.IsMoving);
            Assert.Greater(playerGo.transform.position.z, 2.5f);

            Object.Destroy(playerGo);
        }

        [UnityTest]
        public IEnumerator EntityView_ReturnsToIdleAfterConfigure()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(new EntityId(2), isPlayer: false, "Enemy");

            yield return null;

            view.ReturnToIdle();
            Assert.IsFalse(view == null);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator TargetingService_SelectsEnemyByCollider()
        {
            GameObject player = CreateTargetable(new EntityId(1), new Vector3(-2f, 0f, 0f), "Player");
            GameObject enemy = CreateTargetable(new EntityId(2), new Vector3(2f, 0f, 0f), "Enemy");

            var camGo = new GameObject("TestCamera");
            Camera camera = camGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 5f, -6f);
            camera.transform.LookAt(Vector3.zero);

            var targeting = new TargetingService();
            targeting.AttachCamera(camera);
            targeting.SetSingleSelection(new EntityId(2));

            Assert.AreEqual(new EntityId(2), targeting.SelectedTarget.Value);

            Object.Destroy(player);
            Object.Destroy(enemy);
            Object.Destroy(camGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetSelectionIndicator_ShowsRingWhenTargetSet()
        {
            const float groundY = 0f;
            (TargetSelectionIndicator indicator, GameObject indicatorGo) = TestPresentationFactory.CreateTargetIndicator();
            indicator.SetGroundSurfaceY(groundY);

            var targetGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetGo.transform.position = new Vector3(2f, 1f, 0f);
            EntityView view = targetGo.AddComponent<EntityView>();
            view.Configure(new EntityId(10), isPlayer: false, "Enemy");

            indicator.SetTarget(view);
            yield return null;

            Assert.IsTrue(indicatorGo.activeSelf);
            Assert.AreEqual(new EntityId(10), indicator.CurrentTarget);

            SpriteRenderer ring = indicatorGo.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(ring);
            Assert.IsTrue(ring.enabled, "Indicator ring should be enabled.");
            Assert.IsNotNull(ring.sprite, "Indicator should use a world-space ring sprite.");

            indicator.LateFollow();
            Assert.Greater(ring.transform.localScale.x, 0f);
            Assert.GreaterOrEqual(ring.transform.position.y, groundY, "Ring should sit on or above ground.");

            Object.Destroy(indicatorGo);
            Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator TargetSelectionIndicator_HidesWhenCleared()
        {
            (TargetSelectionIndicator indicator, GameObject indicatorGo) = TestPresentationFactory.CreateTargetIndicator();

            var targetGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            EntityView view = targetGo.AddComponent<EntityView>();
            view.Configure(new EntityId(11), isPlayer: false);

            indicator.SetTarget(view);
            yield return null;
            indicator.Hide();
            yield return null;

            Assert.IsNull(indicator.CurrentTarget);

            SpriteRenderer ring = indicatorGo.GetComponentInChildren<SpriteRenderer>();
            if (ring != null)
                Assert.IsFalse(ring.enabled);

            Object.Destroy(indicatorGo);
            Object.Destroy(targetGo);
        }

        private static GameObject CreateTargetable(EntityId id, Vector3 position, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Targeting");
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, id.Value == 1, name);
            return go;
        }
    }
}
