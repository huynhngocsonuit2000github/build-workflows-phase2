---
id: schema-01
type: schema
title: Chuyển đổi người phụ trách task sang danh sách nhiều người (AssigneeIds)
jira_issue_type: Task
---

## Description
Hiện tại hệ thống chỉ cho phép gán một người phụ trách duy nhất cho mỗi công việc thông qua thuộc tính `Assignee` (`string?`). Trong thực tế vận hành dự án, nhiều đầu việc đòi hỏi sự phối hợp xử lý của từ hai thành viên trở lên.

Yêu cầu thay đổi cấu trúc dữ liệu:
- Thay thế thuộc tính `Assignee` (`string?`) trong entity `TaskItem` bằng `AssigneeIds` (`List<string>`), lưu trữ dưới dạng primitive collection JSON trong cơ sở dữ liệu SQLite (tương tự trường `MemberIds` của `Project`).
- Cập nhật DTO `CreateTaskRequest` để nhận mảng `assigneeIds` (`List<string>?`). Nếu có truyền, từng `userId` phải tồn tại trong `MemberIds` của dự án và không được rỗng.
- Cập nhật DTO `TaskResponse` để trả về danh sách `assigneeIds`.
- Thực hiện migration EF Core an toàn:
  - Giữ lại toàn bộ dữ liệu đang có: các task đã có `Assignee = "bob"` phải được chuyển đổi thành `["bob"]`, các task chưa có người phụ trách (`Assignee = null`) chuyển thành danh sách rỗng `[]`.
  - Không được xóa trắng dữ liệu cột cũ khi chưa hoàn tất việc sao chép và chuyển đổi dữ liệu.
- Kiểm tra tính tương thích ngược và đảm bảo ứng dụng khởi động thành công với cơ sở dữ liệu đã có dữ liệu trước đó.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Chuyển đổi kiểu dữ liệu TaskItem.Assignee (string?) sang TaskItem.AssigneeIds (List<string>) cấu hình primitive collection JSON"
  - "Cập nhật CreateTaskRequest và TaskResponse tương ứng, cập nhật validation để xác thực mọi assigneeId đều thuộc project"
  - "Tạo EF Core migration tùy biến có lệnh SQL sao chép dữ liệu từ cột Assignee cũ sang AssigneeIds mới trước khi drop cột cũ"
  - "Bảo toàn 100% dữ liệu của các task hiện có trong cơ sở dữ liệu sau khi chạy migration"
must_not:
  - "Không được tạo migration tự động dạng drop column rồi add column gây mất trắng dữ liệu Assignee đang có"
  - "Không được cho phép gán AssigneeIds chứa user không phải thành viên của project"
notes: "Yêu cầu bắt buộc: Migration phải có kịch bản giữ nguyên dữ liệu cũ trong phương thức Up() và phải cung cấp phương thức Down() để hỗ trợ rollback quay lui an toàn nếu cần khôi phục lại schema cũ."
```
