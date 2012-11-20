using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace EasyWeb
{
    public class IOLoop
    {
        private ManualResetEvent m_ResetEvent = new ManualResetEvent(true);
        private ConcurrentQueue<Action> m_EventQueue = new ConcurrentQueue<Action>();

        private static IOLoop s_Instance = new IOLoop();
        public static IOLoop Instance { get { return s_Instance; } }

        private IOLoop() { }

        public void Run()
        {
            while (true)
            {
                m_ResetEvent.WaitOne();
                m_ResetEvent.Reset();

                // process all waiting events
                Action action;
                while (m_EventQueue.TryDequeue(out action))
                    action();
            }
        }

        public void PushEvent(Action action)
        {
            if (action != null)
            {
                m_EventQueue.Enqueue(action);
                m_ResetEvent.Set();
            }
        }
    }
}