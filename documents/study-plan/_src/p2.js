const MS = 'https://learn.microsoft.com';
const GH = 'https://docs.github.com';
module.exports = {
  id: 'p2', code: 'P2', color: '#f59e0b',
  title: ['Team Pilot: độ tin cậy, chạy đồng thời, QA và bảo mật', 'Team Pilot: reliability, concurrency, QA and security'],
  goal: ['6–8 tuần · một nhóm nhỏ dùng được trên repo và việc rủi ro thấp', '6–8 weeks · a small team uses it on selected repos and low-risk tasks'],
  desc: ['Runner ở phase 1 chạy thẳng trên máy bạn với quyền của bạn. Ở đây runner trở thành service cách ly, việc chạy qua hàng đợi có lease, CI có cổng bảo mật, và duyệt PR tuân theo branch protection thật (không giữ lời duyệt cũ khi có commit mới).', 'The phase 1 runner ran directly on your machine with your rights. Here the runner becomes an isolated service, work goes through a leased job queue, CI gets security gates, and PR approval follows real branch protection (no keeping an old approval after a new push).'],
  milestones: [
    {
      id: 's7', title: ['Kiến trúc runner và cách ly', 'Runner architecture and isolation'],
      items: [
        {
          id: 's7-1', kind: 'learn', h: '2–3',
          t: ['Học mô hình job, cách ly tiến trình và container, môi trường dùng-một-lần và least privilege', 'Learn job models, process and container isolation, ephemeral environments and least privilege'],
          goal: ['Liệt kê được các đường mà code không tin cậy có thể thoát ra khỏi runner và cách chặn từng đường.', 'You can list the ways untrusted code could escape the runner and how to block each one.'],
          learn: [
            ['Vì sao không chạy code không tin cậy trên máy điều phối', 'Why untrusted code must not run on the orchestration host'],
            ['Không mount Docker socket cho agent: có socket là có quyền root trên host', 'Never mount the Docker socket for the agent: socket access is root on the host'],
            ['Giới hạn CPU, bộ nhớ, thời gian, mạng ra ngoài và filesystem', 'Limits on CPU, memory, time, outbound network and filesystem'],
            ['Bí mật ngắn hạn, phạm vi hẹp; không có bí mật production trong workspace', 'Short-lived, narrowly scoped secrets; no production secrets in the workspace'],
          ],
          ai: [['Claude', ['Mô hình hóa mối đe dọa runner', 'Threat-model the runner'], ['Runner ở phase 1 của tôi là một server Node chạy trên máy cá nhân, gọi git, CLI agent, dotnet, npm và docker với quyền của tôi, token GitHub nằm trong file `.env`. Hãy liệt kê các mối đe dọa khi agent chạy code từ repo (đọc token, gọi mạng ra ngoài, dùng docker để chiếm host, chiếm hết CPU) và biện pháp cho từng cái trong một runner dạng container dùng-một-lần.', 'My phase 1 runner is a Node server on my personal machine that calls git, the agent CLI, dotnet, npm and docker with my rights, with GitHub tokens in a `.env` file. List the threats when the agent runs repo code (reading tokens, outbound calls, using docker to take over the host, exhausting CPU) and the countermeasure for each in an ephemeral container runner.']]],
          res: [['Docker security', 'https://docs.docker.com/engine/security/'], ['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Viết bảng mối đe dọa và biện pháp cho runner mới', 'Write the threat and countermeasure table for the new runner'],
            ['Liệt kê các loại job và lệnh được phép cho từng loại', 'List the job types and the allowed commands for each'],
          ],
          done: [
            ['Bảng mối đe dọa và danh sách lệnh được phép nằm trong `docs/security/runner.md`', 'The threat table and the command allowlist are in `docs/security/runner.md`'],
          ],
        },
        {
          id: 's7-2', kind: 'build', h: '4–5',
          t: ['Dựng Runner service .NET: nhận yêu cầu có xác thực, kiểm tra loại job và chính sách, checkout đúng revision', 'Build the .NET Runner service: authenticated requests, job type and policy checks, checkout of the approved revision'],
          goal: ['Runner chỉ nhận job hợp lệ từ bên được xác thực và chỉ chạy lệnh trong danh sách cho phép.', 'The runner only accepts valid jobs from authenticated callers and only runs allowlisted commands.'],
          learn: [
            ['Job = loại, repo, revision, lệnh, giới hạn tài nguyên, người yêu cầu', 'A job = type, repo, revision, commands, resource limits, requester'],
            ['Chính sách là dữ liệu cấu hình, kiểm tra trước khi tạo môi trường', 'Policy is configuration data, checked before creating the environment'],
            ['Ghi lại lệnh nào chạy, do ai yêu cầu, kết quả ra sao', 'Record which command ran, who asked, and the outcome'],
          ],
          ai: [['Claude Code', ['Dựng Runner service', 'Build the Runner service'], ['Tạo `services/runner` bằng ASP.NET Core: `POST /jobs` (xác thực bằng token dịch vụ hoặc mTLS), kiểm tra loại job và lệnh theo `runner-policy.json`, từ chối lệnh ngoài danh sách. Job chỉ nhận repo và commit SHA cụ thể, không nhận nhánh tùy ý. Ghi bảng `RunnerJob` với người yêu cầu, lệnh, exit code, thời gian.', 'Create `services/runner` with ASP.NET Core: `POST /jobs` (authenticated with a service token or mTLS), check job type and commands against `runner-policy.json`, reject anything not allowlisted. A job takes a repo and a specific commit SHA, not an arbitrary branch. Write a `RunnerJob` table with requester, commands, exit codes and timings.']]],
          res: [['ASP.NET Core security', MS + '/aspnet/core/security/']],
          pr: [
            ['Viết `runner-policy.json` với các loại job build, test, lint', 'Write `runner-policy.json` with build, test and lint job types'],
            ['Cài endpoint tạo job có xác thực và kiểm tra chính sách', 'Implement the authenticated create-job endpoint with policy checks'],
            ['Ghi audit mỗi job', 'Audit every job'],
          ],
          done: [
            ['Gọi không có token bị 401, lệnh ngoài danh sách bị từ chối', 'Calls without a token get 401; commands outside the allowlist are rejected'],
            ['Mỗi job có dòng audit với người yêu cầu và lệnh', 'Each job has an audit row with requester and commands'],
          ],
        },
        {
          id: 's7-3', kind: 'core', h: '4–5',
          t: ['Tự viết phần tạo môi trường dùng-một-lần: container riêng mỗi job, giới hạn tài nguyên, chặn mạng, hủy sau khi xong', 'Write the ephemeral environment yourself: one container per job, resource limits, network blocked, destroyed afterwards'],
          goal: ['Mỗi job chạy trong container mới, có giới hạn, không có mạng ra ngoài trừ nguồn package, và bị xóa khi xong.', 'Each job runs in a fresh, limited container with no outbound network except package sources, and is removed when done.'],
          learn: [
            ['`--cpus`, `--memory`, `--pids-limit`, `--read-only`, người dùng không phải root', '`--cpus`, `--memory`, `--pids-limit`, `--read-only`, non-root user'],
            ['Mạng: chặn ra ngoài, chỉ cho phép proxy package nếu cần', 'Network: block egress, allow a package proxy only if needed'],
            ['Thu log, exit code và artifact trước khi hủy môi trường', 'Collect logs, exit codes and artifacts before destroying the environment'],
          ],
          ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết đoạn tạo container cho mỗi job runner: <dán code và lệnh docker run>. Đừng viết lại. Kiểm tra: có cách nào job đọc được bí mật của host, gọi mạng ra ngoài, tồn tại sau khi xong, hoặc làm đầy đĩa host không?', 'I wrote the per-job container creation myself: <paste code and the docker run command>. Do not rewrite it. Check: can a job read host secrets, reach the outside network, outlive its completion, or fill the host disk?']]],
          res: [['Docker run reference', 'https://docs.docker.com/reference/cli/docker/container/run/'], ['Kubernetes Jobs', 'https://kubernetes.io/docs/concepts/workloads/controllers/job/']],
          pr: [
            ['Tạo image runner chứa SDK cần thiết, chạy bằng người dùng không phải root', 'Build a runner image with the needed SDKs, running as a non-root user'],
            ['Chạy job với giới hạn tài nguyên, mạng hạn chế và timeout', 'Run jobs with resource limits, restricted network and a timeout'],
            ['Lưu log và artifact ra kho lưu trữ, rồi xóa container và workspace', 'Store logs and artifacts, then remove the container and workspace'],
          ],
          done: [
            ['Sau mỗi job không còn container hay workspace nào sót lại', 'After each job no container or workspace is left behind'],
            ['Job vượt thời gian bị dừng và đánh dấu timeout', 'A job over its time limit is stopped and marked as timed out'],
          ],
        },
        {
          id: 's7-4', kind: 'eval', h: '2–3',
          t: ['Thử tấn công runner: đọc bí mật, gọi mạng ra ngoài, lệnh bị cấm, ăn hết tài nguyên', 'Attack the runner: read secrets, call out to the network, forbidden commands, resource exhaustion'],
          goal: ['Có bằng chứng các biện pháp cách ly thực sự chặn được tấn công phổ biến.', 'There is evidence that the isolation measures really stop common attacks.'],
          learn: [
            ['Kiểm thử bảo mật bằng job độc hại có chủ đích', 'Security testing with deliberately malicious jobs'],
            ['Mỗi lần chặn phải để lại dấu vết audit', 'Every block must leave an audit trail'],
          ],
          ai: [['ChatGPT', ['Soạn job độc hại', 'Draft malicious jobs'], ['Soạn 6 test project .NET nhỏ đóng vai code độc hại trong repo: đọc biến môi trường và file trong home, gọi HTTP ra ngoài, fork bomb, ghi file 10 GB, cố truy cập `/var/run/docker.sock`, đổi lệnh build thành lệnh khác. Mỗi cái kèm kết quả mong đợi khi chạy trong runner an toàn.', 'Write 6 tiny .NET test projects acting as malicious repo code: read env vars and home files, make an outbound HTTP call, fork bomb, write a 10 GB file, try `/var/run/docker.sock`, swap the build command for another. For each, give the expected result in a safe runner.']]],
          res: [['Docker security', 'https://docs.docker.com/engine/security/']],
          pr: [
            ['Chạy 6 job độc hại qua runner', 'Run the 6 malicious jobs through the runner'],
            ['Ghi kết quả vào `tests/security/runner.md` và sửa chỗ lọt', 'Record results in `tests/security/runner.md` and fix any leak'],
          ],
          done: [
            ['Cả 6 job bị chặn hoặc giới hạn đúng mong đợi', 'All 6 jobs are blocked or limited as expected'],
            ['Mỗi lần chặn có dòng audit', 'Each block has an audit row'],
          ],
        },
      ],
    },
    {
      id: 's8', title: ['Hàng đợi, chạy đồng thời và hệ phân tán', 'Queues, concurrency and distributed systems'],
      items: [
        {
          id: 's8-1', kind: 'learn', h: '3',
          t: ['Học hàng đợi và worker, at-least-once, lease, backoff có jitter, dead-letter, outbox/inbox, saga', 'Learn queues and workers, at-least-once delivery, leases, backoff with jitter, dead-letter, outbox/inbox, sagas'],
          goal: ['Giải thích được vì sao consumer phải idempotent và lease khác khóa phân tán thế nào.', 'You can explain why consumers must be idempotent and how a lease differs from a distributed lock.'],
          learn: [
            ['At-least-once: một job có thể chạy hai lần, nên tác động phụ phải idempotent', 'At-least-once: a job may run twice, so side effects must be idempotent'],
            ['Lease có thời hạn và gia hạn; hết hạn thì worker khác nhận lại', 'Leases have a deadline and renewal; when expired another worker takes over'],
            ['Retry có backoff mũ và jitter, phân loại lỗi, dead-letter cho lỗi vĩnh viễn', 'Exponential backoff with jitter, error classification, dead-letter for permanent failures'],
            ['Outbox/inbox, eventual consistency, saga và bù trừ, job đối soát', 'Outbox/inbox, eventual consistency, sagas and compensation, reconciliation jobs'],
          ],
          ai: [['Claude', ['Học bằng tình huống', 'Learn through scenarios'], ['Dạy tôi hàng đợi có lease bằng tình huống: worker A nhận job, bị treo 10 phút, lease hết hạn, worker B nhận lại, rồi A tỉnh dậy và cố ghi kết quả. Điều gì có thể sai, fencing token giải quyết thế nào, và vì sao khóa phân tán không thay được idempotency. Kết thúc bằng 3 câu đố.', 'Teach me leased queues with a scenario: worker A takes a job, hangs for 10 minutes, the lease expires, worker B takes it, then A wakes up and tries to write its result. What can go wrong, how a fencing token helps, and why a distributed lock does not replace idempotency. End with 3 quiz questions.']]],
          res: [['Transactional outbox', 'https://microservices.io/patterns/data/transactional-outbox.html'], ['Saga pattern', 'https://microservices.io/patterns/data/saga.html']],
          pr: [
            ['Vẽ sơ đồ trạng thái của một job: Queued → Leased → Succeeded/Failed/DeadLettered', 'Draw a job\'s state diagram: Queued → Leased → Succeeded/Failed/DeadLettered'],
            ['Viết ADR: hàng đợi bằng database hay message broker cho pilot, theo 7 câu hỏi checkpoint', 'Write an ADR: database queue or message broker for the pilot, using the 7 checkpoint questions'],
          ],
          done: [
            ['Có sơ đồ trạng thái job và ADR chọn loại hàng đợi', 'There is a job state diagram and an ADR choosing the queue type'],
          ],
        },
        {
          id: 's8-2', kind: 'core', h: '4–5',
          t: ['Tự viết hàng đợi bằng database có lease: nhận job, gia hạn, hết hạn và thu hồi, giới hạn đồng thời', 'Write a database-backed queue with leases yourself: acquire, renew, expire and reclaim, concurrency limits'],
          goal: ['Nhiều worker chạy song song không nhận trùng job, job của worker chết được nhận lại.', 'Several workers run in parallel without taking the same job, and a dead worker\'s jobs are reclaimed.'],
          learn: [
            ['PostgreSQL `FOR UPDATE SKIP LOCKED` (hoặc `UPDLOCK, READPAST` trên SQL Server)', 'PostgreSQL `FOR UPDATE SKIP LOCKED` (or `UPDLOCK, READPAST` on SQL Server)'],
            ['Fencing token (số phiên bản lease) chặn worker cũ ghi đè', 'A fencing token (lease version) stops a stale worker from overwriting'],
            ['Giới hạn đồng thời toàn cục và theo repo', 'Global and per-repo concurrency limits'],
          ],
          ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết hàng đợi job bằng PostgreSQL với `SKIP LOCKED`, lease có hạn và gia hạn: <dán SQL và code>. Đừng viết lại. Tìm race condition khi hai worker cùng nhận, khi lease hết hạn đúng lúc worker ghi kết quả, và khi đồng hồ các máy lệch nhau.', 'I wrote a PostgreSQL job queue with `SKIP LOCKED`, expiring leases and renewal myself: <paste SQL and code>. Do not rewrite it. Find race conditions when two workers acquire at once, when a lease expires exactly as a worker writes its result, and when machine clocks drift.']]],
          res: [['PostgreSQL SELECT … FOR UPDATE', 'https://www.postgresql.org/docs/current/sql-select.html']],
          pr: [
            ['Tạo bảng `Job` (trạng thái, lease_until, lease_version, attempts, idempotency_key)', 'Create a `Job` table (status, lease_until, lease_version, attempts, idempotency_key)'],
            ['Viết acquire, renew, complete có kiểm tra lease_version', 'Write acquire, renew and complete with lease_version checks'],
            ['Viết tiến trình thu hồi lease hết hạn và giới hạn đồng thời', 'Write the expired-lease reclaimer and concurrency limits'],
          ],
          done: [
            ['Test: 5 worker, 50 job, không job nào chạy hai lần đồng thời', 'Test: 5 workers, 50 jobs, no job runs twice at the same time'],
            ['Test: giết worker giữa chừng, job được nhận lại sau khi lease hết hạn', 'Test: kill a worker mid-job; the job is reclaimed after the lease expires'],
          ],
        },
        {
          id: 's8-3', kind: 'build', h: '3',
          t: ['Thêm phân loại retry, backoff có jitter và dead-letter cho lỗi vĩnh viễn', 'Add retry classification, backoff with jitter and dead-lettering for permanent failures'],
          goal: ['Lỗi tạm thời được thử lại có giới hạn, lỗi vĩnh viễn vào dead-letter và hiện rõ cho người.', 'Temporary failures get bounded retries; permanent ones go to dead-letter and are visible to people.'],
          learn: [
            ['Bảng chính sách retry theo loại lỗi của roadmap', 'The roadmap\'s retry policy table by failure type'],
            ['Jitter tránh nhiều job cùng retry một lúc', 'Jitter avoids many jobs retrying at the same moment'],
            ['Dead-letter cần thông báo và đường xử lý tay', 'Dead-letters need alerts and a manual path'],
          ],
          ai: [['Claude Code', ['Cài chính sách retry', 'Implement the retry policy'], ['Thêm phân loại lỗi cho job: Validation, Unauthorized, RateLimit, TransientNetwork, RunnerCrash, TestFailure, InvalidAiOutput, PermanentProvider. Mỗi loại có số lần tối đa và chính sách theo bảng trong `docs/architecture/retry-policy.md`. Backoff mũ có jitter. Hết lần thì chuyển DeadLettered và gửi thông báo qua n8n.', 'Add job error classification: Validation, Unauthorized, RateLimit, TransientNetwork, RunnerCrash, TestFailure, InvalidAiOutput, PermanentProvider. Each type has a max attempt count and a policy from `docs/architecture/retry-policy.md`. Exponential backoff with jitter. When attempts run out, move to DeadLettered and notify through n8n.']]],
          res: [['Polly resilience library', 'https://github.com/App-vNext/Polly']],
          pr: [
            ['Viết `retry-policy.md` từ bảng của roadmap', 'Write `retry-policy.md` from the roadmap\'s table'],
            ['Cài phân loại lỗi, backoff và dead-letter', 'Implement classification, backoff and dead-letter'],
            ['Thêm màn hoặc API liệt kê job dead-letter và nút chạy lại', 'Add a screen or API listing dead-lettered jobs with a retry action'],
          ],
          done: [
            ['Lỗi validation không retry; rate limit retry có backoff', 'Validation errors are not retried; rate limits retry with backoff'],
            ['Job hết lần retry nằm trong dead-letter và có thông báo', 'Jobs out of retries sit in dead-letter and trigger an alert'],
          ],
        },
        {
          id: 's8-4', kind: 'build', h: '3–4',
          t: ['Dùng outbox cho tác động phụ ra Jira/GitHub và job đối soát cho lỗi giữa chừng', 'Use an outbox for side effects to Jira/GitHub and a reconciliation job for partial failures'],
          goal: ['Thay đổi trạng thái và tác động phụ không bao giờ lệch nhau vĩnh viễn.', 'State changes and side effects never stay out of sync permanently.'],
          learn: [
            ['Outbox: ghi sự kiện cùng giao dịch với trạng thái, gửi sau', 'Outbox: write the event in the same transaction as the state, send it later'],
            ['Inbox: nhớ sự kiện đã xử lý để bỏ qua bản trùng', 'Inbox: remember processed events to skip duplicates'],
            ['Đối soát: so trạng thái hệ thống với Jira/GitHub và sửa chỗ lệch', 'Reconciliation: compare system state with Jira/GitHub and fix drift'],
          ],
          ai: [['ChatGPT', ['Thiết kế đối soát', 'Design reconciliation'], ['Hệ thống của tôi có trạng thái lần chạy trong PostgreSQL, PR trên GitHub và ticket trên Jira. Liệt kê các kiểu lệch có thể xảy ra (PR đã merge nhưng lần chạy vẫn AwaitingCodeApproval, Jira chưa sang Done…) và thiết kế một job đối soát chạy định kỳ: phát hiện, sửa tự động khi an toàn, báo người khi không chắc.', 'My system keeps run state in PostgreSQL, PRs on GitHub and tickets on Jira. List the kinds of drift that can happen (PR merged but the run still AwaitingCodeApproval, Jira not moved to Done…) and design a periodic reconciliation job: detect, auto-fix when safe, alert a person when unsure.']]],
          res: [['Transactional outbox', 'https://microservices.io/patterns/data/transactional-outbox.html']],
          pr: [
            ['Thêm bảng `Outbox` và tiến trình gửi có idempotency key', 'Add an `Outbox` table and a sender with idempotency keys'],
            ['Thêm bảng `Inbox` cho webhook từ GitHub và Jira', 'Add an `Inbox` table for GitHub and Jira webhooks'],
            ['Viết job đối soát chạy mỗi 15 phút', 'Write a reconciliation job that runs every 15 minutes'],
          ],
          done: [
            ['Giả lập Jira lỗi sau khi merge: job đối soát sửa được trạng thái Jira', 'Simulate Jira failing after a merge: reconciliation fixes the Jira state'],
          ],
        },
        {
          id: 's8-5', kind: 'eval', h: '2–3',
          t: ['Thử hỗn loạn: giết worker, gửi sự kiện trùng, gây lỗi giữa chừng và đo phục hồi', 'Chaos test: kill workers, send duplicate events, cause partial failures and measure recovery'],
          goal: ['Tiêu chí bước 8 có bằng chứng: chạy đồng thời đúng giới hạn, không tác động phụ trùng, phục hồi được.', 'Step 8\'s criteria have evidence: concurrency within limits, no duplicate side effects, recovery works.'],
          learn: [
            ['Thử hỗn loạn có kế hoạch, có giả thuyết và có đo lường', 'Planned chaos testing with a hypothesis and measurements'],
          ],
          ai: [['Claude', ['Viết kế hoạch thử', 'Write the test plan'], ['Viết kế hoạch thử hỗn loạn cho hàng đợi job của tôi theo 6 tiêu chí: chạy đồng thời trong giới hạn, sự kiện trùng không gây tác động phụ trùng, job bị sập phục hồi được, lease hết hạn được thu hồi, lỗi vĩnh viễn hiện rõ, lỗi tích hợp giữa chừng được đối soát. Mỗi tiêu chí: cách gây lỗi, số đo, ngưỡng đạt.', 'Write a chaos test plan for my job queue against 6 criteria: concurrency within limits, duplicate events cause no duplicate side effects, crashed jobs recover, expired leases are reclaimed, permanent failures are visible, partial integration failures are reconciled. For each: how to cause it, what to measure, pass threshold.']]],
          res: [['Principles of Chaos Engineering', 'https://principlesofchaos.org/']],
          pr: [
            ['Chạy từng thử nghiệm và ghi số đo', 'Run each experiment and record measurements'],
            ['Ghi kết quả vào `tests/reliability/queue.md`', 'Record results in `tests/reliability/queue.md`'],
          ],
          done: [
            ['Cả 6 tiêu chí của bước 8 có kết quả đạt và số đo', 'All 6 step-8 criteria pass with measurements'],
          ],
        },
      ],
    },
    {
      id: 's9', title: ['AI QA và kỹ thuật kiểm thử', 'AI QA and test engineering'],
      items: [
        {
          id: 's9-1', kind: 'learn', h: '2',
          t: ['Học kim tự tháp test, test hợp đồng, E2E, dữ liệu test, test chập chờn và truy vết yêu cầu → test', 'Learn the test pyramid, contract tests, E2E, test data, flaky tests and requirement-to-test traceability'],
          goal: ['Chọn đúng tầng test cho từng loại AC và biết nhận ra test chập chờn.', 'You pick the right test layer for each kind of AC and can spot a flaky test.'],
          learn: [
            ['Unit, integration, contract, E2E, regression: chi phí và độ tin cậy mỗi tầng', 'Unit, integration, contract, E2E, regression: cost and confidence per layer'],
            ['Dữ liệu test cô lập, test không phụ thuộc thứ tự', 'Isolated test data; tests that do not depend on order'],
            ['Test chập chờn: chạy lại cùng code ra kết quả khác', 'Flaky tests: same code, different results on reruns'],
            ['Ma trận truy vết: mỗi AC ứng với ít nhất một kịch bản test', 'A traceability matrix: each AC maps to at least one test scenario'],
          ],
          ai: [['ChatGPT', ['Luyện chọn tầng test', 'Practise choosing test layers'], ['Cho tôi 8 tiêu chí nghiệm thu của ứng dụng TaskLite (phân quyền, phân trang, chuyển trạng thái task, thông báo hạn chót…). Với mỗi cái, đố tôi nên test ở tầng nào và vì sao, rồi chấm câu trả lời của tôi.', 'Give me 8 acceptance criteria for the TaskLite app (permissions, pagination, task status transitions, due-date notifications…). For each, quiz me on which test layer fits and why, then grade my answers.']]],
          res: [['.NET testing', MS + '/dotnet/core/testing/'], ['Playwright', 'https://playwright.dev/']],
          pr: [
            ['Viết chiến lược test ngắn trong `docs/architecture/testing.md`', 'Write a short test strategy in `docs/architecture/testing.md`'],
          ],
          done: [
            ['Chiến lược test nêu rõ tầng nào cho loại AC nào', 'The test strategy says which layer covers which kind of AC'],
          ],
        },
        {
          id: 's9-2', kind: 'build', h: '3–4',
          t: ['Làm AI QA agent biến AC đã duyệt thành kịch bản test và ma trận truy vết', 'Build the AI QA agent that turns approved ACs into test scenarios and a traceability matrix'],
          goal: ['Mỗi AC có kịch bản dương, âm, biên và phân quyền, kèm gợi ý dữ liệu test và khung test.', 'Each AC gets positive, negative, boundary and authorization scenarios, with test data suggestions and test skeletons.'],
          learn: [
            ['AI đề xuất test; CI mới là bên chạy và giữ bằng chứng', 'AI proposes tests; CI runs them and keeps the evidence'],
            ['Khung test sinh ra phải được người review trước khi giữ', 'Generated test skeletons must be reviewed before being kept'],
          ],
          ai: [['Claude Code', ['Làm QA agent', 'Build the QA agent'], ['Thêm QA agent: đầu vào là `RequirementArtifact` đã duyệt, đầu ra theo schema `TestScenario[]` (acId, loại: positive/negative/boundary/authorization, bước, dữ liệu, kết quả mong đợi, tầng test). Sinh file `traceability.md` liệt kê AC → kịch bản → tên test. Có tùy chọn sinh khung test xUnit hoặc Playwright cho kịch bản được chọn.', 'Add a QA agent: input is the approved `RequirementArtifact`, output follows the `TestScenario[]` schema (acId, type: positive/negative/boundary/authorization, steps, data, expected result, test layer). Generate a `traceability.md` listing AC → scenario → test name. Optionally generate xUnit or Playwright skeletons for selected scenarios.']]],
          res: [['Playwright', 'https://playwright.dev/']],
          pr: [
            ['Viết schema `TestScenario` và prompt QA', 'Write the `TestScenario` schema and the QA prompt'],
            ['Chạy QA agent trên 3 yêu cầu đã duyệt', 'Run the QA agent on 3 approved requirements'],
            ['Review và giữ lại các khung test hữu ích', 'Review and keep the useful test skeletons'],
          ],
          done: [
            ['Mỗi AC của 3 yêu cầu có ít nhất một kịch bản test', 'Every AC of the 3 requirements has at least one test scenario'],
            ['Ma trận truy vết được sinh và nằm trong PR', 'The traceability matrix is generated and included in the PR'],
          ],
        },
        {
          id: 's9-3', kind: 'build', h: '2–3',
          t: ['Thêm 2 test E2E Playwright cho trang TaskLite và giữ kết quả trong CI', 'Add 2 Playwright E2E tests for the TaskLite page and keep the results in CI'],
          goal: ['Luồng chính (tạo task, đổi trạng thái) có test E2E chạy trong CI kèm báo cáo, để QA agent và cổng CI có thứ để dựa vào.', 'The main flow (create a task, change its status) has E2E tests in CI with reports, so the QA agent and CI gates have something to rely on.'],
          learn: [
            ['Locator ổn định, chờ theo điều kiện thay vì chờ cứng', 'Stable locators, condition-based waits instead of fixed sleeps'],
            ['Trace, ảnh chụp và video khi test lỗi', 'Traces, screenshots and video on failure'],
            ['Phân biệt test chập chờn với test lỗi thật bằng chạy lại có ghi nhận', 'Tell flaky from failing tests with recorded reruns'],
          ],
          ai: [['Claude Code', ['Viết test E2E', 'Write the E2E tests'], ['Thêm `tests/e2e` dùng Playwright: 2 test cho tạo task và đổi trạng thái task trên trang TaskLite. Chạy trên Docker Compose trong CI, upload báo cáo HTML và trace khi lỗi. Bật retry 1 lần và đánh dấu test chập chờn trong báo cáo.', 'Add `tests/e2e` with Playwright: 2 tests for creating a task and changing its status on the TaskLite page. Run them against Docker Compose in CI, upload the HTML report and traces on failure. Enable 1 retry and mark flaky tests in the report.']]],
          res: [['Playwright', 'https://playwright.dev/'], ['GitHub Actions', GH + '/actions']],
          pr: [
            ['Viết 2 test E2E', 'Write the 2 E2E tests'],
            ['Thêm job E2E vào CI chạy trên Compose', 'Add an E2E job to CI running against Compose'],
            ['Giữ báo cáo và trace làm artifact', 'Keep the report and traces as artifacts'],
          ],
          done: [
            ['Job E2E chạy trong CI và báo cáo được lưu', 'The E2E job runs in CI and its report is kept'],
            ['Báo cáo phân biệt được test chập chờn và test lỗi', 'The report distinguishes flaky tests from failing ones'],
          ],
        },
        {
          id: 's9-4', kind: 'eval', h: '2',
          t: ['Kiểm chứng AI không đánh dấu test đạt khi không có bằng chứng chạy', 'Verify the AI never marks a test as passed without execution evidence'],
          goal: ['Chỉ kết quả CI được dùng để quyết định đạt hay không, kể cả khi agent nói đạt.', 'Only CI results decide pass or fail, even when the agent claims a pass.'],
          learn: [
            ['Nguồn sự thật cho kết quả test là artifact của CI', 'The source of truth for test results is the CI artifact'],
          ],
          ai: [['Claude', ['Tìm đường lọt', 'Find bypasses'], ['Đây là đoạn code quyết định một lần chạy đã qua bước test hay chưa: <dán code>. Tìm mọi đường mà một câu trả lời của agent (ví dụ "all tests passed") có thể làm trạng thái chuyển sang bước sau mà không có kết quả CI.', 'Here is the code that decides whether a run passed the test step: <paste code>. Find every way an agent answer (e.g. "all tests passed") could move the state forward without a CI result.']]],
          res: [['GitHub REST API: check runs', GH + '/rest/checks/runs']],
          pr: [
            ['Cho agent trả lời "đạt" trong khi một test CI hỏng', 'Make the agent answer "passed" while a CI test fails'],
            ['Kiểm tra trạng thái không chuyển và có ghi lý do', 'Check the state does not move and the reason is recorded'],
          ],
          done: [
            ['Test chứng minh trạng thái chỉ đổi theo kết quả CI', 'A test proves the state only changes on CI results'],
            ['Mọi AC đã duyệt ánh xạ tới ít nhất một kịch bản test', 'Every approved AC maps to at least one test scenario'],
          ],
        },
      ],
    },
    {
      id: 's10', title: ['Chất lượng code và bảo mật chuỗi cung ứng', 'Code quality and supply-chain security'],
      items: [
        {
          id: 's10-1', kind: 'learn', h: '2',
          t: ['Học SAST, SCA, quét bí mật, quét image, SBOM, chính sách license và quality gate', 'Learn SAST, SCA, secret scanning, image scanning, SBOMs, license policy and quality gates'],
          goal: ['Biết mỗi loại quét bắt lỗi gì và đặt nó ở bước nào của CI.', 'You know what each scan catches and where it sits in CI.'],
          learn: [
            ['SAST (CodeQL) tìm lỗ hổng trong code; SCA tìm lỗ hổng trong thư viện', 'SAST (CodeQL) finds flaws in code; SCA finds flaws in dependencies'],
            ['Quét bí mật và push protection', 'Secret scanning and push protection'],
            ['Quét image container và SBOM', 'Container image scanning and SBOMs'],
            ['Artifact có chữ ký và provenance', 'Signed artifacts and provenance'],
          ],
          ai: [['ChatGPT', ['Tóm tắt có ví dụ', 'Summarize with examples'], ['Giải thích SAST, SCA, secret scanning, container scanning, SBOM và license policy bằng một ví dụ lỗi cụ thể cho mỗi loại trong một dự án ASP.NET Core + Angular, và nói công cụ miễn phí nào trên GitHub phù hợp.', 'Explain SAST, SCA, secret scanning, container scanning, SBOM and license policy with one concrete flaw example for each in an ASP.NET Core + Angular project, and which free GitHub tools fit.']]],
          res: [['GitHub code security', GH + '/code-security'], ['Trivy', 'https://github.com/aquasecurity/trivy']],
          pr: [
            ['Lập bảng: loại quét, công cụ, bước CI, ngưỡng chặn', 'Make a table: scan type, tool, CI step, blocking threshold'],
          ],
          done: [
            ['Bảng cổng chất lượng nằm trong `docs/security/quality-gates.md`', 'The quality-gate table is in `docs/security/quality-gates.md`'],
          ],
        },
        {
          id: 's10-2', kind: 'build', h: '4–5',
          t: ['Thêm cổng CI: độ phủ, CodeQL, dependency review, quét bí mật, quét image, SBOM, giữ kết quả', 'Add CI gates: coverage, CodeQL, dependency review, secret scanning, image scanning, SBOM, kept results'],
          goal: ['PR có lỗ hổng nghiêm trọng, bí mật bị lộ hoặc độ phủ dưới ngưỡng không merge được.', 'A PR with a critical vulnerability, a leaked secret or coverage below threshold cannot merge.'],
          learn: [
            ['Ngưỡng độ phủ hợp với repo, không đặt con số tùy tiện', 'Coverage thresholds that fit the repo, not arbitrary numbers'],
            ['Kết quả quét lưu làm artifact để audit', 'Scan results kept as artifacts for audit'],
          ],
          ai: [['Claude Code', ['Viết các job bảo mật', 'Write the security jobs'], ['Thêm vào CI: thu độ phủ bằng coverlet và chặn khi dưới ngưỡng trong `coverage.json`; CodeQL cho C# và TypeScript; `actions/dependency-review-action` chặn lỗ hổng mức high; bật secret scanning và push protection; Trivy quét image API và web, chặn mức critical; sinh SBOM (CycloneDX hoặc SPDX). Upload mọi kết quả làm artifact.', 'Add to CI: coverage with coverlet, blocking below the threshold in `coverage.json`; CodeQL for C# and TypeScript; `actions/dependency-review-action` blocking high-severity issues; enable secret scanning and push protection; Trivy scanning the API and web images, blocking critical issues; generate an SBOM (CycloneDX or SPDX). Upload every result as an artifact.']]],
          res: [['GitHub code security', GH + '/code-security'], ['Trivy', 'https://github.com/aquasecurity/trivy']],
          pr: [
            ['Thêm từng job và đặt ngưỡng chặn', 'Add each job and set its blocking threshold'],
            ['Thêm các job vào required checks', 'Add the jobs to the required checks'],
          ],
          done: [
            ['Mọi job quét chạy trên mỗi PR và kết quả được lưu', 'Every scan job runs on each PR and its results are kept'],
          ],
        },
        {
          id: 's10-3', kind: 'eval', h: '2–3',
          t: ['Siết branch protection (bỏ duyệt cũ khi có push mới, CODEOWNERS) và thử cố tình vượt cổng', 'Tighten branch protection (dismiss stale approvals, CODEOWNERS) and try to get past the gates on purpose'],
          goal: ['Commit mới trên PR luôn cần duyệt lại; mọi cổng thực sự chặn khi bị vi phạm.', 'A new commit on a PR always needs re-approval; every gate really blocks when violated.'],
          learn: [
            ['Dismiss stale approvals và require approval of the most recent push', 'Dismiss stale approvals and require approval of the most recent push'],
            ['Phase 1 đã giữ lời duyệt qua commit "AI update"; roadmap yêu cầu không làm vậy', 'Phase 1 kept approvals across "AI update" commits; the roadmap says not to'],
            ['CODEOWNERS cho phần nhạy cảm', 'CODEOWNERS for sensitive areas'],
          ],
          ai: [['ChatGPT', ['Lập danh sách thử vượt cổng', 'List gate-bypass attempts'], ['Liệt kê 6 cách một agent hoặc developer có thể cố đưa code vào main mà né cổng (push thẳng, sửa workflow CI trong PR, tắt test, thêm bí mật vào file mẫu, đổi phiên bản thư viện có lỗ hổng, push sau khi được duyệt). Mỗi cách: cấu hình GitHub nào chặn.', 'List 6 ways an agent or developer might try to get code into main past the gates (direct push, editing the CI workflow in the PR, disabling tests, adding a secret to a sample file, bumping to a vulnerable dependency, pushing after approval). For each, which GitHub setting blocks it.']]],
          res: [['Rulesets', GH + '/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets'], ['CODEOWNERS', GH + '/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners']],
          pr: [
            ['Bật ruleset: bắt buộc PR, required checks, dismiss stale, approval of latest push, CODEOWNERS', 'Enable a ruleset: required PR, required checks, dismiss stale, approval of latest push, CODEOWNERS'],
            ['Thử 6 cách vượt cổng trên một repo thử', 'Try the 6 bypasses on a test repo'],
            ['Sửa luồng merge của nền tảng để không giữ lời duyệt cũ', 'Change the platform\'s merge flow so it never keeps an old approval'],
          ],
          done: [
            ['Cả 6 cách vượt cổng đều bị chặn', 'All 6 bypass attempts are blocked'],
            ['PR có push mới sau khi duyệt không merge được cho tới khi duyệt lại', 'A PR pushed after approval cannot merge until re-approved'],
          ],
        },
      ],
    },
    {
      id: 's11', title: ['Migration database và an toàn khi deploy', 'Database migrations and deployment safety'],
      items: [
        {
          id: 's11-1', kind: 'learn', h: '2',
          t: ['Học expand-and-contract, tương thích ngược, rolling, blue/green, canary, rollback và roll-forward', 'Learn expand-and-contract, backward compatibility, rolling, blue/green, canary, rollback and roll-forward'],
          goal: ['Giải thích được vì sao rollback container không hoàn tác thay đổi database.', 'You can explain why rolling back containers does not undo database changes.'],
          learn: [
            ['Expand → migrate data → contract qua nhiều lần phát hành', 'Expand → migrate data → contract across several releases'],
            ['Khi rolling, bản cũ và bản mới chạy cùng lúc với cùng database', 'During a rollout, old and new versions run at once on the same database'],
            ['Readiness khác liveness', 'Readiness vs liveness'],
            ['Rollback khác roll-forward; khi nào chọn cái nào', 'Rollback vs roll-forward; when to pick which'],
          ],
          ai: [['Claude', ['Học bằng ví dụ đổi tên cột', 'Learn with a column rename'], ['Tôi muốn đổi cột `Task.Title` thành `Task.Name` trong hệ thống chạy rolling với 2 bản sao. Hướng dẫn tôi làm theo expand-and-contract qua 3 lần phát hành, mỗi lần ghi rõ migration, code đọc/ghi cột nào, và rollback an toàn tới đâu.', 'I want to rename `Task.Title` to `Task.Name` in a system rolled out over 2 replicas. Walk me through expand-and-contract across 3 releases: for each, the migration, which columns the code reads/writes, and how far rollback is safe.']]],
          res: [['EF Core migrations', MS + '/ef/core/managing-schemas/migrations/'], ['Kubernetes probes', 'https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/']],
          pr: [
            ['Viết ghi chú so sánh rolling, blue/green, canary cho dự án', 'Write a note comparing rolling, blue/green and canary for the project'],
          ],
          done: [
            ['Giải thích bằng lời 3 lần phát hành của ví dụ đổi tên cột', 'Explain out loud the 3 releases of the column-rename example'],
          ],
        },
        {
          id: 's11-3', kind: 'build', h: '3–4',
          t: ['Thêm probe, sao lưu và khôi phục database của nền tảng (Workflow API), test sau deploy và quy trình rollback/roll-forward', 'Add probes, backup and restore of the platform database (Workflow API), post-deploy tests and a rollback/roll-forward procedure'],
          goal: ['Mỗi lần deploy được kiểm chứng tự động, và đã khôi phục database thật từ bản sao lưu ít nhất một lần.', 'Every deploy is verified automatically, and a database restore from backup has been done for real at least once.'],
          learn: [
            ['Bản sao lưu chỉ có giá trị khi đã khôi phục thử', 'A backup is only worth something once a restore has been tested'],
            ['Smoke test và business-flow test sau deploy', 'Smoke and business-flow tests after deploy'],
          ],
          ai: [['ChatGPT', ['Soạn quy trình', 'Draft the procedure'], ['Soạn quy trình rollback và roll-forward cho hệ thống ASP.NET Core + PostgreSQL chạy trên Kubernetes, gồm: khi nào chọn cái nào, các bước cụ thể, ai quyết định, cách xử lý migration đã chạy, và cách kiểm chứng sau khi xong.', 'Draft a rollback and roll-forward procedure for an ASP.NET Core + PostgreSQL system on Kubernetes: when to pick which, concrete steps, who decides, how to handle migrations already applied, and how to verify afterwards.']]],
          res: [['PostgreSQL backup and restore', 'https://www.postgresql.org/docs/current/backup.html'], ['Kubernetes probes', 'https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/']],
          pr: [
            ['Thêm readiness kiểm tra database, liveness đơn giản', 'Add a readiness probe that checks the database, and a simple liveness probe'],
            ['Sao lưu database, khôi phục vào môi trường khác và kiểm tra dữ liệu', 'Back up the database, restore it into another environment and check the data'],
            ['Thêm business-flow test sau deploy (tạo project → giao task → đổi trạng thái)', 'Add a post-deploy business-flow test (create project → assign task → change status)'],
            ['Viết `docs/runbooks/rollback-rollforward.md`', 'Write `docs/runbooks/rollback-rollforward.md`'],
          ],
          done: [
            ['Đã khôi phục database từ bản sao lưu và ghi lại thời gian khôi phục', 'A database restore from backup was done and its duration recorded'],
            ['Deploy lỗi business-flow test thì dừng và theo quy trình', 'A deploy failing the business-flow test stops and follows the procedure'],
          ],
        },
      ],
    },
    {
      id: 's12', title: ['Quản trị pilot và phản hồi của nhóm', 'Pilot governance and team feedback'],
      items: [
        {
          id: 's12-1', kind: 'build', h: '3–4',
          t: ['Đặt chính sách pilot: RBAC, tách nhiệm vụ, repo bị loại trừ, hủy lần chạy, đánh dấu thay đổi do AI', 'Set pilot policies: RBAC, separation of duties, excluded repos, run cancellation, AI-change labelling'],
          goal: ['Chính sách được thực thi bằng code: AI tạo nhánh và PR được nhưng không vượt branch protection, người hủy được lần chạy.', 'Policies are enforced in code: AI may create branches and PRs but never bypass branch protection, and people can cancel runs.'],
          learn: [
            ['Tách nhiệm vụ: bên viết code không phải bên duyệt', 'Separation of duties: the author is not the approver'],
            ['Chính sách sử dụng AI và loại trừ repo nhạy cảm', 'AI usage policy and exclusion of sensitive repos'],
            ['Mọi thay đổi do AI tạo phải nhận diện được', 'Every AI-generated change must be identifiable'],
          ],
          ai: [['Claude', ['Soạn chính sách', 'Draft the policy'], ['Soạn chính sách sử dụng cho pilot nền tảng AI delivery của nhóm 3–5 developer: AI được làm gì, không được làm gì, repo và loại việc bị loại trừ, ai duyệt merge và production, cách hủy một lần chạy, cách nhận diện code do AI tạo, và cách báo sự cố. Ngắn, dạng gạch đầu dòng.', 'Draft a usage policy for a pilot of the AI delivery platform with a team of 3–5 developers: what AI may and may not do, excluded repos and task types, who approves merges and production, how to cancel a run, how AI-written code is identified, how to report incidents. Short, bullet points.']]],
          res: [['GitHub rulesets', GH + '/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets']],
          pr: [
            ['Viết `docs/security/ai-usage-policy.md`', 'Write `docs/security/ai-usage-policy.md`'],
            ['Cài danh sách repo cho phép và loại trừ trong Workflow API', 'Implement allowed and excluded repo lists in the Workflow API'],
            ['Thêm `POST /executions/{id}/cancel` và nhãn `ai-generated` trên PR', 'Add `POST /executions/{id}/cancel` and an `ai-generated` label on PRs'],
          ],
          done: [
            ['Lần chạy trên repo bị loại trừ bị từ chối', 'A run on an excluded repo is refused'],
            ['Developer hủy được một lần chạy đang code', 'A developer can cancel a run that is coding'],
          ],
        },
        {
          id: 's12-2', kind: 'build', h: '3–4',
          t: ['Ghi audit trail và thu số đo pilot: tỷ lệ hoàn thành, thời gian tới PR, tỷ lệ làm lại, chi phí mỗi việc', 'Record the audit trail and collect pilot metrics: completion rate, time to PR, rework rate, cost per task'],
          goal: ['Trả lời được bằng số: nền tảng có giúp nhóm nhanh hơn mà không giảm chất lượng không.', 'You can answer with numbers whether the platform makes the team faster without lowering quality.'],
          learn: [
            ['Thu baseline trước, đặt ngưỡng sau cùng với nhóm', 'Collect a baseline first; set thresholds later with the team'],
            ['Số đo của roadmap: completion, time to PR, PR acceptance, rework, escaped defects, agent failure, cost per task, satisfaction', 'The roadmap\'s metrics: completion, time to PR, PR acceptance, rework, escaped defects, agent failure, cost per task, satisfaction'],
          ],
          ai: [['Claude Code', ['Làm truy vấn số đo', 'Build the metric queries'], ['Từ các bảng WorkflowExecution, WorkflowHistory, LlmCall, RunnerJob và dữ liệu PR, viết view SQL tính: tỷ lệ hoàn thành, trung vị thời gian từ duyệt yêu cầu tới PR, tỷ lệ PR được chấp nhận không cần làm lại nhiều, số vòng sửa trung bình, tỷ lệ lỗi agent, và chi phí LLM cùng runner trên mỗi việc hoàn thành.', 'From the WorkflowExecution, WorkflowHistory, LlmCall, RunnerJob tables and PR data, write SQL views computing: completion rate, median time from requirement approval to PR, PR acceptance without major rework, average fix rounds, agent failure rate, and LLM plus runner cost per completed task.']]],
          res: [['PostgreSQL docs', 'https://www.postgresql.org/docs/current/']],
          pr: [
            ['Bảo đảm mọi hành động có quyền đều có dòng audit', 'Make sure every privileged action has an audit row'],
            ['Viết view số đo và một trang báo cáo đơn giản', 'Write the metric views and a simple report page'],
            ['Chuẩn bị khảo sát mức hài lòng ngắn cho developer', 'Prepare a short developer satisfaction survey'],
          ],
          done: [
            ['Báo cáo hiện đủ 8 số đo của roadmap', 'The report shows all 8 roadmap metrics'],
          ],
        },
        {
          id: 's12-3', kind: 'eval', h: '4–6',
          t: ['Chạy pilot với nhóm nhỏ trên repo được chọn và thu phản hồi', 'Run the pilot with a small team on selected repos and collect feedback'],
          goal: ['Ít nhất 2 developer dùng nền tảng cho việc thật, có số đo và phản hồi.', 'At least 2 developers use the platform for real work, with metrics and feedback.'],
          learn: [
            ['Bắt đầu với việc rủi ro thấp và repo không nhạy cảm', 'Start with low-risk tasks and non-sensitive repos'],
            ['Phản hồi định tính quan trọng như số đo', 'Qualitative feedback matters as much as metrics'],
          ],
          ai: [['ChatGPT', ['Phân tích phản hồi', 'Analyse the feedback'], ['Đây là phản hồi của developer sau pilot và bảng số đo: <dán>. Nhóm vấn đề theo chủ đề, chỉ ra 3 cải tiến có tác động lớn nhất, và điểm nào cho thấy chất lượng hoặc bảo mật bị giảm.', 'Here is the developer feedback after the pilot and the metrics table: <paste>. Group the issues by theme, point out the 3 improvements with the biggest impact, and anything suggesting quality or security got worse.']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/']],
          pr: [
            ['Chọn 2–3 developer, 1–2 repo và danh sách việc rủi ro thấp', 'Pick 2–3 developers, 1–2 repos and a list of low-risk tasks'],
            ['Chạy pilot 1–2 tuần, theo dõi số đo hằng ngày', 'Run the pilot for 1–2 weeks, watching metrics daily'],
            ['Thu khảo sát và phỏng vấn ngắn', 'Collect the survey and short interviews'],
          ],
          done: [
            ['Có báo cáo pilot với số đo và phản hồi', 'There is a pilot report with metrics and feedback'],
          ],
        },
        {
          id: 's12-4', kind: 'gate', h: '2–3',
          t: ['Chốt Team Pilot: đủ tiêu chí và có quy trình vận hành cho nhóm', 'Close the Team Pilot: all criteria met and a team operating procedure in place'],
          goal: ['Bằng chứng cho từng tiêu chí Team Pilot và một tài liệu vận hành nhóm dùng được.', 'Evidence for each Team Pilot criterion and a usable team operating procedure.'],
          learn: [
            ['Quyết định đi tiếp dựa trên bằng chứng pilot, không dựa trên cảm giác', 'Decide to continue on pilot evidence, not on a feeling'],
          ],
          ai: [['Claude', ['Rà bằng chứng', 'Review the evidence'], ['Đây là bằng chứng Team Pilot: <dán>. Đối chiếu với 8 tiêu chí (nhiều developer dùng thành công, chạy đồng thời an toàn, runner cách ly và có phân quyền, retry và phục hồi đã chứng minh, cổng QA và bảo mật chạy tự động, có bằng chứng audit, số đo cho thấy giá trị mà không giảm chất lượng hay bảo mật, có quy trình vận hành). Tiêu chí nào còn yếu?', 'Here is the Team Pilot evidence: <paste>. Check it against the 8 criteria (several developers use it successfully, safe concurrency, isolated and permissioned runner, retry and recovery demonstrated, automatic QA and security gates, audit evidence, metrics showing value without quality or security regression, an operating procedure). Which criteria are weak?']]],
          res: [['Google SRE book', 'https://sre.google/sre-book/table-of-contents/']],
          pr: [
            ['Viết `docs/pilot-evidence.md` theo 8 tiêu chí', 'Write `docs/pilot-evidence.md` against the 8 criteria'],
            ['Viết `docs/runbooks/team-operating-procedure.md`', 'Write `docs/runbooks/team-operating-procedure.md`'],
          ],
          done: [
            ['Nhiều developer dùng thành công; chạy đồng thời an toàn; runner cách ly và có phân quyền', 'Several developers use it successfully; concurrency is safe; the runner is isolated and permissioned'],
            ['Retry và phục hồi đã chứng minh; cổng QA và bảo mật chạy tự động; có bằng chứng audit', 'Retry and recovery demonstrated; QA and security gates run automatically; audit evidence exists'],
            ['Số đo cho thấy giá trị mà không giảm chất lượng hay bảo mật; có quy trình vận hành', 'Metrics show value without quality or security regression; an operating procedure exists'],
          ],
        },
      ],
    },
  ],
};
