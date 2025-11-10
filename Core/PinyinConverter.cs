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
	/// </summary>
	public static class PinyinConverter
	{
		// 缓存物品名称的拼音和首字母
		private static Dictionary<string, string> _pinyinCache;
		private static Dictionary<string, string> _initialsCache;
		private static bool _isInitialized = false;

		// 单字多音字映射 - 为常见多音字提供所有可能的读音
		// 这是一劳永逸的解决方案：只需维护单个多音字，系统会自动为所有包含该字的词生成拼音变体
		// 例如：添加 '匙' 后，"钥匙"、"汤匙"、"茶匙" 等所有包含"匙"的词都会自动支持多音字搜索
		private static readonly Dictionary<char, string[]> _multiPronunciationDict = new Dictionary<char, string[]>
		{
			{ '匙', new[] { "chi", "shi" } },      // 汤匙(chí) / 钥匙(shi)
			{ '钥', new[] { "yue", "yao" } },      // 锁钥(yuè) / 钥匙(yào)
			{ '重', new[] { "zhong", "chong" } },  // 重量(zhòng) / 重复(chóng)
			{ '长', new[] { "chang", "zhang" } },  // 长度(cháng) / 长大(zhǎng)
			{ '调', new[] { "tiao", "diao" } },    // 调整(tiáo) / 调料(diào)
			{ '角', new[] { "jiao", "jue" } },     // 角度(jiǎo) / 角色(jué)
			{ '传', new[] { "chuan", "zhuan" } },  // 传说(chuán) / 传记(zhuàn)
			{ '弹', new[] { "dan", "tan" } },      // 弹药(dàn) / 弹琴(tán)
			{ '血', new[] { "xue", "xie" } },      // 血液(xuè) / 血腥(xiě)
			// 发现新的多音字问题时，只需在此添加一行即可，格式：
			// { '字', new[] { "读音1", "读音2", "读音3" } },
		};

		/// <summary>
		/// 初始化拼音转换系统
		/// </summary>
		public static void Initialize()
		{
			if (_isInitialized)
				return;

			_pinyinCache = new Dictionary<string, string>();
			_initialsCache = new Dictionary<string, string>();

			// 预计算所有物品的拼音（延迟到首次使用时）
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
			_isInitialized = false;
		}

		/// <summary>
		/// 获取字符串的拼音（小写，无音调）
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

			// 使用 TinyPinyin 获取拼音（无分隔符，转小写）
			// TinyPinyin 返回大写拼音，我们需要转为小写
			string result = PinyinHelper.GetPinyin(text, "").ToLower();

			// 缓存结果
			if (_pinyinCache != null)
			{
				_pinyinCache[text] = result;
			}

			return result;
		}

		/// <summary>
		/// 获取字符串所有可能的拼音变体（用于处理多音字）
		/// </summary>
		/// <param name="text">输入文本</param>
		/// <returns>所有可能的拼音组合列表</returns>
		private static List<string> GetAllPinyinVariants(string text)
		{
			if (string.IsNullOrEmpty(text))
				return new List<string>();

			// 为每个字符获取所有可能的拼音
			List<List<string>> charPinyinList = new List<List<string>>();

			foreach (char c in text)
			{
				List<string> pinyins = new List<string>();

				// 如果是多音字，添加所有可能的读音
				if (_multiPronunciationDict.TryGetValue(c, out string[] variants))
				{
					pinyins.AddRange(variants);
				}

				// 添加 TinyPinyin 的默认读音
				string defaultPinyin = PinyinHelper.GetPinyin(c).ToLower();
				if (!pinyins.Contains(defaultPinyin))
				{
					pinyins.Add(defaultPinyin);
				}

				charPinyinList.Add(pinyins);
			}

			// 生成所有可能的组合
			return GeneratePinyinCombinations(charPinyinList);
		}

		/// <summary>
		/// 生成拼音组合（笛卡尔积）
		/// </summary>
		private static List<string> GeneratePinyinCombinations(List<List<string>> charPinyinList)
		{
			if (charPinyinList.Count == 0)
				return new List<string>();

			List<string> result = new List<string> { "" };

			foreach (var pinyins in charPinyinList)
			{
				List<string> newResult = new List<string>();
				foreach (var existing in result)
				{
					foreach (var pinyin in pinyins)
					{
						newResult.Add(existing + pinyin);
					}
				}
				result = newResult;
			}

			return result;
		}

		/// <summary>
		/// 获取字符串的拼音首字母
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

			// 使用 TinyPinyin 获取拼音首字母（无分隔符，转小写）
			// TinyPinyin 返回大写首字母，我们需要转为小写
			string result = PinyinHelper.GetPinyinInitials(text, "").ToLower();

			// 缓存结果
			if (_initialsCache != null)
			{
				_initialsCache[text] = result;
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

				// 获取默认拼音和首字母
				string pinyin = GetPinyin(itemName);
				string initials = GetInitials(itemName);

				// 1. 默认全拼匹配
				if (pinyin.Contains(search))
					return true;

				// 2. 默认首字母匹配
				if (initials.Contains(search))
					return true;

				// 3. 多音字变体匹配 - 检查是否包含多音字
				bool hasMultiPronunciation = false;
				foreach (char c in itemName)
				{
					if (_multiPronunciationDict.ContainsKey(c))
					{
						hasMultiPronunciation = true;
						break;
					}
				}

				// 如果包含多音字，尝试所有可能的拼音组合
				if (hasMultiPronunciation)
				{
					List<string> variants = GetAllPinyinVariants(itemName);
					foreach (string variant in variants)
					{
						// 检查全拼匹配
						if (variant.Contains(search))
							return true;

						// 检查首字母匹配
						string variantInitials = ExtractInitialsFromPinyin(variant);
						if (variantInitials.Contains(search))
							return true;
					}
				}

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

