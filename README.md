# Unity RedDot System

[![CI](https://github.com/Frenil-client/unity-reddot-system/actions/workflows/ci.yml/badge.svg)](https://github.com/Frenil-client/unity-reddot-system/actions/workflows/ci.yml)

트리 구조 기반 레드닷(신규 알림) 시스템입니다.  
모바일 RPG 실서비스 개발 경험을 토대로 독립 라이브러리로 재구현했습니다.

## 특징

- **enum 기반 노드 타입** — Inspector 드롭다운으로 연결 노드를 직관적으로 설정
- **계층 자동 유도** — enum 숫자 구간 규칙(1000/1100/1110)에서 부모-자식이 자동 연결, enum에 값만 추가하면 트리에 반영
- **카운트 집계 트리** — 노드 값은 카운트(int), 부모에는 자식 합계가 자동 집계 (표시 여부는 `Count > 0`)
- **O(1) 읽기 / 델타 전파** — 자식 합계를 캐싱해 읽기는 트리 순회 없이 O(1), 갱신은 실제로 바뀐 노드만 콜백 발화
- **Locked 상태 지원** — 콘텐츠 미해금 시 레드닷 숨김, 잠긴 동안의 변화는 해제 시점에 일괄 반영
- **자동 구독 관리** — `OnEnable/OnDisable`에서 콜백 등록/해제 자동 처리
- **트리 디버거 창** — 계층·실효 카운트·잠금을 실시간으로 보고 값을 주입하는 EditorWindow, enum 계층의 구조적 실수까지 진단

## 구조

```
RedDotType (enum)   노드 타입 정의 — 숫자 구간이 곧 계층, Inspector 드롭다운 지원
RedDotHierarchy     enum 숫자 규칙 -> 부모 타입 유도 (순수 C#)
RedDotNode          트리 노드 핵심 로직 (순수 C#, Unity 비의존)
RedDotTree          트리 소유자 - 첫 접근 시 자동 구성 (순수 C#, 매니저 불필요)
RedDotManager       씬 측 진입점 (선택 사항 - RedDotTree에 위임)
RedDotIcon          아이콘 표시/숨김 UI 컴포넌트
RedDotCountIcon     숫자 카운트 표시 UI 컴포넌트 (9+, 99+, 999+)

RedDotTreeModel     enum -> 표시용 트리 구성 + 구조 진단 (순수 C#, Editor 전용)
RedDotDebuggerWindow  트리 디버거 창 (IMGUI)
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

### 트리는 스스로 구성된다 (초기화 순서 의존 제거)

씬 로드 시 Unity는 오브젝트마다 `Awake` → `OnEnable`을 이어서 호출합니다. 트리를 매니저의
`Awake`에서 만들면, 아이콘이 매니저보다 먼저 처리됐을 때 `RedDotIcon.OnEnable` 시점에 트리가
아직 없습니다. 재바인딩 경로가 없으면 그 아이콘은 영구히 죽고, 씬에서 오브젝트 순서를 바꾸거나
프리팹을 다른 위치에 배치하는 것만으로 증상이 나타났다 사라집니다.

`[DefaultExecutionOrder]`로 순서를 강제하는 방법도 있지만, 여기서는 **트리를 MonoBehaviour에서
떼어냈습니다.** `RedDotTree`는 순수 C# 정적 서비스이고 **첫 접근 시점에 스스로 구성**되므로,
누가 먼저 물어보든 그 시점에 준비되어 있습니다. 초기화 순서라는 개념 자체가 사라집니다.

```csharp
RedDotTree.SetCount(RedDotType.QuestDaily, 3);   // 매니저 없이도 동작
```

EditMode 테스트에는 씬도 GameObject도 없으므로, `RedDotTreeTests`가 통과한다는 것 자체가
매니저 없이 동작한다는 증거입니다.

### 기존 사용자를 위한 안내

**할 일이 없습니다.** `RedDotManager.Instance.SetCount(...)` 같은 기존 호출은 그대로 동작하고,
씬에 배치해 둔 `RedDotManager`도 그대로 두면 됩니다. 달라진 것은 그 컴포넌트가 이제 **필수가
아니라는 점**뿐입니다. 새로 쓰는 코드라면 매니저를 거치지 않는 `RedDotTree.SetCount(...)` 쪽을
권합니다.

## 트리 디버거 창

`Window ▸ RedDot ▸ Tree Debugger`

레드닷 버그의 대부분은 "왜 안 켜지지" 또는 "왜 안 꺼지지"이고, 원인은 보통 셋 중 하나다 —
값이 안 들어왔거나, 노드가 잠겨 있거나, enum 값을 잘못 매겨 부모에 안 붙었거나.
이 창은 그 세 가지를 한 화면에서 구분할 수 있게 만든 진단 도구다.

```
구조 진단 (0)

노드                              카운트 (자기/자식)   잠금   값 주입
● MainMenu (1000)                 5  (0/5)            ☐     [ 0 ] 설정 +1 -1
  ● Shop (1100)                   0  (0/0)            ☐     [ 0 ] 설정 +1 -1
    ○ ShopPackage (1110)          0  (0/0)            ☐     ...
    ○ ShopLimit (1120)            0  (0/0)            ☐     ...
  ○ Character (1200)              0  (0/0)            ☑     ...
  ● Quest (1300)                  5  (0/5)            ☐     ...
    ● QuestDaily (1310)           3  (3/0)            ☐     ...
    ● QuestWeekly (1320)          2  (2/0)            ☐     ...
```

- **실효 카운트를 자기/자식으로 분해해서 표시한다.** `5 (0/5)`는 "내 값은 0인데 자식 때문에 켜졌다"는
  뜻이라, 어느 노드를 파고들어야 하는지가 바로 보인다
- **값 주입** — 서버 응답이나 콘텐츠 조건 없이도 특정 노드에 카운트를 넣어 UI 반응을 확인할 수 있다.
  잠금 토글도 즉시 반영된다
- **구조 진단** — enum 값만 보고 계층을 재구성해, 부모 값이 없어 조용히 루트가 된 노드,
  중간 단계를 건너뛰고 조부모에 붙은 노드, 자식 자리(1~9)를 다 쓴 노드, 값이 중복된 이름을 찾아낸다.
  플레이하지 않아도 동작하므로 enum에 값을 추가한 직후 확인하는 용도로 쓴다

표시할 내용은 `RedDotTreeModel`(순수 C#)이 만들고 창은 그리기만 한다. 트리를 구성할 때
런타임과 **같은** `RedDotHierarchy.GetParentType()`을 쓰기 때문에 "툴에서는 맞는데 실행하면 다른"
상황이 생기지 않으며, 이 전제는 EditMode 테스트로 고정되어 있다.

## 파일 구성

```
Runtime/
├─ RedDotType.cs          노드 타입 enum 정의 (숫자 구간 = 계층)
├─ RedDotHierarchy.cs     enum 숫자 규칙 -> 부모 유도 (순수 C#)
├─ RedDotNode.cs          트리 노드 (순수 C#)
├─ RedDotTree.cs          트리 소유자 · 지연 구성 (순수 C#)
├─ RedDotManager.cs       씬 측 진입점 (선택 사항)
├─ RedDotIcon.cs          UI 컴포넌트 — 아이콘 표시/숨김
└─ RedDotCountIcon.cs     UI 컴포넌트 — 숫자 카운트 표시
Editor/
├─ RedDotTreeModel.cs        enum -> 트리 구성 + 구조 진단 (순수 C#)
└─ RedDotDebuggerWindow.cs   트리 디버거 창 (IMGUI)
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
(`com.unity.test-framework` 필요 · EditMode 테스트 47종 — RedDotNode 카운트 집계/Lock/콜백/무결성, RedDotHierarchy 계층 유도, RedDotTree 지연 구성, RedDotTreeModel 트리 구성/진단)

## CI

`.github/workflows/ci.yml` 이 두 단계로 돌아간다.

| Job | 하는 일 | Unity 라이선스 |
|---|---|---|
| `core-build` | 트리 코어를 netstandard2.1 / C# 9 로 컴파일 | 불필요 |
| `editmode-tests` | game-ci로 EditMode 테스트 47종 실행 | 필요 |

`core-build`는 컴파일 회귀를 잡는 동시에 "`RedDotNode` / `RedDotHierarchy`는 순수 C#이며
Unity에 의존하지 않는다"는 위의 주장을 빌드로 강제한다. 이 두 파일에 UnityEngine 참조가
들어오는 순간 job이 깨진다. Manager / Icon 계열은 Unity 계층이라 이 빌드에서 제외한다.

`editmode-tests`는 패키지 저장소에 Unity 프로젝트가 없으므로, 이 패키지만 참조하는 최소
프로젝트를 워크플로에서 만들어 그 안에서 테스트를 돌린다(로컬 패키지의 `Tests/`가 잡히도록
manifest의 `testables`에 등록). 라이선스 시크릿(`UNITY_LICENSE`, `UNITY_EMAIL`,
`UNITY_PASSWORD`)이 없는 저장소에서는 이 job을 건너뛴다 — 포크 PR에서 라이선스가 없다는
이유로 빨간 X가 뜨는 것을 막기 위한 게이트다.

## 스레딩

**메인 스레드 전용입니다.** 값 변경과 통지는 호출한 스레드에서 그대로 동기 실행되며,
내부에 락이나 스레드 마샬링이 없습니다. 백그라운드 스레드(네트워크 응답 콜백, `Task`
연속 실행 등)에서 값을 바꾸면 구독자도 그 스레드에서 깨어나고, 구독자가 Unity API를
건드리는 순간 예외가 납니다.

서버 응답처럼 다른 스레드에서 값이 들어오는 경우에는 **호출하는 쪽이 메인 스레드로
넘긴 뒤** 값을 설정해야 합니다. 이 제약을 라이브러리 안으로 들이지 않은 이유는,
마샬링 방식(코루틴 / `SynchronizationContext` / 자체 디스패처)이 프로젝트마다 다르고
그 선택을 패키지가 강제하면 오히려 걸림돌이 되기 때문입니다.

## 요구 사항

- Unity 2021.3+
- TextMeshPro (`RedDotCountIcon` 사용 시)
