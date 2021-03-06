﻿using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.Basket.Base
{
    /// <summary>
    /// Extensions <see cref="IDistributedCache"/>.
    /// </summary>
    public static class DistributedCacheExtensions
	{
		private static readonly JsonSerializerOptions _serializerOptions = new()
        {
			IgnoreReadOnlyProperties = true
		};

		/// <summary>
		/// Gets value from specified cache with the specified <paramref name="key"/>
		/// and if it doesn't exist use <paramref name="valueFactory"/> to get it and cache it.
		/// Warning: This operation is not atomic!
		/// There may be situations when I find that there is nothing in the cache with that key and another
		/// thread or service puts a new value with same key. In this situation the last wins.
		/// </summary>
		/// <typeparam name="T">Type of value.</typeparam>
		/// <param name="distributedCache">The cache in which to store the data.</param>
		/// <param name="key">The key to get the stored data for.</param>
		/// <param name="valueFactory">Value factory for obtaining value if don't exist in cache.</param>
		/// <param name="options">The cache options for the entry.</param>
		/// <param name="token">Optional. A System.Threading.CancellationToken to cancel the operation.</param>
		/// <returns> A task that gets the value from the stored cache key.</returns>
		public static async Task<T> GetAndSetAsync<T>(
			this IDistributedCache distributedCache,
			string key,
			Func<Task<T>> valueFactory,
			DistributedCacheEntryOptions options,
			CancellationToken token = default)
		{
			T result = await distributedCache.GetAsync<T>(key, token);

			if (result == null)
			{
				result = await valueFactory();
				await distributedCache.SetAsync(key, result, options, token);
			}

			return result;
		}

		public static async Task<T> GetAsync<T>(
			this IDistributedCache distributedCache,
			string key,
			CancellationToken token = default)
		{
			string result = await distributedCache.GetStringAsync(key, token);

			return string.IsNullOrWhiteSpace(result) ? default : JsonSerializer.Deserialize<T>(result, _serializerOptions);
		}

		public static async Task SetAsync<T>(
			this IDistributedCache distributedCache,
			string key,
			T value,
			DistributedCacheEntryOptions options,
			CancellationToken token = default)
		   => await distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value, _serializerOptions), options, token);
	}
}
