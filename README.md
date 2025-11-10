# Magic Storage 拼音搜索助手

> 为 [Magic Storage](https://github.com/blushiemagic/MagicStorage) 模组添加拼音搜索支持

## ✨ 功能特性

- ✅ **拼音全拼搜索** - 输入 `tukuai` 即可找到"土块"
- ✅ **拼音首字母搜索** - 输入 `tk` 也能找到"土块"
- ✅ **完全兼容** - 不影响原有的中文/英文搜索
- ✅ **零配置** - 安装即用，无需任何设置

## 📦 安装

1. 确保已安装 [tModLoader](https://github.com/tModLoader/tModLoader)
2. 确保已安装 [Magic Storage](https://github.com/blushiemagic/MagicStorage) 模组
3. 下载本模组并放入 `Mods` 文件夹
4. 在游戏中启用模组

## 🎮 使用方法

在 Magic Storage 的搜索框中直接输入拼音即可！

**示例：**
- 搜索 `tukuai` 或 `tk` → 找到"土块"
- 搜索 `muban` 或 `mb` → 找到"木板"
- 搜索 `tiejian` 或 `tj` → 找到"铁剑"

## 🛠️ 技术实现

- 使用 [TinyPinyin](https://github.com/promeG/TinyPinyin) 进行拼音转换
- 通过 MonoMod IL 补丁增强 Magic Storage 的搜索功能
- **智能多音字处理** - 自动识别并支持多音字的所有读音（如"钥匙"的"匙"字）
- 智能缓存机制，确保流畅的游戏体验

## 🔧 多音字支持

本模组采用智能多音字处理方案，无需手动维护庞大的词典：

- **自动组合** - 只需维护单个多音字的读音，系统自动生成所有可能的拼音组合
- **高效匹配** - 对于不包含多音字的物品使用快速路径，包含多音字时才进行变体匹配
- **易于扩展** - 发现新的多音字问题时，只需在代码中添加一行配置即可

已支持的常见多音字：
- 匙 (chí/shi) - 如"钥匙"
- 钥 (yuè/yào) - 如"钥匙"
- 重 (zhòng/chóng) - 如"重剑"
- 长 (cháng/zhǎng) - 如"长剑"
- 调 (tiáo/diào) - 如"调料"
- 角 (jiǎo/jué) - 如"角色"
- 传 (chuán/zhuàn) - 如"传送"
- 弹 (dàn/tán) - 如"弹药"
- 血 (xuè/xiě) - 如"血腥"

## 更新日志

### v0.2.0 (2025-11-10)
- ✨ 新增智能多音字处理系统
- 🔧 修复"钥匙"等多音字词无法正确搜索的问题
- 🚀 优化匹配算法，支持所有多音字的自动识别
- 📚 完善文档说明

### v0.1.0
- 🎉 初始版本发布
- ✅ 支持拼音全拼搜索
- ✅ 支持拼音首字母搜索

## �📄 开源协议

本项目采用 MIT 协议开源

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 👤 作者

**xixifu**

- GitHub: [@Sisyphean-a](https://github.com/Sisyphean-a)

---

⭐ 如果这个模组对你有帮助，欢迎给个 Star！

