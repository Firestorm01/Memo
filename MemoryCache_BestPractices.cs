public class WaitToFinishMemoryCache<TItem>
{
    private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
 
    public async Task<TItem> GetOrCreate(object key, Func<Task<TItem>> createItem)
    {
        TItem cacheEntry;
 
        if (!_cache.TryGetValue(key, out cacheEntry))// Look for cache key.
        {
            SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
 
            await mylock.WaitAsync();
            try
            {
                if (!_cache.TryGetValue(key, out cacheEntry))
                {
                    // Key not in cache, so get data.
                    cacheEntry = await createItem();
                    _cache.Set(key, cacheEntry);
                }
            }
            finally
            {
                mylock.Release();
            }
        }
        return cacheEntry;
    }
}


// Usage
var _avatarCache = new WaitToFinishMemoryCache<byte[]>();
// ...
var myAvatar = 
 await _avatarCache.GetOrCreate(userId, async () => await _database.GetAvatar(userId));
