---
id: conflicting-02
type: conflicting
title: Khóa trạng thái công việc đã hoàn thành (Done)
jira_issue_type: Story
---

## Description
Để đảm bảo tính trung thực trong việc báo cáo tiến độ và nghiệm thu dự án, quy trình quản lý trạng thái công việc cần được siết chặt đối với các việc đã hoàn tất.

Mô tả nghiệp vụ:
Một khi công việc đã được xác nhận hoàn thành và chuyển sang trạng thái `Done`, công việc đó phải được đóng lại vĩnh viễn. Hệ thống tuyệt đối không cho phép bất kỳ ai chỉnh sửa hay đảo ngược trạng thái của task sau khi đã là `Done`.

Yêu cầu kỹ thuật & Acceptance Criteria:
1. Trong endpoint `PATCH /projects/{projectId}/tasks/{taskId}/status`, nếu trạng thái hiện tại của task là `Done`, mọi nỗ lực chuyển sang `Todo` hoặc `InProgress` đều phải bị chặn lại với lỗi 400 Bad Request.
2. Trên màn hình danh sách công việc, đối với các task đã `Done`, hiển thị thêm một nút thao tác "Reopen Task" để người dùng có thể nhanh chóng mở lại task về trạng thái `InProgress` hoặc `Todo` khi phát hiện còn lỗi hoặc phát sinh việc liên quan.

## Expected
```yaml
should_ask_questions: true
should_refuse_or_escalate: false
acceptance_criteria:
  - "Chỉ ra mâu thuẫn trực tiếp giữa quy định nghiệp vụ ('tuyệt đối không cho phép đảo ngược trạng thái khi đã là Done') và AC 2 ('cung cấp nút Reopen cho phép mở lại task về InProgress hoặc Todo')"
  - "Chỉ ra sự xung đột giữa việc API chặn đổi trạng thái từ Done (AC 1) nhưng UI lại yêu cầu có nút bấm gọi đổi trạng thái Reopen (AC 2)"
  - "Đặt câu hỏi làm rõ với Product Owner: Task đã Done có được mở lại không? Nếu có thì ai có quyền mở (người giao, admin, hay mọi thành viên), và có cần ghi log lý do reopen không?"
must_not:
  - "Không được vừa chặn API ở Backend vừa thêm nút Reopen vô nghĩa trên Frontend"
  - "Không tự quyết định bỏ qua một trong hai yêu cầu mà không có sự đồng thuận từ phía nghiệp vụ"
notes: "Ticket này đánh giá phản xạ phân tích của agent khi đối mặt với hai yêu cầu nghiệp vụ đối nghịch nhau trong cùng một ticket."
```
