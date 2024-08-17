using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(Image))]
    public class ImageAlphaHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        Image? _image;

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            _image ??= GetComponent<Image>();
            var texture = _image.mainTexture as Texture2D;
            if (texture == null)
            {
                return float.MaxValue;
            }

            if (!texture.isReadable)
            {
                SetDebugRect(rectTransform.rect, Color.red);
                Debug.LogWarning($"Not a Read/Write setting for {texture.name}. Confirm setting.");
                return float.MaxValue;
            }

            var coord = localPoint / rectTransform.rect.size + rectTransform.pivot;
            var alpha = _image.color.a * texture.GetPixel((int)(coord.x * texture.width), (int)(coord.y * texture.height)).a;
            SetDebugRect(rectTransform.rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
            return alpha;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ImageAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}