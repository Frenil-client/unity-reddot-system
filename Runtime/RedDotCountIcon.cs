using TMPro;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotIcon을 상속해 숫자 카운트를 함께 표시하는 UI 컴포넌트.
    /// 9+, 99+, 999+ 표시 처리를 포함합니다.
    /// </summary>
    public class RedDotCountIcon : RedDotIcon
    {
        [SerializeField] private TextMeshProUGUI _countText;

        /// <summary>
        /// 카운트를 설정합니다. count가 0이면 아이콘을 숨깁니다.
        /// </summary>
        public void SetCount(ulong count)
        {
            OnNodeChanged(count > 0);

            if (_countText == null) return;

            _countText.text = count switch
            {
                >= 1000 => "999+",
                >= 100  => "99+",
                >= 10   => "9+",
                _       => count.ToString()
            };
        }
    }
}
