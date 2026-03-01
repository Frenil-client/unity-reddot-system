namespace RedDotSystem
{
    /// <summary>
    /// 레드닷 노드 타입 정의.
    /// Inspector 드롭다운에서 직접 선택 가능하며, 숫자 범위로 메뉴 계층을 표현합니다.
    ///
    /// 계층 구조:
    ///   MainMenu (1000)
    ///   ├─ Shop (1100)
    ///   │   ├─ Shop.Package (1110)
    ///   │   └─ Shop.Limit  (1120)
    ///   ├─ Character (1200)
    ///   │   ├─ Character.LevelUp   (1210)
    ///   │   └─ Character.Equipment (1220)
    ///   └─ Quest (1300)
    ///       ├─ Quest.Daily  (1310)
    ///       └─ Quest.Weekly (1320)
    ///
    /// 프로젝트에 맞게 enum 값을 추가하고
    /// RedDotManager.BuildTree()에서 부모-자식 관계를 설정하세요.
    /// </summary>
    public enum RedDotType
    {
        None = 0,

        // 최상위 메뉴
        MainMenu    = 1000,

        // 상점
        Shop        = 1100,
        ShopPackage = 1110,
        ShopLimit   = 1120,

        // 캐릭터
        Character           = 1200,
        CharacterLevelUp    = 1210,
        CharacterEquipment  = 1220,

        // 퀘스트
        Quest        = 1300,
        QuestDaily   = 1310,
        QuestWeekly  = 1320,
    }
}
