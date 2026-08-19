using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotNode와 연결해 아이콘 GameObject를 켜고 끄는 UI 컴포넌트.
    /// Inspector에서 RedDotType 드롭다운으로 연결할 노드를 직접 지정합니다.
    /// OnEnable/OnDisable에서 콜백을 자동으로 등록/해제합니다.
    ///
    /// 트리는 RedDotTree가 첫 접근 시 구성하므로, 씬에 RedDotManager가 있든 없든
    /// 또 이 아이콘이 매니저보다 먼저 활성화되든 상관없이 항상 연결됩니다.
    /// </summary>
    public class RedDotIcon : MonoBehaviour
    {
        [Tooltip("이 아이콘이 표시할 레드닷 타입 (Inspector 드롭다운)")]
        [SerializeField] protected RedDotType _redDotType;

        [Tooltip("실제로 켜고 끌 레드닷 아이콘 오브젝트")]
        [SerializeField] protected GameObject _icon;

        private RedDotNode _node;

        /// <summary>연결된 노드의 잠금 상태. 잠금은 노드가 단일 진실입니다.</summary>
        public bool Locked => _node != null && _node.Locked;

        protected virtual void OnEnable()
        {
            _node = RedDotTree.GetNode(_redDotType);

            if (_node == null)
            {
                Debug.LogError(
                    $"[RedDot] 미등록 타입: {_redDotType}. RedDotType enum에 정의된 값인지 확인하세요. ({name})",
                    this);
                return;
            }

            _node.AddCallback(OnNodeChanged);
            OnNodeChanged(_node.Count);
        }

        protected virtual void OnDisable()
        {
            if (_node != null)
                _node.RemoveCallback(OnNodeChanged);
            _node = null;
        }

        /// <summary>
        /// 연결된 노드를 잠급니다. 노드 상태이므로 같은 노드를 보는 모든 아이콘에 일괄 적용됩니다.
        /// </summary>
        public virtual void SetLocked(bool locked)
        {
            _node?.SetLocked(locked);
        }

        protected virtual void OnNodeChanged(int count)
        {
            if (_icon != null)
                _icon.SetActive(count > 0);
        }

        public virtual bool IsActive()
        {
            return _icon != null && _icon.activeInHierarchy;
        }
    }
}
