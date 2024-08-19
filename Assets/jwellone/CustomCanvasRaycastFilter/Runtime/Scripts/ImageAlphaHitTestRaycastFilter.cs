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
                        switch (_image.fillMethod)
                        {
                            case Image.FillMethod.Vertical: return ValidToFillMethodHorizontalOrVertical(localPoint, _image);
                            case Image.FillMethod.Horizontal: return ValidToFillMethodHorizontalOrVertical(localPoint, _image);
                            case Image.FillMethod.Radial90: return ValidToFillMethodRadial90(localPoint, _image);
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
            var fillAmount = image.fillAmount;
            var coord = localPoint / (sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size) + rectTransform.pivot;
            if (fillOrigin == (int)Image.Origin90.BottomLeft)
            {
                var dir = (coord - Vector2.zero).normalized;
                var angle = Vector2ToAngle(dir);

                angle /= 90f;
                var min = 0f;
                var max = fillAmount;
                if (image.fillClockwise)
                {
                    min = 1f - fillAmount;
                    max = 1f;
                }

                if (min > angle || angle > max)
                {
                    return false;
                }
            }
            else if (fillOrigin == (int)Image.Origin90.BottomRight)
            {
                var dir = (coord - new Vector2(1, 0)).normalized;
                var angle = Vector2ToAngle(dir);
                angle = (180 - angle) / 90;

                var min = 1f - fillAmount;
                var max = 1f;
                if (image.fillClockwise)
                {
                    min = 0;
                    max = fillAmount;
                }

                if (min > angle || angle > max)
                {
                    return false;
                }
            }
            else if (fillOrigin == (int)Image.Origin90.TopLeft)
            {
                var dir = (coord - new Vector2(0, 1)).normalized;
                var angle = Vector2ToAngle(dir);
                angle = angle / -90f;

                var min = 1f - fillAmount;
                var max = 1f;
                if (image.fillClockwise)
                {
                    min = 0;
                    max = fillAmount;
                }

                if (min > angle || angle > max)
                {
                    return false;
                }
            }
            else if (fillOrigin == (int)Image.Origin90.TopRight)
            {
                var dir = (coord - new Vector2(1, 1)).normalized;
                var angle = Vector2ToAngle(dir);
                angle = (180 + angle) / 90f;

                var min = 0f;
                var max = fillAmount;
                if (image.fillClockwise)
                {
                    min = 1f - fillAmount;
                    max = 1f;
                }

                if (min > angle || angle > max)
                {
                    return false;
                }
            }
            return true;
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

        float Vector2ToAngle(in Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ImageAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}