using System;
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
        public void Count_AggregatesChildren_UpToGrandparent()
        {
            var root = new RedDotNode(RedDotType.MainMenu);
            var quest = new RedDotNode(RedDotType.Quest);
            var daily = new RedDotNode(RedDotType.QuestDaily);
            var weekly = new RedDotNode(RedDotType.QuestWeekly);
            root.AddChild(quest);
            quest.AddChild(daily);
            quest.AddChild(weekly);

            daily.SetCount(3);
            weekly.SetCount(2);

            Assert.AreEqual(5, quest.Count);
            Assert.AreEqual(5, root.Count);

            daily.SetCount(1);

            Assert.AreEqual(3, quest.Count);
            Assert.AreEqual(3, root.Count);
        }

        [Test]
        public void Locked_ReturnsFalse_EvenWhenValueTrue()
        {
            var node = new RedDotNode(RedDotType.Shop);
            node.SetValue(true);
            Assert.IsTrue(node.Value);

            node.SetLocked(true);

            Assert.IsFalse(node.Value);
            Assert.AreEqual(0, node.Count);
        }

        [Test]
        public void Locked_ExcludesContribution_FromParent()
        {
            var parent = new RedDotNode(RedDotType.Quest);
            var child = new RedDotNode(RedDotType.QuestDaily);
            parent.AddChild(child);
            child.SetCount(3);
            Assert.AreEqual(3, parent.Count);

            child.SetLocked(true);
            Assert.AreEqual(0, parent.Count);

            child.SetLocked(false);
            Assert.AreEqual(3, parent.Count);
        }

        [Test]
        public void Locked_AbsorbsChanges_AppliedOnUnlock()
        {
            var parent = new RedDotNode(RedDotType.Quest);
            var child = new RedDotNode(RedDotType.QuestDaily);
            parent.AddChild(child);

            child.SetLocked(true);
            child.SetCount(7); // 잠긴 동안의 변화는 위로 전파되지 않음
            Assert.AreEqual(0, parent.Count);

            child.SetLocked(false); // 해제 시점에 일괄 반영
            Assert.AreEqual(7, parent.Count);
        }

        [Test]
        public void LockedParent_PrunesPropagation_ToGrandparent()
        {
            var root = new RedDotNode(RedDotType.MainMenu);
            var quest = new RedDotNode(RedDotType.Quest);
            var daily = new RedDotNode(RedDotType.QuestDaily);
            root.AddChild(quest);
            quest.AddChild(daily);

            int rootCalls = 0;
            root.AddCallback(_ => rootCalls++);

            quest.SetLocked(true);
            daily.SetCount(3); // 잠긴 quest에서 흡수 - root 콜백 미발화

            Assert.AreEqual(0, rootCalls);
            Assert.AreEqual(0, root.Count);
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
        public void ChildChange_TriggersParentCallback_WithAggregatedCount()
        {
            int parentCalls = 0;
            int lastCount = -1;
            var parent = new RedDotNode(RedDotType.Shop);
            var child = new RedDotNode(RedDotType.ShopPackage);
            parent.AddChild(child);
            parent.AddCallback(c => { parentCalls++; lastCount = c; });

            child.SetCount(4);

            Assert.AreEqual(1, parentCalls);
            Assert.AreEqual(4, lastCount);
        }

        [Test]
        public void RemoveCallback_StopsReceivingUpdates()
        {
            int calls = 0;
            void Handler(int _) => calls++;
            var node = new RedDotNode(RedDotType.Shop);
            node.AddCallback(Handler);

            node.SetValue(true);
            node.RemoveCallback(Handler);
            node.SetValue(false);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddChild_Cycle_Throws()
        {
            var a = new RedDotNode(RedDotType.Shop);
            var b = new RedDotNode(RedDotType.ShopPackage);
            a.AddChild(b);

            Assert.Throws<InvalidOperationException>(() => b.AddChild(a));
        }

        [Test]
        public void AddChild_SecondParent_Throws()
        {
            var p1 = new RedDotNode(RedDotType.Shop);
            var p2 = new RedDotNode(RedDotType.Quest);
            var child = new RedDotNode(RedDotType.ShopPackage);
            p1.AddChild(child);

            Assert.Throws<InvalidOperationException>(() => p2.AddChild(child));
        }

        [Test]
        public void AddChild_WithExistingCount_ReflectsImmediately()
        {
            var parent = new RedDotNode(RedDotType.Shop);
            var child = new RedDotNode(RedDotType.ShopPackage);
            child.SetCount(2);

            parent.AddChild(child);

            Assert.AreEqual(2, parent.Count);
        }

        [Test]
        public void NegativeCount_ClampsToZero()
        {
            var node = new RedDotNode(RedDotType.Shop);
            node.SetCount(-5);
            Assert.AreEqual(0, node.Count);
        }
    }
}
