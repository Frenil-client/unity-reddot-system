using System;
using System.Collections.Generic;

namespace RedDotSystem.EditorTools
{
    /// <summary>표시용 트리의 노드 하나. 런타임 RedDotNode와 별개로 enum만 보고 구성됩니다.</summary>
    public sealed class RedDotTreeEntry
    {
        public RedDotType Type { get; }
        public RedDotType ParentType { get; }
        public int Depth { get; internal set; }
        public List<RedDotTreeEntry> Children { get; } = new List<RedDotTreeEntry>();

        internal RedDotTreeEntry(RedDotType type, RedDotType parentType)
        {
            Type = type;
            ParentType = parentType;
        }
    }

    public enum RedDotTreeIssueKind
    {
        /// <summary>부모 자리 번호가 enum에 없어 의도치 않게 루트가 된 노드.</summary>
        UnintendedRoot,

        /// <summary>바로 위 자리의 부모가 없어 조부모에 붙은 노드. 의도한 것일 수도 있다.</summary>
        SkippedIntermediate,

        /// <summary>같은 숫자를 가진 enum 이름이 둘 이상.</summary>
        DuplicateValue,

        /// <summary>자식 자리(1~9)를 모두 써서 더 추가할 수 없는 노드.</summary>
        ChildSlotsFull,
    }

    public readonly struct RedDotTreeIssue
    {
        public RedDotTreeIssueKind Kind { get; }
        public RedDotType Type { get; }
        public string Message { get; }

        public RedDotTreeIssue(RedDotTreeIssueKind kind, RedDotType type, string message)
        {
            Kind = kind;
            Type = type;
            Message = message;
        }

        /// <summary>경고로 표시할지 여부. SkippedIntermediate는 의도된 설계일 수 있어 정보성이다.</summary>
        public bool IsWarning => Kind != RedDotTreeIssueKind.SkippedIntermediate;

        public override string ToString() => $"[{Kind}] {Type}: {Message}";
    }

    /// <summary>
    /// RedDotType enum 하나만 보고 트리 구조를 재구성하고 문제를 진단합니다.
    ///
    /// 런타임(RedDotManager)이 트리를 만드는 규칙과 같은 함수(RedDotHierarchy.GetParentType)를
    /// 사용하므로, 여기 보이는 구조가 곧 실행 시 만들어지는 구조다. 에디터가 별도 규칙을
    /// 구현하면 "툴에서는 맞는데 실행하면 다른" 상황이 생기기 때문에 의도적으로 공유한다.
    ///
    /// UnityEngine에 의존하지 않는 순수 C#이라 EditMode 테스트로 검증할 수 있다.
    /// </summary>
    public static class RedDotTreeModel
    {
        /// <summary>enum 전체에서 루트 목록을 만들고, 각 루트 아래에 자식을 연결해 돌려줍니다.</summary>
        public static IReadOnlyList<RedDotTreeEntry> BuildRoots()
        {
            var entries = new Dictionary<RedDotType, RedDotTreeEntry>();
            foreach (var type in DistinctTypes())
                entries[type] = new RedDotTreeEntry(type, RedDotHierarchy.GetParentType(type));

            var roots = new List<RedDotTreeEntry>();
            foreach (var entry in entries.Values)
            {
                if (entry.ParentType != RedDotType.None && entries.TryGetValue(entry.ParentType, out var parent))
                    parent.Children.Add(entry);
                else
                    roots.Add(entry);
            }

            foreach (var root in roots)
                AssignDepth(root, 0);

            SortByValue(roots);
            return roots;
        }

        /// <summary>
        /// enum 숫자 규칙이 의도대로 트리를 만드는지 점검합니다.
        /// 값을 잘못 매기면 조용히 루트가 되거나 엉뚱한 부모에 붙는데,
        /// 런타임에서는 "레드닷이 안 올라온다"는 증상으로만 드러나 원인 추적이 어렵다.
        /// </summary>
        public static IReadOnlyList<RedDotTreeIssue> Validate()
        {
            var issues = new List<RedDotTreeIssue>();
            var defined = new HashSet<int>();
            var seenNames = new Dictionary<int, string>();
            var childCounts = new Dictionary<RedDotType, int>();

            foreach (RedDotType type in Enum.GetValues(typeof(RedDotType)))
            {
                if (type == RedDotType.None) continue;

                int value = (int)type;
                string name = type.ToString();

                if (!defined.Add(value))
                {
                    issues.Add(new RedDotTreeIssue(
                        RedDotTreeIssueKind.DuplicateValue, type,
                        $"{value} 값을 '{seenNames[value]}' 와(과) 공유합니다. 같은 값의 이름이 둘 이상이면 노드가 하나로 합쳐집니다."));
                }
                else
                {
                    seenNames[value] = name;
                }
            }

            foreach (var type in DistinctTypes())
            {
                int value = (int)type;
                var parent = RedDotHierarchy.GetParentType(type);

                if (parent != RedDotType.None)
                {
                    childCounts.TryGetValue(parent, out int c);
                    childCounts[parent] = c + 1;

                    int immediate = ImmediateParentValue(value);
                    if (immediate != 0 && immediate != (int)parent)
                    {
                        issues.Add(new RedDotTreeIssue(
                            RedDotTreeIssueKind.SkippedIntermediate, type,
                            $"바로 위 자리 {immediate} 이(가) enum에 없어 {parent}({(int)parent}) 에 붙습니다."));
                    }
                    continue;
                }

                if (!IsTopLevel(value))
                {
                    issues.Add(new RedDotTreeIssue(
                        RedDotTreeIssueKind.UnintendedRoot, type,
                        $"상위 자리에 정의된 조상이 없어 루트가 됩니다. {ImmediateParentValue(value)} 같은 부모 값을 enum에 추가해야 전파됩니다."));
                }
            }

            foreach (var kv in childCounts)
            {
                if (kv.Value >= 9)
                {
                    issues.Add(new RedDotTreeIssue(
                        RedDotTreeIssueKind.ChildSlotsFull, kv.Key,
                        $"자식이 {kv.Value}개라 해당 자리(1~9)가 가득 찼습니다. 더 추가하려면 자릿수를 늘려야 합니다."));
                }
            }

            return issues;
        }

        /// <summary>
        /// 가장 낮은 0이 아닌 자리를 0으로 만든 값. 규칙상 "바로 위 부모"에 해당합니다.
        /// 최상위 노드(1000처럼 자리가 하나뿐)면 0을 돌려줍니다.
        /// </summary>
        public static int ImmediateParentValue(int value)
        {
            if (value <= 0) return 0;

            for (int unit = 1; unit <= value; unit *= 10)
            {
                int digit = (value / unit) % 10;
                if (digit == 0) continue;
                return value - digit * unit;
            }
            return 0;
        }

        /// <summary>0이 아닌 자리가 하나뿐인 값(1000, 2000, 300 ...)인지. 이런 값은 루트가 정상입니다.</summary>
        public static bool IsTopLevel(int value) => ImmediateParentValue(value) == 0;

        private static IEnumerable<RedDotType> DistinctTypes()
        {
            var seen = new HashSet<int>();
            foreach (RedDotType type in Enum.GetValues(typeof(RedDotType)))
            {
                if (type == RedDotType.None) continue;
                if (seen.Add((int)type)) yield return type;
            }
        }

        private static void AssignDepth(RedDotTreeEntry entry, int depth)
        {
            entry.Depth = depth;
            SortByValue(entry.Children);
            foreach (var child in entry.Children)
                AssignDepth(child, depth + 1);
        }

        private static void SortByValue(List<RedDotTreeEntry> list) =>
            list.Sort((a, b) => ((int)a.Type).CompareTo((int)b.Type));
    }
}
