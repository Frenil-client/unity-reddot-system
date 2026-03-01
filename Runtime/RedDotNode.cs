using System;
using System.Collections.Generic;

namespace RedDotSystem
{
    /// <summary>
    /// 레드닷 트리의 단일 노드.
    /// 자식 노드 중 하나라도 true이면 부모 노드도 자동으로 true를 반환합니다.
    /// </summary>
    public class RedDotNode
    {
        private readonly RedDotType _type;
        private bool _value = false;
        private bool _locked = false;
        private readonly List<RedDotNode> _children = new();
        private Action<bool> _callback;

        public RedDotType Type => _type;

        public RedDotNode(RedDotType type)
        {
            _type = type;
        }

        /// <summary>
        /// 현재 노드의 값.
        /// 자식 노드 중 하나라도 true이면 이 노드도 true를 반환합니다.
        /// locked 상태이면 항상 false를 반환합니다.
        /// </summary>
        public bool Value
        {
            get
            {
                if (_locked)
                    return false;

                var result = _value;
                foreach (var child in _children)
                    result |= child.Value;

                return result;
            }
        }

        /// <summary>
        /// 노드 값을 설정합니다. 값이 변경되면 콜백을 호출합니다.
        /// </summary>
        /// <param name="value">설정할 값</param>
        /// <param name="forceCallback">값이 동일해도 콜백 강제 호출 여부</param>
        public void SetValue(bool value, bool forceCallback = false)
        {
            if (_value != value)
            {
                _value = value;
                _callback?.Invoke(Value);
            }
            else if (forceCallback)
            {
                _callback?.Invoke(Value);
            }
        }

        /// <summary>
        /// 노드를 잠급니다. locked 상태에서는 Value가 항상 false를 반환합니다.
        /// 콘텐츠 미해금 상태에서 레드닷을 숨길 때 사용합니다.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (_locked == locked) return;
            _locked = locked;
            _callback?.Invoke(Value);
        }

        /// <summary>
        /// 자식 노드를 추가합니다.
        /// 자식의 값이 변경되면 이 노드의 콜백도 자동으로 호출됩니다.
        /// </summary>
        public void AddChild(RedDotNode child)
        {
            if (!_children.Contains(child))
            {
                _children.Add(child);
                child.AddCallback(_ => Refresh());
            }
        }

        /// <summary>
        /// 현재 값으로 콜백을 즉시 호출합니다.
        /// </summary>
        public void Refresh()
        {
            _callback?.Invoke(Value);
        }

        public void AddCallback(Action<bool> callback)    => _callback += callback;
        public void RemoveCallback(Action<bool> callback) => _callback -= callback;
    }
}
