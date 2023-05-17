using System;
using System.Collections.Concurrent;

public class EventSafeQueue<T>
{
    private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

    public event Action<object, T> Enqueued;
    public event Action<object, T> Dequeued;
    public event Action<object, T> Enqueuing;

    public void Enqueue(T item)
    {
        OnEnqueuing(item);
        queue.Enqueue(item);
        OnEnqueued(item);
    }

    public bool TryDequeue(out T result)
    {
        bool success = queue.TryDequeue(out result);
        if (success)
        {
            OnDequeued(result);
        }
        return success;
    }

    public void Clear()
    {
        while (queue.Count > 0)
        {
            queue.TryDequeue(out T _);
        }
    }

    public int Count { get => queue.Count; }

    protected virtual void OnEnqueued(T item)
    {
        Enqueued?.Invoke(this, item);
    }

    protected virtual void OnDequeued(T item)
    {
        Dequeued?.Invoke(this, item);
    }

    protected virtual void OnEnqueuing(T item)
    {
        Enqueuing?.Invoke(this, item);
    }
}
