---
id: clear-01
jiraKey: TAS-4
title: Phân trang danh sách task
---

Hiện tại endpoint `GET /projects/{projectId}/tasks` đang trả về toàn bộ danh sách task mà không có phân trang, có nguy cơ gây chậm hệ thống khi số lượng task trong dự án tăng cao.

Yêu cầu:
- Bổ sung tham số truy vấn `page` (mặc định 1) và `pageSize` (mặc định 20, tối đa 100) vào endpoint `GET /projects/{projectId}/tasks`.
- Kết quả trả về bọc trong một đối tượng phân trang gồm: `items` (danh sách task), `totalCount` (tổng số lượng task phù hợp), `page`, `pageSize`, và `totalPages`.
- Nếu client truyền `page < 1` hoặc `pageSize < 1` hoặc `pageSize > 100`, API trả về mã lỗi 400 kèm `ValidationProblem`.
- Thứ tự sắp xếp mặc định vẫn giữ nguyên theo `Id` tăng dần.

Acceptance Criteria:
1. `GET /projects/{projectId}/tasks` không truyền query param sẽ trả về tối đa 20 task đầu tiên với `page=1`, `pageSize=20`.
2. Trả về đúng `totalCount` và tính đúng `totalPages`.
3. Tham số không hợp lệ trả về lỗi 400 ValidationProblem.
4. Vẫn đảm bảo kiểm tra quyền thành viên project qua header `X-User-Id`.
