# Contributing

感谢参与 DiskFill。

## 开发环境

- Windows 10 或 Windows 11。
- Visual Studio 2022，安装 .NET 桌面开发工作负载。
- .NET Framework 4.8 targeting pack。

## 本地验证

```powershell
dotnet restore .\DiskFill.sln
dotnet build .\DiskFill.sln -c Release
dotnet test .\DiskFill.sln -c Release
```

提交前请在最小窗口 `1040 x 720` 下检查：

- 中文字体没有截断。
- 磁盘卡片没有横向滚动条。
- 右侧任务配置没有按钮或提示条重叠。
- 运行态、完成态和确认对话框均可正常操作。

## 代码约束

- 运行时项目保持零第三方依赖。
- 使用 C# 7.3，保持 `.NET Framework 4.8` 兼容。
- UI 线程不得执行磁盘 I/O。
- 修改写入、恢复、清理或路径处理时，必须增加或更新自动化测试。
- 不得使用真实机械硬盘作为日常单元测试目标。
- 禁止通过递归删除整个卷或未验证目录来清理任务。
- 新增失败分支应考虑任务清单不可写、磁盘已满和设备断开的情况。

## Pull Request

PR 应说明：

- 解决了什么问题。
- 对磁盘安全边界是否有影响。
- 如何测试。
- 是否改变任务清单 Schema；如改变，需要提供迁移或兼容策略。

界面改动请附上离屏预览截图：

```powershell
dotnet run --project .\tools\PreviewRenderer\PreviewRenderer.csproj -c Debug -- .\artifacts
```
