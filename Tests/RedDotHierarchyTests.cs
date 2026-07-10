using NUnit.Framework;

namespace RedDotSystem.Tests
{
    // enum 숫자 구간 규칙에서 부모를 유도하는 순수 함수 검증.
    public class RedDotHierarchyTests
    {
        [Test]
        public void Leaf_ReturnsDirectParent()
        {
            Assert.AreEqual(RedDotType.Shop, RedDotHierarchy.GetParentType(RedDotType.ShopPackage));   // 1110 -> 1100
            Assert.AreEqual(RedDotType.Quest, RedDotHierarchy.GetParentType(RedDotType.QuestWeekly));  // 1320 -> 1300
        }

        [Test]
        public void MidLevel_ReturnsTopLevel()
        {
            Assert.AreEqual(RedDotType.MainMenu, RedDotHierarchy.GetParentType(RedDotType.Shop));      // 1100 -> 1000
            Assert.AreEqual(RedDotType.MainMenu, RedDotHierarchy.GetParentType(RedDotType.Character)); // 1200 -> 1000
        }

        [Test]
        public void Root_ReturnsNone()
        {
            Assert.AreEqual(RedDotType.None, RedDotHierarchy.GetParentType(RedDotType.MainMenu)); // 1000 -> None
        }

        [Test]
        public void None_ReturnsNone()
        {
            Assert.AreEqual(RedDotType.None, RedDotHierarchy.GetParentType(RedDotType.None));
        }

        [Test]
        public void UndefinedIntermediate_SkipsToNearestDefinedAncestor()
        {
            // 1234는 enum에 없는 가상 값 - 1230, 1200 순으로 올라가며 정의된 조상을 찾는다
            Assert.AreEqual(RedDotType.Character, RedDotHierarchy.GetParentType((RedDotType)1234)); // -> 1230(미정의) -> 1200
        }
    }
}
