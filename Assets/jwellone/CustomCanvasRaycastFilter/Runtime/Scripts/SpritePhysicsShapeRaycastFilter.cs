using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace jwellone.UI
{
    public class SpritePhysicsShapeRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        readonly Vector2 _offset = new Vector2(0.5f, 0.5f);
        readonly static List<Vector2> _physicsShape = new List<Vector2>();


        [SerializeField] Sprite _sprite = null!;

        RectTransform? _cacheRectTransform;

        RectTransform rectTransform => _cacheRectTransform ??= (RectTransform)transform;

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            _physicsShape.Clear();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localPoint))
            {
                return false;
            }

            if (_sprite == null || _sprite.GetPhysicsShapeCount() == 0)
            {
                return true;
            }

            var coord = (localPoint / rectTransform.rect.size + rectTransform.pivot - _offset) * _sprite.rect.size / _sprite.pixelsPerUnit;
            for (var i = 0; i < _sprite.GetPhysicsShapeCount(); ++i)
            {
                _sprite.GetPhysicsShape(i, _physicsShape);
                if (Contains(coord, _physicsShape))
                {
                    SetDebugGraphic(_sprite, i);
                    return true;
                }
            }

            SetDebugGraphic(_sprite, -1);
            return false;
        }

        bool Contains(in Vector2 localPoint, IList<Vector2> vertices)
        {
            var count = 0;
            for (var i = 0; i < vertices.Count; ++i)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Count];
                if (((a.y > localPoint.y) || (b.y <= localPoint.y)) && ((a.y <= localPoint.y) || (b.y > localPoint.y)))
                {
                    continue;
                }

                var x = a.x + (localPoint.y - a.y) / (b.y - a.y) * (b.x - a.x);
                if (localPoint.x == x)
                {
                    return true;
                }

                if (localPoint.x < x)
                {
                    ++count;
                }
            }

            return (count % 2) == 1;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void SetDebugGraphic(Sprite? sprite, int selectIndex)
        {
#if UNITY_EDITOR
            if (_debugGraphic == null)
            {
                _debugGraphic = new GameObject("DebugGraphic").AddComponent<DebugGraphic>();
                _debugGraphic.gameObject.hideFlags = HideFlags.HideAndDontSave;
                _debugGraphic.rectTransform.SetParent(rectTransform, false);
                _debugGraphic.rectTransform.localPosition = Vector3.zero;
                _debugGraphic.enabled = false;
            }

            _debugGraphic.enabled = _debugDisp;
            _debugGraphic.Setup(sprite, selectIndex);
#endif
        }

#if UNITY_EDITOR
        DebugGraphic? _debugGraphic;

        bool _debugDisp;

        [RequireComponent(typeof(CanvasRenderer))]
        [RequireComponent(typeof(RectTransform))]
        class DebugGraphic : Graphic
        {
            readonly static UIVertex[] _vertices = new UIVertex[4];
            const float _rectWidth = 0.1f;
            const float _rectHeight = 0.1f;

            int _selectIndex = -1;
            float _weight = 0.01f;

            Sprite? _sprite;

            internal void Setup(Sprite? sprite, int selectIndex)
            {
                if (_sprite != sprite || _selectIndex != selectIndex)
                {
                    SetVerticesDirty();
                }

                _sprite = sprite;
                _selectIndex = selectIndex;
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                if (_sprite == null || _sprite.GetPhysicsShapeCount() == 0)
                {
                    return;
                }

                rectTransform.localScale = new Vector3(_sprite!.pixelsPerUnit, _sprite.pixelsPerUnit, 1f);

                var defaultLines = new List<Vector3>();
                var outLines = new List<Vector3>();
                var points = new List<(int, List<Vector2>)>();

                for (var i = 0; i < _sprite.GetPhysicsShapeCount(); ++i)
                {
                    _sprite!.GetPhysicsShape(i, _physicsShape);
                    for (var k = 0; k < _physicsShape.Count; ++k)
                    {
                        var a = _physicsShape[k];
                        var b = _physicsShape[(k + 1) % _physicsShape.Count];
                        var cross = Vector3.Cross(b - a, Vector3.forward).normalized * Vector2.one;
                        var dir = cross * (_weight + 0.015f);
                        outLines.Add(a + dir);
                        outLines.Add(a - dir);
                        outLines.Add(b - dir);
                        outLines.Add(b + dir);

                        dir = cross * _weight;
                        defaultLines.Add(a + dir);
                        defaultLines.Add(a - dir);
                        defaultLines.Add(b - dir);
                        defaultLines.Add(b + dir);
                    }

                    points.Add((i, new List<Vector2>(_physicsShape)));
                }

                for (var i = 0; i < _vertices.Length; ++i)
                {
                    _vertices[i].color = Color.black;
                }
                for (var i = 0; i < outLines.Count; i += 4)
                {
                    for (var idx = 0; idx < _vertices.Length; ++idx)
                    {
                        _vertices[idx].position = outLines[i + idx];
                    }
                    vh.AddUIVertexQuad(_vertices);
                }

                for (var i = 0; i < _vertices.Length; ++i)
                {
                    _vertices[i].color = color;
                }
                for (var i = 0; i < defaultLines.Count; i += 4)
                {
                    for (var idx = 0; idx < _vertices.Length; ++idx)
                    {
                        _vertices[idx].position = defaultLines[i + idx];
                    }
                    vh.AddUIVertexQuad(_vertices);
                }

                var pos = Vector3.zero;
                for (var i = 0; i < points.Count; ++i)
                {
                    var color = _selectIndex == points[i].Item1 ? Color.green : Color.white;
                    for (var k = 0; k < _vertices.Length; ++k)
                    {
                        _vertices[k].color = color;
                    }

                    foreach (var point in points[i].Item2)
                    {
                        pos.Set(point.x - _rectWidth, point.y - _rectHeight, 1f);
                        _vertices[0].position = pos;
                        pos.Set(point.x - _rectWidth, point.y + _rectHeight, 1f);
                        _vertices[1].position = pos;
                        pos.Set(point.x + _rectWidth, point.y + _rectHeight, 1f);
                        _vertices[2].position = pos;
                        pos.Set(point.x + _rectWidth, point.y - _rectHeight, 1f);
                        _vertices[3].position = pos;
                        vh.AddUIVertexQuad(_vertices);
                    }
                }
            }
        }

        [CustomEditor(typeof(SpritePhysicsShapeRaycastFilter))]
        class CustomInspector : Editor
        {
            static readonly string[] _unitTexts = new string[] { "B", "KB", "MB", "GB" };

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var instance = (SpritePhysicsShapeRaycastFilter)target;

                EditorGUILayout.Space();

                instance._debugDisp = EditorGUILayout.Toggle("Debug Disp", instance._debugDisp);


                EditorGUI.BeginDisabledGroup(true);
                var mainTexture = instance._sprite?.texture;
                var mainTextureAssetPath = AssetDatabase.GetAssetPath(mainTexture);
                GUILayout.Label($"Physics Shape Count : {instance._sprite?.GetPhysicsShapeCount()}");

                EditorGUILayout.ObjectField("mainTexture", mainTexture, typeof(Texture), false);
                GUILayout.Label($"path : {mainTextureAssetPath}");
                GUILayout.Label($"guid : {AssetDatabase.AssetPathToGUID(mainTextureAssetPath)}");
                GUILayout.Label($"size : {mainTexture?.width} x {mainTexture?.height}");
                GUILayout.Label($"format : {mainTexture?.graphicsFormat}");
                GUILayout.Label($"memory : {GetRuntimeMemorySizeText(mainTexture)}");
                EditorGUI.EndDisabledGroup();

                if (instance._sprite != null && instance._sprite.GetPhysicsShapeCount() == 0)
                {
                    EditorGUILayout.HelpBox("Physics shape count is zero.", MessageType.Warning);
                }
            }

            protected string GetRuntimeMemorySizeText(Texture? texture)
            {
                var size = texture != null ? (double)UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture) : 0;
                var index = 0;
                while (size >= 1024)
                {
                    size /= 1024;
                    ++index;
                }

                return $"{size:#.00} {_unitTexts[index]}";
            }
        }
#endif
    }
}