using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace jwellone.UI
{
    public abstract class AlphaHitTestRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField, Range(0, 1)] float _alphaHitTestMinimumThreshold;
        [SerializeField] float _rayPositionZ = -100f;

        RectTransform? _cacheRectTransform;

        public RectTransform rectTransform => _cacheRectTransform ??= (RectTransform)transform;

        public float alphaHitTestMinimumThreshold
        {
            get => _alphaHitTestMinimumThreshold;
            set => _alphaHitTestMinimumThreshold = value;
        }

        bool ICanvasRaycastFilter.IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
#if UNITY_EDITOR
            _obtainedAlpha = -1f;
            SetDebugRect(Rect.zero, Color.clear);
#endif

            if (_alphaHitTestMinimumThreshold <= 0f)
            {
                SetDebugRect(rectTransform.rect, Color.green);
                return true;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out var localPoint, _rayPositionZ))
            {
                CheckRayPositionZandLogWarningIfNeeded();
                return false;
            }

#if UNITY_EDITOR
            _obtainedAlpha = GetAlphaOfRaycastLocation(localPoint, eventCamera);
            return alphaHitTestMinimumThreshold <= _obtainedAlpha;
#else
            return alphaHitTestMinimumThreshold <= GetAlphaOfRaycastLocation(localPoint, eventCamera);
#endif
        }

        protected abstract float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void CheckRayPositionZandLogWarningIfNeeded()
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var minZ = float.MaxValue;
            for (var i = 0; i < corners.Length; ++i)
            {
                minZ = Mathf.Min(minZ, corners[i].z);
            }

            if (_rayPositionZ > minZ)
            {
                Debug.LogWarning($"Please adjust \"Ray Position Z({_rayPositionZ})\" > {minZ} of {name}.", gameObject);
                SetDebugRect(rectTransform.rect, Color.yellow);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        protected void SetDebugRect(Rect rect, Color color)
        {
            var leftBottom = new Vector3(rect.x, rect.y);
            var leftTop = new Vector3(rect.x, rect.y + rect.height);
            var rightTop = new Vector3(rect.x + rect.width, rect.y + rect.height);
            var rightBottom = new Vector3(rect.x + rect.width, rect.y);
            SetDebugRect(leftBottom, leftTop, rightTop, rightBottom, color);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        protected void SetDebugRect(Vector3 leftBottom, Vector3 leftTop, Vector3 rightTop, Vector3 rightBottom, Color color)
        {
#if UNITY_EDITOR
            if (!_debugDisp)
            {
                if (_squareGraphic != null)
                {
                    _squareGraphic.enabled = false;
                }
                return;
            }

            if (_squareGraphic == null)
            {
                _squareGraphic = new GameObject("SquareGraphic").AddComponent<SquareGraphic>();
                _squareGraphic.gameObject.hideFlags = HideFlags.HideAndDontSave;
                _squareGraphic.raycastTarget = false;
                var graphicRectTransform = _squareGraphic.rectTransform;
                graphicRectTransform.SetParent(rectTransform, false);
                graphicRectTransform.localPosition = Vector3.zero;
            }

            color.a *= 0.35f;
            _squareGraphic.enabled = true;
            _squareGraphic.color = color;
            _squareGraphic.SetVertices(leftBottom, leftTop, rightTop, rightBottom);
#endif
        }

#if UNITY_EDITOR
        [RequireComponent(typeof(CanvasRenderer))]
        [RequireComponent(typeof(RectTransform))]
        class SquareGraphic : Graphic
        {
            readonly Vector3[] _vertices = new Vector3[4];
            static readonly UIVertex[] _uiVertices = new UIVertex[4];
            bool _isDirty;

            void Update()
            {
                if (_isDirty)
                {
                    _isDirty = false;
                }
                else
                {
                    color = Color.clear;
                }
            }

            internal void SetVertices(Rect rect)
            {
                _vertices[0] = new Vector3(rect.x, rect.y);
                _vertices[1] = new Vector3(rect.x, rect.y + rect.height);
                _vertices[2] = new Vector3(rect.x + rect.width, rect.y + rect.height);
                _vertices[3] = new Vector3(rect.x + rect.width, rect.y);
                _isDirty = true;
                SetVerticesDirty();
            }

            internal void SetVertices(Vector3 leftBottom, Vector3 leftTop, Vector3 rightTop, Vector3 rightBottom)
            {
                _vertices[0] = leftBottom;
                _vertices[1] = leftTop;
                _vertices[2] = rightTop;
                _vertices[3] = rightBottom;
                _isDirty = true;
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                for (var i = 0; i < _vertices.Length; ++i)
                {
                    _uiVertices[i].position = _vertices[i];
                    _uiVertices[i].color = color;
                }

                vh.AddUIVertexQuad(_uiVertices);
            }
        }

        bool _debugDisp;
        float _obtainedAlpha;
        SquareGraphic? _squareGraphic;

        protected abstract class BaseInspector : Editor
        {
            static readonly string[] _unitTexts = new string[] { "B", "KB", "MB", "GB" };
            static readonly Vector3[] _corners = new Vector3[4];

            bool _isCornersFoldout;
            float _minZ;

            protected virtual bool _showWarningTextureReadWrite
            {
                get
                {
                    var graphic = target as MaskableGraphic;
                    return !graphic?.mainTexture?.isReadable ?? false;
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();

                OnInspectorGUIForExclusive();
                OnInspectorGUIForEditorOnlyProperties();
                OnInspectorGUIForHelpBox();

                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(target);
            }

            protected virtual void OnInspectorGUIForExclusive()
            {
            }

            protected virtual void OnInspectorGUIForEditorOnlyProperties()
            {
                var instance = (AlphaHitTestRaycastFilter)target;
                var rectTransform = (RectTransform)instance.transform;
                rectTransform.GetWorldCorners(_corners);
                _minZ = float.MaxValue;
                for (var i = 0; i < _corners.Length; ++i)
                {
                    _minZ = Mathf.Min(_minZ, _corners[i].z);
                }

                EditorGUILayout.Space();

                instance._debugDisp = EditorGUILayout.Toggle("Debug Disp", instance._debugDisp);

                EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.FloatField("obtained alpha", instance._obtainedAlpha);

                _isCornersFoldout = EditorGUILayout.Foldout(_isCornersFoldout, "Corners");
                if (_isCornersFoldout)
                {
                    ++EditorGUI.indentLevel;
                    {
                        for (var i = 0; i < _corners.Length; ++i)
                        {
                            EditorGUILayout.Vector3Field($"v[{i}]", _corners[i]);
                        }

                        EditorGUILayout.FloatField("Min Z", _minZ);
                    }
                    --EditorGUI.indentLevel;
                }

                EditorGUILayout.Space();

                var mainTexture = instance.gameObject.GetComponent<MaskableGraphic>()?.mainTexture;
                var mainTextureAssetPath = AssetDatabase.GetAssetPath(mainTexture);
                EditorGUILayout.ObjectField("mainTexture", mainTexture, typeof(Texture), false);
                GUILayout.Label($"path : {mainTextureAssetPath}");
                GUILayout.Label($"guid : {AssetDatabase.AssetPathToGUID(mainTextureAssetPath)}");
                GUILayout.Label($"size : {mainTexture?.width} x {mainTexture?.height}");
                GUILayout.Label($"format : {mainTexture?.graphicsFormat}");
                GUILayout.Label($"memory : {GetRuntimeMemorySizeText(mainTexture)}");
                EditorGUI.EndDisabledGroup();
            }

            protected virtual void OnInspectorGUIForHelpBox()
            {
                var instance = (AlphaHitTestRaycastFilter)target;
                if (instance._rayPositionZ > _minZ)
                {
                    EditorGUILayout.HelpBox($"Please adjust \"Ray Position Z({instance._rayPositionZ})\" > {_minZ}.", MessageType.Warning);
                }

                if (_showWarningTextureReadWrite)
                {
                    EditorGUILayout.HelpBox("Set the texture to read/write.", MessageType.Warning);
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
