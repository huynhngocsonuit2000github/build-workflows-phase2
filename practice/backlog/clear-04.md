---
id: clear-04
type: clear
title: Thêm thành viên vào project
jira_issue_type: Story
---

## Description
Hiện tại các thành viên trong project chỉ được thiết lập một lần duy nhất lúc tạo project. Cần có endpoint cho phép bổ sung thêm thành viên vào project đang có để các nhân sự mới có thể tham gia và nhận việc.

Yêu cầu:
- Xây dựng endpoint `POST /projects/{projectId}/members`.
- Yêu cầu header `X-User-Id`. Người gửi yêu cầu phải là thành viên hiện tại của project (nếu không, trả 403 Forbidden).
- Body request: `{ "userId": "string" }`.
- Quy tắc kiểm tra tính hợp lệ:
  - `userId` không được để trống hoặc chỉ chứa khoảng trắng, độ dài từ 1 đến 50 ký tự.
  - Nếu `userId` đã tồn tại trong danh sách thành viên của project, trả về lỗi 400 `ValidationProblem` với thông báo user đã là thành viên.
- Khi thêm thành viên thành công:
  - Bổ sung user mới vào `MemberIds` của project và lưu database.
  - Trả về mã 200 OK kèm theo `ProjectResponse` với danh sách thành viên cập nhật.

Acceptance Criteria:
1. Thành viên hợp lệ có thể gọi POST để thêm user mới vào project thành công.
2. User mới sau khi được thêm có thể gọi `GET /projects` và thấy project này trong danh sách.
3. User mới có thể được gán vào trường `Assignee` khi tạo task.
4. Người ngoài project gọi API này bị trả về 403.
5. Thêm user rỗng hoặc trùng lặp bị trả về 400 ValidationProblem.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Bổ sung endpoint POST /projects/{projectId}/members nhận request { userId }"
  - "Chỉ thành viên hiện tại của project mới có quyền thêm thành viên khác (403 nếu không phải)"
  - "Validate userId: trimmed, không rỗng, tối đa 50 ký tự"
  - "Kiểm tra trùng lặp: nếu đã là thành viên trả 400 ValidationProblem"
  - "Cập nhật MemberIds trong cơ sở dữ liệu và trả về ProjectResponse cập nhật"
must_not:
  - "Không cho phép người ngoài project tự thêm mình hoặc thêm người khác vào project"
  - "Không lưu userId rỗng hoặc quá 50 ký tự vào collection MemberIds"
notes: "Cần chú ý cách EF Core cập nhật primitive collection JSON trong SQLite."
```
