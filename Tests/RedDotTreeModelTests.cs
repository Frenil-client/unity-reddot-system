using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RedDotSystem.EditorTools;

namespace RedDotSystem.Tests
{
    /// <summary>
    /// 디버거 창이 그리는 트리는 RedDotTreeModel이 만든다.
    /// 창 자체(IMGUI)는 테스트하기 어렵지만, 창이 보여주는 "내용"은 순수 C#이라 고정할 수 있다.
    /// </summary>
    public class RedDotTreeModelTests
    {
        [Test]
        public void BuildRoots_DefaultEnum_HasSingleRoot()
        {
            var roots = RedDotTreeModel.BuildRoots();

            Assert.AreEqual(1, roots.Count);
            Assert.AreEqual(RedDotType.MainMenu, roots[0].Type);
        }

        [Test]
        public void BuildRoots_AssignsDepthByLevel()
        {
            var byType = Flatten(RedDotTreeModel.BuildRoots());

            Assert.AreEqual(0, byType[RedDotType.MainMenu].Depth);
            Assert.AreEqual(1, byType[RedDotType.Shop].Depth);
            Assert.AreEqual(2, byType[RedDotType.ShopPackage].Depth);
        }

        [Test]
        public void BuildRoots_CoversEveryTypeExceptNone()
        {
            var byType = Flatten(RedDotTreeModel.BuildRoots());
            var expected = System.Enum.GetValues(typeof(RedDotType))
                .Cast<RedDotType>()
                .Where(t => t != RedDotType.None)
                .Distinct()
                .ToList();

            CollectionAssert.AreEquivalent(expected, byType.Keys);
        }

        // 에디터가 런타임과 다른 계층 규칙을 구현하면 "툴에서는 맞는데 실행하면 다른" 상황이 된다.
        // 두 경로가 같은 부모를 내는지 enum 전체에 대해 고정한다.
        [Test]
        public void BuildRoots_ParentMatchesRuntimeHierarchy()
        {
            var byType = Flatten(RedDotTreeModel.BuildRoots());

            foreach (var kv in byType)
                Assert.AreEqual(RedDotHierarchy.GetParentType(kv.Key), kv.Value.ParentType,
                    $"{kv.Key} 의 부모가 런타임 규칙과 다릅니다.");
        }

        [Test]
        public void BuildRoots_ChildrenSortedByEnumValue()
        {
            var byType = Flatten(RedDotTreeModel.BuildRoots());
            var shopChildren = byType[RedDotType.Shop].Children.Select(c => (int)c.Type).ToList();

            CollectionAssert.IsOrdered(shopChildren);
        }

        [Test]
        public void Validate_DefaultEnum_ReportsNoWarnings()
        {
            var warnings = RedDotTreeModel.Validate().Where(i => i.IsWarning).ToList();

            Assert.IsEmpty(warnings,
                "기본 enum은 구조 경고가 없어야 합니다: " + string.Join(", ", warnings.Select(w => w.ToString())));
        }

        [TestCase(1000, 0)]
        [TestCase(1100, 1000)]
        [TestCase(1110, 1100)]
        [TestCase(1320, 1300)]
        [TestCase(1, 0)]
        [TestCase(0, 0)]
        public void ImmediateParentValue_StripsLowestNonZeroDigit(int value, int expected)
        {
            Assert.AreEqual(expected, RedDotTreeModel.ImmediateParentValue(value));
        }

        [TestCase(1000, true)]
        [TestCase(2000, true)]
        [TestCase(300, true)]
        [TestCase(1100, false)]
        [TestCase(1110, false)]
        public void IsTopLevel_TrueOnlyForSingleNonZeroDigit(int value, bool expected)
        {
            Assert.AreEqual(expected, RedDotTreeModel.IsTopLevel(value));
        }

        private static Dictionary<RedDotType, RedDotTreeEntry> Flatten(IReadOnlyList<RedDotTreeEntry> roots)
        {
            var result = new Dictionary<RedDotType, RedDotTreeEntry>();

            void Walk(RedDotTreeEntry entry)
            {
                result[entry.Type] = entry;
                foreach (var child in entry.Children) Walk(child);
            }

            foreach (var root in roots) Walk(root);
            return result;
        }
    }
}
