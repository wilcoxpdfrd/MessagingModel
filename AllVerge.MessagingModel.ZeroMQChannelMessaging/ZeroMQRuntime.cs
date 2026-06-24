using AllVerge.SystemPrimitives.Threading.Tasks;
using NetMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    internal class ZeroMQRuntime
    {
        static Object lockObj = new object();
        static Task _runtime;
        static TaskCollection _runtimeTasks;
        private static TaskScheduler _runtimeTaskScheduler;

        public static void Start(CancellationToken cancellationToken)
        {
            if (_runtime == null)
            {
                lock (lockObj)
                {
                    if (_runtime == null)
                    {
                        TaskCompletionSource<VoidTaskResult> startedSource = new TaskCompletionSource<VoidTaskResult>();

                        _runtime = new Task(() =>
                        {
                            using (NetMQRuntime runtime = new NetMQRuntime())
                            {
                                _runtimeTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                                _runtimeTasks = new TaskCollection(cancellationToken);

                                startedSource.SetResult(new VoidTaskResult());

                                // runtime.Run blocks ...

                                runtime.Run(_runtimeTasks);
                            }
                        });

                        _runtime.Start();

                        startedSource.Task.GetAwaiter().GetResult();
                    }
                }
            }
        }

        internal static Task WaitUntilStoppedAsync()
        {
            TaskCompletionSource<VoidTaskResult> tcs = new TaskCompletionSource<VoidTaskResult>();

            _runtimeTasks.ContinueWith(t => { tcs.SetResult(new VoidTaskResult()); });

            return tcs.Task;
        }

        /// <summary>
        /// Runs <paramref name="action"/> as a task, adding it to the <see cref="ZeroMQRuntime"/> task collection, and using the runtimes internal task scheduler.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static Task Run(Action action, CancellationToken cancellationToken = default(CancellationToken))
        {
            Task task = new Task(action, cancellationToken);

            if (_runtimeTasks.TryAdd(task))
            {
                task.Start(_runtimeTaskScheduler);

                return task;
            }

            return Task.FromException(new InvalidOperationException("Could not start task."));
        }

        /// <summary>
        /// Runs <paramref name="action"/> as a task, adding it to the <see cref="ZeroMQRuntime"/> task collection, and using the runtimes internal task scheduler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        internal static Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken = default(CancellationToken))
        {
            Task<T> task = new Task<T>(action, cancellationToken);

            if (_runtimeTasks.TryAdd(task))
            {
                task.Start(_runtimeTaskScheduler);

                return task;
            }

            return (Task<T>)Task.FromException(new InvalidOperationException("Could not start task."));
        }
    }
}
