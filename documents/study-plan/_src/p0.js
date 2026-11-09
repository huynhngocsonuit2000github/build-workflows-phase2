const MS = 'https://learn.microsoft.com';
module.exports = {
  id: 'p0', code: 'P0', color: '#6d5efc',
  title: ['Dự án thực hành tối giản (TaskLite)', 'A minimal practice project (TaskLite)'],
  goal: ['3–5 ngày · một ứng dụng nhỏ đủ để agent làm việc trên đó, kèm backlog ticket mẫu', '3–5 days · a small app the agents can work on, plus a backlog of practice tickets'],
  desc: ['Dự án chỉ là bãi tập, không phải sản phẩm. Giữ nó nhỏ: một API .NET với Project và Task, một trang web đơn giản, test và CI. Thời gian dành cho AI engineering, không dành cho tính năng. Dựng mới từ đầu trong một repo riêng, không tái sử dụng code của phase 1.', 'The project is a practice ground, not a product. Keep it small: a .NET API with Project and Task, a simple web page, tests and CI. Time goes into AI engineering, not features. Build it from scratch in its own repo, reusing no phase 1 code.'],
  milestones: [
    {
      id: 's0a', title: ['TaskLite: ứng dụng nhỏ làm bãi tập cho agent', 'TaskLite: a small app as the agents\' practice ground'],
      items: [
        {
          id: 's0a-1', kind: 'build', h: '3–4',
          t: ['Dựng TaskLite: .NET minimal API với Project và Task, EF Core + SQLite, phân quyền đơn giản bằng header', 'Build TaskLite: a .NET minimal API with Project and Task, EF Core + SQLite, simple header-based permissions'],
          goal: ['Có một API nhỏ, chạy được, đủ quy tắc nghiệp vụ để agent có việc thật để làm.', 'A small working API with enough business rules to give the agents real work.'],
          learn: [
            ['Chỉ 2 entity: Project (thành viên là danh sách user id) và Task (title, status, assignee, dueDate)', 'Only 2 entities: Project (members as a list of user ids) and Task (title, status, assignee, dueDate)'],
            ['Phân quyền giả lập bằng header `X-User-Id`: chỉ thành viên project mới đọc và tạo task, đủ để luyện các ticket về quyền', 'Fake auth via an `X-User-Id` header: only project members can read and create tasks, enough to practise permission tickets'],
            ['SQLite cho đơn giản; migration EF Core vẫn dùng để agent luyện thay đổi schema', 'SQLite for simplicity; EF Core migrations still used so agents can practise schema changes'],
            ['Cố tình để một vài chỗ chưa làm (phân trang, lọc, comment) làm việc cho agent sau này', 'Deliberately leave some gaps (pagination, filtering, comments) as future work for the agents'],
          ],
          ai: [['Claude Code', ['Dựng nhanh bộ khung', 'Scaffold it quickly'], ['Tạo repo `tasklite` với `src/TaskLite.Api` (ASP.NET Core minimal API, .NET 10), EF Core + SQLite, 2 entity Project và Task, migration ban đầu. Endpoint: tạo/xem project, tạo/xem/đổi trạng thái task trong project. Đọc user từ header `X-User-Id`; người không phải thành viên project nhận 403. Lỗi trả ProblemDetails. Chưa làm phân trang, lọc, comment.', 'Create a `tasklite` repo with `src/TaskLite.Api` (ASP.NET Core minimal API, .NET 10), EF Core + SQLite, the 2 entities Project and Task, an initial migration. Endpoints: create/view projects, create/view/change status of tasks in a project. Read the user from an `X-User-Id` header; non-members get 403. Errors as ProblemDetails. No pagination, filtering or comments yet.']]],
          res: [['ASP.NET Core minimal APIs', MS + '/aspnet/core/fundamentals/minimal-apis'], ['EF Core', MS + '/ef/core/']],
          pr: [
            ['Tạo repo, project API, 2 entity và migration', 'Create the repo, the API project, the 2 entities and the migration'],
            ['Thêm endpoint và kiểm tra quyền bằng header', 'Add the endpoints and the header-based permission check'],
            ['Thử bằng file `.http`', 'Try it with an `.http` file'],
          ],
          done: [
            ['Tạo được project, task và đổi trạng thái task qua API', 'Projects and tasks can be created and task status changed through the API'],
            ['Người không phải thành viên nhận 403', 'Non-members get 403'],
          ],
        },
        {
          id: 's0a-2', kind: 'build', h: '2–3',
          t: ['Viết backlog 12–15 ticket mẫu đủ loại để luyện agent suốt plan', 'Write a backlog of 12–15 practice tickets of every kind to train the agents throughout the plan'],
          goal: ['Có sẵn một bộ ticket cố định dùng lại cho mọi bước: luyện BA, Developer, Reviewer, QA và làm bộ đánh giá.', 'A fixed ticket set reused in every step: practising the BA, Developer, Reviewer and QA, and serving as an eval set.'],
          learn: [
            ['Một bộ ticket cố định giúp so sánh trước và sau mỗi thay đổi prompt, model hay kiến trúc', 'A fixed ticket set lets you compare before and after each prompt, model or architecture change'],
            ['Phân loại: rõ ràng, mơ hồ, mâu thuẫn, nhạy cảm bảo mật, chứa prompt injection, thay đổi schema', 'Categories: clear, ambiguous, conflicting, security-sensitive, containing prompt injection, schema-changing'],
            ['Mỗi ticket ghi kết quả mong đợi để chấm tự động được', 'Each ticket notes the expected outcome so it can be scored automatically'],
          ],
          ai: [['ChatGPT', ['Soạn backlog', 'Draft the backlog'], ['Ứng dụng TaskLite có Project và Task (title, status, assignee, dueDate), phân quyền theo thành viên project bằng header `X-User-Id`. Soạn 14 ticket Jira cho nó: 5 rõ ràng (ví dụ phân trang, lọc theo status, thêm priority), 3 mơ hồ, 2 mâu thuẫn nội tại, 2 nhạy cảm bảo mật (ví dụ cho người ngoài xem task), 1 chứa prompt injection trong mô tả, 1 cần đổi schema có dữ liệu cũ. Mỗi ticket: tiêu đề, mô tả, loại, và điều một hệ thống AI tốt phải làm.', 'TaskLite has Project and Task (title, status, assignee, dueDate) and project-membership permissions via an `X-User-Id` header. Write 14 Jira tickets for it: 5 clear (e.g. pagination, filter by status, add priority), 3 ambiguous, 2 self-contradictory, 2 security-sensitive (e.g. letting outsiders see tasks), 1 with prompt injection in the description, 1 needing a schema change with existing data. For each: title, description, category, and what a good AI system must do.']]],
          res: [['Jira workflows', 'https://support.atlassian.com/jira-cloud-administration/docs/manage-issue-workflows/']],
          pr: [
            ['Lưu backlog vào `practice/backlog/*.md` của repo nền tảng, mỗi ticket một file có phần `expected`. Không để trong repo TaskLite: agent làm việc ở đó không được thấy đáp án', 'Store the backlog in `practice/backlog/*.md` in the platform repo, one file per ticket with an `expected` section. Not in the TaskLite repo: agents working there must not see the answers'],
            ['Tạo các ticket đó trên Jira project thực hành', 'Create those tickets in the practice Jira project'],
          ],
          done: [
            ['Có 12–15 ticket thuộc đủ 6 loại, mỗi ticket có kết quả mong đợi', 'There are 12–15 tickets covering all 6 categories, each with an expected outcome'],
          ],
        },
        {
          id: 's0a-3', kind: 'build', h: '1–2',
          t: ['Thêm một trang web đơn giản liệt kê và tạo task (hoặc chỉ dùng Swagger)', 'Add one simple web page to list and create tasks (or just use Swagger)'],
          goal: ['Có một giao diện tối thiểu để agent luyện sửa frontend và để chạy được một test E2E.', 'A minimal UI for agents to practise frontend changes and to run one E2E test.'],
          learn: [
            ['Giữ tối giản: một trang, một form; không cần router hay state management', 'Keep it minimal: one page, one form; no router or state management'],
            ['Địa chỉ API lấy từ cấu hình lúc chạy, không hard-code (bài học phase 1)', 'Read the API address from runtime configuration, not hard-coded (a phase 1 lesson)'],
          ],
          ai: [['Claude Code', ['Dựng trang nhanh', 'Build the page quickly'], ['Thêm `web/` là ứng dụng Angular một trang (hoặc HTML + fetch thuần nếu muốn nhẹ hơn): chọn project, liệt kê task, form tạo task, hiện lỗi validation từ API. Địa chỉ API đọc từ `config.json` lúc chạy.', 'Add `web/` as a one-page Angular app (or plain HTML + fetch for something lighter): pick a project, list tasks, a create-task form, show API validation errors. The API address comes from `config.json` at runtime.']]],
          res: [['Angular', 'https://angular.dev/']],
          pr: [
            ['Dựng trang và nối với API', 'Build the page and connect it to the API'],
          ],
          done: [
            ['Tạo được task từ trang web và thấy nó trong danh sách', 'A task created from the page shows up in the list'],
          ],
        },
      ],
    },
    {
      id: 's0b', title: ['Chuẩn tối thiểu để agent làm việc an toàn: test, container, CI', 'The minimum for agents to work safely: tests, containers, CI'],
      items: [
        {
          id: 's0b-1', kind: 'build', h: '2',
          t: ['Viết unit test và test tích hợp API để có tín hiệu đúng/sai cho agent', 'Write unit and API integration tests so agents get a right/wrong signal'],
          goal: ['Agent sửa sai thì test đỏ; đây là bằng chứng khách quan mà nền tảng dựa vào.', 'When an agent breaks something, tests go red; this is the objective evidence the platform relies on.'],
          learn: [
            ['Test là "trọng tài" của nền tảng AI: không có test thì không kiểm chứng được agent', 'Tests are the AI platform\'s referee: without them agents cannot be verified'],
            ['`WebApplicationFactory` và database SQLite riêng cho mỗi test', '`WebApplicationFactory` and a separate SQLite database per test'],
          ],
          ai: [['Claude Code', ['Dựng test', 'Set up tests'], ['Thêm `tests/TaskLite.Tests` dùng xUnit và `WebApplicationFactory`, mỗi test một file SQLite tạm. Viết test cho: tạo task thành công, thiếu title trả 400, người ngoài project trả 403, đổi trạng thái hợp lệ và không hợp lệ.', 'Add `tests/TaskLite.Tests` using xUnit and `WebApplicationFactory`, with a temporary SQLite file per test. Test: task created, missing title returns 400, non-member returns 403, valid and invalid status changes.']]],
          res: [['.NET testing', MS + '/dotnet/core/testing/']],
          pr: [
            ['Tạo project test và các test ở trên', 'Create the test project and the tests above'],
            ['Chạy `dotnet test` hai lần liên tiếp, kết quả như nhau', 'Run `dotnet test` twice in a row and get the same result'],
          ],
          done: [
            ['`dotnet test` chạy xanh và ổn định', '`dotnet test` passes and is stable'],
          ],
        },
        {
          id: 's0b-2', kind: 'build', h: '1–2',
          t: ['Đóng gói TaskLite bằng Dockerfile và Docker Compose', 'Package TaskLite with a Dockerfile and Docker Compose'],
          goal: ['Một lệnh `docker compose up` chạy API và trang web; runner sau này cũng build được trong container.', 'One `docker compose up` runs the API and web page; the runner can later build it in a container too.'],
          learn: [
            ['Multi-stage build cho image nhỏ', 'Multi-stage builds for small images'],
            ['Healthcheck để bước smoke test sau này có tín hiệu', 'A healthcheck so later smoke tests have a signal'],
          ],
          ai: [['Claude Code', ['Viết Dockerfile và Compose', 'Write the Dockerfile and Compose'], ['Viết Dockerfile multi-stage cho `TaskLite.Api` (có endpoint `/health`), Dockerfile cho `web/` phục vụ bằng nginx với `config.json` lúc chạy, và `docker-compose.yml` chạy cả hai. Migration SQLite chạy khi API khởi động là đủ cho dự án thực hành.', 'Write a multi-stage Dockerfile for `TaskLite.Api` (with a `/health` endpoint), a Dockerfile for `web/` served by nginx with runtime `config.json`, and a `docker-compose.yml` running both. Running SQLite migrations at API startup is fine for a practice project.']]],
          res: [['Docker docs', 'https://docs.docker.com/']],
          pr: [
            ['Viết Dockerfile và Compose', 'Write the Dockerfiles and Compose'],
            ['`docker compose up` từ máy sạch', '`docker compose up` from a clean machine'],
          ],
          done: [
            ['`docker compose up` cho ứng dụng dùng được, `/health` trả ok', '`docker compose up` gives a usable app and `/health` returns ok'],
          ],
        },
        {
          id: 's0b-3', kind: 'build', h: '1–2',
          t: ['Thêm GitHub Actions build và test, bật branch protection cho main', 'Add GitHub Actions build and test, and protect main'],
          goal: ['Mọi PR (kể cả do agent mở) được build và test tự động; CI đỏ thì không merge được.', 'Every PR (including agent-opened ones) is built and tested automatically; red CI blocks the merge.'],
          learn: [
            ['Required status checks; kết quả test lưu làm artifact để làm bằng chứng', 'Required status checks; test results kept as artifacts for evidence'],
          ],
          ai: [['Claude Code', ['Viết workflow CI', 'Write the CI workflow'], ['Tạo `.github/workflows/ci.yml` chạy `dotnet build`, `dotnet test` (và build `web/` nếu có), upload kết quả test dạng TRX làm artifact, chạy trên pull_request và push vào main.', 'Create `.github/workflows/ci.yml` running `dotnet build`, `dotnet test` (and building `web/` if present), uploading TRX test results as artifacts, on pull_request and push to main.']]],
          res: [['GitHub Actions', 'https://docs.github.com/actions']],
          pr: [
            ['Viết workflow CI', 'Write the CI workflow'],
            ['Bật branch protection: bắt buộc PR và CI xanh', 'Protect main: require a PR and green CI'],
          ],
          done: [
            ['PR có test hỏng không merge được', 'A PR with a failing test cannot be merged'],
          ],
        },
        {
          id: 's0b-4', kind: 'gate', h: '1',
          t: ['Chốt P0: bãi tập sẵn sàng cho agent', 'Close P0: the practice ground is ready for agents'],
          goal: ['Từ đây toàn bộ thời gian dành cho AI engineering.', 'From here on all the time goes to AI engineering.'],
          learn: [
            ['Không thêm tính năng cho TaskLite bằng tay nữa: tính năng mới đến từ backlog, do agent làm', 'No more hand-written TaskLite features: new features come from the backlog, built by the agents'],
          ],
          ai: [['ChatGPT', ['Rà README như người mới', 'Review the README as a newcomer'], ['Đọc README này như một người mới: <dán README>. Liệt kê mọi chỗ bạn sẽ bị kẹt khi clone, chạy, test lần đầu.', 'Read this README as a newcomer: <paste README>. List every place you would get stuck cloning, running and testing it for the first time.']]],
          res: [['GitHub Actions', 'https://docs.github.com/actions']],
          pr: [
            ['Viết README ngắn: chạy, test, cấu trúc', 'Write a short README: run, test, layout'],
            ['Kiểm tra lại backlog đã có trên Jira', 'Check the backlog is in Jira'],
          ],
          done: [
            ['Ứng dụng chạy bằng Compose, CI xanh, test ổn định', 'The app runs with Compose, CI is green, tests are stable'],
            ['Backlog ticket mẫu đã sẵn trên Jira', 'The practice ticket backlog is in Jira'],
          ],
        },
      ],
    },
  ],
};
