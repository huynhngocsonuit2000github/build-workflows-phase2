# Tham chiếu nền tảng: yêu cầu, thành phần, tên gọi

Tổng hợp từ roadmap gốc (`documents/AI_Engineering_Roadmap_POC_to_Production.md`) và study plan (`documents/study-plan/plan-data.js`), để mọi phiên làm việc dùng cùng một bộ tên gọi và ranh giới.

> Đây là **yêu cầu và tên gọi**, không phải thiết kế đã chốt. Thiết kế chi tiết (sơ đồ C4, state diagram, schema cụ thể, ADR) là bài tập của user tại các việc tương ứng (`s0b-5`, `s1-1`, `s2-3`…). Khi user chốt thiết kế khác, cập nhật file này cho khớp và ghi ADR.

## 1. Mục tiêu hệ thống

Nhận một ticket Jira đã được chấp thuận và đưa nó qua: phân tích yêu cầu → người duyệt → lập kế hoạch → viết code trong môi trường cách ly → build và test → review độc lập → cổng bảo mật và chất lượng → PR → người duyệt → merge → CI/CD → Dev/QA → smoke và business test → người duyệt Production → deploy → giám sát → cập nhật Jira và tài liệu. Mỗi bước có bằng chứng, truy vết được, và phục hồi an toàn khi retry, crash, sự kiện trùng hay lỗi giữa chừng.

Ba cổng người bắt buộc: **duyệt yêu cầu**, **duyệt PR**, **duyệt Production**.

## 2. Hai repo

| Repo | Vai trò | Ghi chú |
|---|---|---|
| `tasklite` (dự kiến `D:\AI engineer\tasklite` + GitHub) | Bãi tập: ứng dụng nghiệp vụ nhỏ mà agent làm việc trên đó | Dựng mới ở P0. Có CI và branch protection. Agent chỉ mở PR vào đây |
| `build-workflows-phase2` (repo này) | Nền tảng AI delivery, thứ đang học | Chứa cả backlog thực hành và đáp án đánh giá |

Hai lý do phải tách: agent làm việc trong TaskLite không được sửa code hay CI của nền tảng, và không được nhìn thấy đáp án `expected` của backlog (nếu thấy thì kết quả đánh giá vô nghĩa).

## 3. Thành phần và ranh giới trách nhiệm (roadmap §3.2)

| Thành phần | Nắm giữ | Không được nắm |
|---|---|---|
| n8n | Trigger, định tuyến, thông báo, tích hợp | Thực thi tùy ý trên host; trạng thái |
| Workflow API (.NET) | Trạng thái lần chạy, luật chuyển trạng thái, chính sách, quyền, bản ghi thực thi | Suy luận LLM |
| AI BA | Diễn giải yêu cầu, câu hỏi, user story, AC | Chấp thuận nghiệp vụ cuối cùng |
| AI Developer | Phân tích repo, kế hoạch, thay đổi code, test | Quyền merge hay deploy Production |
| AI Reviewer | Review độc lập và phát hiện | Thay thế CI hay người review |
| AI QA | Thiết kế test, gợi ý dữ liệu test, phân tích lỗi | Tuyên bố cổng đã qua khi không có bằng chứng |
| Runner | Chạy lệnh và công cụ được phép trong môi trường hạn chế | Truy cập rộng vào bí mật hay hạ tầng Production |
| CI/CD | Build, test, scan, artifact, deploy lặp lại được | Tin kết quả agent tự báo |
| Người | Quyết định yêu cầu, duyệt code, duyệt phát hành | Lặp lại thủ công việc tất định |

## 4. Thành phần dự kiến trong repo này

| Thư mục | Thành phần | Bước tạo | Ghi chú |
|---|---|---|---|
| `apps/workflow-api/` | Workflow API | `s1` | PostgreSQL, EF Core, state machine |
| `services/agent-service/` | LLM layer, ContextBuilder, AgentRuntime, các agent | `s2`–`s5`, `ctx`, `mag` | Gọi LLM qua gateway từ `gw` |
| `services/docs-mcp/` | MCP server chỉ đọc tài liệu | `s3-3` | MCP C# SDK |
| `services/runner/` | Runner cách ly | `s7` | Container dùng-một-lần cho mỗi job |
| `services/llm-gateway/` (hoặc module trong agent-service, quyết định ở `gw-1`) | LLM gateway | `gw` | Định tuyến, ngân sách, guardrails |
| `workflows/n8n/` | Export workflow n8n | `s1-4` trở đi | Instance n8n riêng cho phase 2 |
| `agents/<role>/prompts/` | Prompt có version | `s2-3` trở đi | |
| `contracts/schemas/`, `contracts/events/` | Schema và sự kiện | `s2-3`, `s5-1`, `mag-2` | |
| `tests/evaluation/` | Eval harness và bộ ca | `s2-5` trở đi | |
| `practice/backlog/` | Backlog ticket thực hành + `expected` | `s0a-2` | Không đưa sang repo TaskLite |
| `infra/docker/`, `infra/terraform/` | Hạ tầng | `s0b-2`, `s13-2` | |
| `docs/` | Tài liệu hệ thống | Mọi bước | Xem mục 9 |

## 5. Trạng thái lần chạy (roadmap gợi ý, user thiết kế lại ở `s1-1`)

```
Created → RequirementAnalysis → AwaitingRequirementApproval → ImplementationPlanning → Coding
→ BuildAndTest → CodeReview → AwaitingCodeApproval → PullRequestCreated → ReadyForDeployment → Completed
(bất kỳ bước nào) → Failed | Cancelled
```

Quy tắc bắt buộc: kiểm tra mọi chuyển trạng thái trong .NET; từ chối chuyển sai; lưu mọi thay đổi kèm lịch sử; optimistic concurrency; không dùng nhãn Jira làm nguồn trạng thái duy nhất.

## 6. Dữ liệu cốt lõi (tên và nơi xuất hiện trong plan)

| Thực thể | Trường chính | Việc |
|---|---|---|
| `WorkflowExecution` | Id, JiraIssueKey, CurrentState, Status, CorrelationId, Version, CreatedAt, UpdatedAt | `s1-2` |
| `WorkflowHistory` | Id, WorkflowExecutionId, FromState, ToState, ChangedAt, ChangedBy, Reason | `s1-2` |
| Bản ghi idempotency | Idempotency key, kết quả đã lưu, unique index | `s1-4` |
| `LlmCall` | Execution/correlation id, model, prompt version, token vào/ra, chi phí ước lượng, độ trễ | `s2-4` |
| `ToolCall` | Agent, công cụ, tham số đã che, kết quả, thời gian | `s3-2` |
| `ContextSource` | Loại, độ ưu tiên, mức tin cậy, nội dung | `ctx-2` |
| `AgentDefinition` / `AgentRun` | Prompt, công cụ, schema đầu ra, ngân sách / trạng thái, các bước, ngân sách đã dùng | `s4-5` |
| `RequirementArtifact` | Bất biến, có version, người duyệt | `s4-2` |
| `Artifact` (chỉ-thêm) | id, executionId, kind, schemaVersion, contentHash, content, producedBy, model, promptVersion, inputArtifactIds, createdAt | `mag-2` |
| `RunnerJob` | Người yêu cầu, repo, commit SHA, lệnh, exit code, thời gian | `s7-2` |
| `Job` (hàng đợi) | status, lease_until, lease_version, attempts, idempotency_key | `s8-2` |
| `Outbox` / `Inbox` | Sự kiện chờ gửi / sự kiện đã xử lý | `s8-4` |

Trạng thái job: `Queued → Leased → Succeeded | Failed | DeadLettered`. Phân loại lỗi (`s8-3`): Validation, Unauthorized, RateLimit, TransientNetwork, RunnerCrash, TestFailure, InvalidAiOutput, PermanentProvider.

## 7. Hợp đồng dữ liệu giữa các agent

| Hợp đồng | Trường | Việc |
|---|---|---|
| `RequirementAnalysis` (đầu ra AI BA) | summary, userStory, acceptanceCriteria[], openQuestions[], assumptions[] | `s2-3` |
| `ImplementationPlan` | file, thay đổi, test, rủi ro | `s5-1` |
| `ReviewFinding` | file, line, severity, description, suggestion | `s5-1` |
| Kết luận review | approve / request_changes / block, **tính bằng code** từ mức độ phát hiện | `s5-3` |
| `TestScenario` | acId, type (positive/negative/boundary/authorization), steps, data, expected, layer | `s9-2` |
| Sự kiện miền | RequirementApproved, PlanProduced, CodeChanged, ReviewCompleted, TestsGenerated | `mag-2` |

## 8. API và công cụ đã nêu trong plan

**Workflow API**

| Endpoint | Mục đích | Việc |
|---|---|---|
| `POST /executions` + header `Idempotency-Key` | Tạo lần chạy, gọi lặp trả lại kết quả cũ | `s1-4` |
| `GET /executions/{id}`, `GET /executions/{id}/history` | Trạng thái và lịch sử | `s1-3` |
| `POST /executions/{id}/transitions` (toState, actor, reason, expectedVersion) | Chuyển trạng thái, 409 khi lệch version | `s1-3` |
| `POST /executions/{id}/retry` | Chỉ từ Failed | `s1-5` |
| `POST /executions/{id}/cancel` | Người hủy lần chạy | `s12-1` |

**Runner**: `POST /jobs` (xác thực bằng service token, sau là mTLS), chính sách trong `runner-policy.json`, chỉ nhận repo + commit SHA cụ thể (`s7-2`).

**Công cụ của agent**

| Vai trò | Công cụ | Việc |
|---|---|---|
| BA | lấy Jira issue, metadata project, tìm yêu cầu, đọc tài liệu; `add_draft_jira_comment` (cần duyệt) | `s3-3` |
| docs-mcp | `search_requirements(query)`, `read_document(path)` chỉ trong `docs/approved/` | `s3-3` |
| Developer | `grep(pattern, glob)`, `glob(pattern)`, `read_file(path, startLine, endLine)`, `find_symbol(name)` trong worktree; `search_docs(query)` (pgvector) | `ctx-3` |
| Reviewer | Chỉ công cụ đọc file | `s5-3` |

Lệnh được phép của Developer (`s5-2`): `dotnet build`, `dotnet test`, `ng build`, `ng test`. Nhánh trong TaskLite: `ai/<JIRA-KEY>-<slug>`, mỗi lần chạy một worktree.

## 9. Tài liệu hệ thống mà plan yêu cầu

| File | Việc |
|---|---|
| `docs/architecture/` C4 (Mermaid), `workflow-states.md`, `context.md`, `multi-agent.md`, `testing.md`, `retry-policy.md`, `telemetry.md`, `slo.md`, `README.md` (arc42) | `s0b-5`, `s1-1`, `ctx-1`, `mag-1`, `s9-1`, `s8-3`, `s16-1`, `s17-1`, `arch-1` |
| `docs/security/` `llm-data-policy.md`, `runner.md`, `quality-gates.md`, `ai-usage-policy.md`, `threat-model.md` | `s2-6`, `s7-1`, `s10-1`, `s12-1`, `s15-1` |
| `docs/runbooks/` `model-change.md`, `rollback-rollforward.md`, `team-operating-procedure.md`, `incident-response.md`, 5 runbook phục hồi | `s2-7`, `s11-3`, `s12-4`, `s15-3`, `s17-3` |
| Bằng chứng: `docs/poc-evidence.md`, `docs/pilot-evidence.md`, `docs/production-readiness.md` | `s6-3`, `s12-4`, `s17-4` |
| Kết quả kiểm thử: `tests/evaluation/` (ba, tools, injection, security), `tests/security/runner.md`, `tests/reliability/queue.md` | `s2-4`, `s3-4`, `s4-3`, `s7-4`, `s8-5`, `s15-4` |

## 10. ADR mà plan yêu cầu

Đánh số `docs/decisions/0001-...` theo thứ tự chốt.

| ADR | Việc |
|---|---|
| n8n + .NET trước, LangGraph/Temporal khi có lý do | `s0b-5` |
| So sánh và chọn model | `s2-1` |
| Pattern agentic cho từng vai trò | `s4-6` |
| Agent implementation (SDK .NET hay tự viết) | `s4-4` |
| Retrieval strategy (code và tài liệu) | `ctx-4` |
| Coding agent cho AI Developer | `s5-5` |
| Một agent hay nhiều agent | `mag-3` |
| Loại hàng đợi (DB hay broker) | `s8-1` |
| LLM gateway (tự viết hay dùng sẵn) và bảng định tuyến | `gw-1`, `gw-4` |
| Chọn cloud | `s13-1` |
| Temporal | `s14-1` |
| LangGraph | `s14-2` |
| LLM observability | `s16-4` |
| Build hay mua cho 8 việc core | `s1-2`, `s2-2`, `s3-2`, `ctx-2`, `s4-5`, `s7-3`, `s8-2`, `gw-2` |

## 11. Cổng dịch vụ (đề xuất, chốt khi dựng `infra/docker`)

Tránh các cổng phase 1 đang dùng (5678, 8787, 4200, 5154, 4201, 5155).

| Dịch vụ | Cổng |
|---|---|
| TaskLite API / web | 5200 / 4300 |
| Workflow API | 5300 |
| agent-service | 5310 |
| runner | 5320 |
| docs-mcp (nếu chạy HTTP) | 5330 |
| PostgreSQL của nền tảng | 5433 |
| n8n của phase 2 | 5679 |
