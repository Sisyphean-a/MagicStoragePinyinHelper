using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Terraria;
using Terraria.ModLoader;
using MagicStoragePinyinHelper.Core;

namespace MagicStoragePinyinHelper.Patches
{
	/// <summary>
	/// IL 补丁类 - 拦截并增强 MagicStorage 的搜索功能
	/// </summary>
	public static class ItemSorterPatch
	{
		private static bool _isApplied = false;
		private static MethodInfo _targetMethod;

		/// <summary>
		/// 应用 IL 补丁
		/// </summary>
		public static void Apply()
		{
			if (_isApplied)
				return;

			try
			{
				// 获取 MagicStorage.Sorting.ItemSorter 类型
				Type itemSorterType = GetItemSorterType();
				if (itemSorterType == null)
				{
					throw new Exception("无法找到 MagicStorage.Sorting.ItemSorter 类型！请确保 MagicStorage 已正确加载。");
				}

				// 获取 FilterBySearchText 方法
				_targetMethod = itemSorterType.GetMethod(
					"FilterBySearchText",
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public
				);

				if (_targetMethod == null)
				{
					throw new Exception("无法找到 FilterBySearchText 方法！MagicStorage 版本可能不兼容。");
				}

				// 使用 MonoMod 的 Modify 方法应用 IL 编辑
				MonoModHooks.Modify(_targetMethod, ModifyFilterBySearchText);
				_isApplied = true;

				ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger.Info("拼音搜索补丁已成功应用！");
			}
			catch (Exception ex)
			{
				ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger.Error($"应用拼音搜索补丁失败: {ex}");
				throw;
			}
		}

		/// <summary>
		/// 撤销 IL 补丁（tModLoader 会自动管理 hook 的生命周期）
		/// </summary>
		public static void Undo()
		{
			// tModLoader 的 MonoModHooks 会在 Mod 卸载时自动清理所有 hooks
			// 不需要手动撤销
			_targetMethod = null;
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
		/// IL 编辑方法 - 修改 FilterBySearchText 的逻辑
		/// </summary>
		private static void ModifyFilterBySearchText(ILContext il)
		{
			var logger = ModContent.GetInstance<MagicStoragePinyinHelper>()?.Logger;
			try
			{
				logger?.Info("开始 IL 编辑...");
				// logger?.Info($"目标方法: {il.Method.FullName}");

				// 输出原始 IL 代码用于调试（仅在需要调试时取消注释）
				// logger?.Info("原始 IL 代码:");
				// logger?.Info(il.ToString());

				ILCursor cursor = new ILCursor(il);

				// 定位到 return item.Name.Contains(...) 的位置
				// 在 ItemSorter.cs 第 328 行
				// 从 IL 代码看，模式是: ... ldc.i4.5 -> callvirt Contains -> ret
				// 我们需要找到最后一个 ret 之前的 Contains 调用

				// 找到所有的 Contains(string, StringComparison) 调用
				var containsPositions = new List<int>();
				cursor.Index = 0;

				while (cursor.TryGotoNext(MoveType.Before,
					i => i.MatchLdcI4(5),  // StringComparison.OrdinalIgnoreCase
					i => i.MatchCallvirt<string>("Contains")
				))
				{
					containsPositions.Add(cursor.Index);
					cursor.Index++;  // 移动到下一个位置继续搜索
				}

				if (containsPositions.Count == 0)
				{
					logger?.Error("未找到任何 Contains 调用！");
					logger?.Error("请检查 MagicStorage 的 ItemSorter.FilterBySearchText 方法是否已更改。");
					throw new Exception("无法定位到 FilterBySearchText 的 Contains 调用！MagicStorage 代码结构可能已更改。");
				}

				// 使用最后一个 Contains 调用（应该是 return item.Name.Contains(filter, ...) 那一行）
				int targetIndex = containsPositions[containsPositions.Count - 1];
				cursor.Index = targetIndex;
				logger?.Info($"成功定位到目标 Contains 调用（共找到 {containsPositions.Count} 个 Contains 调用）");

				// 移动到 Contains 调用之后（跳过 ldc.i4.5, Contains）
				cursor.Index += 2;

				// 此时栈顶是 Contains 的返回值 (bool)
				// 我们需要实现: originalResult || PinyinConverter.MatchesPinyin(item.Name, filter)

				// 保存原始结果
				cursor.Emit(OpCodes.Dup);  // 复制栈顶的 bool 值

				// 创建标签用于短路求值
				ILLabel skipPinyinCheck = cursor.DefineLabel();

				// 如果原始结果为 true，跳过拼音检查
				cursor.Emit(OpCodes.Brtrue_S, skipPinyinCheck);

				// 原始结果为 false，执行拼音匹配
				cursor.Emit(OpCodes.Pop);  // 弹出栈顶的 false

				// 加载 item 和 filter 参数
				// 注意：由于编译器优化，我们需要从参数中获取
				cursor.Emit(OpCodes.Ldarg_0);  // 加载 item (第一个参数)
				cursor.Emit(OpCodes.Ldarg_1);  // 加载 filter (第二个参数，string)

				// 调用拼音匹配（注释掉频繁的日志输出）
				cursor.EmitDelegate<Func<Item, string, bool>>((item, filter) => {
					bool result = PinyinConverter.MatchesPinyin(item.Name, filter);
					// logger?.Debug($"拼音匹配: '{item.Name}' vs '{filter}' = {result}");
					return result;
				});

				// 标记跳过位置
				cursor.MarkLabel(skipPinyinCheck);

				logger?.Info("IL 编辑成功完成！");
				// 输出修改后的 IL 代码（仅在需要调试时取消注释）
				// logger?.Info("修改后的 IL 代码:");
				// logger?.Info(il.ToString());
			}
			catch (Exception ex)
			{
				logger?.Error($"IL 编辑失败: {ex}");
				logger?.Error($"堆栈跟踪: {ex.StackTrace}");
				throw;
			}
		}
	}
}

