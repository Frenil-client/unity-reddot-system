# Unity RedDot System

트리 구조 기반 레드닷(신규 알림) 시스템입니다.  
모바일 RPG 실서비스 개발 경험을 토대로 독립 라이브러리로 재구현했습니다.

## 특징

- **enum 기반 노드 타입** — Inspector 드롭다운으로 연결 노드를 직관적으로 설정
- **트리 자동 전파** — 자식 노드가 true이면 부모 노드도 자동으로 true 반환
- **콜백 기반 반응형 갱신** — `Action<bool>` 콜백으로 값 변경 시 UI 즉시 반영
- **Locked 상태 지원** — 콘텐츠 미해금 시 레드닷 숨김 처리
- **자동 구독 관리** — `OnEnable/OnDisable`에서 콜백 등록/해제 자동 처리

## 구조

```
RedDotType (enum)   노드 타입 정의 — Inspector 드롭다운 지원
RedDotNode          트리 노드 핵심 로직 (순수 C#, Unity 비의존)
RedDotManager       노드 트리 초기화 및 관리 (MonoBehaviour Singleton)
RedDotIcon          아이콘 표시/숨김 UI 컴포넌트
RedDotCountIcon     숫자 카운트 표시 UI 컴포넌트 (9+, 99+, 999+)
```

## Inspector 연동

`RedDotIcon` 컴포넌트를 UI GameObject에 붙이면 Inspector에서 드롭다운으로 노드를 지정할 수 있습니다.

```
[RedDotType ▼] ShopPackage
[Icon        ] (레드닷 이미지 오브젝트)
```

enum으로 정의되어 있어 오타 없이 안전하게 설정 가능하고, 어떤 콘텐츠의 알림인지 한눈에 확인됩니다.

## 트리 전파 예시

```
MainMenu (1000)
├─ Shop (1100)
│   ├─ ShopPackage (1110)  ← SetValue(true) 호출
│   └─ ShopLimit   (1120)
├─ Character (1200)
│   ├─ CharacterLevelUp   (1210)
│   └─ CharacterEquipment (1220)
└─ Quest (1300)
    ├─ QuestDaily  (1310)
    └─ QuestWeekly (1320)
```

leaf 노드 하나만 `SetValue(true)` 호출하면 부모 트리 전체에 자동 전파됩니다.

```csharp
RedDotManager.Instance.SetValue(RedDotType.ShopPackage, true);
// → Shop, MainMenu 모두 자동으로 true
```

## 사용법

**1. RedDotType에 프로젝트 노드 추가**

```csharp
public enum RedDotType
{
    None = 0,
    MainMenu = 1000,
    Shop     = 1100,
    ShopPackage = 1110,
    // 필요한 콘텐츠 추가...
}
```

**2. RedDotManager.BuildTree()에서 트리 구성**

```csharp
private void BuildTree()
{
    Register(RedDotType.MainMenu);
    Register(RedDotType.Shop);
    Register(RedDotType.ShopPackage);

    GetNode(RedDotType.MainMenu).AddChild(GetNode(RedDotType.Shop));
    GetNode(RedDotType.Shop).AddChild(GetNode(RedDotType.ShopPackage));
}
```

**3. 코드에서 값 설정**

```csharp
// 신규 상품 도착
RedDotManager.Instance.SetValue(RedDotType.ShopPackage, true);

// 콘텐츠 미해금 시 잠금
RedDotManager.Instance.SetLocked(RedDotType.Shop, true);
```

## 파일 구성

```
Runtime/
├─ RedDotType.cs          노드 타입 enum 정의
├─ RedDotNode.cs          트리 노드 (순수 C#)
├─ RedDotManager.cs       노드 트리 관리 매니저
├─ RedDotIcon.cs          UI 컴포넌트 — 아이콘 표시/숨김
└─ RedDotCountIcon.cs     UI 컴포넌트 — 숫자 카운트 표시
Example/
└─ RedDotExample.cs       사용 예시
```

## 요구 사항

- Unity 2021.3+
- TextMeshPro (`RedDotCountIcon` 사용 시)
