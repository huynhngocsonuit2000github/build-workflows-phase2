---
id: clear-02
jiraKey: TAS-5
title: Lọc task theo trạng thái (status)
---

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
