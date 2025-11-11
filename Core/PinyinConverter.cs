using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.Localization;
using TinyPinyin;

namespace MagicStoragePinyinHelper.Core
{
	/// <summary>
	/// 拼音转换核心类 - 提供汉字到拼音的转换和匹配功能
	/// 使用词组字典优先查询，解决多音字问题（如"钥匙"）
	/// 使用 LRU 缓存限制内存使用
	/// </summary>
	public static class PinyinConverter
	{
		// LRU 缓存 - 限制内存使用（最多缓存 5000 个物品名称）
		private static LRUCache<string, string> _pinyinCache;
		private static LRUCache<string, string> _initialsCache;
		private static bool _isInitialized = false;

		// 缓存容量配置
		private const int CACHE_CAPACITY = 5000;

		/// <summary>
		/// 初始化拼音转换系统
		/// </summary>
		public static void Initialize()
		{
			if (_isInitialized)
				return;

			// 初始化 LRU 缓存
			_pinyinCache = new LRUCache<string, string>(CACHE_CAPACITY);
			_initialsCache = new LRUCache<string, string>(CACHE_CAPACITY);

			// 加载词组拼音字典
			PhrasePinyinDict.Load();

			_isInitialized = true;
		}

		/// <summary>
		/// 卸载并清理资源
		/// </summary>
		public static void Unload()
		{
			_pinyinCache?.Clear();
			_initialsCache?.Clear();
			_pinyinCache = null;
			_initialsCache = null;

			// 卸载词组字典
			PhrasePinyinDict.Unload();

			_isInitialized = false;
		}

		/// <summary>
		/// 获取字符串的拼音（小写，无音调）
		/// 优先从词组字典查询，解决多音字问题
		/// </summary>
		/// <param name="text">输入文本</param>
		/// <returns>拼音字符串</returns>
		public static string GetPinyin(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			// 检查缓存
			if (_pinyinCache != null && _pinyinCache.TryGetValue(text, out string cached))
				return cached;

			string result;

			// 1. 优先从词组字典查询（解决多音字问题）
			if (PhrasePinyinDict.TryGetPinyin(text, out string phrasePinyin))
			{
				result = phrasePinyin;
			}
			else
			{
				// 2. 使用 TinyPinyin 获取拼音（无分隔符，转小写）
				result = PinyinHelper.GetPinyin(text, "").ToLower();
			}

			// 缓存结果
			if (_pinyinCache != null)
			{
				_pinyinCache.Set(text, result);
			}

			return result;
		}



		/// <summary>
		/// 获取字符串的拼音首字母
		/// 优先从词组字典查询，解决多音字问题
		/// </summary>
		/// <param name="text">输入文本</param>
		/// <returns>首字母字符串</returns>
		public static string GetInitials(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			// 检查缓存
			if (_initialsCache != null && _initialsCache.TryGetValue(text, out string cached))
				return cached;

			string result;

			// 1. 优先从词组字典查询拼音，然后提取首字母
			if (PhrasePinyinDict.TryGetPinyin(text, out string phrasePinyin))
			{
				result = ExtractInitialsFromPinyin(phrasePinyin);
			}
			else
			{
				// 2. 使用 TinyPinyin 获取拼音首字母（无分隔符，转小写）
				result = PinyinHelper.GetPinyinInitials(text, "").ToLower();
			}

			// 缓存结果
			if (_initialsCache != null)
			{
				_initialsCache.Set(text, result);
			}

			return result;
		}

		/// <summary>
		/// 获取首字母（从拼音字符串中提取）
		/// 例如: "yaoshi" -> "ys", "jinyaoshi" -> "jys"
		/// </summary>
		private static string ExtractInitialsFromPinyin(string pinyin)
		{
			if (string.IsNullOrEmpty(pinyin))
				return string.Empty;

			StringBuilder result = new StringBuilder();
			result.Append(pinyin[0]); // 第一个字符一定是首字母

			// 查找每个音节的首字母（辅音后跟元音的位置）
			for (int i = 1; i < pinyin.Length; i++)
			{
				char current = pinyin[i];
				char previous = pinyin[i - 1];

				// 如果前一个是元音，当前是辅音，说明是新音节的开始
				if (IsVowel(previous) && !IsVowel(current))
				{
					result.Append(current);
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// 判断是否是元音
		/// </summary>
		private static bool IsVowel(char c)
		{
			return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'ü';
		}

		/// <summary>
		/// 检查搜索词是否匹配物品名称（拼音匹配）
		/// 使用词组字典优先查询，自动解决多音字问题
		/// </summary>
		/// <param name="itemName">物品名称</param>
		/// <param name="searchText">搜索文本</param>
		/// <returns>如果匹配返回 true</returns>
		public static bool MatchesPinyin(string itemName, string searchText)
		{
			if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(searchText))
			{
				return false;
			}

			try
			{
				string search = searchText.ToLower();

				// 获取拼音和首字母（已经通过词组字典解决了多音字问题）
				string pinyin = GetPinyin(itemName);
				string initials = GetInitials(itemName);

				// 1. 全拼匹配
				if (pinyin.Contains(search))
					return true;

				// 2. 首字母匹配
				if (initials.Contains(search))
					return true;

				return false;
			}
			catch (Exception)
			{
				// 如果出现任何异常，返回 false 以避免破坏游戏
				return false;
			}
		}

		/// <summary>
		/// 预热缓存 - 为所有已加载的物品预计算拼音
		/// </summary>
		public static void WarmupCache()
		{
			if (!_isInitialized)
				return;

			try
			{
				// 遍历所有物品类型
				for (int i = 0; i < Terraria.ID.ItemID.Count; i++)
				{
					Item item = new Item();
					item.SetDefaults(i);

					if (!string.IsNullOrEmpty(item.Name))
					{
						// 预计算拼音和首字母
						GetPinyin(item.Name);
						GetInitials(item.Name);
					}
				}
			}
			catch (Exception)
			{
				// 忽略预热过程中的错误
			}
		}
	}
}

