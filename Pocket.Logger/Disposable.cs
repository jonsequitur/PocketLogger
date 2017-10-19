using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pocket
{
    [DebuggerStepThrough]
    internal static class Disposable
    {
        private static readonly IDisposable empty = Create(() =>
        {
        });

        public static IDisposable Create(Action dispose) =>
            new AnonymousDisposable(dispose);

        public static IDisposable Empty { get; } = empty;

        private class AnonymousDisposable : IDisposable
        {
            private Action dispose;

            public AnonymousDisposable(Action dispose) =>
                this.dispose = dispose ??
                               throw new ArgumentNullException(nameof(dispose));

            public void Dispose()
            {
                dispose?.Invoke();
                dispose = null;
            }
        }
    }

    [DebuggerStepThrough]
    internal class CompositeDisposable : IDisposable, IEnumerable<IDisposable>
    {
        private bool isDisposed = false;
 
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            if (isDisposed)
            {
                disposable.Dispose();
            }

            disposables.Add(disposable);
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
//
//            while (disposables.TryTake(out var disposable))
//            {
//                disposable.Dispose();
//            }
        }

        public IEnumerator<IDisposable> GetEnumerator() => disposables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
