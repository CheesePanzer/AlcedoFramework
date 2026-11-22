# AlcedoFramework 第一轮代码审查

审查日期：2026-11-19（Asia/Shanghai）。基线：本地 `master`，commit `7d5820c25b313bc729147dcc2ce83e4e9b6cec45`。仓库：https://github.com/CheesePanzer/AlcedoFramework 。审查前工作区干净；本轮仅新增本报告，不修改源码、不提交、不创建 PR。

## 范围与验证口径

以本地工作副本为准，枚举全部 657 个 Git 跟踪文件，包含 500 个 `.cs`、69 个 `.razor`、33 个项目文件、40 个 Tests 目录文件，以及配置、工作流、示例、文档和静态资源。逐文件清单见附录。检查采用全文静态规则扫描，并对发现的问题人工阅读上下文和跨层调用链；清单中的“扫描”不代表每个文件都做了逐行人工深审。第三方压缩 JS/CSS 仅识别用途和接入边界，不声称审计了其第三方内部实现。本报告是第一轮发现清单，不是无缺陷证明或依赖漏洞扫描报告。

没有安装 SDK：`dotnet --list-sdks` 无输出，`dotnet --info` 显示只有 Runtime（含 10.0.7）。因此未运行 restore/build/test，没有安装 SDK，没有生成 bin/obj，也没有连接 PostgreSQL、Redis、RabbitMQ、SMTP 或 S3。以下除 SDK 配置诊断外均为代码可推导的问题，未声称已动态复现。未使用 EF InMemory 的结果代替真实 PostgreSQL 验证，未给出未经测量的测试覆盖率。

严重程度：**高**为权限/错误对象写入/主要功能阻断或关键会话一致性问题；**中**为特定场景失败、状态一致性、分层或重要测试缺口；**低**为诊断与维护问题。条目内路径相对仓库根，行号对应上述 commit。相同根因合并列出。

## 高

### H01 首次改密只由前端限制，默认 root 凭据可以直接调用业务 API

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Seeder/RootUserSeeder.cs:18`、`:22`、`:37`；`Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/User/UserCacheDto.cs:5`；`Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/AuthSessionMiddleware.cs:86`；`Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/AlcedoRbacHandler.cs:94`；`Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/AlcedoIdentityRouteView.razor:55`。
- **现象**：Seeder 创建已启用的 `admin/admin` root，并设置 `RequiresPasswordChange`。登录仍签发正常 JWT；后端会话检查和 RBAC 不检查这一标记，缓存 DTO 也没有该字段。改密导航只在浏览器中执行。
- **影响**：初始化后尚未改密的 root 可绕过页面直接访问所有业务 API；普通重置密码账号也能绕过强制改密。风险成立于示例按当前 Seeder 启动并对外可达时。
- **建议改法**：在服务端将首次登录约束为仅允许改密、退出和必要资料接口的受限会话；改密后重新认证。首次 root 密码使用外部配置或一次性随机凭据，避免固定密码。加入“未改密 JWT 调用普通业务 API 必须失败”的 HTTP 集成测试。

### H03 详情页只在 firstRender 加载，切换路由参数后可能向错误对象写入旧数据

- **位置**：`Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataDetailPageBase.cs:23`、`:33`、`:58`、`:83`；`Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Page/Formula/FormulaDetailPage.razor:215`、`:227`。
- **现象**：通用详情页只在 `OnAfterRenderAsync(firstRender)` 获取 DTO。Blazor 在同一组件类型的 A→B 路由参数导航中可复用实例，`Id` 更新但 DTO 仍是 A。编辑命令从旧 DTO 生成，提交却使用新的 `Id`。Formula 详情页存在同样的首次加载限制。
- **影响**：Users/Roles/Notification 详情可能显示 A、操作 B；当两个对象 Version 相同，旧字段可能覆盖 B，乐观并发检查无法识别对象身份错误。Formula 页也会展示与 URL 不一致的数据。
- **建议改法**：在 `OnParametersSetAsync` 按 Id 变化重新加载并清空旧状态，使用请求序号或取消令牌避免旧响应覆盖新结果；加载期间禁止编辑。提交时绑定 DTO 身份并校验 `Dto.Id == Id`。增加 A→B 同组件导航与编辑的组件测试。

### H04 并发登录的数据库 Session 与 Redis 缓存/RefreshToken 可以交错覆盖

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Auth/LoginServiceComponent.cs:224`、`:239`、`:243`、`:254`、`:260`；`Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/RefreshTokenStore.cs:34`。
- **现象**：先提交数据库的新 Session，再单独写 RefreshToken 和 UserCache；写 RefreshToken 时使用 `AddS` 共享锁，而且 UserCache 写入位于该锁释放之后。两个登录可以先后提交 S1/S2，再以相反顺序写 Redis，返回的 JWT、缓存 Session 与 RefreshToken 不再是一组。
- **影响**：并发登录会出现刚登录即 SessionInvalid，或数据库中最新会话与缓存认可会话不一致；Redis 双向 token 索引还可能残留一个已被后续登录替代的 token。
- **建议改法**：同一用户从数据库会话创建到 Redis 发布采用统一的互斥协调策略，至少使用同一把 `AddX` 锁覆盖完整流程；token 双索引用 Lua/事务原子更新，并校验 Session/version 防止旧写覆盖。测试应控制两次登录的交错顺序，而非只测单次成功。

## 中

### M01 嵌套 UnitOfWork 会改变外层事务的所有权并提前清空事务引用

- **位置**：`Src/Core/Backend/Alcedo.Core.Infrastructure/TransactionManager.cs:22`、`:26`、`:71`、`:78`、`:145`；`Src/Core/Backend/Alcedo.Core.Infrastructure/UnitOfWorkExecutor.cs:56`、`:141`、`:171`。
- **现象**：同一 scoped Executor 中嵌套 ExecuteAsync，第二次 Begin 发现 CurrentTransaction 后将 `_owningTransaction` 改为 false。内层 Commit 仍 SaveChanges，再把共享 `_transaction` 设 null；外层 Commit 随后报事务不存在。内层 finally 还清理共享事件集合。
- **影响**：组合应用服务时事务无法正确提交，内外层事件和追踪状态相互干扰。当前主要调用多为顺序 UoW；问题的明确触发条件是嵌套调用，不能把它描述为所有请求都会失败。
- **建议改法**：定义嵌套语义：使用深度计数且仅最外层提交/清理，或一开始明确拒绝嵌套；外部事务的参与者不能覆盖所有者状态。补充嵌套成功、内层失败、外部事务参与测试。

### M02 MFA 启用在确认用户成功绑定之前立即生效，禁用也没有二次认证

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Auth/AuthApplicationService.cs:125`、`:132`、`:138`；`Src/Framework/Backend/Alcedo.Framework.Identity/Controller/AuthController.cs:125`、`:134`。
- **现象**：enable 请求生成 Secret 后直接设置 `UseTwoFactorAuthenticator` 并返回 URI，尚未验证用户设备生成的 TOTP。再次 enable 还会直接替换 Secret。disable 仅依赖现有登录态，不要求密码或 TOTP。
- **影响**：关闭绑定对话框、扫码失败或重复点击可能让用户下次登录被锁在 MFA 外；持有会话者可关闭或重新绑定第二因素。
- **建议改法**：使用有期限的 pending setup，确认 TOTP 成功后原子启用；已启用时禁止直接覆盖，重新绑定/关闭要求近期密码或第二因素验证，并提供明确恢复流程。

### M03 pending TOTP 上下文读取和删除不是原子消费

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Auth/LoginServiceComponent.cs:85`、`:103`、`:105`；`Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/LoginContextStore.cs:22`。
- **现象**：先 GET pendingToken，验证后再 DELETE。两个并发请求可以同时取得上下文、通过同一个有效 TOTP，并分别执行 FinalizeLoginOnSuccess；删除结果没有参与判定。
- **影响**：一次性登录上下文可以重复使用，并放大 H04 的并发会话问题。
- **建议改法**：用原子 claim/consume 状态机，保证同一 pendingToken 只成功一次；将 TOTP 已接受时间步纳入重放防护，避免简单依赖非原子的读后删。

### M04 通知中一个不可用模板会取消同批其他有效通知

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageDispatch/MessageDispatchApplicationService.cs:141`、`:145`、`:152`、`:160`、`:174`。
- **现象**：循环遇到 template/sendEvent 为 null 时 `return` 整个方法；此前构建的有效 events 尚未发布。Sms/InApp 分支固定返回 null，还可能没有对应错误记录。
- **影响**：一个模板失效、收件人缺渠道信息或包含尚未实现的渠道，都会使其他正常邮件也不发送，消费者却正常结束，消息队列不会重试。
- **建议改法**：明确独立渠道或全有全无语义。独立渠道应记录单项失败并 continue，发布其余有效事件；全有全无则持久化整批失败并提供可靠重试。未支持的渠道在配置阶段拒绝或明确记录失败。

### M05 本机静态模板缓存没有过期，多实例更新只能刷新其中一个实例

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/TemplateMemoryCacheProvider.cs:14`、`:26`、`:41`、`:43`；`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Consumer/MessageTemplateCacheRefreshConsumer.cs:20`；`Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/MasstransitExtensions.cs:84`、`:145`。
- **现象**：缓存是进程内 static Dictionary，无 TTL；相同服务名称的消费实例共享队列，刷新事件是竞争消费，不会通知每个进程。数据库查不到时也只 return null，没有删除旧缓存。
- **影响**：扩容后部分实例可能无限期使用旧模板/启用状态；缓存持有与数据库不存在的数据时也无法自行修复。
- **建议改法**：给本地缓存增加有界 TTL/version 校验，或改为共享缓存；若广播失效则每个实例需独立订阅。不存在的记录必须驱逐已有缓存。

### M06 修改 MessageTypeCode 后旧缓存键不会失效

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Entity/MessageConfig.cs:44`；`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Provider/MessageConfigCacheProvider.cs:72`；`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Store/MessageConfigRedisStore.cs:29`。
- **现象**：配置允许变更 MessageTypeCode；事件只含 AggregateId。刷新从数据库读取新 DTO，只写 `dto.MessageTypeCode` 对应的新键，不删除旧 code 的键。
- **影响**：旧消息类型在 TTL 到期前仍可能使用已迁移配置发送通知（默认配置 120 分钟），与数据库实际配置不一致。
- **建议改法**：事件携带旧/新键，在一致性流程中失效旧键；或者将消息类型与配置关联设计为不可变。补充 A→B 变更后 A 不再命中旧 DTO 的测试。

### M07 Formula 默认值类型标识在生成端和执行端不一致

- **位置**：`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/ParamFlattener.cs:57`；`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Core.cs:48`、`:57`。
- **现象**：GetTypeName 用 CLR `Type.Name.ToLowerInvariant()`，`int?` 生成 `int32`，bool 生成 `boolean`；ResolveType 只识别 `int`/`int?` 和 `bool`/`bool?` 等别名。
- **影响**：例如可空 int 输入为 null 且配置了默认值时，执行报 Unsupported variable type，无法应用默认值。不是所有非空数字输入都会失败。
- **建议改法**：集中维护类型标识与转换表，或在元数据中保存实际 Type；为 nullable int/bool、decimal、DateTime 的默认值建立往返测试，转换使用明确 CultureInfo。

### M08 Formula 输出别名无法反向定位实际属性

- **位置**：`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/ParamFlattener.cs:27`；`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Core.cs:235`、`:241`、`:253`。
- **现象**：展平输出时支持 `ParamAttribute.VariableName`，构造输出时却直接把变量名当 CLR 属性名查找，并用下划线切分嵌套路径，没有保存变量到 PropertyInfo 路径的映射。
- **影响**：如属性 Total 标注别名 Result，公式可生成 Result 输出，但赋值找不到 Result 属性而失败；合法属性名本身包含下划线时也可能被误拆。
- **建议改法**：展平时构建可逆的变量→属性路径描述，初始化时验证唯一性，执行时按元数据赋值，不通过字符串拆分猜测路径。

### M09 Formula 展平递归缺少循环类型检测

- **位置**：`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/ParamFlattener.cs:21`、`:44`；`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Core.cs:101`、`:115`。
- **现象**：注册输入/输出类型时对所有非 primitive 属性递归，未限制深度或检测当前类型路径。Node.Child: Node 这样的合法 CLR 类型会无限递归；null 叶子补齐也有相同问题。
- **影响**：开发者一旦注册递归 DTO，初始化或执行可发生 StackOverflow，使进程终止，无法由普通业务异常机制恢复。触发点是注册类型，并非任意远程 JSON 都能触发。
- **建议改法**：使用当前递归路径的类型集合检测循环，明确支持类型和最大深度，在启动注册阶段给出可定位错误。

### M10 Formula 页忽略验证返回值，验证失败仍提交

- **位置**：`Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Page/Formula/FormulaDetailPage.razor:306`、`:308`、`:331`、`:333`、`:354`、`:356`；`Src/Core/Frontend/Alcedo.Frontend.Blazor/AutoValidatorService.cs:8`。
- **现象**：三处 await ValidateAsync 后没有判断 bool，就调用更新接口。
- **影响**：用户看完失败提示仍发出无效请求；若后端对应约束不足，无效配置可保存，否则产生额外错误和误导交互。
- **建议改法**：验证返回 false 时立即 return，并确保后端独立验证完整命令；加入验证失败时 API 调用次数为零的组件测试。

### M11 AutoValidator 会跳过所有有值的 nullable 非字符串属性

- **位置**：`Src/Core/Frontend/Alcedo.Frontend.Abstraction/Helpers/AutoValidator.cs:28`、`:31`、`:68`、`:86`。
- **现象**：`isNullable && (value is null || string.IsNullOrWhiteSpace(value as string))` 对任何非字符串有值对象，`value as string` 都为 null，因此直接 continue。nullable 属性的显式 Required 也在该提前返回之后。
- **影响**：`int?` 超范围值、nullable Guid 等自定义校验被跳过；nullable string 标记 Required 时空值也可能不报错。
- **建议改法**：先处理显式 Required，再区分“null 值可跳过”与“字符串空白可跳过”；非字符串有值对象必须进入 range/custom 检查。加入 `int? = -1` 配范围约束和 nullable string + Required 的测试。

### M12 EF Core 乐观并发异常被错误映射为 500

- **位置**：`Src/Core/Backend/Alcedo.Core.Presentation/AlcedoProblemDetailsFactory.cs:26`；`Src/Core/Backend/Alcedo.Core.Infrastructure/UnitOfWorkExecutor.cs:120`；`Src/Core/Backend/Alcedo.Core.Infrastructure/Configuration/AggregateRootConfigurationBase.cs:12`。
- **现象**：ProblemDetails 匹配的是 `System.Data.DBConcurrencyException`；EF Core 抛出的类型为 `DbUpdateConcurrencyException`，Executor 的 retry 分支使用的也是后者。
- **影响**：并发更新冲突在未重试或重试耗尽后返回 500，而非预期 409，前端无法准确提示刷新或重试；现有同名 DBConcurrencyException 测试不能证明 EF 路径正确。
- **建议改法**：明确映射 EF Core 并发异常到 409，并保留对其他类型的必要兼容；增加真实 Version 冲突的 HTTP 测试。

### M13 查询仓储自行 BeginTransaction，不能安全组合到已有 UnitOfWork

- **位置**：`Src/Core/Backend/Alcedo.Core.Infrastructure/QueryRepositoryBase/EntityQueryRepositoryBase.cs:40`；`Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/QueryRepository/MasterDataAuditLogQueryRepository.cs:30`。
- **现象**：分页查询每次无条件开启 RepeatableRead，没有检查同一 DbContext 的 CurrentTransaction。
- **影响**：应用服务在已开启 UoW 的写流程中组合一次分页查询，会因连接上已有事务而失败；查询基础设施隐式决定事务策略，也限制了应用层编排。
- **建议改法**：由应用层统一控制事务边界；需要 count/items 一致性时仅在没有现存事务时创建并拥有事务，在已有事务下复用并说明隔离保证。补充“UoW 内调用分页”的集成测试。

### M14 strict 幂等键生成和消费使用不同 token 格式

- **位置**：`Src/Core/Backend/Alcedo.Core.Infrastructure/RequestIdempotencyKeyFactory.cs:23`、`:26`、`:33`；`Src/Core/Backend/Alcedo.Core.Presentation/Filters/IdempotencyKeyFilter.cs:61`、`:76`、`:104`。
- **现象**：生成端 Redis 键为 `Strict:{endpoint}:{rawToken}`；Filter 将 Header 转为 `{userId或anon}:{rawToken}` 再交给消费端，实际删除的键多出用户前缀。
- **影响**：通过公开生成 API 产生的 strict token 无法通过正常 Filter 消费，一律被判 expired；目前仓库未见业务 action 实际标记 Strict，因此是框架能力接入时的确定性缺陷。
- **建议改法**：生成/校验统一传入用户和原始 token，在单一组件内部组装键；保留用户隔离，不能简单取消消费端绑定。补充匿名和已登录用户的生成→消费→重放测试。

### M15 Domain Service 直接依赖 Presentation 的端点注册表

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Service/RoleDomainService.cs:3`、`:15`、`:33`。
- **现象**：Domain Service 使用 `Alcedo.Core.Presentation.EndpointResourceOperationCenter` 校验权限，并抛普通 ArgumentException。
- **影响**：权限领域规则绑定 HTTP Controller 的反射描述；替换入口或做纯领域测试必须带入 Presentation，职责边界模糊；非法权限输入还会被当作 500。
- **建议改法**：在 Domain/应用契约中定义 permission catalog 抽象，Presentation 提供适配器，或将 HTTP endpoint 合法性验证移到应用层；领域只处理稳定权限标识，失败使用明确业务异常。

### M16 Quartz trigger 操作未检查 trigger 是否属于路径中的 job

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Admin/Application/QuartzAppService.cs:145`、`:165`、`:210`、`:251`。
- **现象**：方法分别检查 jobKey 和 triggerKey 存在，然后对 trigger 操作，没有验证该 trigger.JobKey 等于传入的 jobKey；UpdateCron 还以路径 jobKey 构建 replacement trigger。
- **影响**：请求可携带 A job 的路径却暂停/删除 B job 的 trigger，更新 Cron 时可能错误重关联。当前权限按操作划分，不把这一点夸大为已存在的 job 级越权；确定的问题是对象关联与接口语义错误。
- **建议改法**：读取 trigger 并验证所属 job，一致后再操作；更新时基于现有 trigger builder 保留时区、misfire 等配置，只修改 Cron。

### M17 示例在所有环境启用敏感异常详情

- **位置**：`Samples/Alcedo.Framework.Sample/back/Program.cs:26`；`Src/Core/Backend/Alcedo.Core.Presentation/AlcedoProblemDetailsFactory.cs:38`。
- **现象**：DisplaySensitiveExceptions 固定 true，未限定 Development；普通未处理异常的 Message 直接返回客户端。
- **影响**：按示例发布时，数据库错误、内部对象/配置细节可能通过响应泄露，且与框架默认的隐藏策略相反。
- **建议改法**：只在明确的开发环境打开，生产响应保留友好信息与 traceId，完整细节仅记录到服务端日志。

### M18 示例前后端地址和 CORS origin 不匹配

- **位置**：`Samples/Alcedo.Framework.Sample/front/wwwroot/appsettings.json:2`；`Samples/Alcedo.Framework.Sample/back/Properties/launchSettings.json:9`；`Samples/Alcedo.Framework.Sample/front/Properties/launchSettings.json:9`；`Samples/Alcedo.Framework.Sample/back/appsettings.json:33`。
- **现象**：前端默认访问 `http://localhost:5149`，后端 launch profile 为 50722/50723；前端 profile 为 50720/50721，后端允许 origins 却是 7041/5173。
- **影响**：不额外覆盖这些值时，按仓库默认 profile 启动的配对示例无法正常调用 API；修正端口后仍可能遇到 CORS 拒绝。
- **建议改法**：统一示例 profile、ApiBaseUrl 和允许 origins，并在 README 给出同一套可运行配置；检查 CookieSecureOnly 与实际 HTTPS 方案匹配。

### M19 前端 Docker 镜像没有配置 SPA history fallback

- **位置**：`Samples/Alcedo.Framework.Sample/front/Dockerfile:22`、`:23`。
- **现象**：仅向标准 nginx 镜像复制静态内容，仓库没有配套的 `try_files ... /index.html` 路由回退配置。
- **影响**：客户端导航到 `/identity/users` 等页面可以工作，但浏览器刷新或直接打开深层链接时 nginx 按文件路径查找并返回 404。成立于直接使用该镜像且上游未另设回退时。
- **建议改法**：提供 nginx location 配置，实现资源正常 404、页面路径回退 index.html；验证根路径和深层 URL 的直接访问与刷新。

### M20 关键持久化、登录成功路径和 Blazor 行为缺少相应测试

- **位置**：`Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Services/LoginServiceComponentTests.cs:63`；`Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/Alcedo.Core.Infrastructure.Test.csproj:22`；`Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/ReferenceIntegrityCompilerTests.cs:29`；待覆盖实现：`Src/Core/Backend/Alcedo.Core.Infrastructure/UnitOfWorkExecutor.cs:56`、`Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AggregateSaveChangesInterceptor.cs:33`。
- **现象**：LoginServiceComponentTests 仅含未找到用户、密码错误、账号锁定三种失败用例；Infrastructure 的引用编译测试使用 EF InMemory。当前 Tests 清单未见真实 PostgreSQL 事务/锁/SaveChangesInterceptor/Outbox 集成测试，也未见 Blazor、Notification、FormulaEngine 测试项目。
- **影响**：现有测试不能保护 H03/H04、M01、M04、M07、M12 等跨组件或数据库语义问题。并不意味着已有 Domain/RBAC/密码工具测试没有价值，也不能据此计算覆盖率百分比。
- **建议改法**：优先加入真实 PostgreSQL 的并发 Version、嵌套 UoW、Outbox 回滚测试；可控制时序的 Redis 登录/轮换测试；Blazor 路由复用与验证失败不提交测试；通知混合成功/失败模板和 Formula 类型往返测试。测试应断言业务结果而非重复实现细节。

## 低

### L01 泛型批量请求成功后仍走失败回调

- **位置**：`Src/Core/Frontend/Alcedo.Frontend.Blazor/AlcedoRequestService.cs:232`、`:235`、`:281`、`:293`。
- **现象**：`ExecuteAsync<T>(IReadOnlyCollection<Func<Task<T>>>)` 初始化 isSuccess=false；无异常分支只 NotifySuccess，没有设 true，然后 ExecuteAfterActions 根据 false 选择 OnFailure。
- **影响**：使用该重载的调用方会看到成功提示但不执行成功刷新，反而执行失败处理；返回结果本身可能正确。
- **建议改法**：全部成功时设置 isSuccess=true；明确部分失败策略，并验证成功/失败回调各自只执行一次。

### L02 MessageConfig 删除/清空模板记录了错误审计动作

- **位置**：`Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Entity/MessageConfig.cs:85`、`:93`。
- **现象**：BatchRemoveTemplates 和 ClearTemplates 都调用 BatchAssignTemplates，尽管已有独立 BatchDeleteTemplates/ClearTemplates 常量。
- **影响**：审计日志将删除/清空显示为分配，且未来状态 guard 对这些动作的不同限制不会生效。
- **建议改法**：使用各自的动作常量；断言事件/审计 OperationName 与实际操作一致。

### L03 预期业务失败全部记录为 Error

- **位置**：`Src/Core/Backend/Alcedo.Core.Presentation/Middlewares/AlcedoErrorExceptionHandler.cs:32`、`:34`。
- **现象**：400/401/403/404 等已知业务失败与未知 500 都无差别 LogError 并附异常堆栈。
- **影响**：常规校验失败和认证失败污染错误告警，真正基础设施故障更难筛选。
- **建议改法**：按异常分类与状态码选择日志等级，保留 traceId 和事件编号；未知 5xx 使用 Error，预期失败根据运维意图使用 Information/Warning。

### L04 PostgreSQL 异常翻译丢失原始异常链且把所有 QueryCanceled 当锁超时

- **位置**：`Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/PostgresExceptionsInterceptor.cs:36`、`:40`。
- **现象**：SQLSTATE QueryCanceled 统一转换为 InfraErrLockTimeout，但该状态也可来自 statement_timeout 或查询取消；新异常未保留 inner exception，默认分支 `throw pgEx` 还会重置抛出点堆栈。
- **影响**：客户端及上层重试策略可能误判原因，追踪原始 SQL 错误需额外关联底层日志。
- **建议改法**：区分取消/statement timeout/锁等待超时，保留 SQLSTATE 与 inner exception，未知异常不在该回调中重新抛出同一个实例。

### L05 全局 static 配置让多个 Host/测试实例互相覆盖

- **位置**：`Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoColumnEncryptor.cs:7`、`:9`、`:11`；`Src/Core/Backend/Alcedo.Core.Presentation/AlcedoProblemDetailsFactory.cs:10`；`Src/Framework/Backend/Alcedo.Framework.FormulaEngine/FormulaCenter/FormulaStoreCenter.cs:39`、`:45`、`:102`。
- **现象**：加密 key、敏感异常显示开关和 Formula 注册表存于 static；不同 Host 初始化会修改共享状态。FormulaStoreCenter 重复初始化还继续向已有 _metaData 添加，随后 ToDictionary 可能冲突。
- **影响**：单 Host 正常运行时不一定显现，但同进程多 Host、并行集成测试或重复初始化时会出现配置污染、解密失败或重复注册异常。
- **建议改法**：注册表/配置以 Host 为边界使用不可变 DI singleton；初始化构建完整新快照后替换；加密转换器捕获所属配置，避免运行时读取可变全局 key。

### L06 分页 PageNo 缺少正数范围约束

- **位置**：`Src/Core/Backend/Alcedo.Core.Application/SearchQuery/PaginationQuery.cs:7`；`Src/Core/Backend/Alcedo.Core.Infrastructure/QueryRepositoryBase/EntityQueryRepositoryBase.cs:53`。
- **现象**：PageSize 有 Range，PageNo 无约束；直接以 int 计算 `(PageNo-1)*PageSize`。
- **影响**：PageNo=0/负数造成无效 OFFSET，极大数乘法还可溢出，最终产生数据库错误而不是清晰的请求校验错误。
- **建议改法**：验证 PageNo>=1，在应用边界限制可接受范围，并使用 checked/long 计算和上界检查。

## 核验说明与未计为问题的事项

- 已复核 `UserAgg.ChangePassword`（`Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Entity/User.cs:124`）及 ResetPassword 均清空数据库 CurrentSession；不将“改密只删缓存、旧 JWT 可通过 DB 回填恢复”计为缺陷。缓存传播延迟与故障恢复仍需要集成测试，不能据此声称数据库会话未撤销。
- MassTransit 在 RabbitMQ 和 PostgreSQL 两条配置路径均使用 EF Bus Outbox；因此不将 UoW 中 Publish 位于 Commit 之前直接判为“消息先于数据库提交”的缺陷。
- 查询排序器对缺少 Id 的排序补充 ThenBy(Id)，因此未报告“分页没有稳定排序”。
- Repository 的 SQL 值使用 NpgsqlParameter，不将参数化值误报为 SQL 注入。自定义表/列标识符支持及具体数据库组合查询行为仍宜做 PostgreSQL 集成测试。
- 没有更改默认分支、源码、项目文件、配置、测试或工作流，没有创建 PR。未访问远端分支状态或宣称当前本地 commit 等于 GitHub 最新 master。

## 后续修复验证顺序（本轮不执行）

1. 优先处理 H01 的服务端首次改密限制与 root 初始化凭据；SDK 配置结论见下述复核更正。
2. 用可控并发测试固化 H03/H04、M01/M03，再修改状态与事务边界。
3. 覆盖通知混合模板、缓存旧键和 Formula 类型/别名的回归场景。
4. 最后核验示例端口、CORS、Docker 深层链接，以及 409/500 日志和响应契约。

## 2026-11-22 SDK 安装后的复核更正

经用户授权，已在本次工作的工具目录安装 .NET SDK 10.0.404。此前只装 Runtime 的 `dotnet --info` 提示 `global.json` 中 `10.0.0` 不是有效 feature-band 版本；但安装 SDK 后，保留原文件并从仓库目录执行 `dotnet --version` 实际成功返回 10.0.404。因此撤回原 H02“阻断常规构建入口”的高严重度结论，不能将仅 Runtime 环境的提示推导为安装 SDK 后 CI 必然失败。H 编号保留空缺以便追踪，当前有效发现为高 3、中 20、低 6，共 29 条。

本报告前文所述“未运行测试”描述的是 2026-11-19 的第一轮审查；2026-11-22 新增测试的执行情况另见 `Tests/TESTING.md`，不将后续验证追溯为首轮已运行。
## 附录：逐文件扫描清单

以下逐项记录跟踪文件的路径、行数和静态扫描关注项。规则命中只用于决定关注方向，不是独立缺陷；无规则命中也不代表无问题。正文条目是已人工核对证据的问题清单。第三方压缩资源不作内部实现审计；非代码文件关注配置、构建、示例接入和文档范围。本轮不把范围清单伪装成 657 份逐行人工审查结论。
- `.dockerignore`（28 行）— 配置/构建/文档/资源扫描。
- `.github/workflows/build.yml`（30 行）— 配置/构建/文档/资源扫描。
- `.github/workflows/publish-nuget.yml`（52 行）— 配置/构建/文档/资源扫描。
- `.gitignore`（28 行）— 配置/构建/文档/资源扫描。
- `AlcedoFramework.slnx`（63 行）— 配置/构建/文档/资源扫描。
- `Directory.Build.props`（29 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/00-Alcedo.Core-Overview.md`（124 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/00a-Alcedo.Core-Startup-Assembly-Scanning.md`（133 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/01-Alcedo.Core.Abstractions.md`（404 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02-Alcedo.Core.Infrastructure-Overview.md`（159 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02a-Core.Infrastructure-EFCore.md`（462 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02b-Core.Infrastructure-Repository.md`（412 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02c-Core.Infrastructure-Transaction-Events.md`（280 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02d-Core.Infrastructure-Advanced.md`（276 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/02e-Core.Infrastructure-Misc.md`（236 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/03-Alcedo.Core.Domain.md`（257 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/03a-Core.Domain-StateMachine.md`（340 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/04-Alcedo.Core.Application.md`（391 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/05-Alcedo.Core.Presentation.md`（287 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Backend/06-Alcedo.Core.Host.md`（249 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Frontend/00-Alcedo.Frontend-Overview.md`（158 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Frontend/01-Alcedo.Frontend.Abstraction.md`（762 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Frontend/02-Alcedo.Frontend.Core.md`（477 行）— 配置/构建/文档/资源扫描。
- `Docs/Core/Frontend/03-Alcedo.Frontend.Blazor.md`（801 行）— 配置/构建/文档/资源扫描。
- `Docs/Framework/Backend/10-Alcedo.Framework.Shared.md`（535 行）— 配置/构建/文档/资源扫描。
- `Docs/Framework/Backend/11-Alcedo.Framework.Identity.md`（277 行）— 配置/构建/文档/资源扫描。
- `LICENSE`（21 行）— 配置/构建/文档/资源扫描。
- `README.md`（69 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj`（35 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/back/Dockerfile`（40 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/back/Program.cs`（45 行）— 源码静态扫描：声明/展示/接口。
- `Samples/Alcedo.Framework.Sample/back/Properties/launchSettings.json`（12 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/back/appsettings.json`（128 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/front/Alcedo.Frontend.Sample.csproj`（32 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/front/App.razor`（19 行）— 源码静态扫描：声明/展示/接口。
- `Samples/Alcedo.Framework.Sample/front/Dockerfile`（24 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/front/Pages/Home.razor`（63 行）— 源码静态扫描：状态/异步。
- `Samples/Alcedo.Framework.Sample/front/Program.cs`（31 行）— 源码静态扫描：认证/缓存。
- `Samples/Alcedo.Framework.Sample/front/Properties/launchSettings.json`（12 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/front/_Imports.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Samples/Alcedo.Framework.Sample/front/wwwroot/appsettings.json`（3 行）— 配置/构建/文档/资源扫描。
- `Samples/Alcedo.Framework.Sample/front/wwwroot/index.html`（69 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Abstractions/Alcedo.Core.Abstractions.csproj`（28 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Abstractions/AlcedoExceptionBase.cs`（109 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/AlcedoModulesRegistry.cs`（64 行）— 源码静态扫描：异常/日志。
- `Src/Core/Backend/Alcedo.Core.Abstractions/AlcedoStorageLengthConvention.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/AppSettingsBase.cs`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/ContextUser.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/EventBase.cs`（21 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/IAlcedoCoreComponent.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/IAlcedoModule.cs`（76 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Abstractions/ServiceRegister.cs`（106 行）— 源码静态扫描：异常/日志。
- `Src/Core/Backend/Alcedo.Core.Abstractions/ValueAtom.cs`（40 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/Alcedo.Core.Application.csproj`（24 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Application/AlcedoCoreApplicationComponent.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/AppException/AppExceptions.cs`（32 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/ApplicationEventBase.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/Component/AggregateRootSearchServiceComponent.cs`（19 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Application/Component/EntitySearchServiceComponent.cs`（38 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/Dto/AggregateRootDtoBase.cs`（10 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Application/Dto/EntityCacheDto.cs`（6 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/Dto/EntityDtoBase.cs`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/EntityCacheProvider.cs`（64 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/IEntityCacheStore.cs`（11 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/IQueryRepository/IEntityQueryRepository.cs`（34 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/PagedResult.cs`（25 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/ProgramExtensions.cs`（28 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Application/SearchQuery/Attributes.cs`（61 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/SearchQuery/PaginationQuery.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/SearchQuery/SearchQueryBase.cs`（23 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Application/SearchQuery/SortQuery.cs`（21 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Application/Tools/ApplicationLockProvider.cs`（239 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/Tools/IIdempotencyKeyProvider.cs`（12 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/Tools/IUnitOfWorkExecutor.cs`（52 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Application/UploadedFileDto.cs`（35 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Domain.Kit.Test/AggregateRootTestExtensions.cs`（47 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain.Kit.Test/Alcedo.Core.Domain.Kit.Test.csproj`（13 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Domain/AggregateRootMetadataCenter.cs`（184 行）— 源码静态扫描：异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/Alcedo.Core.Domain.csproj`（24 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Domain/AlcedoCoreDomainComponent.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Domain/Attributes/DomainAttribute.cs`（21 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/DomException/Exceptions.cs`（200 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Domain/Entity/AggregateRoot.cs`（79 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/Entity/EntityBase.cs`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Domain/Event/DomainEventBase.cs`（6 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/Event/EntityCacheRefreshEvent.cs`（4 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/Helper/TokenHasher.cs`（16 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/IRepositoryBase/IAggregateRepository.cs`（6 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/IRepositoryBase/IEntityStoreBase.cs`（124 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/IStorageService/AggregateObjectNameBuilder.cs`（20 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/IStorageService/IFileStorageService.cs`（29 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/IStorageService/IStorageBucket.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Domain/IStorageService/IStorageKeyCache.cs`（40 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/ProgramExtension.cs`（18 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/ReferenceIntegrity/AggRefRelationBuilder.cs`（304 行）— 源码静态扫描：异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/ReferenceIntegrity/IAggRefChecker.cs`（28 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Domain/ReferenceIntegrity/IAggRefScaner.cs`（12 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Domain/StateMachine.cs`（94 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Host/Alcedo.Core.Host.csproj`（30 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Host/AlcedoApplicationInfo.cs`（62 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Host/AlcedoCoreOptions.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Host/AlcedoCoreProgram.cs`（263 行）— 源码静态扫描：EF/事务、异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Host/CorsPolicyInjection.cs`（75 行）— 源码静态扫描：异常/日志。
- `Src/Core/Backend/Alcedo.Core.Host/EnumDisplayStringTransformer.cs`（18 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Host/HealthCheck.cs`（28 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Host/IdempotencyKeyTransformer.cs`（53 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Host/NumberTypeSchemaTransformer.cs`（32 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Host/OpenApiGen.cs`（67 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Host/ValueAtomTransformer.cs`（67 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Alcedo.Core.Infrastructure.csproj`（42 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/AlcedoCoreInfrastructureComponent.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/AppSettings.cs`（106 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Configuration/AggregateRootConfigurationBase.cs`（22 行）— 源码静态扫描：EF/事务、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Configuration/EntityConfigurationBase.cs`（132 行）— 源码静态扫描：EF/事务。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Configuration/EntityConfigurationExtensions.cs`（286 行）— 源码静态扫描：EF/事务、异常/日志。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Consumer/EntityCacheRefreshConsumer.cs`（23 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Dapper/JsonTypeHandler.cs`（23 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/DistributedLockFactory.cs`（40 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AesGcmEncryption.cs`（48 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AggregateSaveChangesInterceptor.cs`（247 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoAggregateRootGraphCenter.cs`（172 行）— 源码静态扫描：EF/事务、异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoColumnEncryptor.cs`（14 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoDbContext.cs`（32 行）— 源码静态扫描：EF/事务、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoModelCustomizer.cs`（61 行）— 源码静态扫描：EF/事务。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/AlcedoOptionsExtension.cs`（46 行）— 源码静态扫描：EF/事务。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/DbContextExtensions.cs`（37 行）— 源码静态扫描：EF/事务、异常/日志。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EfCore/PostgresExceptionsInterceptor.cs`（46 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/EventsCollector.cs`（56 行）— 源码静态扫描：EF/事务、异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/IAlcedoReceiveEndpointConfiguration.cs`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/InfraException/Exceptions.cs`（95 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Job/Attributes.cs`（29 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Job/BaseJob.cs`（51 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Job/Registry.cs`（85 行）— 源码静态扫描：异常/日志。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions.cs`（85 行）— 源码静态扫描：EF/事务、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/AppSettingsRegister.cs`（52 行）— 源码静态扫描：异常/日志。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/EfCoreExtensions.cs`（70 行）— 源码静态扫描：EF/事务、异常/日志。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/MasstransitExtensions.cs`（151 行）— 源码静态扫描：EF/事务、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/RedisExtensions.cs`（85 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ProgramExtensions/StorageExtensions.cs`（39 行）— 源码静态扫描：EF/事务、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/QueryRepositoryBase/EntityQueryRepositoryBase.cs`（112 行）— 源码静态扫描：EF/事务、状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/EntityRedisStoreBase.cs`（35 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/KeySpace/DistributedLockRedisKeySpace.cs`（14 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/KeySpace/IdempotencyKeyRedisKeySpace.cs`（13 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/RedisKeySpace.cs`（30 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/RedisStoreBase.cs`（124 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Redis/RedisvalueExtension.cs`（16 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/AggRefChecker.cs`（132 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/AggRefScanner.cs`（25 行）— 源码静态扫描：异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/ReferenceIntegrityCompiler.cs`（170 行）— 源码静态扫描：EF/事务、状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/ReferenceIntegrityConfigCenter.cs`（63 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/ReferenceMetadataFactory.cs`（233 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ReferenceIntegrityImpl/Registry.cs`（28 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/RepositoryBase/AggregateRepositoryBase.cs`（14 行）— 源码静态扫描：EF/事务、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/RepositoryBase/EntityStoreBase.cs`（213 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/RepositoryBase/PessimisticLockQueryBuilder.cs`（136 行）— 源码静态扫描：EF/事务。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/RequestIdempotencyKeyFactory.cs`（65 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/SearchQuery/SearchQueryBuilder.cs`（264 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/SearchQuery/SearchQueryMetaCache.cs`（173 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/EntityTypeBuilderExtensions.cs`（37 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/IStorageGcExecutor.cs`（65 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/S3StorageService.cs`（107 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/StorageGcInterceptor.cs`（72 行）— 源码静态扫描：EF/事务、状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/StorageGcTask.cs`（36 行）— 源码静态扫描：EF/事务、状态/异步。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/Storage/StorageKeyCache.cs`（88 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/TransactionManager.cs`（167 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/UnitOfWorkExecutor.cs`（224 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Infrastructure/ValueAtomConverter.cs`（15 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/AggregateRootListDocGenerator.cs`（76 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/AggregateRootReferenceRelationsDocGenerator.cs`（68 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/Alcedo.Core.Plugin.SmartDoc.csproj`（16 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/AlcedoCorePluginSmartDoc.cs`（41 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/AppSettingsDemoScanner.cs`（259 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/SmartDocMiddleware.cs`（192 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI.csproj`（19 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/ViewModels/PaginationControlVm.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/ViewModels/SearchControlVm.cs`（47 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Views/Shared/_AlcedoMvcDaisyUICdnHead.cshtml`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Views/Shared/_AlcedoMvcDaisyUIHead.cshtml`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Views/Shared/_PaginationControl.cshtml`（100 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Views/Shared/_SearchControl.cshtml`（95 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Views/_ViewImports.cshtml`（1 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/css/daisyui.min.css`（7 行）— 第三方资源：仅接入边界。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/css/daisyui.themes.css`（1 行）— 第三方资源：仅接入边界。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/js/alcedo-info-modal.js`（43 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/js/alcedo-overlay.js`（51 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/js/alcedo-ui-daisyui.js`（30 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/wwwroot/js/tailwind-browser.v4.js`（8 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/Alcedo.Core.Presentation.Mvc.csproj`（27 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/AlcedoCorePresentationMvcComponent.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/AlcedoMvcExceptionHandler.cs`（31 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/AlcedoPageControllerBase.cs`（27 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/Controllers/ErrorController.cs`（50 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/ProgramExtensions.cs`（27 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/TagHelpers/AntiforgeryInjectionTagHelper.cs`（67 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/TagHelpers/IdempotencyTokenTagHelper.cs`（47 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/ViewModels/ErrorVm.cs`（26 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/ViewModels/PageNotFoundVm.cs`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/Views/Shared/_AlcedoMvcCoreHead.cshtml`（3 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/Views/_ViewImports.cshtml`（1 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/wwwroot/js/AlcedoUI.js`（27 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/wwwroot/js/alcedo-htmx-extension.js`（290 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation.Mvc/wwwroot/js/htmx.min.js`（1 行）— 第三方资源：仅接入边界。
- `Src/Core/Backend/Alcedo.Core.Presentation/Alcedo.Core.Presentation.csproj`（29 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Backend/Alcedo.Core.Presentation/AlcedoApiControllerBase.cs`（40 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/AlcedoCorePresentationComponent.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/AlcedoPresentationOptions.cs`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/AlcedoProblemDetailsFactory.cs`（69 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/AlcedoValidatorProvider.cs`（118 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/Attributes.cs`（29 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/EndpointResourceOperationCenter.cs`（203 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation/Filters/IdempotencyKeyFilter.cs`（127 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation/HttpContextIpHelper.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/Middlewares/AlcedoErrorExceptionHandler.cs`（39 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Backend/Alcedo.Core.Presentation/PreException/PreExceptions.cs`（51 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/ProgramExtension.cs`（99 行）— 源码静态扫描：异常/日志、DDD/契约。
- `Src/Core/Backend/Alcedo.Core.Presentation/RequestJsonConverter/JsonStringEmptyConverter.cs`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/Validation/Attributes.cs`（148 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Backend/Alcedo.Core.Presentation/ValueAtomModel/ValueAtomModelBinder.cs`（37 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Core/Backend/Alcedo.Core.Presentation/ValueAtomModel/ValueAtomModelBinderProvider.cs`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Alcedo.Frontend.Abstraction.csproj`（9 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/ApiBusinessException.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/ApiClientAttributes.cs`（55 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Attributes/AutoComponentsAttributes.cs`（154 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/BackednInterface/Base.cs`（13 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/BackednInterface/IMasterDataController.cs`（47 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/BackednInterface/IReferenceController.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Command/MasterDataCommandBase.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/AggregateRootDtoBase.cs`（14 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/EntityDtoBase.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/FormFile.cs`（3 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/MasterDataDto.cs`（22 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/MasterDataLogDto.cs`（18 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/OperatorDto.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/PagedResult.cs`（25 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Dto/ReferenceDataDto.cs`（6 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Helpers/AutoValidator.cs`（135 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Helpers/EmailChecker.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Helpers/PasswordStrengthChecker.cs`（39 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/Helpers/PhoneNumberChecker.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/MasterDataLogSearchQuery.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/MasterDataSearchQueryBase.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/PaginationQuery.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/ReferenceDataSearchQueryBase.cs`（6 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/SearchQueryBase.cs`（19 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/SearchQuery/SortQuery.cs`（55 行）— 源码静态扫描：DDD/契约。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/StateMachine/MasterDataStateMachine.cs`（107 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/StateMachine/StateMachine.cs`（56 行）— 源码静态扫描：认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Abstraction/StateMachine/ValueAtom.cs`（40 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Alcedo.Frontend.Blazor.csproj`（28 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/AlcedoLogo.razor`（39 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/AlcedoRequestService.cs`（323 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/AutoValidatorService.cs`（22 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/BarItem.cs`（18 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/AlcedoApplicationInfoModal.razor`（219 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/AlcedoWatermark.razor`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoComponentOrderedProperties.cs`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoForm.razor`（395 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoFormDialog.razor`（41 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoPicker.razor`（120 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoSearch.razor`（392 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Auto/AutoTable.razor`（134 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/EditableTable.razor`（138 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/GlobalErrorBoundary.razor`（56 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/GlobalOverlay.razor`（25 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataCreatePage.razor`（72 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataCreatePageBase.cs`（46 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataDetailPageBase.cs`（88 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataDetailPageFormCard.razor`（24 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataDetailPageToolBar.razor`（70 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataFormActionToolBar.razor`（134 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataListPage.razor`（96 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataLogDrawer.razor`（201 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MasterData/MasterDataTableActionToolBar.razor`（131 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/MudBlazorProvider.razor`（40 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Pagination.razor`（139 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/PickerInput.razor`（80 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/Shared/GenericListPage.razor`（73 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Components/SubEntityBatchTable.razor`（96 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/GlobalOverlayService.cs`（30 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Layout/EmptyLayout.razor`（4 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Layout/MainLayout.razor`（80 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Layout/NavItemRenderer.razor`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Layout/NavMenu.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/NavItem.cs`（25 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/Pages/NotFound.razor`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/ProgramExtensions.cs`（81 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/ThemeProvider.cs`（29 行）— 源码静态扫描：状态/异步。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/_Imports.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/wwwroot/css/app.css`（24 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Blazor/wwwroot/favicon.svg`（26 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Core/Alcedo.Frontend.Core.csproj`（24 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Core/AlcedoApplicationInfo.cs`（61 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Core/AlcedoModulesRegistry.cs`（64 行）— 源码静态扫描：异常/日志。
- `Src/Core/Frontend/Alcedo.Frontend.Core/AlcedoUseModuleProgramExtensions.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Core/HttpClient/AlcedoDispatchClient.cs`（503 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Core/HttpClient/AlcedoHealthClient.cs`（57 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Core/HttpClient/Handlers.cs`（57 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Core/Frontend/Alcedo.Frontend.Core/IAlcedoModule.cs`（68 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Core/ProgramExtensions.cs`（102 行）— 源码静态扫描：异常/日志。
- `Src/Core/Frontend/Alcedo.Frontend.Host/Alcedo.Frontend.Host.csproj`（19 行）— 配置/构建/文档/资源扫描。
- `Src/Core/Frontend/Alcedo.Frontend.Host/AlcedoFrontendOptions.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Core/Frontend/Alcedo.Frontend.Host/AlcedoFrontendProgramExtensions.cs`（42 行）— 源码静态扫描：声明/展示/接口。
- `Src/Directory.Build.props`（7 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Admin/AdminException/Exception.cs`（15 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Alcedo.Framework.Admin.csproj`（16 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Admin/AlcedoAdminModule.cs`（25 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Application/AccessLog/AccessLogApplication.cs`（82 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Application/AccessLog/AccessLogDto.cs`（30 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Application/AccessLog/AccessLogSearchQuery.cs`（22 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Application/QuartzAppService.cs`（270 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Application/QuartzDto.cs`（28 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Controller/LogController.cs`（32 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Controller/QuartzController.cs`（113 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Domain/Entity/AccessLogEntity.cs`（66 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Infrastructure/Appsettings.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Infrastructure/Configuration/AccessLogEntityConfiguration.cs`（16 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Infrastructure/RedisKeySpace/AccessLogRedisKeySpace.cs`（15 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Infrastructure/Repository/AccessLogRedisProvider.cs`（54 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Job/AccessLogPersistor.cs`（24 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Job/StorageCleaner.cs`（21 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Presentation/Interface/IAccessLogRedisQueueProvider.cs`（12 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Admin/Presentation/Middlemare/AccessLogRecorder.cs`（67 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/Abstraction/Attributes.cs`（33 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/Abstraction/Formula.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/Alcedo.Framework.FormulaEngine.Contract.csproj`（9 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/IFormulaEngine.cs`（28 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Alcedo.Framework.FormulaEngine.csproj`（19 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/AlcedoFormulaEngineModule.cs`（34 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Application/Command/FormulaCommand.cs`（66 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Application/Dto/FormulaDto.cs`（56 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Application/Helpers/TestFormulaHelper.cs`（40 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Application/SearchQuery/FormulaSearchQuery.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Application/Service/FormulaApplicationService.cs`（137 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Controller/FormulaController.cs`（72 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Domain/FormulaAggRoot.cs`（224 行）— 源码静态扫描：异常/日志、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Domain/FormulaExecutionLogEntity.cs`（34 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Domain/HistoryFormulaEntity.cs`（131 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Domain/IRepository/IFormulaRepository.cs`（10 行）— 源码静态扫描：状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Domain/IRepository/IHistoryFormulaStore.cs`（8 行）— 源码静态扫描：状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Core.cs`（270 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Dto.cs`（70 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Exceptions.cs`（19 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/ParamFlattener.cs`（62 行）— 源码静态扫描：异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/SandBox.cs`（72 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/FormulaCenter/Exception.cs`（14 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/FormulaCenter/FormulaEngine.cs`（97 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/FormulaCenter/FormulaStoreCenter.cs`（132 行）— 源码静态扫描：异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Configuration/FormulaAggRootConfiguration.cs`（78 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Configuration/FormulaExecutionLogEntityConfiguration.cs`（15 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Configuration/HistoryFormulaEntityConfiguration.cs`（86 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Repository/FormulaRepository.cs`（23 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Repository/HistoryFormulaStore.cs`（19 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Seeder/FormulaSeeder.cs`（113 行）— 源码静态扫描：EF/事务、异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/Alcedo.Framework.Identity.Contracts.csproj`（14 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/AlcedoIdentityScheme.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/Attributes.cs`（7 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/IOperatorApplicationService.cs`（38 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/IUserQueryIdentityService.cs`（9 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/OperatorDto.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/UserInfoDto.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Alcedo.Framework.Identity.csproj`（29 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Identity/AlcedoIdentityModule.cs`（58 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Command/AuthCommand.cs`（15 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Command/RoleCommand.cs`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Command/UserCommand.cs`（58 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/Auth/Result.cs`（23 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/Log/LoginLogDto.cs`（14 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/PermDto.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/Role/RoleCacheDto.cs`（10 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/Role/RoleDto.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/User/UserCacheDto.cs`（20 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/User/UserDto.cs`（39 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Dto/User/UserProfileDto.cs`（20 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Event/LoginLogEvent.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Factory/LoginThrottleDecisionFactory.cs`（54 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Helper/EmailFormatChecker.cs`（33 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Helper/PasswordStrengthChecker.cs`（24 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Helper/PhoneNumberParser.cs`（49 行）— 源码静态扫描：异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IQueryRepository/IRoleQueryRepository.cs`（17 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IQueryRepository/IUserQueryRepository.cs`（19 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IStore/ILoginContextDataStore.cs`（12 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IStore/ILoginThrottleStore.cs`（10 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IStore/IRefreshTokenStore.cs`（14 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/IStore/IRoleCacheStore.cs`（19 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Provider/JwtProvider.cs`（49 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Provider/RoleCacheProvider.cs`（71 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Provider/TotpProvider.cs`（66 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Provider/UserCacheProvider.cs`（22 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/SearchQuery/LoginLogSearchQuery.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/SearchQuery/PermSearchQuery.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/SearchQuery/RoleSearchQuery.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/SearchQuery/UserSearchQuery.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Auth/AuthApplicationService.cs`（146 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Auth/LoginServiceComponent.cs`（342 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/ContractImpl/OperatorApplicationService.cs`（43 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/ContractImpl/UserInfoApplicationService.cs`（44 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Log/LoginLogApplicationService.cs`（57 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Perm/PermApplicationService.cs`（59 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Role/RoleApplicationService.cs`（201 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/Role/RoleApplicationServiceComponent.cs`（50 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/User/UserApplicationService.cs`（334 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Application/Service/User/UserApplicationServiceComponent.cs`（57 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Consumer/Log/LoginLogConsumer.cs`（18 行）— 源码静态扫描：状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Consumer/Role/RoleCacheRefreshConsumer.cs`（13 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Consumer/User/UserCacheRefreshConsumer.cs`（13 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/AuthController.cs`（142 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/LogController.cs`（33 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/PermsController.cs`（31 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/RolesController.cs`（73 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/UsersController.cs`（102 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Controller/Wrapper/RefreshTokenCookieProvider.cs`（76 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Entity/LoginLogEntity.cs`（38 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Entity/Role.cs`（174 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Entity/User.cs`（394 行）— 源码静态扫描：异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/IRepository/IUserRepository.cs`（50 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Service/RoleDomainService.cs`（41 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Domain/Service/UserDomainService.cs`（35 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Exceptions/Exceptions.cs`（157 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/AppSettings.cs`（53 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Configuration/LoginLogEntityConfiguration.cs`（6 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Configuration/RoleAggregateRootConfigurations.cs`（23 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Configuration/UserAggregateRootConfigurations.cs`（57 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/PasswordHasher.cs`（27 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/QueryRepository/RoleQueryRepository.cs`（54 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/QueryRepository/UserQueryRepository.cs`（80 行）— 源码静态扫描：EF/事务、状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/RedisKeySpace/LoginRedisKeySpace.cs`（23 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/RedisKeySpace/RefreshTokenRedisKeySpace.cs`（15 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/RedisKeySpace/RoleRedisKeySpace.cs`（15 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/RedisKeySpace/UserRedisKeySpace.cs`（17 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Repository/UserRepository.cs`（56 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Seeder/RootUserSeeder.cs`（49 行）— 源码静态扫描：EF/事务、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/LoginContextStore.cs`（27 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/LoginThrottleStore.cs`（43 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/RefreshTokenStore.cs`（109 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Infrastructure/Store/RoleCacheStore.cs`（55 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/OpenapiTransformer/BearerSecuritySchemeTransformer.cs`（63 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/AlcedoRbacHandler.cs`（120 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/AuthSessionMiddleware.cs`（100 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/HttpContextUserStoreKey.cs`（6 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Identity/Presentation/PermissionRequirement.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Identity/ProgramExtensions/Authentication.cs`（80 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts.csproj`（13 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/INotificationMessage.cs`（42 行）— 源码静态扫描：异常/日志。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/MessageExampleGenerator.cs`（65 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/MessageTypeAttribute.cs`（24 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/MessageTypeMetadataCenter.cs`（130 行）— 源码静态扫描：异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/NotificationDispatchEvent.cs`（56 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Alcedo.Framework.Notification.csproj`（22 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/AlcedoNotificationModule.cs`（31 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Command/MessageConfigCommand.cs`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Command/MessageTemplateCommand.cs`（46 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageConfigCacheDto.cs`（13 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageConfigDto.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageErrorDto.cs`（24 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageTemplateCacheDto.cs`（21 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageTemplateDto.cs`（15 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageTemplateTestRenderResult.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Dto/MessageTypeDto.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Events/NotificationEmailSendEvent.cs`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Helper/MessageTemplateRenderHelper.cs`（28 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/ICacheStore/IMessageConfigCacheStore.cs`（12 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/ICacheStore/ITemplateMemoryCacheProvider.cs`（12 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/IQueryRepository/IMessageConfigQueryRepository.cs`（14 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Provider/MessageConfigCacheProvider.cs`（80 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/SearchQuery/MessageConfigSearchQuery.cs`（7 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/SearchQuery/MessageErrorSearchQuery.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/SearchQuery/MessageTemplateSearchQuery.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/SearchQuery/MessageTypeSearchQuery.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageConfig/MessageConfigApplicationService.cs`（209 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageConfig/MessageConfigApplicationServiceComponent.cs`（76 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageDispatch/MessageDispatchApplicationService.cs`（232 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageError/MessageErrorApplicationService.cs`（88 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageTemplate/MessageTemplateApplicationService.cs`（100 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageTemplate/MessageTemplateApplicationServiceComponent.cs`（46 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageType/MessageTypeApplicationService.cs`（51 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Consumer/MessageConfigCacheRefreshConsumer.cs`（30 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Consumer/MessageTemplateCacheRefreshConsumer.cs`（22 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Consumer/NotificationDispatchConsumer.cs`（18 行）— 源码静态扫描：状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Consumer/NotificationEmailSender.cs`（26 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Controller/MessageConfigController.cs`（90 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Controller/MessageErrorController.cs`（59 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Controller/MessageTemplatesController.cs`（32 行）— 源码静态扫描：状态/异步、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Controller/MessageTypesController.cs`（38 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Entity/MessageConfig.cs`（184 行）— 源码静态扫描：异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Entity/MessageError.cs`（45 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Entity/MessageTemplate.cs`（83 行）— 源码静态扫描：DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/IRepository/IMessageConfigRepository.cs`（9 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Service/ITemplateRenderer.cs`（5 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Domain/Service/MessageConfigDomainService.cs`（68 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Configuration/MessageConfigConfiguration.cs`（37 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Configuration/MessageErrorConfiguration.cs`（15 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Configuration/MessageTemplateConfiguration.cs`（17 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Email/EmailAppSettings.cs`（27 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Email/EmailService.cs`（58 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/NotificationOptions.cs`（9 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/QueryRepository/MessageConfigQueryRepository.cs`（36 行）— 源码静态扫描：EF/事务、状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Repository/MessageConfigRepository.cs`（19 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/Store/MessageConfigRedisStore.cs`（37 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Infrastructure/TemplateMemoryCacheProvider.cs`（68 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Alcedo.Framework.Shared.csproj`（20 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Backend/Alcedo.Framework.Shared/AlcedoClaimTypes.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/AlcedoFrameworkModule.cs`（59 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Command/MasterDataCommandBase.cs`（14 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Components/MasterData/MasterDataEditServiceComponent.cs`（141 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Components/MasterData/MasterDataSearchServiceComponent.cs`（19 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Components/MasterData/MasterDataStatusServiceComponent.cs`（57 行）— 源码静态扫描：状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Components/ReferenceData/ReferenceDataSearchServiceComponent.cs`（20 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Dto/MasterDataDto.cs`（14 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Dto/MasterDataLogDto.cs`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Dto/OperatorDto.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Dto/ReferenceDataDto.cs`（8 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/IQueryRepository/IMasterDataLogQueryRepository.cs`（10 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/IQueryRepository/IMasterDataQueryRepository.cs`（14 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/SearchQuery/MasterDataLogSearchQuery.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/SearchQuery/MasterDataSearchQueryBase.cs`（10 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/SearchQuery/ReferenceDataSearchQueryBase.cs`（8 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Service/IMasterDataApplicationService.cs`（31 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Service/IReferenceDataApplicationService.cs`（16 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Service/MasterDataApplicationServiceBase.cs`（59 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Service/MasterDataLogServiceBase.cs`（52 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Application/Service/ReferenceDataApplicationService.cs`（43 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Consumer/MasterDataConsumer.cs`（19 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Backend/Alcedo.Framework.Shared/ContextOperator.cs`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Controller/AlcedoFrameworkApiControllerBase.cs`（38 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Controller/MasterDataControllerBase.cs`（80 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Controller/ReferenceDataControllerBase.cs`（39 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Entity/MasterDataAggregateRoot.cs`（207 行）— 源码静态扫描：异常/日志、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Entity/MasterDataArchivedEntityBase.cs`（32 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Entity/MasterDataAuditLogAggRoot.cs`（82 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Entity/Operator.cs`（27 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Entity/ReferenceDataAggregateRoot.cs`（20 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/Exceptions/MasterDataExceptions.cs`（81 行）— 源码静态扫描：DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/IRepositoryBase/IMasterDataRepository.cs`（82 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/IRepositoryBase/IMasterDataStatusValidatorRepository.cs`（46 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Domain/IRepositoryBase/IReferenceDataRepository.cs`（46 行）— 源码静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Configuration/MasterDataArchivedEntityConfiguration.cs`（22 行）— 源码静态扫描：EF/事务。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Configuration/MasterDataConfigurationBase.cs`（42 行）— 源码静态扫描：EF/事务、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Configuration/ReferenceDataConfigurationBase.cs`（22 行）— 源码静态扫描：EF/事务、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/QueryRepository/MasterDataAuditLogQueryRepository.cs`（53 行）— 源码静态扫描：EF/事务、状态/异步、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/QueryRepository/MasterDataQueryRepository.cs`（46 行）— 源码静态扫描：EF/事务、DDD/契约。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Repository/MasterDataRepositoryBase.cs`（217 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Repository/MasterDataStatusValidatorRepository.cs`（158 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/Infrastructure/Repository/ReferenceDataRepository.cs`（123 行）— 源码静态扫描：EF/事务、状态/异步、异常/日志、DDD/契约、认证/缓存。
- `Src/Framework/Backend/Alcedo.Framework.Shared/SystemUser.cs`（15 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/AdminNav.cs`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/Alcedo.Frontend.Admin.csproj`（15 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/AlcedoAdminModuel.cs`（7 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/BackendInterface/ILog.cs`（59 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/BackendInterface/IQuartz.cs`（75 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/Components/UpdateCronDialog.razor`（24 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/Pages/AccessLogPage.razor`（68 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/Pages/QuartzPage.razor`（216 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Admin/_Imports.razor`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Alcedo.Frontend.FormulaEngine.csproj`（15 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/AlcedoFormulaEngineModule.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/BackendInterfaces/IFormula.cs`（137 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Components/FormulaTestDialog.razor`（128 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/FormulaNav.cs`（20 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Page/Formula/FormulaDetailPage.razor`（364 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Page/Formula/FormulaListPage.razor`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/_Imports.razor`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Alcedo.Frontend.Identity.csproj`（25 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/AlcedoIdentityModule.cs`（27 行）— 源码静态扫描：异常/日志。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Auth/AuthClient.cs`（31 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Auth/AuthCommand.cs`（17 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Auth/AuthDto.cs`（49 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Log/logClient.cs`（32 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Perm/Dto.cs`（26 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Perm/PermClient.cs`（9 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Role/DTO.cs`（26 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/Role/RoleClient.cs`（29 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/User/Command.cs`（26 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/User/Dto.cs`（67 行）— 源码静态扫描：认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/User/UserClient.cs`（40 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Clients/User/UserSearchQuery.cs`（12 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/AlcedoIdentityRouteView.razor`（67 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/ChangePasswordDialog.razor`（124 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/PasswordStrengthInput.razor`（59 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/TotpVerifyDialog.razor`（135 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Components/TwoFactorEnableDialog.razor`（116 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Exceptions/AuthenticationExceptions.cs`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Handlers/TokenRefreshHandler.cs`（154 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/IdentityNav.cs`（41 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Login.razor`（125 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/LoginLogPage.razor`（68 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/PasswordChange.razor`（46 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/PermsPage.razor`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Profile.razor`（210 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/RedirectToLogin.razor`（42 行）— 源码静态扫描：状态/异步、异常/日志、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Roles/RoleCreate.razor`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Roles/RoleDetail.razor`（84 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Roles/RoleList.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Users/UserCreate.razor`（56 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Users/UserDetail.razor`（161 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Pages/Users/UserList.razor`（111 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/ProfileBarItem.cs`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/Providers/AuthStateProvider.cs`（86 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Identity/_Imports.razor`（16 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Alcedo.Frontend.Notification.csproj`（20 行）— 配置/构建/文档/资源扫描。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/AlcedoNotificationModule.cs`（7 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/BackendInterface/IMessageConfig.cs`（61 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/BackendInterface/IMessageError.cs`（42 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/BackendInterface/IMessageTemplate.cs`（76 行）— 源码静态扫描：状态/异步、认证/缓存。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/BackendInterface/IMessageType.cs`（29 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Components/MessageConfigTestDialog.razor`（104 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Components/MessageTemplateTestDialog.razor`（145 行）— 源码静态扫描：状态/异步、异常/日志。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/NotificationNav.cs`（46 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageConfig/MessageConfigCreate.razor`（38 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageConfig/MessageConfigDetail.razor`（158 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageConfig/MessageConfigList.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageError/MessageErrorList.razor`（102 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageTemplate/MessageTemplateCreate.razor`（13 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageTemplate/MessageTemplateDetail.razor`（42 行）— 源码静态扫描：状态/异步。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageTemplate/MessageTemplateList.razor`（11 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/Pages/MessageTypes.razor`（8 行）— 源码静态扫描：声明/展示/接口。
- `Src/Framework/Frontend/Alcedo.Frontend.Notification/_Imports.razor`（16 行）— 源码静态扫描：声明/展示/接口。
- `Tests/Core/Backend/Alcedo.Core.Application.Test/Alcedo.Core.Application.Test.csproj`（25 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Application.Test/ApplicationLockProviderTests.cs`（269 行）— 测试静态扫描：状态/异步、认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Application.Test/EntitySearchServiceComponentTests.cs`（108 行）— 测试静态扫描：状态/异步、认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Application.Test/PagedResultTests.cs`（60 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/AggRefRelationBuilderTests.cs`（218 行）— 测试静态扫描：DDD/契约。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/AggregateRootMetadataCenterTests.cs`（143 行）— 测试静态扫描：DDD/契约。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/AggregateRootTests.cs`（136 行）— 测试静态扫描：DDD/契约。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/Alcedo.Core.Domain.Test.csproj`（25 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/EntityBaseTests.cs`（39 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Domain.Test/StateMachineTests.cs`（129 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/Alcedo.Core.Infrastructure.Test.csproj`（29 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/DistributedLockFactoryTests.cs`（132 行）— 测试静态扫描：状态/异步、异常/日志、认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/ReferenceIntegrityCompilerTests.cs`（328 行）— 测试静态扫描：EF/事务、状态/异步、DDD/契约、认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/ReferenceIntegrityConfigCenterTests.cs`（95 行）— 测试静态扫描：DDD/契约。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/ReferenceMetadataFactoryTests.cs`（137 行）— 测试静态扫描：DDD/契约。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/SearchQueryMetaCacheTests.cs`（182 行）— 测试静态扫描：认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Infrastructure.Test/ValueAtomConverterTests.cs`（40 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/Alcedo.Core.Presentation.Test.csproj`（29 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/AlcedoProblemDetailsFactoryTests.cs`（258 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/ClaimsPrincipalExtensionsTests.cs`（64 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/EndpointPermissionCenterTests.cs`（151 行）— 测试静态扫描：。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/JsonEmptyStringToNullConverterTests.cs`（59 行）— 测试静态扫描：认证/缓存。
- `Tests/Core/Backend/Alcedo.Core.Presentation.Test/ValueAtomModelBinderTests.cs`（125 行）— 测试静态扫描：状态/异步、异常/日志。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Alcedo.Framework.Identity.Test.csproj`（28 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Helpers/EmailFormatCheckerTests.cs`（61 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Helpers/PasswordStrengthCheckerTests.cs`（43 行）— 测试静态扫描：认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Helpers/PhoneNumberParserTests.cs`（59 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Providers/JwtProviderTests.cs`（69 行）— 测试静态扫描：认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Providers/LoginThrottleDecisionFactoryTests.cs`（54 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Providers/TotpProviderTests.cs`（83 行）— 测试静态扫描：认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Services/LoginServiceComponentTests.cs`（97 行）— 测试静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Services/OperatorModuleServiceTests.cs`（96 行）— 测试静态扫描：状态/异步、认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Services/RoleApplicationServiceTests.cs`（123 行）— 测试静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Application/Services/UserApplicationServiceTests.cs`（159 行）— 测试静态扫描：状态/异步、DDD/契约、认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Domain/DomainTestFixture.cs`（17 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Domain/RoleAggTests.cs`（220 行）— 测试静态扫描：。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Domain/UserAggTests.cs`（538 行）— 测试静态扫描：认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Infrastructure/PasswordHasherTests.cs`（59 行）— 测试静态扫描：认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Presentation/AlcedoRbacHandlerTests.cs`（103 行）— 测试静态扫描：状态/异步、认证/缓存。
- `Tests/Framework/Backend/Alcedo.Framework.Identity.Test/Presentation/AuthSessionMiddlewareTests.cs`（143 行）— 测试静态扫描：状态/异步、认证/缓存。
- `global.json`（7 行）— 配置/构建/文档/资源扫描。
