using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextMeshAlphaHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        TMP_Text? _text;

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            _text ??= GetComponent<TMP_Text>();

            var info = _text.textInfo;
            for (var i = 0; i < info.characterInfo.Length; ++i)
            {
                var characterInfo = info.characterInfo[i];
                var meshInfo = info.meshInfo[characterInfo.materialReferenceIndex];
                var texture = meshInfo.material.mainTexture as Texture2D;
                if (texture == null)
                {
                    SetDebugRect(rectTransform.rect, Color.red);
                    continue;
                }

#if UNITY_EDITOR
                if (!texture.isReadable)
                {
                    Debug.LogWarning($"Not a Read/Write setting for {texture.name}. Confirm setting.");
                    SetDebugRect(rectTransform.rect, Color.red);
                    return float.MaxValue;
                }
#endif

                if (!Contains(_text.fontStyle, localPoint, meshInfo, characterInfo, out var normalized))
                {
                    continue;
                }

                var index = characterInfo.vertexIndex;
                var uv0 = meshInfo.uvs0[index];     // 左下
                var uv1 = meshInfo.uvs0[index + 1]; // 左上
                //var uv2 = meshInfo.uvs0[index + 2]; // 右上
                var uv3 = meshInfo.uvs0[index + 3]; // 右下
                var alpha = _text.color.a * texture.GetPixel(
                    (int)(Mathf.Lerp(uv0.x, uv3.x, normalized.x) * texture.width),
                    (int)(Mathf.Lerp(uv0.y, uv1.y, normalized.y) * texture.height)).a;

                SetDebugRect(
                    meshInfo.vertices[index],
                    meshInfo.vertices[index + 1],
                    meshInfo.vertices[index + 2],
                    meshInfo.vertices[index + 3],
                     alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);

                return alpha;
            }

            return float.MinValue;
        }

        bool Contains(FontStyles style, in Vector2 localPoint, in TMP_MeshInfo meshInfo, in TMP_CharacterInfo characterInfo, out Vector2 normalized)
        {
            var vertices = meshInfo.vertices;
            var index = characterInfo.vertexIndex;
            var v0 = vertices[index];      // 左下
            var v1 = vertices[index + 1];  // 左上
            //var v2 = vertices[index + 2];  // 右上
            var v3 = vertices[index + 3];  // 右下

            if (style != FontStyles.Italic)
            {
                var rect = Rect.zero;
                rect.Set(v0.x, v0.y, Mathf.Abs(v0.x - v3.x), Mathf.Abs(v0.y - v1.y));
                if (!rect.Contains(localPoint))
                {
                    normalized = Vector2.zero;
                    return false;
                }

                normalized = Rect.PointToNormalized(rect, localPoint);
                return true;
            }

            for (int i = 0; i < 4; ++i)
            {
                var a = vertices[index + i];
                var b = vertices[index + ((i + 1) % 4)];
                if ((b.x - a.x) * (localPoint.y - a.y) - (localPoint.x - a.x) * (b.y - a.y) > 0)
                {
                    normalized = Vector2.zero;
                    return false;
                }
            }

            normalized.y = (localPoint.y - v0.y) / (v1.y - v0.y);
            normalized.x = (localPoint.x - v0.x - (v1.x - v0.x) * normalized.y) / (v3.x - v0.x);
            return true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(TextMeshAlphaHitTestRaycastFilter))]
        sealed class CustomInspector : BaseInspector
        {
            TMP_Text? _text;

            protected override bool _showWarningTextureReadWrite
            {
                get
                {
                    if (_text == null)
                    {
                        return false;
                    }

                    var info = _text.textInfo;
                    for (var i = 0; i < info?.characterInfo?.Length; ++i)
                    {
                        var characterInfo = info.characterInfo[i];
                        var meshInfo = info.meshInfo[characterInfo.materialReferenceIndex];
                        if (!meshInfo.material?.mainTexture?.isReadable ?? false)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            protected override void OnInspectorGUIForEditorOnlyProperties()
            {
                base.OnInspectorGUIForEditorOnlyProperties();

                EditorGUI.BeginDisabledGroup(true);

                var instance = target as TextMeshAlphaHitTestRaycastFilter;
                _text ??= instance!.gameObject.GetComponent<TMP_Text>();

                for (var i = 0; i < _text.textInfo?.meshInfo?.Length; ++i)
                {
                    var meshInfo = _text.textInfo.meshInfo[i];
                    var texture = meshInfo.material?.mainTexture;
                    EditorGUILayout.ObjectField("Font Texture", texture, typeof(Texture2D), false);
                }

                EditorGUI.EndDisabledGroup();
            }
        }
#endif
    }
}