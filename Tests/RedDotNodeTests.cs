using NUnit.Framework;

namespace RedDotSystem.Tests
{
    // RedDotNode 는 순수 C# (Unity 비의존)이라 EditMode 테스트로 트리 로직을 직접 검증한다.
    public class RedDotNodeTests
    {
        [Test]
        public void Leaf_SetTrue_PropagatesToParent()
        {
            var parent = new RedDotNode(RedDotType.Shop);
            var child = new RedDotNode(RedDotType.ShopPackage);
            parent.AddChild(child);

            child.SetValue(true);

            Assert.IsTrue(parent.Value);
        }

        [Test]
        public void Parent_FalseWhenAllChildrenFalse()
        {
            var parent = new RedDotNode(RedDotType.Shop);
            parent.AddChild(new RedDotNode(RedDotType.ShopPackage));
            parent.AddChild(new RedDotNode(RedDotType.ShopLimit));

            Assert.IsFalse(parent.Value);
        }

        [Test]
        public void Locked_ReturnsFalse_EvenWhenValueTrue()
        {
            var node = new RedDotNode(RedDotType.Shop);
            node.SetValue(true);
            Assert.IsTrue(node.Value);

            node.SetLocked(true);

            Assert.IsFalse(node.Value);
        }

        [Test]
        public void Callback_FiresOnChange_NotOnSameValue()
        {
            int calls = 0;
            var node = new RedDotNode(RedDotType.Shop);
            node.AddCallback(_ => calls++);

            node.SetValue(true);
            Assert.AreEqual(1, calls);

            node.SetValue(true); // 동일 값 → 콜백 미발생
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Callback_ForceFlag_FiresEvenOnSameValue()
        {
            int calls = 0;
            var node = new RedDotNode(RedDotType.Shop);
            node.AddCallback(_ => calls++);

            node.SetValue(true);
            node.SetValue(true, forceCallback: true);

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void ChildChange_TriggersParentCallback()
        {
            int parentCalls = 0;
            var parent = new RedDotNode(RedDotType.Shop);
            var child = new RedDotNode(RedDotType.ShopPackage);
            parent.AddChild(child);
            parent.AddCallback(_ => parentCalls++);

            child.SetValue(true);

            Assert.AreEqual(1, parentCalls);
        }

        [Test]
        public void RemoveCallback_StopsReceivingUpdates()
        {
            int calls = 0;
            void Handler(bool _) => calls++;
            var node = new RedDotNode(RedDotType.Shop);
            node.AddCallback(Handler);

            node.SetValue(true);
            node.RemoveCallback(Handler);
            node.SetValue(false);

            Assert.AreEqual(1, calls);
        }
    }
}
