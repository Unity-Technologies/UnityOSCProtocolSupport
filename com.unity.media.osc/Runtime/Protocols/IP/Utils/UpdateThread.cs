using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A thread used to update a set of instances when requested.
    /// </summary>
    /// <typeparam name="T">The type of instance to update.</typeparam>
    class UpdateThread<T> where T : class
    {
        readonly string m_ThreadGroupName;
        readonly string m_ThreadName;
        readonly Action<T> m_UpdateCallback;

        readonly List<T> m_Instances = new List<T>();
        readonly object m_InstanceLock = new object();
        CancellationTokenSource m_ThreadCancellationSource;
        AutoResetEvent m_UpdateEvent;
        Thread m_Thread;

        /// <summary>
        /// Creates a new <see cref="UpdateThread{T}"/> instance.
        /// </summary>
        /// <param name="threadGroupName">The thread group to place the thread under in the profiler window.</param>
        /// <param name="threadName">The thread name in the profiler window.</param>
        /// <param name="updateCallback">The update callback to be executed from the thread.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="threadGroupName"/>, <paramref name="threadName"/>,
        /// or <paramref name="updateCallback"/> is <see langowrd="null"/>.</exception>
        public UpdateThread(string threadGroupName, string threadName, Action<T> updateCallback)
        {
            m_ThreadGroupName = threadGroupName ?? throw new ArgumentNullException(nameof(threadGroupName));
            m_ThreadName = threadName ?? throw new ArgumentNullException(nameof(threadName));
            m_UpdateCallback = updateCallback ?? throw new ArgumentNullException(nameof(updateCallback));
        }

        /// <summary>
        /// Start updating the specified instance from the thread.
        /// </summary>
        /// <param name="instance">The instance to start updating.</param>
        public void Add(T instance)
        {
            if (instance == null)
            {
                return;
            }

            lock (m_InstanceLock)
            {
                if (m_Instances.Contains(instance))
                {
                    return;
                }

                m_Instances.Add(instance);

                // start the thread once there are instances to update
                if (m_Thread == null)
                {
                    m_ThreadCancellationSource = new CancellationTokenSource();
                    m_UpdateEvent = new AutoResetEvent(false);

                    m_Thread = new Thread(() => UpdateLoop(m_ThreadCancellationSource.Token, m_UpdateEvent))
                    {
                        Name = nameof(T),
                        IsBackground = true,
                    };
                    m_Thread.Start();
                }
            }
        }

        /// <summary>
        /// Stop updating the specified instance from the thread.
        /// </summary>
        /// <param name="instance">The instance to stop updating.</param>
        public void Remove(T instance)
        {
            if (instance == null)
            {
                return;
            }

            lock (m_InstanceLock)
            {
                m_Instances.Remove(instance);

                // stop the update thread when there are no instance to update
                if (m_Thread != null && m_Instances.Count <= 0)
                {
                    m_ThreadCancellationSource?.Cancel();
                    m_UpdateEvent?.Set();

                    m_Thread.Join();
                    m_Thread = null;

                    m_ThreadCancellationSource.Dispose();
                    m_ThreadCancellationSource = null;

                    m_UpdateEvent.Dispose();
                    m_UpdateEvent = null;
                }
            }
        }

        /// <summary>
        /// Queues an update of the instances from the thread.
        /// </summary>
        public void QueueUpdate()
        {
            m_UpdateEvent?.Set();
        }

        void UpdateLoop(CancellationToken cancellationToken, AutoResetEvent updateEvent)
        {
            Profiler.BeginThreadProfiling(m_ThreadGroupName, m_ThreadName);

            try
            {
                var tempUpdateQueue = new List<T>();

                while (!cancellationToken.IsCancellationRequested)
                {
                    Profiler.BeginSample("Update");

                    try
                    {
                        // Iterate a copy of the instance list so instances can be added or remove from the
                        // update callback without a deadlock.
                        tempUpdateQueue.Clear();

                        lock (m_InstanceLock)
                        {
                            tempUpdateQueue.AddRange(m_Instances);
                        }

                        foreach (var instance in tempUpdateQueue)
                        {
                            m_UpdateCallback.Invoke(instance);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        Profiler.EndSample();
                    }

                    // Wait until the next update is triggered or the thread is being stopped. This avoids
                    // busy waiting, consuming CPU resources when there is nothing to do.
                    updateEvent.WaitOne();
                }
            }
            finally
            {
                Profiler.EndThreadProfiling();
            }
        }
    }
}
