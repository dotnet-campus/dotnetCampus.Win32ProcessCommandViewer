# dotnetCampus.Win32ProcessCommandViewer 　　　　　　　[中文](README_zh-CN.md)

| Build | NuGet |
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Win32ProcessCommandViewer/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Win32ProcessCommandViewer.svg)](https://www.nuget.org/packages/dotnetCampus.Win32ProcessCommandViewer.Tool)|

A dotnet tool to output the process commandline.

## Install

```
dotnet tool install -g dotnetCampus.Win32ProcessCommandViewer.Tool
```

## Usage

Output all processes commandline:

```
pscv
```

Output special process by process name:

```
pscv -n [Process Name]
```

Output special process by process id:

```
pscv -i [Process Id]
```