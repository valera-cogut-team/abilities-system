using AvantajPrim.AbilitiesDemo.Application;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class DemoGroundAlignmentTests
    {
        [Test]
        public void ResolveGroundSurfaceY_IgnoresNonGroundLayerChildren()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            Assume.That(groundLayer, Is.GreaterThanOrEqualTo(0), "Ground layer must exist in project settings.");

            var arena = new GameObject("Arena");
            try
            {
                arena.layer = groundLayer;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(arena.transform, false);
                floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
                floor.layer = groundLayer;

                var tree = new GameObject("Tree_01");
                tree.transform.SetParent(arena.transform, false);
                tree.layer = 0;
                BoxCollider treeCollider = tree.AddComponent<BoxCollider>();
                treeCollider.center = new Vector3(0f, 2.5f, 0f);
                treeCollider.size = new Vector3(1f, 5f, 1f);

                float surfaceY = DemoGroundAlignment.ResolveGroundSurfaceY(arena);

                Assert.That(surfaceY, Is.LessThan(1f));
                Assert.That(surfaceY, Is.GreaterThan(-0.5f));
            }
            finally
            {
                Object.DestroyImmediate(arena);
            }
        }

        [Test]
        public void SnapFeetToGround_UsesCapsuleBottom_NotRendererExtents()
        {
            var entity = new GameObject("Entity");
            try
            {
                CapsuleCollider capsule = entity.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.45f;
                capsule.center = new Vector3(0f, 1f, 0f);

                var robe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                robe.transform.SetParent(entity.transform, false);
                robe.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                robe.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);

                entity.transform.position = new Vector3(0f, 3f, 0f);
                DemoGroundAlignment.SnapFeetToGround(entity, 0f);

                Assert.That(capsule.bounds.min.y, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(entity);
            }
        }
    }
}
