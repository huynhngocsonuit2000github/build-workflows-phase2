---
id: clear-02
type: clear
title: Lọc task theo trạng thái (status)
jira_issue_type: Story
---

## Description
Người dùng cần khả năng lọc các công việc theo trạng thái để tập trung vào những việc cần làm hoặc đang thực hiện mà không bị che khuất bởi các việc đã hoàn thành.

Yêu cầu:
- Bổ sung tham số truy vấn tùy chọn `status` vào endpoint `GET /projects/{projectId}/tasks`.
- Giá trị hợp lệ của `status`: `Todo`, `InProgress`, `Done` (cho phép case-insensitive).
- Nếu không truyền `status`, API tiếp tục trả về toàn bộ task thuộc mọi trạng thái như hiện tại.
- Nếu truyền giá trị `status` không nằm trong danh sách enum hợp lệ, trả về lỗi 400 `ValidationProblem` chỉ rõ trường `status` không hợp lệ.

Acceptance Criteria:
1. `GET /projects/{projectId}/tasks?status=Todo` chỉ trả về danh sách các task có trạng thái `Todo`.
2. `GET /projects/{projectId}/tasks?status=InProgress` và `Done` hoạt động tương ứng.
3. Không truyền `status` trả về task của tất cả trạng thái.
4. Truyền giá trị lạ như `status=Archived` trả về 400 ValidationProblem.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Hỗ trợ query parameter tùy chọn status trong GET /projects/{projectId}/tasks"
  - "Lọc chính xác theo TaskItemStatus (Todo, InProgress, Done), hỗ trợ không phân biệt hoa thường khi parse"
  - "Trả về 400 ValidationProblem nếu giá trị status không thuộc enum hợp lệ"
  - "Khi không truyền query status thì giữ nguyên hành vi trả về tất cả task"
  - "Bảo toàn kiểm tra phân quyền thành viên project (401, 403, 404)"
must_not:
  - "Không được trả về kết quả rỗng âm thầm khi status không hợp lệ (phải báo lỗi 400)"
  - "Không được bỏ qua kiểm tra quyền thành viên của user hiện tại"
notes: "Kiểm tra kỹ việc chuyển đổi string sang enum TaskItemStatus trong endpoint minimal API."
```
