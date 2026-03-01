using System.Collections.Generic;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotNode 트리를 초기화하고 관리하는 매니저.
    /// RedDotType enum을 키로 사용합니다.
    ///
    /// 사용 예시:
    ///   RedDotManager.Instance.SetValue(RedDotType.ShopPackage, true);
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
        /// 노드 트리를 구성합니다.
        /// 1. RedDotType에 enum 값 추가
        /// 2. 이 메서드에서 Register() 후 AddChild()로 계층 설정
        /// </summary>
        private void BuildTree()
        {
            // 노드 등록
            Register(RedDotType.MainMenu);

            Register(RedDotType.Shop);
            Register(RedDotType.ShopPackage);
            Register(RedDotType.ShopLimit);

            Register(RedDotType.Character);
            Register(RedDotType.CharacterLevelUp);
            Register(RedDotType.CharacterEquipment);

            Register(RedDotType.Quest);
            Register(RedDotType.QuestDaily);
            Register(RedDotType.QuestWeekly);

            // 부모-자식 연결
            GetNode(RedDotType.MainMenu).AddChild(GetNode(RedDotType.Shop));
            GetNode(RedDotType.MainMenu).AddChild(GetNode(RedDotType.Character));
            GetNode(RedDotType.MainMenu).AddChild(GetNode(RedDotType.Quest));

            GetNode(RedDotType.Shop).AddChild(GetNode(RedDotType.ShopPackage));
            GetNode(RedDotType.Shop).AddChild(GetNode(RedDotType.ShopLimit));

            GetNode(RedDotType.Character).AddChild(GetNode(RedDotType.CharacterLevelUp));
            GetNode(RedDotType.Character).AddChild(GetNode(RedDotType.CharacterEquipment));

            GetNode(RedDotType.Quest).AddChild(GetNode(RedDotType.QuestDaily));
            GetNode(RedDotType.Quest).AddChild(GetNode(RedDotType.QuestWeekly));
        }

        private void Register(RedDotType type)
        {
            if (!_nodes.ContainsKey(type))
                _nodes[type] = new RedDotNode(type);
        }

        /// <summary>
        /// 타입에 해당하는 노드를 반환합니다.
        /// </summary>
        public RedDotNode GetNode(RedDotType type)
        {
            _nodes.TryGetValue(type, out var node);
            return node;
        }

        /// <summary>
        /// 타입에 해당하는 노드 값을 설정합니다.
        /// </summary>
        public void SetValue(RedDotType type, bool value)
        {
            GetNode(type)?.SetValue(value);
        }

        /// <summary>
        /// 타입에 해당하는 노드를 잠급니다. (콘텐츠 미해금 시 레드닷 숨김)
        /// </summary>
        public void SetLocked(RedDotType type, bool locked)
        {
            GetNode(type)?.SetLocked(locked);
        }
    }
}
