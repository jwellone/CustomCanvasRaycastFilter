using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(Image))]
    public class ImageAlphaHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        const float PI = Mathf.PI;
        const float TWO_PI = PI * 2f;
        const float HALF_PI = PI * 0.5f;

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

            if (!ValidType(localPoint))
            {
                SetDebugRect(rectTransform.rect, Color.white);
                return float.MinValue;
            }

            var coord = localPoint / rectTransform.rect.size + rectTransform.pivot;
            var alpha = _image.color.a * texture.GetPixel((int)(coord.x * texture.width), (int)(coord.y * texture.height)).a;
            SetDebugRect(rectTransform.rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
            return alpha;
        }

        bool ValidType(in Vector2 localPoint)
        {
            switch (_image!.type)
            {
                case Image.Type.Filled:
                    {
                        if (_image!.fillAmount >= 1f)
                        {
                            return true;
                        }
                        else if (_image.fillAmount <= 0f)
                        {
                            return false;
                        }

                        switch (_image.fillMethod)
                        {
                            case Image.FillMethod.Vertical: return ValidToFillMethodHorizontalOrVertical(localPoint, _image);
                            case Image.FillMethod.Horizontal: return ValidToFillMethodHorizontalOrVertical(localPoint, _image);
                            case Image.FillMethod.Radial90: return ValidToFillMethodRadial90(localPoint, _image);
                            case Image.FillMethod.Radial180: return ValidToFillMethodRadial180(localPoint, _image);
                            case Image.FillMethod.Radial360: return ValidToFillMethodRadial360(localPoint, _image);
                            default: break;
                        }
                    }
                    break;
            }

            return true;
        }

        bool ValidToFillMethodHorizontalOrVertical(in Vector2 localPoint, Image image)
        {
            var sprite = image.sprite;
            var rate = image.fillMethod == Image.FillMethod.Horizontal
                                        ? localPoint.x / (sprite.textureRect.size.x * rectTransform.rect.size.x / sprite.rect.size.x) + rectTransform.pivot.x
                                        : localPoint.y / (sprite.textureRect.size.y * rectTransform.rect.size.y / sprite.rect.size.y) + rectTransform.pivot.y;

            var min = 0f;
            var max = image.fillAmount;
            if (image.fillOrigin == (int)Image.OriginHorizontal.Right || image.fillOrigin == (int)Image.OriginVertical.Top)
            {
                min = 1f - max;
                max = 1f;
            }
            if (min > rate || rate > max)
            {
                return false;
            }

            return true;
        }

        bool ValidToFillMethodRadial90(in Vector2 localPoint, Image image)
        {
            var sprite = image.sprite;
            var fillOrigin = image.fillOrigin;
            var coord = localPoint / (sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size) + rectTransform.pivot;
            var rad = 0f;

            if (fillOrigin == (int)Image.Origin90.BottomLeft)
            {
                rad = Mathf.Atan2(coord.y, coord.x);
            }
            else if (fillOrigin == (int)Image.Origin90.BottomRight)
            {
                rad = Mathf.Atan2(1f - coord.x, coord.y);
            }
            else if (fillOrigin == (int)Image.Origin90.TopLeft)
            {
                rad = Mathf.Atan2(coord.x, 1f - coord.y);
            }
            else if (fillOrigin == (int)Image.Origin90.TopRight)
            {
                rad = Mathf.Atan2(1f - coord.y, 1f - coord.x);
            }

            rad /= HALF_PI;

            var min = 0f;
            var max = image.fillAmount;
            if (image.fillClockwise)
            {
                min = 1f - max;
                max = 1f;
            }

            return !(min > rad || rad > max);
        }

        bool ValidToFillMethodRadial180(in Vector2 localPoint, Image image)
        {
            var sprite = image.sprite;
            var fillOrigin = image.fillOrigin;
            var texSize = sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size;
            var texHalfSize = texSize / 2f;
            var fillAmount = _image!.fillAmount * Mathf.PI;
            var rad = float.MinValue;

            if (fillOrigin == (int)Image.Origin180.Bottom)
            {
                var x = -Mathf.Cos(fillAmount) * texHalfSize.x;
                var y = -texHalfSize.y + Mathf.Sin(fillAmount) * texSize.y;
                rad = Mathf.Atan2(localPoint.y + texHalfSize.y, -localPoint.x);
                fillAmount = Mathf.Atan2(y + texHalfSize.y, -x);
            }
            else if (fillOrigin == (int)Image.Origin180.Top)
            {
                var x = Mathf.Cos(fillAmount) * texHalfSize.x;
                var y = texHalfSize.y - Mathf.Sin(fillAmount) * texSize.y;
                rad = -Mathf.Atan2(localPoint.y - texHalfSize.y, localPoint.x);
                fillAmount = -Mathf.Atan2(y - texHalfSize.y, x);
            }
            else if (fillOrigin == (int)Image.Origin180.Left)
            {
                var x = -texHalfSize.x + Mathf.Sin(fillAmount) * texSize.x;
                var y = Mathf.Cos(fillAmount) * texHalfSize.y;
                rad = Mathf.Atan2(localPoint.x + texHalfSize.x, localPoint.y);
                fillAmount = Mathf.Atan2(x + texHalfSize.x, y);
            }
            else if (fillOrigin == (int)Image.Origin180.Right)
            {
                var x = texHalfSize.x - Mathf.Sin(fillAmount) * texSize.x;
                var y = -Mathf.Cos(fillAmount) * texHalfSize.y;
                rad = Mathf.Atan2(texHalfSize.x - localPoint.x, -localPoint.y);
                fillAmount = Mathf.Atan2(texHalfSize.x - x, -y);
            }

            var min = 0f;
            var max = fillAmount;
            if (!_image!.fillClockwise)
            {
                min = Mathf.PI - max;
                max = Mathf.PI;
            }

            return !(min > rad || rad > max);
        }


        bool ValidToFillMethodRadial360(in Vector2 localPoint, Image image)
        {
            var sprite = image.sprite;
            var coord = localPoint / (sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size) + rectTransform.pivot;
            var offsets = new[] { -HALF_PI, -PI, HALF_PI, 0f };
            var amount = Mathf.Atan2(0.5f - coord.y, 0.5f - coord.x) + offsets[image.fillOrigin];

            while (amount < 0)
            {
                amount += TWO_PI;
            }

            while (amount > TWO_PI)
            {
                amount -= TWO_PI;
            }

            amount /= TWO_PI;

            var min = 0f;
            var max = image.fillAmount;
            if (image.fillClockwise)
            {
                min = 1f - max;
                max = 1f;
            }

            return !(min > amount || amount > max);
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ImageAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}