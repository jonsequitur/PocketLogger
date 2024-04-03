using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

#nullable enable

namespace Pocket;

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal static class Disposable
{
    private static readonly IDisposable empty = Create(EmptyDispose);

    private static void EmptyDispose()
    {
    }

    public static IDisposable Create(Action dispose) =>
        new AnonymousDisposable(dispose);

    public static IDisposable Empty { get; } = empty;

    private class AnonymousDisposable : IDisposable
    {
        private Action? dispose;

        public AnonymousDisposable(Action dispose) =>
            this.dispose = dispose ??
                           throw new ArgumentNullException(nameof(dispose));

        public void Dispose()
        {
            Interlocked.CompareExchange(ref dispose, null, dispose)?.Invoke();
        }
    }
}

#if !SourceProject
[System.Diagnostics.DebuggerStepThrough]
#endif
internal class CompositeDisposable : IDisposable, IEnumerable<IDisposable>
{
    private bool isDisposed = false;

    private readonly List<IDisposable> disposables = new();

    public void Add(IDisposable disposable)
    {
        if (disposable is null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        if (isDisposed)
        {
            disposable.Dispose();
        }
        else
        {
            disposables.Add(disposable);
        }
    }

    public void Add(Action dispose) => Add(Disposable.Create(dispose));

    public void Dispose()
    {
        isDisposed = true;

        foreach (var disposable in disposables.ToArray())
        {
            disposables.Remove(disposable);
            disposable.Dispose();
        }
    }

    public IEnumerator<IDisposable> GetEnumerator() => disposables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}