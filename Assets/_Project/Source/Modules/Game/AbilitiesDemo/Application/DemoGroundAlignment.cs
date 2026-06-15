using AvantajPrim.AbilitiesDemo.Domain;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class DemoGroundAlignment
    {
        public static float ResolveGroundSurfaceY(GameObject arenaRoot)
        {
            if (arenaRoot == null)
                return 0f;

            int groundLayer = LayerMask.NameToLayer(DemoConstants.Layers.Ground);
            bool hasGroundLayer = groundLayer >= 0;
            float maxY = float.NegativeInfinity;

            Collider[] colliders = arenaRoot.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                if (hasGroundLayer && collider.gameObject.layer != groundLayer)
                    continue;

                maxY = Mathf.Max(maxY, collider.bounds.max.y);
            }

            if (maxY > float.NegativeInfinity)
                return maxY;

            Renderer[] renderers = arenaRoot.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (hasGroundLayer && renderer.gameObject.layer != groundLayer)
                    continue;

                maxY = Mathf.Max(maxY, renderer.bounds.max.y);
            }

            return maxY > float.NegativeInfinity ? maxY : 0f;
        }

        public static float GetFootWorldY(GameObject entity)
        {
            if (entity == null)
                return 0f;

            CapsuleCollider capsule = entity.GetComponentInChildren<CapsuleCollider>();
            if (capsule != null && capsule.enabled)
                return capsule.bounds.min.y;

            float minY = float.PositiveInfinity;
            Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                minY = Mathf.Min(minY, renderer.bounds.min.y);
            }

            return minY < float.PositiveInfinity ? minY : entity.transform.position.y;
        }

        public static void SnapFeetToGround(GameObject entity, float groundY)
        {
            if (entity == null)
                return;

            float footY = GetFootWorldY(entity);
            float delta = groundY - footY;
            if (Mathf.Abs(delta) < DemoConstants.Physics.SnapEpsilon)
                return;

            Vector3 position = entity.transform.position;
            position.y += delta;
            entity.transform.position = position;
        }

        public static Vector3 GetRingPosition(Transform entity, float groundSurfaceY, float heightOffset)
        {
            if (entity == null)
                return new Vector3(0f, groundSurfaceY + heightOffset, 0f);

            float footY = GetFootWorldY(entity.gameObject);
            Vector3 position = entity.position;
            position.y = Mathf.Max(footY, groundSurfaceY) + heightOffset;
            return position;
        }
    }
}
