using TMPro;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotIcon을 상속해 숫자 카운트를 함께 표시하는 UI 컴포넌트.
    /// 카운트는 노드에서 콜백으로 전달받으며(단일 소스),
    /// 부모 노드에 연결하면 자식 서브트리의 카운트 합계가 표시됩니다.
    /// 9+, 99+, 999+ 표시 처리를 포함합니다.
    ///
    /// 카운트 설정: RedDotManager.Instance.SetCount(type, n)
    /// </summary>
    public class RedDotCountIcon : RedDotIcon
    {
        [SerializeField] private TextMeshProUGUI _countText;

        protected override void OnNodeChanged(int count)
        {
            base.OnNodeChanged(count);

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
