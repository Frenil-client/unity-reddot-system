# Changelog

## 트리 초기화를 Unity 생명주기에서 분리

### 배경

트리를 `RedDotManager`가 `Awake`에서 만들고, `RedDotIcon`은 `OnEnable`에서 그 트리를 찾았다.
그런데 씬 로드 시 Unity는 오브젝트마다 Awake -> OnEnable을 이어서 호출하므로, 아이콘이
매니저보다 먼저 처리되면 아이콘의 OnEnable 시점에 트리가 아직 없다. 이때 아이콘은 에러만
남기고 어떤 노드에도 연결되지 못한 채 영구히 죽는다 — 재바인딩 경로가 없었기 때문이다.

씬에서 오브젝트 순서를 바꾸거나 프리팹을 다른 위치에 배치하는 것만으로 재현됐다 사라졌다
하는 종류의 버그라, 실제로 통합 데모를 프리팹으로 옮기는 과정에서 터졌다.

### 변경 사항

- `RedDotTree` 추가 — 트리의 실제 소유자이자 순수 C# 정적 서비스. **첫 접근 시점에**
  **스스로 구성**되므로 누가 먼저 물어보든 준비되어 있다. 초기화 순서라는 개념이 사라진다
- `RedDotIcon`이 매니저 대신 `RedDotTree`를 본다. 씬에 `RedDotManager`가 없어도 동작한다
- `RedDotManager`는 **선택 사항**이 되었다. 인스펙터에 보이는 진입점이 필요하거나
  참조로 접근하고 싶을 때만 배치하면 되며, 내부적으로 `RedDotTree`에 위임한다
- 도메인 리로드를 끈 상태에서 플레이를 반복해도 정적 상태가 남지 않도록
  `RuntimeInitializeOnLoadMethod`로 트리를 초기화한다
- 트리 디버거 창도 매니저 의존을 제거했다

### Breaking Changes

없음. `RedDotManager.Instance.SetCount(...)` 같은 기존 호출은 그대로 동작한다.
다만 이제 `RedDotTree.SetCount(...)`로 매니저 없이 부르는 쪽이 권장 경로다.

### 검증

`Tests/RedDotTreeTests.cs` 10종 추가 (레포 전체 37종 -> 47종). EditMode 테스트에는 씬도
GameObject도 없으므로, **이 테스트들이 통과한다는 것 자체가 매니저 없이 동작한다는 증거**다.
지연 구성, 부모 집계, 잠금 전파, Reset 격리, 미등록 타입 처리를 고정했다.

---

## .meta 파일 추가 (UPM git 설치 대응)

git URL로 설치하면 패키지가 immutable 폴더(Library/PackageCache)에 놓이는데, Unity는 여기에
.meta를 생성하지 못한다. 이 저장소에는 .meta가 하나도 없어서 모든 자산이 무시됐고
(`has no meta file, but it's in an immutable folder. The asset will be ignored`),
asmdef도 임포트되지 않아 `RedDotSystem` 어셈블리 자체가 만들어지지 않았다.
드롭인(Assets/ 복사) 설치에서는 Unity가 meta를 생성해 주기 때문에 드러나지 않던 문제다.

폴더와 자산 전체에 .meta를 추가했다. 코드 변경은 없다.

---

## 트리 디버거 창 추가 · CI 구축

### 배경

레드닷 버그 신고는 대부분 "왜 안 켜지지" 또는 "왜 안 꺼지지" 한 문장으로 들어오는데, 원인은
보통 셋 중 하나다 — 값이 안 들어왔거나, 노드가 잠겨 있거나, enum 값을 잘못 매겨 부모에 안
붙었거나. 셋 다 런타임에서는 "레드닷이 안 보인다"는 같은 증상으로만 드러나서, 로그를 심어
카운트를 찍어보기 전에는 어느 경우인지 구분되지 않았다. 세 번째(계층 오배치)는 특히 고약한데,
`RedDotHierarchy`가 정의되지 않은 중간 단계를 건너뛰고 계속 올라가는 설계라 값을 잘못 매겨도
예외 없이 조용히 다른 부모에 붙거나 루트가 되기 때문이다.

### 변경 사항

- `Editor/RedDotDebuggerWindow` 추가 (`Window ▸ RedDot ▸ Tree Debugger`)
  - 계층을 트리로 표시하고, 플레이 중에는 실효 카운트를 **자기/자식으로 분해**해 보여준다.
    `5 (0/5)`는 "내 값은 0인데 자식 때문에 켜졌다"는 뜻이라 어느 노드를 파고들지가 바로 보인다
  - 노드별 카운트 주입과 잠금 토글을 지원해, 서버 응답이나 콘텐츠 해금 조건 없이도 UI 반응을 확인할 수 있다
  - 플레이 중이 아니어도 enum만으로 구조를 그린다. 리페인트는 10Hz로 제한한다
- `Editor/RedDotTreeModel` 추가 — enum에서 표시용 트리를 만들고 구조 문제를 진단하는 순수 C# 계층.
  창은 그리기만 담당하므로 트리 구성 규칙 자체가 EditMode 테스트 대상이 된다.
  진단 항목: 부모 값이 없어 조용히 루트가 된 노드, 중간 단계를 건너뛰고 조부모에 붙은 노드,
  자식 자리(1~9)를 다 쓴 노드, 값이 중복된 이름
- `RedDotNode`에 진단용 읽기 전용 프로퍼티 `SelfCount` / `ChildrenCount` / `Children` 추가.
  기존에는 실효 `Count`만 노출되어 "자기 값 때문인지 자식 때문인지"를 밖에서 구분할 수 없었다
- `.github/workflows/ci.yml` 추가 — 라이선스 없이 도는 코어 컴파일 job과 game-ci EditMode 테스트 job.
  자세한 구성은 [README.md](README.md)의 CI 절 참고

계층 유도는 툴이 별도 규칙을 구현하지 않고 런타임과 **같은** `RedDotHierarchy.GetParentType()`을
호출한다. 에디터가 규칙을 복제하면 "툴에서는 맞는데 실행하면 다른" 상황이 생기고, 그건 디버깅
도구가 만들 수 있는 최악의 실패다. 두 경로가 같은 부모를 낸다는 전제 자체를 테스트로 고정했다.

### Breaking Changes

없음. 추가만 있다.

### 검증

`Tests/RedDotTreeModelTests.cs` 17종 추가 (레포 전체 20종 -> 37종). 트리 구성(루트/깊이/정렬/전체 커버),
런타임 계층 규칙과의 일치, 기본 enum이 경고를 내지 않음, 자릿수 계산 함수의 경계값을 고정했다.
`RedDotTreeModel`과 트리 코어는 순수 C#이라 외부 dotnet 하네스에서 23/23 통과를 확인했고,
창 자체(IMGUI)는 Unity 에디터에서 육안 확인이 필요하다.

---

## RedDotNode 카운트 캐싱 및 구조 정리

### 배경

unity-stat-system, unity-mvvm 리팩토링과 같은 기준(핫패스 비용, 정확성, 단일 소스)으로 재점검하며 발견된 문제들:

1. Value 읽기가 매번 서브트리 전체를 재귀 순회했다(읽기 O(서브트리)). 전파 경로가 겹치면 리프 하나 갱신에 조상마다 서브트리 재계산이 반복되어, 트리가 커질수록 갱신 비용이 트리 크기에 비례해 커졌다
2. 전파 경로(Refresh)가 변경 감지 없이 무조건 콜백을 발화해, 형제가 이미 켜져 있어 부모 표시가 그대로인 경우에도 부모/조상 UI 콜백이 다시 불렸다
3. RedDotCountIcon.SetCount가 노드를 거치지 않는 별도 경로라, 노드 콜백이 아이콘 on/off를 덮어써도 카운트 텍스트는 남아 표시와 숫자가 어긋날 수 있었다. 부모에 자식 카운트 합계를 표시할 수단도 없었다
4. 계층 정보가 enum 숫자 구간과 BuildTree 수동 선언 두 곳에 중복되어 어긋날 수 있었다
5. 순환 참조(AddChild로 조상을 자식으로)와 다중 부모(카운트 이중 집계)가 무방비였고, 노드/아이콘 양쪽에 별개의 locked 상태가 존재했다

### 변경 사항

- RedDotNode 값을 bool -> int 카운트로 변경. 자식 서브트리 합을 캐싱해 읽기 O(1), 갱신은 델타만 부모로 전파(O(트리 깊이)). 실효 카운트가 실제로 바뀐 노드만 콜백 발화
- RedDotHierarchy 추가 - enum 숫자 구간 규칙에서 부모를 자동 유도(1110 -> 1100 -> 1000). RedDotManager.BuildTree의 수동 Register/AddChild 선언 제거, enum에 값만 추가하면 트리 반영
- RedDotManager.SetCount(type, count) 추가
- RedDotCountIcon은 노드 콜백에서 받은 카운트로 on/off와 텍스트를 함께 갱신 (단일 소스)
- AddChild에서 순환/다중 부모를 InvalidOperationException으로 차단
- 아이콘 레벨 _locked 제거, RedDotIcon.SetLocked은 노드로 위임 (노드가 단일 진실)
- RedDotIcon.OnEnable에서 매니저 부재/미등록 타입을 Debug.LogError로 명시적 보고

### Breaking Changes

- 콜백 시그니처 Action&lt;bool&gt; -> Action&lt;int&gt; (bool이 필요하면 count > 0으로 파생)
- RedDotCountIcon.SetCount(ulong) 제거 -> RedDotManager.SetCount(type, int)로 노드에 직접 설정
- RedDotManager.BuildTree의 수동 트리 구성 제거 - 계층은 enum 숫자 규칙으로만 정의
- RedDotIcon.Locked/SetLocked이 아이콘 상태가 아니라 연결된 노드 상태를 반영
- RedDotNode.SetValue(bool)는 유지 (내부적으로 카운트 0/1)

### 검증

- RedDotNodeTests 15종 재작성: 카운트 집계(손자까지), 잠금 기여 제외/흡수 후 일괄 반영, 잠긴 부모의 전파 가지치기, 순환/다중 부모 예외, 기존 시나리오(전파/변경 감지/force/Remove) 포함
- RedDotHierarchyTests 5종: 직계/중간 건너뛰기/루트/None 유도 규칙
- 외부 dotnet+NUnit 하네스에서 20/20 통과 (UnityEngine은 스텁 대체 - Unity Test Runner에서 재확인 권장)
