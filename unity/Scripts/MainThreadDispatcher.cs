using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Allows background threads to schedule actions on Unity's main thread.
/// Attach to a GameObject in the scene alongside RobotController.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> queue = new Queue<Action>();
    private static MainThreadDispatcher instance;

    public static MainThreadDispatcher Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
                queue.Dequeue().Invoke();
        }
    }

    /// <summary>
    /// Enqueues an action to be called on the main thread on the next Update.
    /// </summary>
    public static void Enqueue(Action action)
    {
        lock (queue)
        {
            queue.Enqueue(action);
        }
    }
}
