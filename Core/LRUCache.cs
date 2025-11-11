using System;
using System.Collections.Generic;

namespace MagicStoragePinyinHelper.Core
{
	/// <summary>
	/// LRU (Least Recently Used) 缓存实现
	/// 用于限制缓存大小，防止内存无限增长
	/// </summary>
	/// <typeparam name="TKey">键类型</typeparam>
	/// <typeparam name="TValue">值类型</typeparam>
	public class LRUCache<TKey, TValue>
	{
		private readonly int _capacity;
		private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
		private readonly LinkedList<CacheItem> _lruList;

		/// <summary>
		/// 缓存项
		/// </summary>
		private class CacheItem
		{
			public TKey Key { get; set; }
			public TValue Value { get; set; }
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="capacity">缓存容量</param>
		public LRUCache(int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentException("容量必须大于0", nameof(capacity));

			_capacity = capacity;
			_cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
			_lruList = new LinkedList<CacheItem>();
		}

		/// <summary>
		/// 获取缓存项
		/// </summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (_cache.TryGetValue(key, out var node))
			{
				// 移动到链表头部（最近使用）
				_lruList.Remove(node);
				_lruList.AddFirst(node);
				value = node.Value.Value;
				return true;
			}

			value = default(TValue);
			return false;
		}

		/// <summary>
		/// 添加或更新缓存项
		/// </summary>
		public void Set(TKey key, TValue value)
		{
			if (_cache.TryGetValue(key, out var existingNode))
			{
				// 更新现有项
				existingNode.Value.Value = value;
				_lruList.Remove(existingNode);
				_lruList.AddFirst(existingNode);
			}
			else
			{
				// 添加新项
				if (_cache.Count >= _capacity)
				{
					// 移除最久未使用的项
					var lruNode = _lruList.Last;
					_lruList.RemoveLast();
					_cache.Remove(lruNode.Value.Key);
				}

				var newItem = new CacheItem { Key = key, Value = value };
				var newNode = new LinkedListNode<CacheItem>(newItem);
				_lruList.AddFirst(newNode);
				_cache[key] = newNode;
			}
		}

		/// <summary>
		/// 清空缓存
		/// </summary>
		public void Clear()
		{
			_cache.Clear();
			_lruList.Clear();
		}

		/// <summary>
		/// 获取当前缓存项数量
		/// </summary>
		public int Count => _cache.Count;

		/// <summary>
		/// 获取缓存容量
		/// </summary>
		public int Capacity => _capacity;
	}
}

