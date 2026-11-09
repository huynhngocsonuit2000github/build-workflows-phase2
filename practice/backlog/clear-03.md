---
id: clear-03
type: clear
title: Thêm mức độ ưu tiên (Priority) cho task
jira_issue_type: Story
---

## Description
Để hỗ trợ người dùng phân loại và ưu tiên công việc quan trọng, hệ thống cần bổ sung thuộc tính mức độ ưu tiên (`Priority`) cho từng task.

Yêu cầu:
- Bổ sung enum `TaskPriority` với 3 giá trị: `Low`, `Medium`, `High`.
- Thêm thuộc tính `Priority` vào entity `TaskItem`, lưu dạng string trong database, giá trị mặc định là `Medium`.
- Cập nhật DTO `CreateTaskRequest` cho phép client truyền `priority` tùy chọn. Nếu không truyền, mặc định nhận giá trị `Medium`.
- Cập nhật DTO `TaskResponse` để trả về trường `priority` dạng chuỗi (`"Low"`, `"Medium"`, `"High"`).
- Tạo migration EF Core bổ sung cột `Priority` vào bảng `Tasks` với giá trị mặc định là `"Medium"` cho các bản ghi hiện có.
- Nếu client truyền giá trị `priority` không hợp lệ trong body tạo task, API trả về 400 `ValidationProblem`.

Acceptance Criteria:
1. Tạo task không truyền `priority` sẽ có `priority = "Medium"`.
2. Tạo task với `priority = "High"` hoặc `"Low"` lưu đúng giá trị đã chọn.
3. Tạo task với giá trị sai như `"Urgent"` trả về 400 ValidationProblem.
4. `TaskResponse` có hiển thị trường `priority`.
5. Dữ liệu cũ trong DB sau migration đều có `Priority = "Medium"`.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Tạo enum TaskPriority (Low, Medium, High) và cấu hình lưu string trong EF Core"
  - "Thêm trường Priority vào TaskItem với mặc định là TaskPriority.Medium"
  - "Cập nhật CreateTaskRequest và TaskResponse để hỗ trợ Priority"
  - "Viết migration EF Core thêm cột Priority với default value 'Medium'"
  - "Validate giá trị Priority khi tạo task, trả 400 ValidationProblem nếu sai"
must_not:
  - "Không được làm mất dữ liệu task hiện có khi chạy migration"
  - "Không cho phép giá trị Priority tùy tiện ngoài 3 mức đã quy định"
notes: "Cần đảm bảo JsonStringEnumConverter áp dụng cho TaskPriority trong response DTO."
```
