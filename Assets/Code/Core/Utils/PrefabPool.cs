using System;
using UnityEngine;
using UnityEngine.Pool;

public sealed class PrefabPool<T> : IDisposable where T : Component
{
    private readonly T _prefab;
    private readonly Transform _root;
    private readonly ObjectPool<T> _pool;
    private bool _disposed;

    public PrefabPool(T prefab, Transform owner, int initialCount, int maxSize)
    {
        if (initialCount < 0 || maxSize < 1 || initialCount > maxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCount));
        }

        _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));

        GameObject root = new($"{typeof(T).Name} Pool");
        root.transform.SetParent(owner, false);
        root.SetActive(false);
        _root = root.transform;

        _pool = new ObjectPool<T>(
            createFunc: Create,
            actionOnGet: null,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,
            defaultCapacity: initialCount,
            maxSize: maxSize
        );

        Prewarm(initialCount);
    }

    public T Get(Transform parent, Vector3 position, Quaternion rotation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PrefabPool<T>));
        }

        T instance = _pool.Get();
        instance.transform.SetParent(parent, false);
        instance.transform.localScale = _prefab.transform.localScale;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.gameObject.SetActive(true);
        return instance;
    }

    public void Release(T instance)
    {
        if (instance == null) return;

        if (_disposed)
        {
            UnityEngine.Object.Destroy(instance.gameObject); return;
        }

        _pool.Release(instance);
    }

    private T Create()
    {
        T instance = UnityEngine.Object.Instantiate(_prefab, _root);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnRelease(T instance)
    {
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(_root, false);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = _prefab.transform.localScale;
    }

    private void OnDestroyItem(T instance)
    {
        if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
    }

    private void Prewarm(int count)
    {
        using var _ = ListPool<T>.Get(out var list);

        for (int i = 0; i < count; i++)
        {
            list.Add(_pool.Get());
        }

        for (int i = 0; i < count; i++)
        {
            _pool.Release(list[i]);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _pool.Dispose();

        if (_root != null)
        {
            UnityEngine.Object.Destroy(_root.gameObject);
        }
    }
}
