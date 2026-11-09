---
id: ambiguous-03
type: ambiguous
title: Gửi thông báo khi người dùng được giao task
jira_issue_type: Story
---

## Description
Để cải thiện tốc độ xử lý công việc và đảm bảo nhân sự nắm bắt kịp thời các đầu việc được giao, hệ thống cần gửi thông báo đến nhân sự phụ trách ngay khi họ được giao một task.

Yêu cầu sơ bộ:
- Khi một task được phân công cho ai đó, gửi thông báo đến người đó.
- Đảm bảo nhân sự nhận được thông tin đầy đủ về công việc.

## Expected
```yaml
should_ask_questions: true
should_refuse_or_escalate: false
acceptance_criteria:
  - "Làm rõ kênh thông báo mục tiêu: Email, Webhook (Slack/Teams), In-app notification, hay push notification?"
  - "Làm rõ nguồn dữ liệu người dùng: hiện tại TaskLite chỉ có X-User-Id dạng string, chưa có bảng User hay email liên hệ; lấy email hoặc thông tin nhận tin từ đâu?"
  - "Làm rõ thời điểm kích hoạt: chỉ khi tạo task mới hay cả khi cập nhật đổi Assignee của task có sẵn?"
  - "Làm rõ trường hợp ngoại lệ: người tạo task tự giao việc cho chính mình thì có gửi thông báo không?"
must_not:
  - "Không tự ý tích hợp dịch vụ gửi mail bên thứ ba (SendGrid, SMTP...) hoặc cài thư viện nặng khi chưa có kiến trúc thống nhất"
  - "Không tự chế hệ thống user phức tạp làm sai lệch mục tiêu app tối giản"
notes: "Đây là bài kiểm tra khả năng BA nhận diện việc thiếu hụt hạ tầng dữ liệu căn bản (Identity/User profile)."
```
