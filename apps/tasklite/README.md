# TaskLite

TaskLite là một ứng dụng quản lý công việc tối giản cung cấp REST API và giao diện web tĩnh để thao tác với Project và Task.

## Yêu cầu cài đặt
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (hỗ trợ `npx`)

## Khởi chạy API
Chạy API từ thư mục `apps/tasklite` (TaskLite nằm trong repo nền tảng `build-workflows-phase2`):
```bash
dotnet run --project src/TaskLite.Api
```
API sẽ khởi động tại `http://localhost:5200`.

Lúc khởi động ở môi trường Development, cơ sở dữ liệu SQLite (`tasklite.db`) sẽ tự động được tạo và seed sẵn:
- Dự án mẫu: `Demo` (ID `1`)
- Người dùng mẫu: `alice`, `bob` (cả hai là thành viên của dự án `Demo`)
- 3 task mẫu với 3 trạng thái (`Todo`, `InProgress`, `Done`).

Health check endpoint:
```bash
curl http://localhost:5200/health
```

## Khởi chạy Web
Mở một terminal mới tại `apps/tasklite` và chạy:
```bash
npx --yes http-server web -p 4300 -c-1
```
Mở trình duyệt tại: `http://localhost:4300`

Giao diện cho phép chọn người dùng (mặc định `alice`), chọn dự án, xem danh sách task và tạo task mới.

## Thử nghiệm API với file `.http`
File `src/TaskLite.Api/TaskLite.Api.http` chứa sẵn các mẫu request:
- Tạo project, liệt kê project.
- Tạo task (có và không có assignee), liệt kê task.
- Cập nhật trạng thái task (`InProgress`, `Done`).
- Các kịch bản lỗi mẫu (401 thiếu `X-User-Id`, 403 không phải thành viên, 404 project không tồn tại, 400 validation error).

Bạn có thể chạy trực tiếp file này bằng REST Client trong VS Code / Visual Studio hoặc IDE tương thích.
