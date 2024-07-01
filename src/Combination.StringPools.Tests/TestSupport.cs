using System.Numerics;

public static class TestSupport
{
    public static int GetAllocationSize(int length) => length + 1 + (BitOperations.Log2((uint)length) / 7);
    public static int GetMaxStringSizeForAllocation(int allocationSize)
    {
        for (var i = allocationSize - 1; i >= 0; --i)
        {
            if (GetAllocationSize(i) == allocationSize)
            {
                return i;
            }
        }
        throw new ArgumentOutOfRangeException("allocationSize");
    }
}
