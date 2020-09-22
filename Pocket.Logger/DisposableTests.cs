using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            disposeAgain.Should().NotThrow();
        }

        [Fact]
        public async Task AnonymousDisposable_is_concurrency_safe_on_dispose()
        {
            var barrier = new Barrier(2);

            var disposeCount = 0;

            var disposable = Disposable.Create(() =>
            {
                Interlocked.Increment(ref disposeCount);

                barrier.SignalAndWait(100);
            });

            await Task.WhenAll(
                Enumerable.Range(1, 20).Select(async _ =>
                {
                    await Task.Run(() =>
                    {
                        disposable.Dispose();
                    });
                }));

            disposeCount.Should().Be(1);
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
        public void Disposables_added_to_a_CompositeDisposable_are_disposed_in_the_order_they_were_added()
        {
            var items = new List<int>();

            var disposable = new CompositeDisposable
            {
                () => items.Add(1),
                () => items.Add(2),
            };

            disposable.Add(() => items.Add(3));

            items.Should().BeInAscendingOrder();
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

        [Fact]
        public async Task CompositeDisposable_is_threadsafe_for_adding_disposables_while_being_disposed()
        {
            var oneWasDisposed = false;
            var twoWasDisposed = false;
            var barrier = new Barrier(2);

            var disposable = new CompositeDisposable
            {
                () => 
                {
                    barrier.SignalAndWait(100);
                    oneWasDisposed = true;
                }
            };

            var task1 = Task.Run(() => disposable.Dispose());

            var task2 = Task.Run(() => disposable.Add(() =>
            {
                barrier.SignalAndWait(100);
                twoWasDisposed = true;
            }));

            await Task.WhenAll(task1, task2);

            oneWasDisposed.Should().BeTrue();
            twoWasDisposed.Should().BeTrue();
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
