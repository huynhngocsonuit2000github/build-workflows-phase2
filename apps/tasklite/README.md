# TaskLite

TaskLite là một ứng dụng quản lý công việc tối giản cung cấp REST API và giao diện web tĩnh để thao tác với Project và Task.

## Yêu cầu cài đặt
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (hỗ trợ `npx`, nếu chạy web cục bộ không qua Docker)
- [Docker & Docker Compose](https://www.docker.com/) (nếu chạy container hóa)

---

## 1. Khởi chạy cục bộ (Local Development)

### Khởi chạy API
Chạy API từ thư mục `apps/tasklite`:
```bash
dotnet run --project src/TaskLite.Api
```
API sẽ khởi động tại `http://localhost:5200`.

Lúc khởi động ở môi trường Development (hoặc khi bật cấu hình `Seed:DemoData=true`), database SQLite sẽ tự động được áp dụng migration và seed dữ liệu mẫu:
- Dự án mẫu: `Demo` (ID `1`)
- Người dùng mẫu: `alice`, `bob` (thành viên của dự án `Demo`)
- 3 task mẫu với các trạng thái `Todo`, `InProgress`, `Done`.

Health check endpoint:
```bash
curl http://localhost:5200/health
```

### Khởi chạy Web
Mở một terminal mới tại thư mục `apps/tasklite`:
```bash
npx --yes http-server web -p 4300 -c-1
```
Mở trình duyệt tại: `http://localhost:4300`. Giao diện cho phép chọn người dùng (mặc định `alice`), chọn dự án, xem danh sách task và tạo task mới.

---

## 2. Kiểm thử tự động (Automated Integration Tests)

Dự án kiểm thử `tests/TaskLite.Tests` sử dụng xUnit và `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`). Mỗi instance test sử dụng một database SQLite riêng biệt trong thư mục temp và tự động dọn dẹp khi dispose, đảm bảo tính cô lập hoàn toàn giữa các test.

Chạy toàn bộ test suite:
```bash
dotnet test TaskLite.slnx
```

Chạy với logger TRX và xuất kết quả:
```bash
dotnet test TaskLite.slnx -c Release --logger trx --results-directory TestResults
```

Bộ test bao gồm 34 ca kiểm thử kiểm tra:
- Health check endpoint (`/health`).
- Đầy đủ luồng thành công và lỗi của Project endpoints (tạo project, danh sách, chi tiết, 401/403/404, trùng tên).
- Đầy đủ luồng của Task endpoints (tạo task, danh sách, cập nhật trạng thái, 401/403/404/400).
- Regression tests cho 3 lỗi được khắc phục trong mốc `s0b`:
  - So sánh `X-User-Id` theo thứ tự byte chuẩn (Ordinal - phân biệt hoa thường).
  - Từ chối trạng thái task dạng số (ví dụ `"1"`, `"0"`, `"99"`).
  - Chuẩn hóa UTC và hậu tố `Z` trên trường `createdAt` của SQLite.

---

## 3. Khởi chạy với Docker & Docker Compose

Dự án cung cấp cấu hình Dockerfile tối ưu và Docker Compose:
- **API Container**: Multi-stage build với .NET 10 SDK, runtime image `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`, chạy với user không có quyền root (`$APP_UID`), lắng nghe cổng `8080` (được map ra host `5200`). Có healthcheck kiểm tra endpoint `/health`.
- **Web Container**: Chạy trên nền `nginx:alpine`, lắng nghe cổng `80` (được map ra host `4300`).
- **Volume Persistence**: Volume `tasklite-data` được gắn vào `/data` của API container để lưu trữ file database SQLite `tasklite.db`. Dữ liệu các project và task đã tạo sẽ được bảo toàn nguyên vẹn kể cả khi container bị dừng hoặc tái tạo (`docker compose down` rồi `docker compose up -d`).
- **Runtime Configuration**: File `web/config.json` được mount runtime vào Nginx container (`./web/config.json:/usr/share/nginx/html/config.json:ro`). Người dùng có thể thay đổi địa chỉ backend API mà không cần build lại Docker image của web.

### Khởi động hệ thống:
```bash
docker compose up -d --build
```

### Kiểm tra trạng thái:
```bash
docker compose ps
```
Khi API khởi động xong và vượt qua healthcheck, trạng thái của `api` sẽ hiển thị `healthy`, và container `web` (phụ thuộc vào `api` với điều kiện `service_healthy`) sẽ bắt đầu phục vụ traffic.

### Dừng hệ thống:
```bash
docker compose down
```

---

## 4. Continuous Integration (CI)

Workflow GitHub Actions được định nghĩa tại `.github/workflows/tasklite-ci.yml`, tự động kích hoạt khi có `push` hoặc `pull_request` vào nhánh chính và có thay đổi trong `apps/tasklite/**` hoặc file cấu hình CI:
1. **Job `build-test`**: Cài đặt .NET 10, restore dependencies, build toàn bộ solution với cờ cảnh báo nghiêm ngặt `-warnaserror`, thực thi toàn bộ unit/integration test, và lưu trữ artifact `test-results` (file `.trx`).
2. **Job `docker-build`**: Phụ thuộc vào `build-test`, tự động xác thực khả năng build thành công Docker image cho cả API và Web.

---

## 5. Thử nghiệm API với file `.http`
File `src/TaskLite.Api/TaskLite.Api.http` chứa sẵn các mẫu request:
- Tạo project, liệt kê project.
- Tạo task (có và không có assignee), liệt kê task.
- Cập nhật trạng thái task (`InProgress`, `Done`).
- Các kịch bản lỗi mẫu (401 thiếu `X-User-Id`, 403 không phải thành viên, 404 project không tồn tại, 400 validation error).
