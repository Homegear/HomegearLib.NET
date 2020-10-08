using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace HomegearLib
{
    internal class Worker<T>
    {
        public Worker(BlockingCollection<T> collection, Action<T> consumeAction, CancellationToken cancelationToken)
        {
            if (collection == null) { throw new ArgumentNullException(nameof(collection)); }
            if (consumeAction == null) { throw new ArgumentNullException(nameof(consumeAction)); }

            this.collection = collection;
            this.consumeAction = consumeAction;
            this.cancelationToken = cancelationToken;

            this.workerThread = new Thread(this.DoWork) { IsBackground = true };
        }

        public void StartWork()
        {
            this.workerThread.Start();
        }

        public void Shutdown()
        {
            this.workerThread.Join();
        }

        private void DoWork()
        {
            while (!this.cancelationToken.IsCancellationRequested)
            {
                try
                {
                    var item = this.collection.Take(this.cancelationToken); // Take should block, until an element was added.
                    this.consumeAction?.Invoke(item); // Invoke the given action with the dequeued item
                }
                catch (Exception)
                {
                }
            }
        }

        private readonly BlockingCollection<T> collection;

        private readonly Action<T> consumeAction;

        private readonly CancellationToken cancelationToken;

        private readonly Thread workerThread;
    }

    internal class Queue<T>
    {
        private readonly ConcurrentBag<Worker<T>> workers = new ConcurrentBag<Worker<T>>();
        private readonly BlockingCollection<T> queue = new BlockingCollection<T>();

        private readonly CancellationTokenSource cancelationSource;

        public Queue(uint numberOfWorkerThreads, Action<T> consumeAction)
        {
            if (numberOfWorkerThreads == 0) { throw new ArgumentException($"{nameof(numberOfWorkerThreads)} must be > 0"); }
            if (consumeAction == null) { throw new ArgumentNullException(nameof(consumeAction)); }

            this.cancelationSource = new CancellationTokenSource();

            for (var i = 0; i < numberOfWorkerThreads; i++)
            {
                var w = new Worker<T>(this.queue, consumeAction, this.cancelationSource.Token);

                this.workers.Add(w);
                w.StartWork();
            }
        }

        public void Enque(T item)
        {
            this.queue.Add(item);
        }

        public int Count()
        {
            return this.queue.Count;
        }

        public void Shutdown()
        {
            this.cancelationSource.Cancel();
            while (!this.workers.IsEmpty)
            {
                this.workers.TryTake(out Worker<T> w);
                w?.Shutdown();
            }
        }
    }
}
