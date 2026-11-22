# Workflow API

Nguồn sự thật về trạng thái một lần chạy (`WorkflowExecution`): luật chuyển trạng thái, lịch sử, optimistic concurrency. Thiết kế: `docs/architecture/workflow-states.md`.

## Cấu trúc

```
apps/workflow-api/
├── WorkflowApi.slnx
├── src/WorkflowApi.Domain/        # logic thuần, không DB: state machine, bảng chuyển, lịch sử
│   ├── WorkflowStateMachine.cs    # hàm Apply: chỗ DUY NHẤT đổi trạng thái
│   ├── TransitionTable.cs         # bảng chuyển hợp lệ (dữ liệu)
│   └── WorkflowExecution.cs       # thực thể (setter internal)
├── src/WorkflowApi.Api/           # ASP.NET minimal API + EF Core + PostgreSQL
│   ├── Endpoints/ExecutionEndpoints.cs
│   ├── Data/WorkflowDbContext.cs  # Version là concurrency token
│   └── Migrations/
└── tests/
    ├── WorkflowApi.Domain.Tests/  # test logic, không cần Docker
    └── WorkflowApi.Api.Tests/     # test API + PostgreSQL thật (Testcontainers, cần Docker)
```

## Chạy test

```bash
dotnet test WorkflowApi.slnx
```

`WorkflowApi.Api.Tests` tự khởi động một container PostgreSQL (Testcontainers), nên cần Docker đang chạy. Mỗi test dùng một database riêng.

## Chạy API ở máy

1. Bật PostgreSQL: trong `infra/docker/`, copy `.env.example` thành `.env`, đổi mật khẩu, rồi `docker compose up -d`.
2. Đặt chuỗi kết nối bằng user-secrets (không ghi vào file):
   ```bash
   cd apps/workflow-api/src/WorkflowApi.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=workflow;Username=workflow;Password=<mật khẩu trong .env>"
   dotnet run
   ```
   API chạy ở `http://localhost:5300` và tự áp migration lúc khởi động.

## Endpoint

| Method | Route | Ý nghĩa | Mã trả về |
|---|---|---|---|
| POST | `/executions` | Tạo lần chạy; header `Idempotency-Key` bắt buộc (xem bên dưới) | 201, 400, 422 |
| GET | `/executions?state=Failed&jiraIssueKey=TAS-5&limit=50` | Tìm lần chạy theo trạng thái / issue (mọi tham số tùy chọn; mới cập nhật trước; `limit` 1–200, mặc định 50) | 200, 400 |
| GET | `/executions/{id}` | Trạng thái hiện tại và `version` | 200, 404 |
| GET | `/executions/{id}/history` | Lịch sử theo thời gian | 200, 404 |
| POST | `/executions/{id}/transitions` | Chuyển trạng thái: `toState`, `actor`, `expectedVersion`, `reason` | 200, 400, 403, 404, 409, 422 |
| POST | `/executions/{id}/retry` | Retry lần chạy `Failed`: `actor` (Human, N8n), `expectedVersion`, `reason`. Không có `toState`: tự quay về `failedFromState`, cùng id, không tạo lần chạy mới | 200, 400, 403, 404, 409, 422 |

### Idempotency của `POST /executions`

| Tình huống | Kết quả |
|---|---|
| Key mới | Tạo lần chạy, 201 |
| Key đã dùng, cùng nội dung (đã cắt khoảng trắng) | Trả lại nguyên văn kết quả lần đầu, 201, header `Idempotency-Replayed: true`; không tạo thêm |
| Key đã dùng, khác nội dung | 422, `error = IdempotencyKeyReused` |
| Nhiều request cùng key đến cùng lúc | Unique index trên key cho một cái thắng; các cái khác nhận lại kết quả của nó |

Key nên có dạng `jira:<issue>:<id sự kiện>` để mỗi sự kiện Jira chỉ tạo một lần chạy.

Mã lỗi của chuyển trạng thái (trường `error` trong ProblemDetails):

| HTTP | `error` | Khi nào |
|---|---|---|
| 409 | `VersionConflict` | `expectedVersion` cũ, hoặc ai đó ghi trước (phát hiện ở DB) |
| 403 | `ActorNotAllowed` | Loại actor không được kích hoạt chuyển này |
| 422 | `NotAllowed`, `TerminalState`, `GuardFailed` | Chuyển không có trong bảng, đã ở trạng thái cuối, hết lượt vòng lặp |

## Bằng chứng bước 1 (`s1-5`)

Mỗi tiêu chí có một test đi qua API thật + PostgreSQL thật trong `tests/WorkflowApi.Api.Tests/Step1CriteriaTests.cs`:

```bash
dotnet test tests/WorkflowApi.Api.Tests --filter "FullyQualifiedName~Step1CriteriaTests"
```

| Tiêu chí | Test chính | Ca chi tiết hơn |
|---|---|---|
| 1. Trạng thái được lưu | `Criterion1_StateIsPersisted_ReadBackByIndependentDbConnection` (đọc lại bằng kết nối DB riêng, không qua API) | `GetExecution_Existing_ReturnsPersistedState`, `Transition_ValidStep_Returns200AndPersistsNewVersion` |
| 2. Chuyển sai bị từ chối | `Criterion2_InvalidTransitionsAreRejected_AndNothingIsWritten` (nhảy bước 422, sai người 403, sai version 409; trạng thái và lịch sử không đổi) | `Transition_*` trong `ExecutionEndpointsTests`, `ConcurrencyTests`, 101 test Domain |
| 3. Sự kiện trùng không tạo lần chạy trùng | `Criterion3_DuplicateEventsDoNotCreateDuplicateRuns` (5 request cùng lúc + 1 gửi lại, cùng key: 1 lần chạy) | `IdempotencyTests` |
| 4. Xem được lịch sử | `Criterion4_HistoryIsInspectable_InOrderWithActorAndReason` | `GetHistory_*` |
| 5. Lần chạy lỗi tìm được và retry an toàn | `Criterion5_FailedRunCanBeFoundAndSafelyRetried_WithoutCreatingANewRun` | `RetryAndListTests` (17 ca: 403, 409, 422, gửi trùng, 8 retry song song × 10 vòng) |

Kiểm tra test có bắt được lỗi thật không (mutation, 2026-11-22):

| Phá code thế này | Kết quả |
|---|---|
| Retry "ngây thơ": tạo lần chạy mới thay vì quay lại lần cũ | 10 test fail, gồm `Criterion5` và `Retry_Failed_DoesNotCreateNewRun` |
| Bỏ `IsConcurrencyToken()` trên `Version` | `Retry_ParallelSameVersion_ExactlyOneSucceeds` fail: 8/8 retry cùng thành công (mong đợi 1) |

Lỗi tạm thời và lỗi vĩnh viễn: hệ thống (`System`) đánh `Failed` kèm `reason`; người hoặc n8n quyết định retry. Retry chỉ hợp lý khi nguyên nhân là tạm thời (mạng, rate limit, nhà cung cấp sập); lỗi vĩnh viễn (ticket sai, hết lượt sửa) thì người xử lý hoặc `Cancelled`. Retry tự động có giới hạn số lần sẽ làm ở `s8` (hàng đợi).

## Giới hạn đã biết

- **Chưa xác thực:** `actor` nằm trong body nên ai cũng tự nhận là `Human` được. Phải thay bằng danh tính từ token của dịch vụ (service token) trước khi cho người dùng thật dùng (`s7-2`, `s12`). Đừng mở API này ra ngoài máy local khi chưa làm.
- Bản ghi idempotency chưa hết hạn (giữ mãi). Khi dữ liệu lớn thì thêm job dọn theo `CreatedAt`.
