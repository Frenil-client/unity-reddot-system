# Changelog

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
