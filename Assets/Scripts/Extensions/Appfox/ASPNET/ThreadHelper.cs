using System;
using System.Collections.Concurrent;

using UnityEngine;

namespace Appfox.Unity.Extensions
{
    public class ThreadHelper : MonoBehaviour
    {
        private static ThreadHelper instance;

        private static ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Update()
        {
            while (queue.TryDequeue(out var action))
            {
                action();
            }
        }

        public static void AddAction(Action action)
        {
            queue.Enqueue(action);
        }
    }
}
