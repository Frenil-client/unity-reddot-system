using System;

namespace RedDotSystem
{
    /// <summary>
    /// RedDotType의 숫자 구간 규칙에서 부모 타입을 유도합니다.
    /// 계층 정의가 enum 숫자 규칙 하나로 단일화되어,
    /// enum에 값만 추가하면 트리에 자동 반영됩니다.
    ///
    /// 규칙: 가장 낮은 0이 아닌 자리부터 차례로 0으로 만들며
    /// enum에 정의된 타입이 나올 때까지 올라갑니다.
    ///   1310 -> 1300 (Quest)
    ///   1300 -> 1000 (MainMenu)
    ///   1000 -> None (루트)
    /// 중간 단계가 enum에 없으면 건너뛰고 계속 올라갑니다.
    /// </summary>
    public static class RedDotHierarchy
    {
        public static RedDotType GetParentType(RedDotType type)
        {
            int value = (int)type;
            if (value <= 0) return RedDotType.None;

            for (int unit = 1; unit < value; unit *= 10)
            {
                int digit = (value / unit) % 10;
                if (digit == 0) continue;

                value -= digit * unit;
                if (value == 0) return RedDotType.None;
                if (Enum.IsDefined(typeof(RedDotType), value))
                    return (RedDotType)value;
            }

            return RedDotType.None;
        }
    }
}
