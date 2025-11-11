using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
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

		// 分词最大词组长度（限制为 4 以提升性能）
		private const int MAX_PHRASE_LENGTH = 4;

		/// <summary>
		/// 初始化拼音转换系统
		/// 即使初始化失败也不会抛出异常，会使用降级模式
		/// </summary>
		public static void Initialize()
		{
			if (_isInitialized)
				return;

			try
			{
				// 初始化 LRU 缓存
				_pinyinCache = new LRUCache<string, string>(CACHE_CAPACITY);
				_initialsCache = new LRUCache<string, string>(CACHE_CAPACITY);

				// 加载词组拼音字典（内部已有异常处理）
				PhrasePinyinDict.Load();

				_isInitialized = true;
			}
			catch (Exception ex)
			{
				// 降级方案：即使初始化失败，也标记为已初始化
				// 这样可以继续使用 TinyPinyin 的基本功能
				var mod = ModContent.GetInstance<MagicStoragePinyinHelper>();
				mod?.Logger.Error($"拼音转换系统初始化失败: {ex.Message}");
				mod?.Logger.Warn("将使用降级模式：部分功能可能受限");

				_isInitialized = true;
			}
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
		/// 如果整个词组不在字典中，尝试分词匹配
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
				// 2. 尝试分词匹配（例如："暗影钥匙" -> "暗影" + "钥匙"）
				result = GetPinyinWithSegmentation(text);
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
		/// 如果整个词组不在字典中，尝试分词匹配
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

			// 1. 优先从词组字典查询拼音（带空格分隔），然后提取首字母
			if (PhrasePinyinDict.TryGetPinyinWithSpaces(text, out string phrasePinyinWithSpaces))
			{
				result = ExtractInitialsFromPinyinWithSpaces(phrasePinyinWithSpaces);
			}
			else
			{
				// 2. 尝试分词匹配，然后提取首字母
				string pinyinWithSpaces = GetPinyinWithSpacesWithSegmentation(text);
				result = ExtractInitialsFromPinyinWithSpaces(pinyinWithSpaces);
			}

			// 缓存结果
			if (_initialsCache != null)
			{
				_initialsCache.Set(text, result);
			}

			return result;
		}

		/// <summary>
		/// 获取首字母（从带空格分隔的拼音字符串中提取）
		/// 例如: "yao shi" -> "ys", "jin yao shi" -> "jys"
		/// </summary>
		private static string ExtractInitialsFromPinyinWithSpaces(string pinyinWithSpaces)
		{
			if (string.IsNullOrEmpty(pinyinWithSpaces))
				return string.Empty;

			StringBuilder result = new StringBuilder();

			// 按空格分割音节
			string[] syllables = pinyinWithSpaces.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			// 提取每个音节的首字母
			foreach (string syllable in syllables)
			{
				if (!string.IsNullOrEmpty(syllable))
				{
					result.Append(syllable[0]);
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// 使用分词匹配获取拼音（无空格）
		/// 尝试从文本中找到最长的词组匹配，然后组合拼音
		/// 例如："暗影钥匙" -> "anying" + "yaoshi" = "anyingyaoshi"
		/// 最大词组长度限制为 4，以提升性能
		/// </summary>
		private static string GetPinyinWithSegmentation(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			try
			{
				StringBuilder result = new StringBuilder();
				int i = 0;

				while (i < text.Length)
				{
					// 尝试从当前位置找最长的词组匹配
					int matchLength = 0;
					string matchedPinyin = null;

					// 从最长可能的词组开始尝试（最多尝试 MAX_PHRASE_LENGTH 个字符）
					int maxLen = Math.Min(MAX_PHRASE_LENGTH, text.Length - i);
					for (int len = maxLen; len >= 2; len--)
					{
						string substring = text.Substring(i, len);
						if (PhrasePinyinDict.TryGetPinyin(substring, out string pinyin))
						{
							matchLength = len;
							matchedPinyin = pinyin;
							break;
						}
					}

					// 如果找到匹配的词组
					if (matchLength > 0)
					{
						result.Append(matchedPinyin);
						i += matchLength;
					}
					else
					{
						// 没有找到匹配，使用 TinyPinyin 转换单个字符
						string singleChar = text.Substring(i, 1);
						string pinyin = PinyinHelper.GetPinyin(singleChar, "").ToLower();
						result.Append(pinyin);
						i++;
					}
				}

				return result.ToString();
			}
			catch (Exception)
			{
				// 降级：直接使用 TinyPinyin 转换整个文本
				return PinyinHelper.GetPinyin(text, "").ToLower();
			}
		}

		/// <summary>
		/// 使用分词匹配获取拼音（带空格分隔）
		/// 尝试从文本中找到最长的词组匹配，然后组合拼音
		/// 例如："暗影钥匙" -> "an ying yao shi"
		/// 最大词组长度限制为 4，以提升性能
		/// </summary>
		private static string GetPinyinWithSpacesWithSegmentation(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			try
			{
				StringBuilder result = new StringBuilder();
				int i = 0;

				while (i < text.Length)
				{
					// 尝试从当前位置找最长的词组匹配
					int matchLength = 0;
					string matchedPinyin = null;

					// 从最长可能的词组开始尝试（最多尝试 MAX_PHRASE_LENGTH 个字符）
					int maxLen = Math.Min(MAX_PHRASE_LENGTH, text.Length - i);
					for (int len = maxLen; len >= 2; len--)
					{
						string substring = text.Substring(i, len);
						if (PhrasePinyinDict.TryGetPinyinWithSpaces(substring, out string pinyin))
						{
							matchLength = len;
							matchedPinyin = pinyin;
							break;
						}
					}

					// 如果找到匹配的词组
					if (matchLength > 0)
					{
						if (result.Length > 0)
							result.Append(' ');
						result.Append(matchedPinyin);
						i += matchLength;
					}
					else
					{
						// 没有找到匹配，使用 TinyPinyin 转换单个字符
						string singleChar = text.Substring(i, 1);
						string pinyin = PinyinHelper.GetPinyin(singleChar, "").ToLower();
						if (result.Length > 0)
							result.Append(' ');
						result.Append(pinyin);
						i++;
					}
				}

				return result.ToString();
			}
			catch (Exception)
			{
				// 降级：直接使用 TinyPinyin 转换整个文本（带空格）
				return PinyinHelper.GetPinyin(text, " ").ToLower();
			}
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
				if (!string.IsNullOrEmpty(pinyin) && pinyin.Contains(search))
					return true;

				// 2. 首字母匹配
				if (!string.IsNullOrEmpty(initials) && initials.Contains(search))
					return true;

				return false;
			}
			catch (Exception ex)
			{
				// 如果出现任何异常，记录日志并返回 false 以避免破坏游戏
				var mod = ModContent.GetInstance<MagicStoragePinyinHelper>();
				mod?.Logger.Debug($"拼音匹配异常 (物品: {itemName}, 搜索: {searchText}): {ex.Message}");
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

