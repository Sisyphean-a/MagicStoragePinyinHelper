using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.Localization;

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

			StringBuilder sb = new StringBuilder();
			foreach (char ch in text)
			{
				if (PinyinData.CharToPinyin.TryGetValue(ch, out string pinyin))
				{
					sb.Append(pinyin);
				}
				else
				{
					// 非汉字字符直接添加（转小写）
					sb.Append(char.ToLower(ch));
				}
			}

			string result = sb.ToString();

			// 缓存结果
			if (_pinyinCache != null)
			{
				_pinyinCache[text] = result;
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

			StringBuilder sb = new StringBuilder();
			foreach (char ch in text)
			{
				char initial = PinyinData.GetInitial(ch);
				sb.Append(char.ToLower(initial));
			}

			string result = sb.ToString();

			// 缓存结果
			if (_initialsCache != null)
			{
				_initialsCache[text] = result;
			}

			return result;
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

				// 获取拼音和首字母
				string pinyin = GetPinyin(itemName);
				string initials = GetInitials(itemName);

				// 全拼匹配 或 首字母匹配
				bool pinyinMatch = pinyin.Contains(search);
				bool initialsMatch = initials.Contains(search);
				bool result = pinyinMatch || initialsMatch;

				// 调试日志（仅在需要调试时取消注释）
				// if (result)
				// {
				//     var logger = Terraria.ModLoader.ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger;
				//     logger?.Info($"✓ 拼音匹配成功: '{itemName}' (拼音:{pinyin}, 首字母:{initials}) 匹配 '{search}'");
				// }

				return result;
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

