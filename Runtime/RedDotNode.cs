using System;
using System.Collections.Generic;

namespace RedDotSystem
{
    /// <summary>
    /// 레드닷 트리의 단일 노드. 값은 카운트(int)로 관리하고 표시 여부는 Count > 0으로 파생됩니다.
    /// 자식 서브트리의 카운트 합을 캐싱하므로 읽기는 O(1), 갱신은 O(트리 깊이)이며,
    /// 실효 카운트가 실제로 바뀐 노드만 콜백을 발행합니다.
    /// </summary>
    public class RedDotNode
    {
        private readonly RedDotType _type;
        private readonly List<RedDotNode> _children = new();
        private RedDotNode _parent;
        private Action<int> _callback;

        private int _selfCount;      // 이 노드 자체의 카운트
        private int _childrenCount;  // 자식 서브트리 실효 카운트 합 (캐시)
        private bool _locked;

        public RedDotType Type => _type;

        /// <summary>잠금 여부. 잠긴 노드의 실효 카운트는 0으로 취급됩니다.</summary>
        public bool Locked => _locked;

        /// <summary>
        /// 실효 카운트. 잠금 상태면 0, 아니면 자기 카운트 + 자식 서브트리 카운트 합.
        /// 캐싱된 필드 합산이라 트리 순회 없이 O(1)로 읽힙니다.
        /// </summary>
        public int Count => _locked ? 0 : _selfCount + _childrenCount;

        /// <summary>레드닷 표시 여부 (Count > 0).</summary>
        public bool Value => Count > 0;

        public RedDotNode(RedDotType type)
        {
            _type = type;
        }

        /// <summary>
        /// bool 값을 설정합니다. 내부적으로 카운트 0/1로 처리됩니다.
        /// </summary>
        public void SetValue(bool value, bool forceCallback = false) =>
            SetCount(value ? 1 : 0, forceCallback);

        /// <summary>
        /// 이 노드 자체의 카운트를 설정합니다 (자식 합계와 별개).
        /// 실효 카운트가 바뀌면 콜백을 발행하고 부모로 델타를 전파합니다.
        /// </summary>
        /// <param name="count">설정할 카운트 (음수는 0으로 처리)</param>
        /// <param name="forceCallback">실효 카운트가 동일해도 콜백 강제 호출 여부</param>
        public void SetCount(int count, bool forceCallback = false)
        {
            if (count < 0) count = 0;
            int before = Count;
            _selfCount = count;
            NotifyIfChanged(before, forceCallback);
        }

        /// <summary>
        /// 노드를 잠급니다. 잠긴 동안 실효 카운트는 0으로 취급되어 부모 집계에서 제외되고,
        /// 잠긴 사이의 변화는 해제 시점에 한 번에 반영됩니다.
        /// 콘텐츠 미해금 상태에서 레드닷을 숨길 때 사용합니다.
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (_locked == locked) return;
            int before = Count;
            _locked = locked;
            NotifyIfChanged(before, forceCallback: false);
        }

        /// <summary>
        /// 자식 노드를 추가합니다. 자식의 실효 카운트 변화는 델타로 이 노드에 누적됩니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 순환(자식이 이미 이 노드의 조상)이거나 자식에게 이미 다른 부모가 있는 경우.
        /// 다중 부모를 허용하면 같은 카운트가 두 경로로 이중 집계됩니다.
        /// </exception>
        public void AddChild(RedDotNode child)
        {
            if (child == null || child == this || _children.Contains(child)) return;

            for (var ancestor = this; ancestor != null; ancestor = ancestor._parent)
            {
                if (ancestor == child)
                    throw new InvalidOperationException(
                        $"순환 참조: {child._type}은(는) {_type}의 조상이라 자식으로 추가할 수 없습니다.");
            }

            if (child._parent != null)
                throw new InvalidOperationException(
                    $"{child._type}에 이미 부모({child._parent._type})가 있습니다. 다중 부모는 카운트를 이중 집계합니다.");

            _children.Add(child);
            child._parent = this;
            ApplyChildDelta(child.Count);
        }

        /// <summary>현재 실효 카운트로 콜백을 즉시 호출합니다.</summary>
        public void Refresh() => _callback?.Invoke(Count);

        public void AddCallback(Action<int> callback) => _callback += callback;
        public void RemoveCallback(Action<int> callback) => _callback -= callback;

        // 자식의 실효 카운트 변화량을 캐시에 반영한다.
        private void ApplyChildDelta(int delta)
        {
            if (delta == 0) return;
            int before = Count;
            _childrenCount += delta;
            NotifyIfChanged(before, forceCallback: false);
        }

        // 실효 카운트가 바뀐 경우에만 콜백을 발행하고 부모로 델타를 전파한다.
        // 잠긴 노드는 before/after가 모두 0이라 여기서 전파가 자연히 멈춘다(가지치기).
        private void NotifyIfChanged(int before, bool forceCallback)
        {
            int after = Count;
            if (after != before)
            {
                _callback?.Invoke(after);
                _parent?.ApplyChildDelta(after - before);
            }
            else if (forceCallback)
            {
                _callback?.Invoke(after);
            }
        }
    }
}
