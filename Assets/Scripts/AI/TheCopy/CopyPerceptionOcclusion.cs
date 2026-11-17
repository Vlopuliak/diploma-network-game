using UnityEngine;

namespace TheCopy.AI
{
    internal static class CopyPerceptionOcclusion
    {
        internal static bool IsBlocked(Vector3 origin, Vector3 destination, LayerMask layers,
            Transform observer, Transform source)
        {
            Vector3 offset = destination - origin;
            float distance = offset.magnitude;
            if (layers.value == 0 || distance <= 0.001f)
                return false;

            foreach (RaycastHit hit in Physics.RaycastAll(origin, offset / distance, distance,
                layers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(observer) ||
                    (source != null && hit.transform.IsChildOf(source)))
                    continue;

                return true;
            }

            return false;
        }
    }
}
