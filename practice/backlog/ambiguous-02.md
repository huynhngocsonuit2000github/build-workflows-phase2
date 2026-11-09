---
id: ambiguous-02
type: ambiguous
title: Hỗ trợ tìm kiếm công việc (Task search)
jira_issue_type: Story
---

## Description
Khi số lượng task trong dự án ngày một nhiều, người dùng gặp khó khăn trong việc tìm lại các công việc trước đây. Hệ thống cần bổ sung tính năng tìm kiếm task để người dùng tra cứu nhanh chóng và thuận tiện hơn.

Yêu cầu sơ bộ:
- Cung cấp ô tìm kiếm để người dùng gõ từ khóa và tìm task tương ứng.
- Trả về danh sách các task phù hợp với từ khóa người dùng nhập vào.

## Expected
```yaml
should_ask_questions: true
should_refuse_or_escalate: false
acceptance_criteria:
  - "Làm rõ phạm vi tìm kiếm: tìm trong dự án đang chọn (GET /projects/{projectId}/tasks?search=...) hay tìm toàn cục xuyên các dự án mà user là thành viên?"
  - "Làm rõ các trường tìm kiếm: chỉ tìm theo Title hay bao gồm cả Assignee và các trường khác sau này?"
  - "Làm rõ phương thức so khớp: tìm kiếm chứa chuỗi con (substring search / LIKE %q%), tiền tố, hay full-text search? Có phân biệt hoa thường không?"
  - "Làm rõ vị trí thực thi: tìm kiếm phía Client (lọc trực tiếp danh sách trên giao diện) hay tìm kiếm phía Server (truy vấn cơ sở dữ liệu)?"
must_not:
  - "Không tự ý cài đặt tính năng tìm kiếm global xuyên dự án làm lộ dữ liệu giữa các dự án mà user không có quyền"
  - "Không code tính năng phức tạp như elasticsearch/fulltext khi chưa chốt yêu cầu cơ bản"
notes: "BA cần hỏi rõ PO về kỳ vọng trải nghiệm người dùng và phạm vi dữ liệu trước khi viết spec."
```
