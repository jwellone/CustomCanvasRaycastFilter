using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace jwellone.UI
{
    public class SpritePhysicsShapeRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        readonly Vector2 _offset = new Vector2(0.5f, 0.5f);
        readonly static List<Vector2> _verts = new List<Vector2>(32);

        [SerializeField] Sprite _sprite = null!;

        RectTransform? _cacheRectTransform;

        RectTransform rectTransform => _cacheRectTransform ??= (RectTransform)transform;

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            _verts.Clear();

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
                _sprite.GetPhysicsShape(i, _verts);
                if (Contains(coord, _verts))
                {
                    return true;
                }
            }

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
    }
}