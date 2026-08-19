using NUnit.Framework;

namespace RedDotSystem.Tests
{
    /// <summary>
    /// 트리가 Unity 생명주기와 무관하게 스스로 구성되는지 검증한다.
    /// 이 테스트들이 통과한다는 것 자체가 "씬에 매니저가 없어도 동작한다"는 뜻이다 —
    /// EditMode 테스트에는 씬도 GameObject도 없기 때문이다.
    /// </summary>
    public class RedDotTreeTests
    {
        [SetUp]
        public void ResetTree() => RedDotTree.Reset();

        [TearDown]
        public void CleanUp() => RedDotTree.Reset();

        [Test]
        public void Tree_IsNotBuilt_UntilFirstAccess()
        {
            Assert.IsFalse(RedDotTree.IsBuilt);

            RedDotTree.GetNode(RedDotType.MainMenu);

            Assert.IsTrue(RedDotTree.IsBuilt);
        }

        [Test]
        public void GetNode_DefinedType_ReturnsNode()
        {
            var node = RedDotTree.GetNode(RedDotType.ShopPackage);

            Assert.IsNotNull(node);
            Assert.AreEqual(RedDotType.ShopPackage, node.Type);
        }

        [Test]
        public void GetNode_UndefinedType_ReturnsNull()
        {
            Assert.IsNull(RedDotTree.GetNode((RedDotType)987654));
            Assert.IsNull(RedDotTree.GetNode(RedDotType.None));
        }

        [Test]
        public void GetNode_SameType_ReturnsSameInstance()
        {
            Assert.AreSame(RedDotTree.GetNode(RedDotType.Quest), RedDotTree.GetNode(RedDotType.Quest));
        }

        // 매니저 없이도 계층이 enum 규칙대로 연결되는지.
        [Test]
        public void Tree_WiresParentsFromEnumRule()
        {
            RedDotTree.SetCount(RedDotType.QuestDaily, 3);
            RedDotTree.SetCount(RedDotType.QuestWeekly, 2);

            Assert.AreEqual(5, RedDotTree.GetNode(RedDotType.Quest).Count);
            Assert.AreEqual(5, RedDotTree.GetNode(RedDotType.MainMenu).Count);
        }

        [Test]
        public void SetValue_DefinedType_ReturnsTrueAndPropagates()
        {
            Assert.IsTrue(RedDotTree.SetValue(RedDotType.ShopPackage, true));

            Assert.IsTrue(RedDotTree.GetNode(RedDotType.Shop).Value);
            Assert.IsTrue(RedDotTree.GetNode(RedDotType.MainMenu).Value);
        }

        [Test]
        public void SetCount_UndefinedType_ReturnsFalse()
        {
            Assert.IsFalse(RedDotTree.SetCount((RedDotType)987654, 3));
            Assert.IsFalse(RedDotTree.SetValue((RedDotType)987654, true));
            Assert.IsFalse(RedDotTree.SetLocked((RedDotType)987654, true));
        }

        [Test]
        public void SetLocked_ExcludesSubtreeFromParent()
        {
            RedDotTree.SetCount(RedDotType.ShopPackage, 4);
            Assert.AreEqual(4, RedDotTree.GetNode(RedDotType.MainMenu).Count);

            RedDotTree.SetLocked(RedDotType.Shop, true);

            Assert.AreEqual(0, RedDotTree.GetNode(RedDotType.MainMenu).Count);
        }

        [Test]
        public void Reset_DiscardsPreviousState()
        {
            RedDotTree.SetCount(RedDotType.QuestDaily, 7);
            Assert.AreEqual(7, RedDotTree.GetNode(RedDotType.MainMenu).Count);

            RedDotTree.Reset();

            Assert.AreEqual(0, RedDotTree.GetNode(RedDotType.MainMenu).Count);
        }

        [Test]
        public void Nodes_CoversEveryTypeExceptNone()
        {
            var nodes = RedDotTree.Nodes;

            foreach (RedDotType type in System.Enum.GetValues(typeof(RedDotType)))
            {
                if (type == RedDotType.None) continue;
                Assert.IsTrue(nodes.ContainsKey(type), $"{type} 노드가 없습니다.");
            }

            Assert.IsFalse(nodes.ContainsKey(RedDotType.None));
        }
    }
}
