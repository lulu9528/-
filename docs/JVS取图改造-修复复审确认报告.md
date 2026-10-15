# JVS 取图工具改造 · 修复复审确认报告

**复审方式**：重新精读修复后 `JvsCamera.cs`（全文 263 行）、`Frm_JvsSetting.cs`（全文 248 行）、`JvsWorkspaceMapping.cs`（全文 109 行），定位核验 `VisionSystem.cs` 新增批量方法（232–256 行）与 `Jabil.Module.Setting.csproj` 引用；构建 EXIT=0 与 smoke 证据（LOAD_OK→RUN「未出图」→CLOSE_OK→**RELOAD_AFTER_CLOSE=True**→RUN→CLOSE_OK）作为输入采信，未重复构建/运行。

**总览**：13 项修复点中 **11 项确认解决、2 项基本解决**（⑨、⑩，各有 1 个遗留问题，见下）；复审新发现 **[建议修改] 2 项、[仅供参考] 2 项**，无 [必须修复] 级新问题。

---

## 一、重点五项详细结论

### ① 释放时序：确认解决，未引入新泄漏

`JvsCamera.cs` 第 104–108 行：重建条件（`_disposed || !_scriptReady || _toolPath != jvtPath`）成立时**先** `ReleasePythonObjectsLocked()` 再创建新模块；验证失败路径（124 行）与异常路径（137–144 行，引擎已初始化时在 `Py.GIL()` 内释放、且吞掉释放异常不覆盖原始错误）都会复位。释放顺序（_lastCaptureResult → _captureFn → _loadFn → _captureModule → 置 `_scriptReady=false`/`_toolPath=null`）正确；Dispose 后 handle 置零 + 字段置 null，double-release 安全。GetAttr 中途抛异常也不再残留「新模块+旧函数」不一致（异常路径统一释放）。**无新泄漏、无悬空路径**。

### ② Close→Load 语义：确认解决，自洽

Close（219–245 行）置 `_disposed=true`、清 `_outputImage`、GIL 内释放；Load 装配条件包含 `_disposed`（104 行），重建成功后第 115 行复位 `_disposed=false`。Close 后 Load 走完整重建流程，`RELOAD_AFTER_CLOSE=True` 佐证；Close 后未 Load 时 Run 被第 158 行守卫正确拒绝。契约与注释（215–217 行）现已一致。

### ⑧ getter 锁：确认解决，无死锁

`OutputImage`/`LastError` 改为字段 + getter 加 `_gate` 锁（51–52、58–67 行）。**关键核查点**：Load/Run/Close 内部全部**直接写字段**（`_lastError` 82/87/97/123/131/136/160/207 行、`_outputImage` 188/191/198/208 行），**没有一处经 getter 读**——不存在锁内读 getter；Monitor 本身可重入，锁内也未调用任何会反向取 `_gate` 的外部回调。调用方（VisionProcess.cs 第 591 行）走 getter 读，Run 执行期间被锁阻塞，读到的是 Run 完成后的新帧/null，串帧与悬空窗口均被消除。

### ⑨ 防御持有引用链：基本解决，遗留 1 个悬空窗口（见新发现 A）

设计自洽：Run 开头（169–173 行）在 GIL 内释放上一帧 `_lastCaptureResult`，`_outputImage`（Image 包装）与 `_lastCaptureResult`（PyObject）共享底层 Python 对象，**底层对象存活由 `_lastCaptureResult` 的强引用保证**——原先「依赖 JVS 工具对 output pin 强引用」的外部保证已被消除（原发现 9 的核心风险解决）；失败路径同步置 `_outputImage=null`；Close 先清 C# 引用（224 行）再释放 Python 引用（250 行），顺序正确。**遗留**：`ReleasePythonObjectsLocked()`（248–256 行）释放 `_lastCaptureResult` 时未同步清 `_outputImage`，而 Close 路径单独做了 `_outputImage=null`（224 行）、**Load 重建路径没有**——换路径 Load 后 `_outputImage` 仍指向已释放对象。

### ⑩ 先表后 JSON：确认解决，遗留 1 个部分更新场景（见新发现 B）

`Frm_JvsSetting.cs` 215–234 行：先批量写 VisionTool 表（228–229 行），成功后 `m.Save()` + `Reload()`（233–234 行）。**表写入失败 → JSON 未写，两者都不变，状态一致**（核心目标达成）；表成功、JSON 失败 → 表已生效、JSON 未写，注释（231–232 行）已说明「下次启动仍以表路径优先，核心功能不受影响」，读取侧（BindVisionTools 优先表路径）与之匹配。**遗留**：批量方法逐条 upsert 无事务，第 k 条失败时前 k-1 条已入库、JSON 未写 → 表部分更新（但重试可收敛，见 B）。

---

## 二、修复点逐项核验

| 修复项 | 验证结论 | 依据 |
|---|---|---|
| ① 重建前 ReleasePythonObjectsLocked + 异常路径释放 | ✅ 确认解决 | JvsCamera.cs 104–108、124、137–144 |
| ② Close 置位 / Load 复位 _disposed | ✅ 确认解决 | 104、115、219–245（RELOAD_AFTER_CLOSE 佐证） |
| ③ 拆 jvs_load/jvs_capture，Load 真实反序列化 | ✅ 确认解决 | 脚本 29–41 行；Load 117–127 行，失败即复位 |
| ④ Where 幂等（CogAcqFifoTool‖JvsCameraTool） | ✅ 确认解决 | Frm 219–222 行 |
| ⑥ 无 JvsCameraTool 记录时橙色提示 | ✅ 确认解决 | Frm 183–190 行 |
| ⑦ Close 释放段包 GIL、引擎未初始化跳过 | ✅ 确认解决 | 227–238 行 |
| ⑧ OutputImage/LastError getter 加锁 | ✅ 确认解决（无死锁） | 58–67 行；内部全直写字段 |
| ⑨ _lastCaptureResult 防御持有 | ⚠ 基本解决 | 遗留见新发现 A |
| ⑩ 先表后 JSON | ⚠ 基本解决 | 遗留见新发现 B |
| ⑪ 保存前 File.Exists 校验 | ✅ 确认解决 | Frm 212–213 行 |
| ⑬ AddOrUpdateVisionToolsAsync 批量、统一刷新 | ✅ 确认解决 | VisionSystem 232–256 行（逐条 upsert、失败抛 InvalidOperationException、255 行统一 RefreshDataAsync 一次） |
| ⑫ CaptureToolPath 默认置空 | ✅ 确认解决 | JvsWorkspaceMapping 37 行 |
| ⑱ csproj 注释更新 + 4 个 SDK 引用清理 | ✅ 确认解决 | csproj 120–121 行注释更新；Jabil.Vision.Camera/.LBAS/MvCamCtrl.Net 已无引用（JvlDotNet 本体与 Python.Runtime 保留，构建 EXIT=0 佐证） |

---

## 三、新发现问题

### [建议修改] A：ReleasePythonObjectsLocked() 未同步清 _outputImage —— 换路径 Load 后存在悬空引用窗口

**文件**：`JvsCamera.cs`　**位置**：第 248–256 行（ReleasePythonObjectsLocked）vs 第 104–128 行（Load 重建）

**问题**：Run 成功后 `_outputImage`（Image 包装）与 `_lastCaptureResult`（PyObject）共享同一底层 Python 对象。换路径 Load 触发重建 → `ReleasePythonObjectsLocked()` 释放 `_lastCaptureResult`（底层对象引用计数归零、可能被回收），但 `_outputImage` 字段**未置 null**（Close 路径第 224 行做了、Load 路径没做）→ 此后 getter 返回指向已释放对象的包装，读 `width/height` 有崩溃风险。当前唯一调用链（VisionProcess：Load→Run→读）在 Run 后读，Run 成功/失败都会在锁内更新 `_outputImage`，**实际流程不触发**；但若存在「Load 后、Run 前」的图像预览/UI 读取路径（设备软件常见），即触发。另：Load 验证失败路径（124 行）同样经此释放，`_disposed` 已在 115 行提前复位为 false（验证失败后对象呈「未关闭未装配」态，语义无破坏但可读性略差）。

**建议**：一行修复——`ReleasePythonObjectsLocked()` 内补 `_outputImage = null;`（与 Close 的 224 行一致），使释放与清引用在同一点原子完成。

### [建议修改] B：批量保存无事务，中途失败仍会部分更新 VisionTool 表

**文件**：`VisionSystem.cs` 第 245–253 行；`Frm_JvsSetting.cs` 第 228–229 行

**问题**：`AddOrUpdateVisionToolsAsync` 逐条 `AddOrUpdateAsync`，第 k 条失败抛 `InvalidOperationException` 时前 k-1 条已入库；此时 JSON 未写（⑩ 已保证），但**表处于部分切换**状态——下次启动部分取像相机走 JVS、部分仍走 Cognex。相比修复前（JSON 已存+表部分更新）一致性已改善：失败可见（UI 提示「保存失败」）、且 Where 幂等（④）使**重试保存可收敛**。

**建议**：可接受现状（失败可重试收敛）；若需更严格：失败时回滚已改内存对象并提示「前 N 条已写入，请重试」，或对多记录引入事务/先校验后写入。

### [仅供参考] C：表成功、JSON Save() 失败时，UI 提示「保存失败」但表已生效

**文件**：`Frm_JvsSetting.cs` 第 233、241–245 行

**说明**：`m.Save()`（File.WriteAllText）失败（磁盘满/权限）时 catch 提示「保存失败」，但 VisionTool 表已写完生效。注释已说明核心功能不受影响；如需更准确，可在 catch 中区分「表已保存、映射文件保存失败」两类提示。

### [仅供参考] D：VisionSystem 原单条 AddOrUpdateVisionToolAsync 仍保留

**文件**：`VisionSystem.cs` 第 200–223 行

**说明**：批量方法新增后旧单条方法未删除（Frm 已改用批量）。保留无碍（源框架 JvsSettingPage 仍调用单条版），如确认无其他调用方可后续清理，避免两套方法并存。

---

## 四、整体总结

**一句话评价**：本轮修复质量高——13 项修复点全部落实，前一轮 2 个 [必须修复]（旧模块泄漏、Close 不可逆）与 5 个 [问题] 均已被代码确认解决，且修复本身未引入新的 [必须修复] 级缺陷；仅剩 1 个悬空引用窗口（A）建议顺手补掉。

**主要问题清单**：
1. [建议修改] `ReleasePythonObjectsLocked()` 未清 `_outputImage`，换路径 Load 后 OutputImage getter 存在悬空窗口（JvsCamera.cs 248–256，一行修复）
2. [建议修改] 批量保存无事务，中途失败表部分更新（可重试收敛）
3. [仅供参考] 表成功/JSON 失败时提示与生效状态轻微错位；单条 upsert 旧方法可择机清理

**建议修改方向**：先补 A 的 `_outputImage = null;`（与 Close 保持一致）；B/C 可接受现状，如需更严谨再按建议增强；其余修复项（①③④⑥⑦⑧⑪⑫⑬⑱）无需再动。
