# 单元测试补充与验证记录

日期：2026-11-22。沿用仓库现有 xUnit 2.9.3、Microsoft.NET.Test.Sdk 17.14.1、coverlet.collector 6.0.4 和 xunit.runner.visualstudio 3.1.4，不修改业务实现。

## 本轮新增 73 个测试用例

- FormulaEngine：25 个。覆盖输入校验、默认值与显式值优先级、中间表达式依赖顺序、输入元数据不匹配、变量冲突、重复输出、嵌套输出、别名展平、参数更新和表达式替换。
- Notification：24 个。覆盖无模板不可启用、模板关系去重和删除、状态转换、缓存刷新事件、skip/unskip、模板更新、收件人渠道资格和 JSON 示例生成。
- Frontend.Abstraction：16 个。覆盖 AutoValidator 的隐式必填、空 Guid、可选值、忽略字段、多个错误收集、邮件/电话校验，以及范围与长度的两端边界。
- Core.Infrastructure：8 个。覆盖 AES-GCM 空字符串和 Unicode 往返、随机 nonce、错误密钥、nonce/tag/ciphertext 篡改检测。

三个新测试项目已加入 `AlcedoFramework.slnx`；加密测试加入现有 Infrastructure 测试项目。所有新增测试都实际执行，无 Skip，不使用反射访问内部实现，也未为测试而修改生产代码的可见性。

## 实际验证结果

Windows x64，.NET SDK 10.0.404，Release。逐项目还原并运行 Tests 目录全部 8 个测试项目，369/369 通过，0 失败、0 跳过：

- Alcedo.Core.Application.Test：19。
- Alcedo.Core.Domain.Test：49。
- Alcedo.Core.Infrastructure.Test：68（含本轮新增 8 个）。
- Alcedo.Core.Presentation.Test：40。
- Alcedo.Frontend.Abstraction.Test：16（新项目）。
- Alcedo.Framework.FormulaEngine.Test：25（新项目）。
- Alcedo.Framework.Identity.Test：128。
- Alcedo.Framework.Notification.Test：24（新项目）。

已核验 solution 能识别新增项目，`git diff --check` 通过。构建仍有原业务代码已有的 nullable 等警告，本轮没有改动这些实现。没有执行整套示例应用的部署或浏览器端端到端验证。

## 重跑方式

在仓库根目录使用 .NET 10 SDK：

```powershell
dotnet test Tests/Core/Frontend/Alcedo.Frontend.Abstraction.Test/Alcedo.Frontend.Abstraction.Test.csproj -c Release
dotnet test Tests/Framework/Backend/Alcedo.Framework.FormulaEngine.Test/Alcedo.Framework.FormulaEngine.Test.csproj -c Release
dotnet test Tests/Framework/Backend/Alcedo.Framework.Notification.Test/Alcedo.Framework.Notification.Test.csproj -c Release
dotnet test Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/Alcedo.Core.Infrastructure.Test.csproj -c Release
```

验证全部测试项目（每个命令失败即停止）：

```powershell
Get-ChildItem Tests -Filter *.csproj -Recurse | ForEach-Object {
    dotnet test $_.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Test project failed: $($_.FullName)" }
}
```

本机从 nuget.org 下载部分依赖反复出现 unexpected EOF；最终通过 Clash Verge 的本地代理以及 Huawei NuGet 镜像完成还原。仅在验证进程中设置代理/证书吊销离线查询模式，没有修改仓库或用户的 NuGet 源配置。若默认源连接不稳，可先执行以下临时源还原，再以 `--no-restore` 运行测试：

```powershell
dotnet restore <test-project.csproj> --source https://repo.huaweicloud.com/repository/nuget/v3/index.json
dotnet test <test-project.csproj> -c Release --no-restore
```

## 仍需补充的验证

本轮保护已支持行为和明确的业务拒绝规则，没有把 CR 中的已知缺陷写成“预期错误行为”来获得通过。nullable 校验绕过、输出别名回填、并发登录、嵌套 UnitOfWork 等，应在单独修复中加入能证明修复有效的回归测试。

尚未覆盖真实 PostgreSQL 行锁/并发/Outbox、Redis 会话轮换、SMTP 发送、通知派发应用服务、Admin 调度及 Blazor 组件生命周期。现有 InMemory 测试不等同于关系数据库验证；本轮没有测量行/分支覆盖率，73 个新增用例不代表整个模块已充分覆盖。

CR 报告保留原始 2026-11-19 审查基线，并另记 SDK 安装后对 H02 的撤回；本文件记录后续测试执行，二者的验证时间和范围不同。
