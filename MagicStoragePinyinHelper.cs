using System;
using Terraria.ModLoader;
using MagicStoragePinyinHelper.Core;
using MagicStoragePinyinHelper.Patches;

namespace MagicStoragePinyinHelper
{
	/// <summary>
	/// Magic Storage 拼音搜索助手 - 主 Mod 类
	/// 为 MagicStorage 模组添加拼音搜索支持
	/// </summary>
	public class MagicStoragePinyinHelper : Mod
	{
		/// <summary>
		/// Mod 加载时调用
		/// </summary>
		public override void Load()
		{
			try
			{
				Logger.Info("正在加载 Magic Storage 拼音搜索助手...");

				// 1. 初始化拼音转换系统
				Logger.Info("初始化拼音转换系统...");
				PinyinConverter.Initialize();

				Logger.Info("Magic Storage 拼音搜索助手基础模块加载成功！");
			}
			catch (Exception ex)
			{
				Logger.Error($"加载 Magic Storage 拼音搜索助手时发生错误: {ex}");
				Logger.Error("拼音搜索功能可能无法正常工作。");
			}
		}

		/// <summary>
		/// Mod 卸载时调用
		/// </summary>
		public override void Unload()
		{
			try
			{
				Logger.Info("正在卸载 Magic Storage 拼音搜索助手...");

				// 1. 撤销 IL 补丁
				ItemSorterPatch.Undo();

				// 2. 清理拼音转换系统资源
				PinyinConverter.Unload();

				Logger.Info("Magic Storage 拼音搜索助手已成功卸载。");
			}
			catch (Exception ex)
			{
				Logger.Error($"卸载 Magic Storage 拼音搜索助手时发生错误: {ex}");
			}
		}

		/// <summary>
		/// 后加载处理 - 在所有 Mod 加载完成后调用
		/// </summary>
		public override void PostSetupContent()
		{
			try
			{
				// 应用 IL 补丁（必须在 PostSetupContent 阶段，确保 MagicStorage 已完全加载）
				Logger.Info("应用搜索功能补丁...");
				ItemSorterPatch.Apply();
				Logger.Info("搜索功能补丁应用成功！");

				// 预热缓存 - 为所有物品预计算拼音
				Logger.Info("预热拼音缓存...");
				PinyinConverter.WarmupCache();
				Logger.Info("拼音缓存预热完成！");

				Logger.Info("现在可以在 Magic Storage 搜索框中使用拼音搜索了！");
			}
			catch (Exception ex)
			{
				Logger.Error($"PostSetupContent 发生错误: {ex}");
				Logger.Error($"堆栈跟踪: {ex.StackTrace}");
				Logger.Error("拼音搜索功能可能无法正常工作。");
			}
		}
	}
}

