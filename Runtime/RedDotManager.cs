using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// 레드닷 트리에 대한 씬 측 진입점입니다.
    ///
    /// 트리 자체는 <see cref="RedDotTree"/>가 소유하며 첫 접근 시 스스로 구성되므로,
    /// 이 컴포넌트는 **선택 사항**입니다. 씬에 배치하지 않아도 아이콘과 값 설정은 동작한다.
    /// 배치하는 이유는 두 가지뿐이다 — 인스펙터에서 눈에 보이는 진입점을 두고 싶을 때,
    /// 그리고 코드에서 정적 API 대신 참조로 접근하고 싶을 때.
    ///
    /// 사용 예시:
    ///   RedDotTree.SetValue(RedDotType.ShopPackage, true);     // 매니저 없이
    ///   RedDotManager.Instance.SetCount(RedDotType.QuestDaily, 3);
    /// </summary>
    public class RedDotManager : MonoBehaviour
    {
        public static RedDotManager Instance { get; private set; }

        // 도메인 리로드를 끈 상태에서 플레이를 반복하면 정적 상태가 남는다.
        // 런타임 진입 시점에 트리와 인스턴스를 비워 세션 간 오염을 막는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            RedDotTree.Reset();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 타입에 해당하는 노드를 반환합니다. 미등록 타입이면 에러를 남기고 null을 반환합니다.
        /// </summary>
        public RedDotNode GetNode(RedDotType type)
        {
            var node = RedDotTree.GetNode(type);
            if (node == null)
                Debug.LogError($"[RedDot] 미등록 타입: {type}. RedDotType enum에 정의된 값인지 확인하세요.", this);

            return node;
        }

        /// <summary>타입에 해당하는 노드 값을 설정합니다.</summary>
        public void SetValue(RedDotType type, bool value) => GetNode(type)?.SetValue(value);

        /// <summary>타입에 해당하는 노드의 카운트를 설정합니다. 부모에는 자식 합계가 집계됩니다.</summary>
        public void SetCount(RedDotType type, int count) => GetNode(type)?.SetCount(count);

        /// <summary>타입에 해당하는 노드를 잠급니다. (콘텐츠 미해금 시 레드닷 숨김)</summary>
        public void SetLocked(RedDotType type, bool locked) => GetNode(type)?.SetLocked(locked);
    }
}
