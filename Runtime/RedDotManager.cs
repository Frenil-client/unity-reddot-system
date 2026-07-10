using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotNode 트리를 초기화하고 관리하는 매니저.
    /// RedDotType enum을 키로 사용하며, 트리 계층은 enum 숫자 구간 규칙에서
    /// 자동으로 유도됩니다(RedDotHierarchy). enum에 값만 추가하면 트리에 반영됩니다.
    ///
    /// 사용 예시:
    ///   RedDotManager.Instance.SetValue(RedDotType.ShopPackage, true);
    ///   RedDotManager.Instance.SetCount(RedDotType.QuestDaily, 3);
    /// </summary>
    public class RedDotManager : MonoBehaviour
    {
        public static RedDotManager Instance { get; private set; }

        private readonly Dictionary<RedDotType, RedDotNode> _nodes = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildTree();
        }

        /// <summary>
        /// enum 전체를 등록하고 숫자 구간 규칙으로 부모-자식을 연결합니다.
        /// 계층 정의는 RedDotType 숫자 규칙이 유일한 소스입니다.
        /// </summary>
        private void BuildTree()
        {
            foreach (RedDotType type in Enum.GetValues(typeof(RedDotType)))
            {
                if (type == RedDotType.None) continue;
                _nodes[type] = new RedDotNode(type);
            }

            foreach (var kv in _nodes)
            {
                var parentType = RedDotHierarchy.GetParentType(kv.Key);
                if (parentType != RedDotType.None && _nodes.TryGetValue(parentType, out var parent))
                    parent.AddChild(kv.Value);
            }
        }

        /// <summary>
        /// 타입에 해당하는 노드를 반환합니다. 미등록 타입이면 에러를 남기고 null을 반환합니다.
        /// </summary>
        public RedDotNode GetNode(RedDotType type)
        {
            if (_nodes.TryGetValue(type, out var node))
                return node;

            Debug.LogError($"[RedDot] 미등록 타입: {type}. RedDotType enum에 정의된 값인지 확인하세요.");
            return null;
        }

        /// <summary>타입에 해당하는 노드 값을 설정합니다.</summary>
        public void SetValue(RedDotType type, bool value)
        {
            GetNode(type)?.SetValue(value);
        }

        /// <summary>타입에 해당하는 노드의 카운트를 설정합니다. 부모에는 자식 합계가 집계됩니다.</summary>
        public void SetCount(RedDotType type, int count)
        {
            GetNode(type)?.SetCount(count);
        }

        /// <summary>타입에 해당하는 노드를 잠급니다. (콘텐츠 미해금 시 레드닷 숨김)</summary>
        public void SetLocked(RedDotType type, bool locked)
        {
            GetNode(type)?.SetLocked(locked);
        }
    }
}
