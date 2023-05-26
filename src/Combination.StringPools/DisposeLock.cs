namespace Combination.StringPools;

internal sealed class DisposeLock : IDisposable
{
    private bool isDisposed;
    private volatile int preventDispose;

    public IDisposable PreventDispose()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("String pool is already disposed");
        }

        Interlocked.Increment(ref preventDispose);

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
