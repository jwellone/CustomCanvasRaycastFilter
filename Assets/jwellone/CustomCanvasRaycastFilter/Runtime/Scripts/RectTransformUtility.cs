using UnityEngine;

#nullable enable

namespace jwellone
{
    public static class RectTransformUtility
    {
        public static Ray ScreenPointToRay(Camera? cam, Vector2 screenPos, float rayPositionZ = -100f)
        {
            if (cam != null)
            {
                return cam.ScreenPointToRay(screenPos);
            }

            Vector3 origin = screenPos;
            origin.z = rayPositionZ;
            return new Ray(origin, Vector3.forward);
        }

        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera? cam, out Vector3 worldPoint, float rayPositionZ = -100f)
        {
            worldPoint = Vector2.zero;
            Ray ray = ScreenPointToRay(cam, screenPoint, rayPositionZ);
            Plane plane = new Plane(rect.rotation * Vector3.back, rect.position);
            float enter = 0f;
            float num = Vector3.Dot(Vector3.Normalize(rect.position - ray.origin), plane.normal);
            if (num != 0f && !plane.Raycast(ray, out enter))
            {
                return false;
            }

            worldPoint = ray.GetPoint(enter);
            return true;
        }

        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera? cam, out Vector2 localPoint, float rayPositionZ = -100f)
        {
            localPoint = Vector2.zero;
            if (ScreenPointToWorldPointInRectangle(rect, screenPoint, cam, out var worldPoint, rayPositionZ))
            {
                localPoint = rect.InverseTransformPoint(worldPoint);
                return true;
            }

            return false;
        }
    }
}