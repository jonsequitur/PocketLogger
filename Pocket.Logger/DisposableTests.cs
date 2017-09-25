using System;
using FluentAssertions;
using Xunit;

namespace Pocket
{
    public class DisposableTests
    {
        [Fact]
        public void When_an_anoymous_disposable_is_disposed_then_the_constructor_delegate_is_invoked()
        {
            var wasDisposed = false;

            var disposable = Disposable.Create(() => wasDisposed = true);

            disposable.Dispose();

            wasDisposed.Should().BeTrue();
        }

        [Fact]
        public void AnonymousDisposable_can_safely_be_disposed_more_than_once()
        {
            var wasDisposed = false;
            var disposable = Disposable.Create(() =>
            {
                if (wasDisposed)
                {
                    throw new ObjectDisposedException("already disposed!");
                }

                wasDisposed = true;
            });

            disposable.Dispose();

            Action disposeAgain = () => disposable.Dispose();

            disposeAgain.ShouldNotThrow();
        }

        [Fact]
        public void All_disposables_added_to_a_CompositeDisposable_are_disposed_when_it_is_disposed()
        {
            var oneWasDisposed = false;
            var twoWasDisposed = false;

            var disposables = new CompositeDisposable
            {
                () => oneWasDisposed = true,
                () => twoWasDisposed = true
            };

            disposables.Dispose();

            oneWasDisposed.Should().BeTrue();
            twoWasDisposed.Should().BeTrue();
        }

        [Fact]
        public void CompositeDisposable_can_safely_be_disposed_more_than_once_when_child_disposables_cannot()
        {
            var disposable = new CompositeDisposable
            {
                new DisposableThatThrowsOnRepeatDisposal(),
                new DisposableThatThrowsOnRepeatDisposal()
            };

            disposable.Dispose();
            disposable.Dispose();
        }

        [Fact]
        public void Disposables_added_to_CompositeDisposable_after_it_is_disposed_are_immediately_disposed()
        {
            var disposables = new CompositeDisposable();

            var wasDisposed = false;

            disposables.Dispose();

            disposables.Add(Disposable.Create(() => wasDisposed = true));

            wasDisposed.Should().BeTrue();
        }

        private class DisposableThatThrowsOnRepeatDisposal : IDisposable
        {
            private bool isDisposed = false;

            public void Dispose()
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("already disposed!");
                }
                isDisposed = true;
            }
        }
    }
}
