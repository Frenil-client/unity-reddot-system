using UnityEngine;

namespace RedDotSystem.Example
{
    /// <summary>
    /// RedDotSystem 사용 예시.
    /// Scene에 RedDotManager가 있는 상태에서 이 컴포넌트를 붙이면 동작합니다.
    /// </summary>
    public class RedDotExample : MonoBehaviour
    {
        private void Start()
        {
            var manager = RedDotManager.Instance;

            // 패키지 상품 신규 도착
            manager.SetValue(RedDotType.ShopPackage, true);

            // ShopPackage true → Shop 자동 true → MainMenu 자동 true
            Debug.Log($"MainMenu : {manager.GetNode(RedDotType.MainMenu).Value}");    // True
            Debug.Log($"Shop     : {manager.GetNode(RedDotType.Shop).Value}");        // True
            Debug.Log($"Package  : {manager.GetNode(RedDotType.ShopPackage).Value}"); // True
            Debug.Log($"Quest    : {manager.GetNode(RedDotType.Quest).Value}");       // False

            // 유저가 패키지 확인 → 레드닷 해제
            manager.SetValue(RedDotType.ShopPackage, false);
            Debug.Log($"해제 후 MainMenu: {manager.GetNode(RedDotType.MainMenu).Value}"); // False

            // 콘텐츠 미해금 — locked 상태에서는 자식이 true여도 표시 안 함
            manager.SetValue(RedDotType.CharacterLevelUp, true);
            manager.SetLocked(RedDotType.Character, true);
            Debug.Log($"Character locked : {manager.GetNode(RedDotType.Character).Value}"); // False

            // 해금 후 잠금 해제
            manager.SetLocked(RedDotType.Character, false);
            Debug.Log($"Character unlocked: {manager.GetNode(RedDotType.Character).Value}"); // True
        }
    }
}
