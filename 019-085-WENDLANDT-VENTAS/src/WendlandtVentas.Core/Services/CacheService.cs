using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WendlandtVentas.Core.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }


        public void InvalidateOrderCache()
        {
            const string generalCacheKey = "OrderTableData";
            _memoryCache.Remove(generalCacheKey);
            InvalidateCacheByPrefix("OrderTableData");
        }

        public void RemoveProductsCache()
        {
            _memoryCache.Remove("ProductsInStock_MXN");
            _memoryCache.Remove("ProductsInStock_USD");
        }

        public void ClearProductCache()
        {
            // Borrar el caché para los productos
            _memoryCache.Remove("ProductTableData");
            InvalidateCacheByPrefix("ProductsInStock_");
            // También podemos borrar el cache por prefijo si tienes otros cachés con prefijos similares
            InvalidateCacheByPrefix("ProductTableData_");

           
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, TimeSpan? absoluteExpiration = null)
        {
            if (_memoryCache.TryGetValue(key, out T cacheEntry))
            {
                return cacheEntry;
            }

            cacheEntry = await getItemCallback();

            if (absoluteExpiration.HasValue)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(absoluteExpiration.Value);

                _memoryCache.Set(key, cacheEntry, cacheEntryOptions);
            }
            else
            {
                // ✅ Guardar sin expiración
                _memoryCache.Set(key, cacheEntry);
            }

            return cacheEntry;
        }


        private void InvalidateCacheByPrefix(string prefix)
        {
            var cacheEntries = _memoryCache.GetType()
                .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_memoryCache) as dynamic;

            if (cacheEntries == null) return;

            var keysToRemove = new List<object>();

            foreach (var cacheEntry in cacheEntries)
            {
                var key = cacheEntry.GetType().GetProperty("Key")?.GetValue(cacheEntry, null);
                if (key != null && key.ToString().StartsWith(prefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }
        }
    }
}
