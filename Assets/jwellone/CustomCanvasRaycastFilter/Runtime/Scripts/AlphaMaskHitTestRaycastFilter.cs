using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

namespace jwellone.UI
{
    [RequireComponent(typeof(MaskableGraphic))]
    public sealed class AlphaMaskHitTestRaycastFilter : AlphaHitTestRaycastFilter
    {
        [SerializeField, HideInInspector] string _sourceTextureGuid = string.Empty;
        [SerializeField, HideInInspector] Vector2Int _sourceTextureSize;
        [SerializeField, HideInInspector] byte[] _compressCacheData = null!;

        byte[]? _cacheData;

        byte[] _compressData
        {
            get => _compressCacheData;
            set
            {
                if (value == null || value.Length == 0)
                {
                    _compressCacheData = Array.Empty<byte>();
                    return;
                }

                using (var ms = new MemoryStream())
                {
                    using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                    {
                        ds.Write(value, 0, value.Length);
                    }

                    ms.Position = 0;
                    _compressCacheData = new byte[ms.Length];
                    ms.Read(_compressCacheData, 0, _compressCacheData.Length);
                }
            }
        }

        byte[] _data
        {
            get
            {
                if (_cacheData != null)
                {
                    return _cacheData;
                }

                if (_compressData?.Length == 0)
                {
                    _cacheData = Array.Empty<byte>();
                    return _cacheData;
                }

                using (var ms = new MemoryStream(_compressData))
                using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
                using (var dest = new MemoryStream())
                {
                    ds.CopyTo(dest);

                    dest.Position = 0;
                    _cacheData = new byte[dest.Length];
                    dest.Read(_cacheData, 0, _cacheData.Length);
                }

                return _cacheData;
            }
        }

        protected override float GetAlphaOfRaycastLocation(Vector2 localPoint, Camera eventCamera)
        {
            if (_data == null || _data.Length == 0)
            {
                return float.MaxValue;
            }

            var cood = localPoint / rectTransform.rect.size + rectTransform.pivot;
            var index = (int)(cood.x * (_sourceTextureSize.x - 1)) + (int)(cood.y * (_sourceTextureSize.y - 1)) * _sourceTextureSize.x;
            if (_data.Length > index)
            {
                var alpha = _data[index] / 255f;
                SetDebugRect(rectTransform.rect, alpha >= alphaHitTestMinimumThreshold ? Color.green : Color.white);
                return alpha;
            }

            Debug.LogWarning($"{name}.GetAlphaOfRaycastLocation access failed. {_data.Length} <= {index}");
            return float.MaxValue;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AlphaMaskHitTestRaycastFilter))]
        class CustomInspector : BaseInspector
        {
            readonly byte[] _emptyData = Array.Empty<byte>();
            string _guid = string.Empty;
            Graphic? _graphic;
            Texture2D? _alphaMapTexture;
            protected override bool _showWarningTextureReadWrite => false;

            void OnDisable()
            {
                DestroyAlphaMapTexture();
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var instance = (AlphaMaskHitTestRaycastFilter)target;
                _graphic ??= instance.gameObject.GetComponent<Graphic>();
                var texture = _graphic?.mainTexture as Texture2D ?? null;

                if (!EditorApplication.isPlaying)
                {
                    var path = AssetDatabase.GetAssetPath(texture);
                    var guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);

                    if (instance._sourceTextureGuid != guid)
                    {
                        instance._sourceTextureGuid = guid;
                        var data = CreateData(path);
                        instance._compressData = data.Item1;
                        instance._sourceTextureSize = data.Item2;
                        instance._cacheData = null;
                    }
                    CreateAlphaMapTextureIfNeeded(texture, instance._sourceTextureGuid, instance._data);
                }
                else
                {
                    CreateAlphaMapTexture(instance._data, instance._sourceTextureSize);
                }
            }

            protected override void OnInspectorGUIForEditorOnlyProperties()
            {
                base.OnInspectorGUIForEditorOnlyProperties();

                var size = new Vector2Int(_alphaMapTexture?.width ?? 0, _alphaMapTexture?.height ?? 0);
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField($"Alpha Map", _alphaMapTexture, typeof(Texture2D), false);
                EditorGUILayout.Vector2IntField("Alpha Map Size", size);
                EditorGUI.EndDisabledGroup();
            }

            public (byte[], Vector2Int) CreateData(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath) || assetPath.EndsWith("unity_builtin_extra"))
                {
                    return (_emptyData, Vector2Int.zero);
                }

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    return (_emptyData, Vector2Int.zero);
                }

                var isReadable = importer.isReadable;

                try
                {
                    if (!isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }

                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    var pixels = texture.GetPixels32();
                    var data = new byte[pixels.Length];
                    for (var i = 0; i < pixels.Length; ++i)
                    {
                        data[i] = pixels[i].a;
                    }

                    Debug.Log($"Create AlphaMap from {assetPath}");
                    return (data, new Vector2Int(texture.width, texture.height));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return (Array.Empty<byte>(), Vector2Int.zero);
                }
                finally
                {
                    if (!isReadable)
                    {
                        importer.isReadable = false;
                        AssetDatabase.SaveAssetIfDirty(importer);
                    }
                }
            }

            void CreateAlphaMapTextureIfNeeded(Texture2D? source, string guid, byte[] data)
            {
                if (_guid == guid)
                {
                    return;
                }

                DestroyAlphaMapTexture();
                _guid = guid;

                if (data.Length == 0 || source == null)
                {
                    return;
                }


                var texture = new Texture2D(source.width, source.height, TextureFormat.Alpha8, false);
                var colors = new Color32[data.Length];
                var color = Color.clear;
                for (var i = 0; i < data.Length; ++i)
                {
                    color.a = data[i];
                    colors[i] = color;
                }

                texture.SetPixels32(colors);
                texture.Apply();

                _alphaMapTexture = texture;
            }

            void CreateAlphaMapTexture(byte[] data, Vector2Int size)
            {
                if (data.Length == 0 || _alphaMapTexture != null || size == Vector2.zero)
                {
                    return;
                }

                var texture = new Texture2D(size.x, size.y, TextureFormat.Alpha8, false);
                var colors = new Color32[size.x * size.y];
                var color = Color.clear;
                for (var i = 0; i < colors.Length; ++i)
                {
                    color.a = data[i];
                    colors[i] = color;
                }

                texture.SetPixels32(colors);
                texture.Apply();

                _alphaMapTexture = texture;
            }

            void DestroyAlphaMapTexture()
            {
                if (_alphaMapTexture != null)
                {
                    DestroyImmediate(_alphaMapTexture);
                    _alphaMapTexture = null;
                }

                _guid = string.Empty;
            }
        }
#endif
    }
}