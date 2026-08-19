using System;
using System.Collections.Generic;

namespace RedDotSystem
{
    /// <summary>
    /// 레드닷 노드 트리의 실제 소유자. 순수 C#이며 Unity 생명주기에 의존하지 않습니다.
    ///
    /// 트리를 MonoBehaviour(RedDotManager)가 Awake에서 만들던 구조에는 순서 문제가 있었다.
    /// 씬 로드 시 Unity는 오브젝트마다 Awake -> OnEnable을 이어서 호출하므로, 아이콘이
    /// 매니저보다 먼저 처리되면 RedDotIcon.OnEnable 시점에 트리가 아직 없다. 그러면 아이콘은
    /// 에러만 남기고 아무 노드에도 연결되지 못한 채 영구히 죽는다. 씬에서 오브젝트 순서를
    /// 바꾸거나 프리팹을 다른 곳에 배치하는 것만으로 재현됐다 사라졌다 하는 종류의 버그다.
    ///
    /// 그래서 트리를 첫 접근 시점에 스스로 구성하는 정적 서비스로 옮겼다. 누가 먼저 물어보든
    /// 그 시점에 트리가 준비되므로 초기화 순서라는 개념 자체가 사라진다. 씬에 매니저를
    /// 배치하지 않아도 동작한다.
    /// </summary>
    public static class RedDotTree
    {
        private static Dictionary<RedDotType, RedDotNode> _nodes;

        /// <summary>트리가 이미 구성되었는지. 진단 도구용입니다.</summary>
        public static bool IsBuilt => _nodes != null;

        /// <summary>등록된 모든 노드. 접근하면 트리가 구성됩니다.</summary>
        public static IReadOnlyDictionary<RedDotType, RedDotNode> Nodes
        {
            get
            {
                EnsureBuilt();
                return _nodes;
            }
        }

        /// <summary>타입에 해당하는 노드를 반환합니다. 미등록 타입이면 null입니다.</summary>
        public static RedDotNode GetNode(RedDotType type)
        {
            EnsureBuilt();
            return _nodes.TryGetValue(type, out var node) ? node : null;
        }

        /// <summary>값을 설정합니다.</summary>
        /// <returns>등록된 타입이면 true.</returns>
        public static bool SetValue(RedDotType type, bool value)
        {
            var node = GetNode(type);
            if (node == null) return false;

            node.SetValue(value);
            return true;
        }

        /// <summary>카운트를 설정합니다. 부모에는 자식 합계가 집계됩니다.</summary>
        /// <returns>등록된 타입이면 true.</returns>
        public static bool SetCount(RedDotType type, int count)
        {
            var node = GetNode(type);
            if (node == null) return false;

            node.SetCount(count);
            return true;
        }

        /// <summary>노드를 잠급니다. (콘텐츠 미해금 시 레드닷 숨김)</summary>
        /// <returns>등록된 타입이면 true.</returns>
        public static bool SetLocked(RedDotType type, bool locked)
        {
            var node = GetNode(type);
            if (node == null) return false;

            node.SetLocked(locked);
            return true;
        }

        /// <summary>
        /// 트리를 버립니다. 다음 접근에서 새로 구성됩니다.
        /// 플레이 세션 사이에 상태가 남지 않도록 런타임 진입 시 호출되며(RedDotManager),
        /// 테스트에서 각 케이스를 격리할 때도 사용합니다.
        /// </summary>
        public static void Reset() => _nodes = null;

        private static void EnsureBuilt()
        {
            if (_nodes != null) return;

            // enum 전체를 등록하고 숫자 구간 규칙으로 부모-자식을 연결한다.
            // 계층 정의는 RedDotType 숫자 규칙이 유일한 소스다.
            var nodes = new Dictionary<RedDotType, RedDotNode>();

            foreach (RedDotType type in Enum.GetValues(typeof(RedDotType)))
            {
                if (type == RedDotType.None) continue;
                nodes[type] = new RedDotNode(type);
            }

            foreach (var kv in nodes)
            {
                var parentType = RedDotHierarchy.GetParentType(kv.Key);
                if (parentType != RedDotType.None && nodes.TryGetValue(parentType, out var parent))
                    parent.AddChild(kv.Value);
            }

            _nodes = nodes;
        }
    }
}
