using System;
using System.Collections;
using System.Collections.Generic;

namespace Pocket
{
    internal static class Disposable
    {
        public static IDisposable Create(Action dispose) =>
            new AnonymousDisposable(dispose);

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
}
