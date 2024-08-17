using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class RawImageAlphaHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        RawImage? _rawImage;

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            _rawImage ??= GetComponent<RawImage>();
            var texture = _rawImage.mainTexture as Texture2D;
            if (texture == null)
            {
                return float.MaxValue;
            }

            if (!texture.isReadable)
            {
                Debug.LogWarning($"Not a Read/Write setting for {texture.name}. Confirm setting.");
                SetDebugRect(rectTransform.rect, Color.red);
                return float.MaxValue;
            }

            var cood = localPoint / rectTransform.rect.size + rectTransform.pivot;
            var alpha = _rawImage.color.a * texture.GetPixel((int)(cood.x * texture.width), (int)(cood.y * texture.height)).a;
            SetDebugRect(rectTransform.rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
            return alpha;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(RawImageAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}