using System;
using System.Collections.Concurrent;

using UnityEngine;

namespace Appfox.Unity.Extensions
{
    public class ThreadHelper : MonoBehaviour
    {
        private static ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
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
