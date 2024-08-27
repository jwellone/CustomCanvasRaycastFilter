using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class TextAlphaHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        Text? _text;

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            _text ??= GetComponent<Text>();
            var texture = _text.mainTexture as Texture2D;
            if (texture == null)
            {
                SetDebugRect(rectTransform.rect, Color.red);
                return float.MinValue;
            }

#if UNITY_EDITOR
            if (!texture.isReadable)
            {
                Debug.LogWarning($"Not a Read/Write setting for {texture.name}. Confirm setting.");
                SetDebugRect(rectTransform.rect, Color.red);
                return float.MaxValue;
            }
#endif

            var verts = _text.cachedTextGenerator.verts;
            var rect = Rect.zero;
            UIVertex v0, v1, v2, v3;
            for (var i = 0; i < verts.Count; i += 4)
            {
                v0 = verts[i];      // 左上
                v1 = verts[i + 1];  // 右上
                v2 = verts[i + 2];  // 右下
                v3 = verts[i + 3];  // 左下

                rect.Set(v3.position.x,
                         v3.position.y,
                         Mathf.Abs(v3.position.x - v2.position.x),
                         Mathf.Abs(v3.position.y - v1.position.y));


                if (!rect.Contains(localPoint))
                {
                    continue;
                }

                var normalized = Rect.PointToNormalized(rect, localPoint);
                var alpha = texture.GetPixel(
                    (int)(Mathf.Lerp(v3.uv0.x, v2.uv0.x, normalized.x) * texture.width),
                    (int)(Mathf.Lerp(v3.uv0.y, v0.uv0.y, normalized.y) * texture.height)).a;

                SetDebugRect(rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
                return alpha;
            }

            return float.MinValue;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(TextAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
        }
#endif
    }
}