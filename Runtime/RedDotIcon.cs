using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotNode와 연결해 아이콘 GameObject를 켜고 끄는 UI 컴포넌트.
    /// Inspector에서 RedDotType 드롭다운으로 연결할 노드를 직접 지정합니다.
    /// OnEnable/OnDisable에서 콜백을 자동으로 등록/해제합니다.
    /// </summary>
    public class RedDotIcon : MonoBehaviour
    {
        [Tooltip("이 아이콘이 표시할 레드닷 타입 (Inspector 드롭다운)")]
        [SerializeField] protected RedDotType _redDotType;

        [Tooltip("실제로 켜고 끌 레드닷 아이콘 오브젝트")]
        [SerializeField] protected GameObject _icon;

        private RedDotNode _node;
        private bool _locked = false;

        public bool Locked => _locked;

        protected virtual void OnEnable()
        {
            _node = RedDotManager.Instance.GetNode(_redDotType);
            if (_node != null)
            {
                _node.AddCallback(OnNodeChanged);
                OnNodeChanged(_node.Value);
            }
        }

        protected virtual void OnDisable()
        {
            if (_node != null && RedDotManager.Instance != null)
                _node.RemoveCallback(OnNodeChanged);
        }

        /// <summary>
        /// 노드를 잠급니다. locked 상태에서는 레드닷이 표시되지 않습니다.
        /// </summary>
        public virtual void SetLocked(bool locked)
        {
            _locked = locked;
            if (_node != null)
                OnNodeChanged(_node.Value);
        }

        protected virtual void OnNodeChanged(bool isOn)
        {
            if (_icon != null)
                _icon.SetActive(!_locked && isOn);
        }

        public virtual bool IsActive()
        {
            return _icon != null && _icon.activeInHierarchy && !_locked;
        }
    }
}
