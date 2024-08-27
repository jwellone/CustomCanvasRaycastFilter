using UnityEngine;

#nullable enable

namespace jwellone
{
    public static class RectTransformUtility
    {
        static Plane _plane = new Plane();
        static Vector3 _worldPoint = Vector3.zero;

        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera? cam, out Vector2 localPoint)
        {
            var forward = rect.forward;
            var position = rect.position;
            if (cam == null)
            {
                _worldPoint.x = screenPoint.x;
                _worldPoint.y = screenPoint.y;
                _worldPoint.z = position.z + (-forward.x * (screenPoint.x - position.x) - forward.y * (screenPoint.y - position.y)) / forward.z;
                localPoint = rect.InverseTransformPoint(_worldPoint);
                return true;
            }

            var ray = cam.ScreenPointToRay(screenPoint, Camera.MonoOrStereoscopicEye.Mono);
            _plane.SetNormalAndPosition(forward, position);
            var num = Vector3.Dot(Vector3.Normalize(position - ray.origin), _plane.normal);
            var enter = 0f;
            if (num != 0f && !_plane.Raycast(ray, out enter))
            {
                localPoint = Vector2.zero;
                return false;
            }

            localPoint = rect.InverseTransformPoint(ray.GetPoint(enter));
            return true;
        }
    }
}