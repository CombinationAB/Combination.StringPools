namespace Combination.StringPools;

internal sealed class DisposeLock : IDisposable
{
    private volatile bool isDisposed;
    private volatile int preventDispose;

    public IDisposable PreventDispose()
    {
        Interlocked.Increment(ref preventDispose);
        if (isDisposed)
        {
            Interlocked.Decrement(ref preventDispose);
            throw new ObjectDisposedException("String pool is already disposed");
        }

        return this;
    }

    public bool IsDisposed => isDisposed;

    public void BeginDispose()
    {
        isDisposed = true;
        if (preventDispose > 0)
        {
            SpinWait.SpinUntil(() => preventDispose <= 0);
        }
    }

    void IDisposable.Dispose()
    {
        Interlocked.Decrement(ref preventDispose);
    }
}
