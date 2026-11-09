# Backlog Ticket Tập Huấn AI Agent

Thư mục này chứa 14 ticket mẫu dùng để huấn luyện và đánh giá năng lực các AI agent (BA, Developer, Reviewer, QA) trong dự án TaskLite.

> [!WARNING]
> **AI agent tuyệt đối không được đọc thư mục này!**
> Các file trong thư mục này chứa phần đáp án chuẩn (`## Expected`) dùng để chấm điểm agent. TaskLite nằm chung repo (`apps/tasklite/`), nên worktree hoặc runner của agent phải chỉ chứa `apps/tasklite/`, và không copy thư mục này vào đó.

## 1. Phân loại 6 nhóm ticket

| Loại (Slug) | Số lượng | Ý nghĩa & Mục đích đánh giá |
|---|---|---|
| `clear` | 5 | Yêu cầu rõ ràng, đầy đủ ngữ cảnh và tiêu chí nghiệm thu (AC). Đánh giá khả năng hiểu đúng và triển khai chuẩn xác không sót chi tiết. |
| `ambiguous` | 3 | Yêu cầu mơ hồ, thiếu thông tin kỹ thuật hoặc nghiệp vụ quyết định. Đánh giá khả năng đặt câu hỏi làm rõ của agent thay vì tự ý giả định. |
| `conflicting` | 2 | Yêu cầu chứa các điểm mâu thuẫn nội tại (ví dụ: mô tả một đằng, AC một nẻo). Đánh giá khả năng phản biện và phát hiện lỗi spec. |
| `security` | 2 | Yêu cầu vi phạm nguyên tắc an toàn hoặc phá vỡ ranh giới bảo mật để đổi lấy sự tiện lợi. Đánh giá năng lực phát hiện lỗ hổng và từ chối/leo thang (escalate). |
| `injection` | 1 | Yêu cầu thông thường nhưng có chèn câu lệnh prompt injection độc hại. Đánh giá khả năng phòng thủ và miễn nhiễm của agent. |
| `schema` | 1 | Yêu cầu thay đổi cấu trúc dữ liệu khi đã có dữ liệu thực tế. Đánh giá khả năng viết migration giữ nguyên dữ liệu và cung cấp kịch bản rollback. |

## 2. Cấu trúc mỗi file ticket

Mỗi file ticket có định dạng:
```markdown
---
id: <slug>-<số>
type: <slug>
title: <Tiêu đề ticket>
jira_issue_type: <Story | Task | Bug>
---

## Description
<!-- Nội dung mô tả nghiệp vụ của ticket - ĐƯỢC PHÉP đưa lên Jira -->

## Expected
<!-- Đáp án chấm điểm agent (should_ask_questions, acceptance_criteria, must_not, notes) - KHÔNG ĐƯA LÊN JIRA -->
```

## 3. Nhập dữ liệu lên Jira

Sử dụng file `jira-import.csv` trong thư mục này để import toàn bộ 14 ticket lên Jira. File CSV này chỉ chứa `## Description`, tiêu đề, issue type và label, hoàn toàn không chứa phần `## Expected`.
