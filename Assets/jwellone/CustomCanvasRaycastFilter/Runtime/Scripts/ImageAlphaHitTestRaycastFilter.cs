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

        delegate float GetAlphaFunc(in Vector2 localPoint, in Texture2D texture, in Image image);
        readonly static GetAlphaFunc[] _getAlphaFuncs = new GetAlphaFunc[]
        {
            GetAlphaForTypeSimple,
            GetAlphaForTypeSliced,
            GetAlphaForTypeTiled,
            GetAlphaForTypeFilled
        };

        delegate void SliceToRectFunc(in Vector4 border, float width, float height, ref Rect rect);
        readonly static SliceToRectFunc[] _sliceToRectFuncs = new SliceToRectFunc[]
        {
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(border.x, border.y, width - (border.x + border.z), height - (border.y + border.w));},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(border.x, height - border.w, width - (border.x + border.z), border.w);},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(border.x, 0, width - (border.x + border.z), border.y);},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(0, height - border.w, border.x, border.w);},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(0, border.y, border.x, height - (border.y + border.w));},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(0, 0, border.x, border.y);},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(width - border.z, height - border.w, border.z, border.w);},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(width - border.z, border.y, border.z, height - (border.y + border.w));},
            (in Vector4 border, float width, float height, ref Rect rect) => {rect.Set(width - border.z, 0, border.z, border.y);}
        };

        static Rect _rect = Rect.zero;

        Image? _image;

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            _image ??= GetComponent<Image>();
            var texture = _image.mainTexture as Texture2D;
            if (texture == null)
            {
                return float.MaxValue;
            }

#if UNITY_EDITOR
            if (!texture.isReadable)
            {
                SetDebugRect(rectTransform.rect, Color.red);
                Debug.LogWarning($"Not a Read/Write setting for {texture.name}. Confirm setting.");
                return float.MaxValue;
            }
#endif

            var alpha = _image.color.a * _getAlphaFuncs[(int)_image.type](localPoint, texture, _image);
            SetDebugRect(rectTransform.rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
            return alpha;
        }

        static float GetAlphaForTypeSimple(in Vector2 localPoint, in Texture2D texture, in Image image)
        {
            var coord = localPoint / image.rectTransform.rect.size + image.rectTransform.pivot;
            return texture.GetPixel((int)(coord.x * texture.width), (int)(coord.y * texture.height)).a;
        }

        static float GetAlphaForTypeSliced(in Vector2 localPoint, in Texture2D texture, in Image image)
        {
            var sprite = image.sprite;
            var texRect = sprite.rect;
            var border = sprite.border;
            var scaleBorder = border / image.pixelsPerUnitMultiplier;
            var rectTransform = image.rectTransform;
            var size = rectTransform.rect.size;
            var coord = localPoint + size * rectTransform.pivot;

            var func = _sliceToRectFuncs[0];
            func(scaleBorder, size.x, size.y, ref _rect);

            if (_rect.Contains(coord))
            {
                if (!image.fillCenter)
                {
                    return float.MinValue;
                }

                var normal = Rect.PointToNormalized(_rect, coord);
                func(border, texRect.width, texRect.height, ref _rect);
                return sprite.texture.GetPixel((int)(_rect.x + _rect.width * normal.x), (int)(_rect.y + _rect.height * normal.y)).a;
            }

            for (var i = 1; i < _sliceToRectFuncs.Length; ++i)
            {
                func = _sliceToRectFuncs[i];
                func(scaleBorder, size.x, size.y, ref _rect);
                if (!_rect.Contains(coord))
                {
                    continue;
                }

                var normal = Rect.PointToNormalized(_rect, coord);
                func(border, texRect.width, texRect.height, ref _rect);
                return sprite.texture.GetPixel((int)(_rect.x + _rect.width * normal.x), (int)(_rect.y + _rect.height * normal.y)).a;
            }

            return float.MinValue;
        }

        static float GetAlphaForTypeTiled(in Vector2 localPoint, in Texture2D texture, in Image image)
        {
            var sprite = image.sprite;
            var pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
            var border = sprite.border;
            var scaleBorder = border / pixelsPerUnitMultiplier;
            var rectTransform = image.rectTransform;
            var size = rectTransform.rect.size;
            var coord = localPoint + size * rectTransform.pivot;

            _sliceToRectFuncs[0](scaleBorder, size.x, size.y, ref _rect);
            if (!image.fillCenter && _rect.Contains(coord))
            {
                return float.MinValue;
            }

            float Calc(float coord, float size, float texSize, float border1, float border2, float scaleBorder1, float scaleBorder2)
            {
                if (coord < scaleBorder1)
                {
                    coord = coord * border1 / scaleBorder1;
                }
                else if (coord >= scaleBorder1 && coord <= size - scaleBorder2)
                {
                    coord = border1 + (coord - scaleBorder1) * pixelsPerUnitMultiplier % (texSize - (border1 + border2));
                }
                else if (coord > size - scaleBorder2)
                {
                    coord = texSize - border2 + (coord - (size - scaleBorder2)) * border2 / scaleBorder2;
                }

                return coord;
            }

            var texRect = sprite.rect;
            coord.x = Calc(coord.x, size.x, texRect.width, border.x, border.z, scaleBorder.x, scaleBorder.z);
            coord.y = Calc(coord.y, size.y, texRect.height, border.y, border.w, scaleBorder.y, scaleBorder.w);
            return sprite.texture.GetPixel((int)coord.x, (int)coord.y).a;
        }

        static float GetAlphaForTypeFilled(in Vector2 localPoint, in Texture2D texture, in Image image)
        {
            if (0 >= image.fillAmount || image.fillAmount >= 1f)
            {
                return GetAlphaForTypeSimple(localPoint, texture, image);
            }

            var rectTransform = image.rectTransform;
            var sprite = image.sprite;
            var fillOrigin = image.fillOrigin;
            var texSize = sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size;
            var fillAmount = image.fillAmount;
            var rad = float.MinValue;
            var fillClockwise = image.fillClockwise;
            switch (image.fillMethod)
            {
                case Image.FillMethod.Vertical:
                    {
                        rad = localPoint.y / texSize.y + rectTransform.pivot.y;
                        fillClockwise = fillOrigin == (int)Image.OriginVertical.Top;
                    }
                    break;
                case Image.FillMethod.Horizontal:
                    {
                        rad = localPoint.x / texSize.x + rectTransform.pivot.x;
                        fillClockwise = fillOrigin == (int)Image.OriginHorizontal.Right;
                    }
                    break;
                case Image.FillMethod.Radial90:
                    {
                        var coord = localPoint / (sprite.textureRect.size * rectTransform.rect.size / sprite.rect.size) + rectTransform.pivot;
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
                    }
                    break;

                case Image.FillMethod.Radial180:
                    {
                        var texHalfSize = texSize / 2f;
                        fillAmount *= Mathf.PI;
                        if (fillOrigin == (int)Image.Origin180.Bottom)
                        {
                            rad = Mathf.Atan2(localPoint.y + texHalfSize.y, localPoint.x);
                            fillAmount = Mathf.Atan2(Mathf.Sin(fillAmount) * texSize.y, Mathf.Cos(fillAmount) * texHalfSize.x);
                        }
                        else if (fillOrigin == (int)Image.Origin180.Top)
                        {
                            rad = -Mathf.Atan2(localPoint.y - texHalfSize.y, -localPoint.x);
                            fillAmount = Mathf.Atan2(Mathf.Sin(fillAmount) * texSize.y, Mathf.Cos(fillAmount) * texHalfSize.x);
                        }
                        else if (fillOrigin == (int)Image.Origin180.Left)
                        {
                            rad = Mathf.Atan2(localPoint.x + texHalfSize.x, -localPoint.y);
                            fillAmount = Mathf.Atan2(Mathf.Sin(fillAmount) * texSize.x, Mathf.Cos(fillAmount) * texHalfSize.y);
                        }
                        else if (fillOrigin == (int)Image.Origin180.Right)
                        {
                            rad = -Mathf.Atan2(localPoint.x - texHalfSize.x, localPoint.y);
                            fillAmount = Mathf.Atan2(Mathf.Sin(fillAmount) * texSize.x, Mathf.Cos(fillAmount) * texHalfSize.y);
                        }

                        rad /= Mathf.PI;
                        fillAmount /= Mathf.PI;
                    }
                    break;
                case Image.FillMethod.Radial360:
                    {
                        var coord = localPoint / texSize + rectTransform.pivot;
                        var offsets = new[] { -HALF_PI, -PI, HALF_PI, 0f };
                        rad = Mathf.Atan2(0.5f - coord.y, 0.5f - coord.x) + offsets[fillOrigin];

                        while (rad < 0)
                        {
                            rad += TWO_PI;
                        }

                        while (rad > TWO_PI)
                        {
                            rad -= TWO_PI;
                        }

                        rad /= TWO_PI;
                    }
                    break;
            }

            var min = 0f;
            var max = fillAmount;
            if (fillClockwise)
            {
                min = 1f - max;
                max = 1f;
            }

            if (min > rad || rad > max)
            {
                return float.MinValue;
            }

            return GetAlphaForTypeSimple(localPoint, texture, image);
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ImageAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}