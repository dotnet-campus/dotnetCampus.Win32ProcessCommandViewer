# dotnetCampus.Win32ProcessCommandViewer

用于输出进程的命令行的 dotnet 工具

## 安装

```
dotnet tool install -g dotnetCampus.Win32ProcessCommandViewer.Tool
```

## 使用方法

输出所有进程的命令行参数:

```
pscv
```

输出指定进程名的进程的命令行:

```
pscv -n [Process Name]
```

输出指定进程 Id 的进程的命令行:

```
pscv -i [Process Id]
```