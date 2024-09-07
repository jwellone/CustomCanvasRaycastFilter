using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace jwellone.Samples
{
    public class SampleScene : MonoBehaviour
    {
        [SerializeField] Image[] _fillAmountImages = null!;

        [SerializeField] Image[] _sizeOperationImages = null!;


        readonly Dictionary<int, int> _dicCount = new Dictionary<int, int>();

        void Update()
        {
            foreach (var img in _fillAmountImages)
            {
                img.fillAmount = (Mathf.Sin(Time.time) + 1f) / 2f;
            }

            foreach (var img in _sizeOperationImages)
            {
                var size = img.rectTransform.sizeDelta;
                size.x = 1404 + Mathf.Lerp(-200, 200, Mathf.Abs(Mathf.Sin(Time.time)));
                size.y = 554 + Mathf.Lerp(-200, 200, Mathf.Abs(Mathf.Cos(Time.time)));
                img.rectTransform.sizeDelta = size;
            }
        }

        public void OnClickCountUp(TextMeshProUGUI text)
        {
            var id = text.GetInstanceID();
            if (!_dicCount.ContainsKey(id))
            {
                _dicCount.Add(id, 0);
            }

            _dicCount[id] += 1;
            text.text = _dicCount[id].ToString();
        }
    }
}