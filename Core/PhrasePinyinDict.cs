using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria.ModLoader;

namespace MagicStoragePinyinHelper.Core
{
	/// <summary>
	/// 词组拼音字典 - 加载和查询词组的正确拼音
	/// 解决多音字问题（如"钥匙"读作 yào shi 而不是 yuè chí）
	/// </summary>
	public static class PhrasePinyinDict
	{
		// 词组 -> 拼音（无音调，无空格）
		private static Dictionary<string, string> _phraseDict;
		// 词组 -> 拼音（无音调，带空格分隔）- 用于提取首字母
		private static Dictionary<string, string> _phrasePinyinWithSpaces;
		private static bool _isLoaded = false;

		/// <summary>
		/// 加载词组拼音字典
		/// 如果加载失败，会初始化空字典并记录警告，不会抛出异常
		/// </summary>
		public static void Load()
		{
			if (_isLoaded)
				return;

			// 初始化空字典（降级方案）
			_phraseDict = new Dictionary<string, string>();
			_phrasePinyinWithSpaces = new Dictionary<string, string>();

			try
			{
				// 从 Mod 文件中加载
				var mod = ModContent.GetInstance<MagicStoragePinyinHelper>();
				string filePath = "Data/phrase_pinyin.txt";

				// 使用 Mod.GetFileBytes 加载文件
				byte[] fileBytes = mod.GetFileBytes(filePath);
				if (fileBytes == null || fileBytes.Length == 0)
				{
					throw new Exception($"无法加载文件: {filePath}");
				}

				using (MemoryStream memStream = new MemoryStream(fileBytes))
				using (StreamReader reader = new StreamReader(memStream, Encoding.UTF8))
				{
					string line;
					int lineCount = 0;
					while ((line = reader.ReadLine()) != null)
					{
						lineCount++;

						// 跳过注释和空行
						if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
							continue;

						// 移除行尾注释
						int commentIndex = line.IndexOf('#');
						if (commentIndex > 0)
						{
							line = line.Substring(0, commentIndex);
						}

						// 解析格式: "词组: 拼音"
						int colonIndex = line.IndexOf(':');
						if (colonIndex <= 0)
							continue;

						string phrase = line.Substring(0, colonIndex).Trim();
						string pinyin = line.Substring(colonIndex + 1).Trim();

						if (string.IsNullOrEmpty(phrase) || string.IsNullOrEmpty(pinyin))
							continue;

						// 移除音调并转小写
						string pinyinWithSpaces = RemoveTones(pinyin);

						// 移除空格，连接成一个字符串
						string pinyinNoSpaces = pinyinWithSpaces.Replace(" ", "");

						// 存储（如果有多个读音，保留第一个）
						if (!_phraseDict.ContainsKey(phrase))
						{
							_phraseDict[phrase] = pinyinNoSpaces;
							_phrasePinyinWithSpaces[phrase] = pinyinWithSpaces;
						}
					}

					mod.Logger.Info($"词组拼音字典加载完成，共 {_phraseDict.Count} 个词组");
				}

				_isLoaded = true;
			}
			catch (Exception ex)
			{
				// 降级方案：字典加载失败时，使用空字典
				// 这样仍可使用 TinyPinyin 进行基本的拼音转换
				var mod = ModContent.GetInstance<MagicStoragePinyinHelper>();
				mod?.Logger.Warn($"词组拼音字典加载失败: {ex.Message}");
				mod?.Logger.Warn("将使用降级模式：仅使用 TinyPinyin 进行拼音转换");
				mod?.Logger.Warn("多音字可能无法正确识别，但基本功能仍可使用");

				// 标记为已加载（使用空字典）
				_isLoaded = true;
			}
		}

		/// <summary>
		/// 卸载字典
		/// </summary>
		public static void Unload()
		{
			_phraseDict?.Clear();
			_phraseDict = null;
			_phrasePinyinWithSpaces?.Clear();
			_phrasePinyinWithSpaces = null;
			_isLoaded = false;
		}

		/// <summary>
		/// 查询词组的拼音
		/// </summary>
		/// <param name="phrase">词组</param>
		/// <param name="pinyin">拼音（无音调，无空格）</param>
		/// <returns>如果找到返回 true</returns>
		public static bool TryGetPinyin(string phrase, out string pinyin)
		{
			if (_phraseDict != null && _phraseDict.TryGetValue(phrase, out pinyin))
			{
				return true;
			}

			pinyin = null;
			return false;
		}

		/// <summary>
		/// 查询词组的拼音（带空格分隔）
		/// </summary>
		/// <param name="phrase">词组</param>
		/// <param name="pinyinWithSpaces">拼音（无音调，带空格分隔）</param>
		/// <returns>如果找到返回 true</returns>
		public static bool TryGetPinyinWithSpaces(string phrase, out string pinyinWithSpaces)
		{
			if (_phrasePinyinWithSpaces != null && _phrasePinyinWithSpaces.TryGetValue(phrase, out pinyinWithSpaces))
			{
				return true;
			}

			pinyinWithSpaces = null;
			return false;
		}

		/// <summary>
		/// 移除拼音中的音调
		/// </summary>
		private static string RemoveTones(string pinyinWithTones)
		{
			if (string.IsNullOrEmpty(pinyinWithTones))
				return string.Empty;

			// 音调字符映射表
			var toneMap = new Dictionary<char, char>
			{
				// a
				{'ā', 'a'}, {'á', 'a'}, {'ǎ', 'a'}, {'à', 'a'},
				// e
				{'ē', 'e'}, {'é', 'e'}, {'ě', 'e'}, {'è', 'e'},
				// i
				{'ī', 'i'}, {'í', 'i'}, {'ǐ', 'i'}, {'ì', 'i'},
				// o
				{'ō', 'o'}, {'ó', 'o'}, {'ǒ', 'o'}, {'ò', 'o'},
				// u
				{'ū', 'u'}, {'ú', 'u'}, {'ǔ', 'u'}, {'ù', 'u'},
				// ü
				{'ǖ', 'v'}, {'ǘ', 'v'}, {'ǚ', 'v'}, {'ǜ', 'v'}, {'ü', 'v'},
			};

			StringBuilder result = new StringBuilder(pinyinWithTones.Length);
			foreach (char c in pinyinWithTones)
			{
				if (toneMap.TryGetValue(c, out char replacement))
				{
					result.Append(replacement);
				}
				else
				{
					result.Append(char.ToLower(c));
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// 获取字典中的词组数量
		/// </summary>
		public static int Count => _phraseDict?.Count ?? 0;

		/// <summary>
		/// 是否已加载
		/// </summary>
		public static bool IsLoaded => _isLoaded;
	}
}

