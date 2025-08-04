// Copyright (C) 2015-2025 The Neo Project.
//
// MethodResolutionCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.SmartContract.Framework.ContractInvocation.Caching
{
    /// <summary>
    /// Thread-safe cache for method resolution results.
    /// </summary>
    public sealed class MethodResolutionCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _defaultExpiration;
        private readonly int _maxCacheSize;
        private bool _disposed;

        /// <summary>
        /// Gets the current cache statistics.
        /// </summary>
        public CacheStatistics Statistics { get; private set; } = new();

        /// <summary>
        /// Initializes a new instance of the MethodResolutionCache class.
        /// </summary>
        /// <param name="defaultExpiration">Default cache entry expiration time</param>
        /// <param name="maxCacheSize">Maximum number of cache entries</param>
        /// <param name="cleanupInterval">Interval for cache cleanup</param>
        public MethodResolutionCache(
            TimeSpan? defaultExpiration = null,
            int maxCacheSize = 10000,
            TimeSpan? cleanupInterval = null)
        {
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(30);
            _maxCacheSize = maxCacheSize;

            var interval = cleanupInterval ?? TimeSpan.FromMinutes(5);
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, interval, interval);
        }

        /// <summary>
        /// Gets a cached method resolution result.
        /// </summary>
        /// <param name="contractReference">The contract reference</param>
        /// <param name="methodName">The method name</param>
        /// <param name="parameters">The method parameters</param>
        /// <param name="sourceType">The source contract type</param>
        /// <returns>The cached resolution result, or null if not found</returns>
        public MethodResolutionInfo? Get(
            IContractReference contractReference,
            string methodName,
            object?[]? parameters,
            Type? sourceType = null)
        {
            if (_disposed)
                return null;

            var key = GenerateCacheKey(contractReference, methodName, parameters, sourceType);

            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpirationTime)
                {
                    Interlocked.Increment(ref Statistics._cacheHits);
                    entry.LastAccessTime = DateTime.UtcNow;
                    return entry.Value;
                }
                else
                {
                    // Remove expired entry
                    _cache.TryRemove(key, out _);
                }
            }

            Interlocked.Increment(ref Statistics._cacheMisses);
            return null;
        }

        /// <summary>
        /// Stores a method resolution result in the cache.
        /// </summary>
        /// <param name="contractReference">The contract reference</param>
        /// <param name="methodName">The method name</param>
        /// <param name="parameters">The method parameters</param>
        /// <param name="resolution">The resolution result</param>
        /// <param name="sourceType">The source contract type</param>
        /// <param name="expiration">Custom expiration time</param>
        public void Set(
            IContractReference contractReference,
            string methodName,
            object?[]? parameters,
            MethodResolutionInfo resolution,
            Type? sourceType = null,
            TimeSpan? expiration = null)
        {
            if (_disposed)
                return;

            var key = GenerateCacheKey(contractReference, methodName, parameters, sourceType);
            var expirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration);

            var entry = new CacheEntry
            {
                Value = resolution,
                ExpirationTime = expirationTime,
                LastAccessTime = DateTime.UtcNow
            };

            _cache.AddOrUpdate(key, entry, (_, _) => entry);

            // Enforce cache size limit
            if (_cache.Count > _maxCacheSize)
            {
                Task.Run(async () => await EvictOldestEntriesAsync());
            }
        }

        /// <summary>
        /// Gets or sets a cached method resolution result.
        /// </summary>
        /// <param name="contractReference">The contract reference</param>
        /// <param name="methodName">The method name</param>
        /// <param name="parameters">The method parameters</param>
        /// <param name="factory">Factory function to create the value if not cached</param>
        /// <param name="sourceType">The source contract type</param>
        /// <param name="expiration">Custom expiration time</param>
        /// <returns>The resolution result</returns>
        public async Task<MethodResolutionInfo> GetOrSetAsync(
            IContractReference contractReference,
            string methodName,
            object?[]? parameters,
            Func<Task<MethodResolutionInfo>> factory,
            Type? sourceType = null,
            TimeSpan? expiration = null)
        {
            if (_disposed)
                return await factory();

            var cached = Get(contractReference, methodName, parameters, sourceType);
            if (cached != null)
                return cached;

            await _cacheLock.WaitAsync();
            try
            {
                // Double-check pattern
                cached = Get(contractReference, methodName, parameters, sourceType);
                if (cached != null)
                    return cached;

                var resolution = await factory();
                Set(contractReference, methodName, parameters, resolution, sourceType, expiration);
                return resolution;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;

            _cache.Clear();
            Statistics = new CacheStatistics();
        }

        /// <summary>
        /// Removes all entries for a specific contract.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        public void InvalidateContract(string contractIdentifier)
        {
            if (_disposed)
                return;

            var keysToRemove = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Key.StartsWith($"{contractIdentifier}:"))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Generates a cache key for the given parameters.
        /// </summary>
        private static string GenerateCacheKey(
            IContractReference contractReference,
            string methodName,
            object?[]? parameters,
            Type? sourceType)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(contractReference.Identifier);
            keyBuilder.Append(':');
            keyBuilder.Append(methodName);

            if (parameters != null && parameters.Length > 0)
            {
                keyBuilder.Append(':');
                foreach (var param in parameters)
                {
                    if (param == null)
                    {
                        keyBuilder.Append("null");
                    }
                    else
                    {
                        keyBuilder.Append(param.GetType().FullName);
                        keyBuilder.Append('=');
                        keyBuilder.Append(param.ToString());
                    }
                    keyBuilder.Append('|');
                }
            }

            if (sourceType != null)
            {
                keyBuilder.Append(':');
                keyBuilder.Append(sourceType.FullName);
            }

            // Hash the key to ensure consistent length and avoid key conflicts
            using var sha256 = SHA256.Create();
            var keyBytes = Encoding.UTF8.GetBytes(keyBuilder.ToString());
            var hashBytes = sha256.ComputeHash(keyBytes);
            return Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Cleans up expired cache entries.
        /// </summary>
        private void CleanupExpiredEntries(object? state)
        {
            if (_disposed)
                return;

            var now = DateTime.UtcNow;
            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (now > kvp.Value.ExpirationTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            Interlocked.Add(ref Statistics._expiredEntries, expiredKeys.Count);
        }

        /// <summary>
        /// Evicts the oldest cache entries when the cache size limit is exceeded.
        /// </summary>
        private async Task EvictOldestEntriesAsync()
        {
            if (_disposed)
                return;

            await _cacheLock.WaitAsync();
            try
            {
                if (_cache.Count <= _maxCacheSize)
                    return;

                var entriesToRemove = _cache.Count - (_maxCacheSize * 8 / 10); // Remove 20% when limit exceeded
                var sortedEntries = _cache.OrderBy(kvp => kvp.Value.LastAccessTime).Take(entriesToRemove);

                foreach (var entry in sortedEntries)
                {
                    _cache.TryRemove(entry.Key, out _);
                }

                Interlocked.Add(ref Statistics._evictedEntries, entriesToRemove);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Disposes the cache and its resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _cacheLock?.Dispose();
            _cache.Clear();
        }

        /// <summary>
        /// Represents a cached entry with expiration information.
        /// </summary>
        private sealed class CacheEntry
        {
            public MethodResolutionInfo Value { get; set; } = null!;
            public DateTime ExpirationTime { get; set; }
            public DateTime LastAccessTime { get; set; }
        }
    }

    /// <summary>
    /// Provides statistics about cache performance.
    /// </summary>
    public sealed class CacheStatistics
    {
        internal long _cacheHits;
        internal long _cacheMisses;
        internal long _expiredEntries;
        internal long _evictedEntries;

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        public long CacheHits => _cacheHits;

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        public long CacheMisses => _cacheMisses;

        /// <summary>
        /// Gets the number of expired entries that were removed.
        /// </summary>
        public long ExpiredEntries => _expiredEntries;

        /// <summary>
        /// Gets the number of entries that were evicted due to cache size limits.
        /// </summary>
        public long EvictedEntries => _evictedEntries;

        /// <summary>
        /// Gets the cache hit rate as a percentage.
        /// </summary>
        public double HitRate
        {
            get
            {
                var total = _cacheHits + _cacheMisses;
                return total > 0 ? (_cacheHits * 100.0) / total : 0;
            }
        }

        /// <summary>
        /// Gets the total number of cache requests.
        /// </summary>
        public long TotalRequests => _cacheHits + _cacheMisses;
    }
}
