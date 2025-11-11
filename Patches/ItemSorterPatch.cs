using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using MagicStoragePinyinHelper.Core;

namespace MagicStoragePinyinHelper.Patches
{
	/// <summary>
	/// Detour 补丁类 - 拦截并增强 MagicStorage 的搜索功能
	/// 使用 Detour 方式替代 IL 补丁，更稳定、更易维护
	/// </summary>
	public static class ItemSorterPatch
	{
		private static Hook _filterHook;
		private static bool _isApplied = false;

		/// <summary>
		/// 应用 Detour 补丁
		/// </summary>
		public static void Apply()
		{
			if (_isApplied)
				return;

			try
			{
				var logger = ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger;

				// 获取 MagicStorage.Sorting.ItemSorter 类型
				Type itemSorterType = GetItemSorterType();
				if (itemSorterType == null)
				{
					throw new Exception("无法找到 MagicStorage.Sorting.ItemSorter 类型！请确保 MagicStorage 已正确加载。");
				}

				// 获取 FilterBySearchText 方法
				MethodInfo targetMethod = itemSorterType.GetMethod(
					"FilterBySearchText",
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public
				);

				if (targetMethod == null)
				{
					throw new Exception("无法找到 FilterBySearchText 方法！MagicStorage 版本可能不兼容。");
				}

				logger?.Info($"找到目标方法: {targetMethod.Name}");
				logger?.Info($"方法签名: {targetMethod}");

				// 获取 Detour 目标方法
				MethodInfo detourMethod = typeof(ItemSorterPatch).GetMethod(
					nameof(FilterBySearchText_Detour),
					BindingFlags.NonPublic | BindingFlags.Static
				);

				if (detourMethod == null)
				{
					throw new Exception("无法找到 Detour 方法！这是内部错误。");
				}

				// 应用 Detour
				_filterHook = new Hook(targetMethod, detourMethod);
				_isApplied = true;

				logger?.Info("✓ 拼音搜索 Detour 补丁已成功应用！");
				logger?.Info("  优势: 比 IL 补丁更稳定，只要方法签名不变就不会失效");
			}
			catch (Exception ex)
			{
				ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger.Error($"应用拼音搜索补丁失败: {ex}");
				throw;
			}
		}

		/// <summary>
		/// 撤销 Detour 补丁
		/// </summary>
		public static void Undo()
		{
			if (_filterHook != null)
			{
				_filterHook.Dispose();
				_filterHook = null;
			}
			_isApplied = false;
		}

		/// <summary>
		/// 获取 ItemSorter 类型
		/// </summary>
		private static Type GetItemSorterType()
		{
			try
			{
				// 尝试从 MagicStorage 程序集中获取类型
				if (!ModLoader.TryGetMod("MagicStorage", out Mod magicStorageMod))
					return null;

				// 获取 MagicStorage 程序集
				Assembly magicStorageAssembly = magicStorageMod.Code;

				// 查找 ItemSorter 类型
				Type itemSorterType = magicStorageAssembly.GetType("MagicStorage.Sorting.ItemSorter");
				return itemSorterType;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Detour 方法 - 替换 FilterBySearchText 的实现
		///
		/// 原始方法签名:
		/// internal static bool FilterBySearchText(Item item, string filter, int modSearchIndex, bool modSearched = false)
		/// </summary>
		private static bool FilterBySearchText_Detour(
			Func<Item, string, int, bool, bool> orig,
			Item item,
			string filter,
			int modSearchIndex,
			bool modSearched)
		{
			// 调用原始方法获取默认搜索结果
			bool originalResult = orig(item, filter, modSearchIndex, modSearched);

			// 如果原始搜索已经匹配，直接返回 true（短路优化）
			if (originalResult)
				return true;

			// 原始搜索未匹配，尝试拼音匹配
			// 注意：只对物品名称进行拼音匹配，不影响 @ 和 # 前缀的特殊搜索
			try
			{
				// 检查是否是特殊搜索（@ 开头 = mod 搜索，# 开头 = tooltip 搜索）
				// 这些特殊搜索已经在原始方法中处理过了，这里只处理普通名称搜索
				if (string.IsNullOrWhiteSpace(filter))
					return false;

				string trimmedFilter = filter.Trim();

				// 如果是特殊搜索前缀，不进行拼音匹配（已经在原始方法中处理）
				if (trimmedFilter.StartsWith("@") || trimmedFilter.StartsWith("#"))
					return false;

				// 执行拼音匹配
				bool pinyinMatch = PinyinConverter.MatchesPinyin(item.Name, trimmedFilter);

				return pinyinMatch;
			}
			catch (Exception ex)
			{
				// 如果拼音匹配出现异常，记录日志但不影响游戏运行
				ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger.Error(
					$"拼音匹配时发生异常: {ex.Message}");
				return false;
			}
		}
	}
}

