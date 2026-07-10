# Unity RedDot System

트리 구조 기반 레드닷(신규 알림) 시스템입니다.  
모바일 RPG 실서비스 개발 경험을 토대로 독립 라이브러리로 재구현했습니다.

## 특징

- **enum 기반 노드 타입** — Inspector 드롭다운으로 연결 노드를 직관적으로 설정
- **계층 자동 유도** — enum 숫자 구간 규칙(1000/1100/1110)에서 부모-자식이 자동 연결, enum에 값만 추가하면 트리에 반영
- **카운트 집계 트리** — 노드 값은 카운트(int), 부모에는 자식 합계가 자동 집계 (표시 여부는 `Count > 0`)
- **O(1) 읽기 / 델타 전파** — 자식 합계를 캐싱해 읽기는 트리 순회 없이 O(1), 갱신은 실제로 바뀐 노드만 콜백 발화
- **Locked 상태 지원** — 콘텐츠 미해금 시 레드닷 숨김, 잠긴 동안의 변화는 해제 시점에 일괄 반영
- **자동 구독 관리** — `OnEnable/OnDisable`에서 콜백 등록/해제 자동 처리

## 구조

```
RedDotType (enum)   노드 타입 정의 — 숫자 구간이 곧 계층, Inspector 드롭다운 지원
RedDotHierarchy     enum 숫자 규칙 -> 부모 타입 유도 (순수 C#)
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

카운트를 쓰면 부모에 자식 합계가 집계됩니다. `RedDotCountIcon`을 부모 노드에
연결하면 하위 콘텐츠의 총 알림 개수가 그대로 표시됩니다.

```csharp
RedDotManager.Instance.SetCount(RedDotType.QuestDaily, 3);
RedDotManager.Instance.SetCount(RedDotType.QuestWeekly, 2);
// Quest.Count == 5, MainMenu.Count == 5
```

## 사용법

**1. RedDotType에 프로젝트 노드 추가 — 숫자 구간이 곧 계층**

```csharp
public enum RedDotType
{
    None = 0,
    MainMenu = 1000,      // 루트
    Shop     = 1100,      // 1000의 자식
    ShopPackage = 1110,   // 1100의 자식
    // 필요한 콘텐츠 추가...
}
```

트리는 `RedDotManager`가 enum 숫자 규칙에서 자동으로 구성합니다.
가장 낮은 0이 아닌 자리를 차례로 0으로 만들며 부모를 찾으므로(1110 → 1100 → 1000)
별도의 트리 구성 코드가 필요 없고, 계층 정의가 enum 한 곳에만 존재합니다.

**2. 코드에서 값 설정**

```csharp
// 신규 상품 도착
RedDotManager.Instance.SetValue(RedDotType.ShopPackage, true);

// 퀘스트 3건 - 카운트는 부모로 합산됨
RedDotManager.Instance.SetCount(RedDotType.QuestDaily, 3);

// 콘텐츠 미해금 시 잠금
RedDotManager.Instance.SetLocked(RedDotType.Shop, true);
```

## 핵심 설계

### 카운트 캐싱 + 델타 전파

노드 값은 카운트(int)이고 표시 여부는 `Count > 0`으로 파생됩니다. 각 노드는
자식 서브트리의 카운트 합을 캐싱하므로:

- **읽기 O(1)**: `Count`는 필드 두 개의 합산. 트리 순회가 없습니다
- **갱신 O(트리 깊이)**: 변경은 델타(변화량)로만 부모에 전파됩니다
- **가지치기**: 실효 카운트가 실제로 바뀐 노드만 콜백을 발화합니다. 형제가 이미
  켜져 있어 부모 표시가 그대로인 경우 같은 불필요한 UI 갱신이 없습니다

수백 노드 트리에서 리프 하나의 갱신 비용이 트리 크기와 무관해집니다.

### 트리 무결성

- 순환(자식이 이미 조상)이나 다중 부모(카운트 이중 집계의 원인)는
  `AddChild`에서 `InvalidOperationException`으로 즉시 차단됩니다
- 미등록 타입 조회나 매니저 부재는 조용히 무시하지 않고 `Debug.LogError`로 보고합니다

### Locked 시맨틱

잠긴 노드의 실효 카운트는 0으로 취급되어 부모 집계에서 제외됩니다.
잠긴 동안의 내부 변화는 위로 전파되지 않고, 해제 시점에 전체 델타가
한 번에 반영됩니다. 잠금은 노드 단일 상태라 같은 노드를 보는 모든
아이콘에 일괄 적용됩니다.

## 파일 구성

```
Runtime/
├─ RedDotType.cs          노드 타입 enum 정의 (숫자 구간 = 계층)
├─ RedDotHierarchy.cs     enum 숫자 규칙 -> 부모 유도 (순수 C#)
├─ RedDotNode.cs          트리 노드 (순수 C#)
├─ RedDotManager.cs       노드 트리 관리 매니저
├─ RedDotIcon.cs          UI 컴포넌트 — 아이콘 표시/숨김
└─ RedDotCountIcon.cs     UI 컴포넌트 — 숫자 카운트 표시
Samples~/RedDotExample/
└─ RedDotExample.cs       사용 예시
```

## 설치

### UPM (Package Manager) — 권장
`Window ▸ Package Manager ▸ + ▸ Add package from git URL` 에 입력:

```
https://github.com/Frenil-client/unity-reddot-system.git
```

또는 `Packages/manifest.json` 에 직접 추가:

```json
"com.frenil.reddot-system": "https://github.com/Frenil-client/unity-reddot-system.git"
```

> TextMeshPro 의존(`RedDotCountIcon`). 프로젝트에 `com.unity.textmeshpro` 가 있어야 합니다.

### 드롭인
`Runtime/` 폴더를 프로젝트 `Assets/` 아래에 복사합니다.

### 샘플
Package Manager에서 이 패키지를 선택 → **Samples ▸ Import** (원본: `Samples~/RedDotExample`).

## 테스트

`Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`
(`com.unity.test-framework` 필요 · EditMode 테스트 20종 — RedDotNode 카운트 집계/Lock/콜백/무결성, RedDotHierarchy 계층 유도)

## 요구 사항

- Unity 2021.3+
- TextMeshPro (`RedDotCountIcon` 사용 시)
