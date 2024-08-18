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

            var sprite = _image.sprite;
            switch (_image.type)
            {
                case Image.Type.Filled:
                    {
                        if (_image.fillMethod == Image.FillMethod.Horizontal || _image.fillMethod == Image.FillMethod.Vertical)
                        {
                            var rate = _image.fillMethod == Image.FillMethod.Horizontal
                                        ? localPoint.x / (sprite.textureRect.size.x * rectTransform.rect.size.x / sprite.rect.size.x) + rectTransform.pivot.x
                                        : localPoint.y / (sprite.textureRect.size.y * rectTransform.rect.size.y / sprite.rect.size.y) + rectTransform.pivot.y;

                            var min = 0f;
                            var max = _image.fillAmount;
                            if (_image.fillOrigin == (int)Image.OriginHorizontal.Right || _image.fillOrigin == (int)Image.OriginVertical.Top)
                            {
                                min = 1f - max;
                                max = 1f;
                            }
                            if (min > rate || rate > max)
                            {
                                SetDebugRect(rectTransform.rect, Color.white);
                                return float.MinValue;
                            }
                        }
                    }
                    break;
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