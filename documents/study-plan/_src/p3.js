const MS = 'https://learn.microsoft.com';
module.exports = {
  id: 'p3', code: 'P3', color: '#10b981',
  title: ['Team Production: mở rộng, quản trị và vận hành xuất sắc', 'Team Production: scale, governance and operational excellence'],
  goal: ['8–12+ tuần · vận hành tin cậy cho nhiều nhóm và nhiều repo', '8–12+ weeks · run reliably across multiple teams and repos'],
  desc: ['Production ở phase 1 là Kubernetes local trên cùng máy với Dev. Ở đây môi trường tách hẳn trên cloud, danh tính và bí mật theo môi trường, có quan sát đầy đủ, đánh giá AI liên tục và quy trình phục hồi đã được thử.', 'Production in phase 1 was local Kubernetes on the same machine as Dev. Here environments are fully separated in the cloud, identities and secrets are per environment, with full observability, continuous AI evaluation and tested recovery procedures.'],
  milestones: [
    {
      id: 's13', title: ['Kiến trúc production và hạ tầng cloud', 'Production architecture and cloud infrastructure'],
      items: [
        {
          id: 's13-1', kind: 'learn', h: '3',
          t: ['Học mạng và danh tính trên cloud, container được quản lý, Kubernetes, IaC và tách môi trường', 'Learn cloud networking and identity, managed containers, Kubernetes, IaC and environment separation'],
          goal: ['Vẽ được kiến trúc cloud có Dev, QA/staging và Production tách danh tính, bí mật và mạng.', 'You can draw a cloud architecture with Dev, QA/staging and Production separated by identity, secrets and network.'],
          learn: [
            ['Managed identity / workload identity thay cho bí mật dài hạn', 'Managed identity / workload identity instead of long-lived secrets'],
            ['Container registry, managed database, key vault, artifact storage', 'Container registry, managed database, key vault, artifact storage'],
            ['Ingress, load balancer, autoscaling, network policy', 'Ingress, load balancing, autoscaling, network policies'],
            ['Infrastructure as Code (Terraform hoặc Bicep) và thiết kế khôi phục thảm họa', 'Infrastructure as Code (Terraform or Bicep) and disaster recovery design'],
          ],
          ai: [['Claude', ['Phản biện kiến trúc', 'Critique the architecture'], ['Đây là phác thảo kiến trúc cloud của tôi cho nền tảng AI delivery (Azure): <mô tả AKS, ACR, PostgreSQL, Key Vault, n8n, workflow API, runner, agent service>. Kiểm tra ranh giới giữa Dev, QA và Production, chỗ nào còn dùng bí mật dài hạn, chỗ nào runner có thể chạm tới tài nguyên production, và chi phí nào dễ bị bỏ quên.', 'Here is my cloud architecture sketch for the AI delivery platform (Azure): <describe AKS, ACR, PostgreSQL, Key Vault, n8n, workflow API, runner, agent service>. Check the boundaries between Dev, QA and Production, where long-lived secrets remain, where the runner could reach production resources, and which costs are easy to forget.']]],
          res: [['Azure Architecture Center', MS + '/azure/architecture/'], ['Kubernetes docs', 'https://kubernetes.io/docs/'], ['AWS Well-Architected', 'https://docs.aws.amazon.com/wellarchitected/latest/framework/welcome.html']],
          pr: [
            ['Chọn cloud và viết ADR theo 7 câu hỏi checkpoint', 'Choose a cloud and write an ADR using the 7 checkpoint questions'],
            ['Vẽ sơ đồ kiến trúc production với 3 môi trường', 'Draw the production architecture with 3 environments'],
          ],
          done: [
            ['Có ADR chọn cloud và sơ đồ kiến trúc 3 môi trường', 'There is a cloud-choice ADR and a 3-environment architecture diagram'],
          ],
        },
        {
          id: 's13-2', kind: 'build', h: '5–6',
          t: ['Viết IaC dựng Dev, QA/staging và Production: cluster, registry, database, key vault', 'Write IaC for Dev, QA/staging and Production: cluster, registry, database, key vault'],
          goal: ['Một môi trường mới được dựng hoàn toàn từ code, không thao tác tay trên portal.', 'A new environment is built entirely from code, with no manual portal clicks.'],
          learn: [
            ['Module tái sử dụng, biến theo môi trường, state từ xa có khóa', 'Reusable modules, per-environment variables, remote locked state'],
            ['Plan trước khi apply và review plan trong PR', 'Plan before apply and review the plan in a PR'],
          ],
          ai: [['Claude Code', ['Viết IaC', 'Write the IaC'], ['Trong `infra/terraform`, viết module cho: Kubernetes cluster được quản lý, container registry, PostgreSQL được quản lý, key vault, log workspace. Thư mục `envs/dev`, `envs/staging`, `envs/prod` dùng chung module với biến khác nhau. State lưu từ xa có khóa. Thêm workflow GitHub Actions chạy `terraform plan` trên PR và đăng kết quả làm comment.', 'In `infra/terraform`, write modules for: a managed Kubernetes cluster, a container registry, managed PostgreSQL, a key vault, a log workspace. `envs/dev`, `envs/staging`, `envs/prod` share the modules with different variables. State is remote and locked. Add a GitHub Actions workflow that runs `terraform plan` on PRs and posts the result as a comment.']]],
          res: [['Terraform docs', 'https://developer.hashicorp.com/terraform/docs'], ['Azure Kubernetes Service', MS + '/azure/aks/']],
          pr: [
            ['Viết module và cấu hình 3 môi trường', 'Write the modules and the 3 environment configurations'],
            ['Dựng môi trường Dev từ code', 'Build the Dev environment from code'],
            ['Xóa và dựng lại Dev để chứng minh lặp lại được', 'Destroy and rebuild Dev to prove it is repeatable'],
          ],
          done: [
            ['Dev được dựng lại hoàn toàn từ `terraform apply`', 'Dev is rebuilt entirely from `terraform apply`'],
            ['PR thay đổi hạ tầng có kết quả plan trong comment', 'Infrastructure PRs carry the plan result in a comment'],
          ],
        },
        {
          id: 's13-3', kind: 'build', h: '4–5',
          t: ['Cấu hình workload identity, tách quyền deploy theo môi trường, ingress, autoscaling và network policy', 'Configure workload identity, per-environment deploy permissions, ingress, autoscaling and network policies'],
          goal: ['Service lấy bí mật bằng danh tính, pipeline Dev không deploy được Production, runner không gọi được database production.', 'Services get secrets through identity, the Dev pipeline cannot deploy to Production, and the runner cannot reach the production database.'],
          learn: [
            ['GitHub Actions OIDC tới cloud thay cho secret dài hạn', 'GitHub Actions OIDC to the cloud instead of long-lived secrets'],
            ['Environment protection rule trên GitHub cho Production', 'GitHub environment protection rules for Production'],
            ['NetworkPolicy chặn mặc định, mở từng đường cần thiết', 'Default-deny NetworkPolicies, opening only the needed paths'],
          ],
          ai: [['ChatGPT', ['Kiểm tra ranh giới quyền', 'Check permission boundaries'], ['Liệt kê các ranh giới quyền tôi cần chứng minh cho nền tảng trên Kubernetes: pipeline Dev không deploy Production, runner không gọi database production, agent service không đọc bí mật của runner, n8n không gọi trực tiếp runner. Mỗi ranh giới: cơ chế thực thi (OIDC, environment rule, NetworkPolicy, RBAC) và cách thử.', 'List the permission boundaries I need to prove for the platform on Kubernetes: the Dev pipeline cannot deploy Production, the runner cannot reach the production database, the agent service cannot read runner secrets, n8n cannot call the runner directly. For each: the enforcement mechanism (OIDC, environment rule, NetworkPolicy, RBAC) and how to test it.']]],
          res: [['GitHub Actions OIDC', 'https://docs.github.com/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect'], ['Kubernetes network policies', 'https://kubernetes.io/docs/concepts/services-networking/network-policies/']],
          pr: [
            ['Cấu hình OIDC cho pipeline và environment rule cần người duyệt cho Production', 'Configure OIDC for pipelines and an approver-gated environment rule for Production'],
            ['Thêm NetworkPolicy chặn mặc định và các đường được phép', 'Add default-deny NetworkPolicies and the allowed paths'],
            ['Cấu hình ingress và autoscaler cho API và worker', 'Configure ingress and autoscaling for the API and workers'],
          ],
          done: [
            ['Thử từ runner gọi database production bị chặn', 'A call from the runner to the production database is blocked'],
            ['Không có secret dài hạn nào cho cloud trong GitHub', 'There are no long-lived cloud secrets in GitHub'],
          ],
        },
      ],
    },
    {
      id: 's14', title: ['Thực thi workflow bền vững', 'Durable workflow execution'],
      items: [
        {
          id: 's14-1', kind: 'eval', h: '5–6',
          t: ['Học durable execution và làm nguyên mẫu một workflow trên Temporal để so với cách hiện tại', 'Learn durable execution and prototype one workflow on Temporal to compare with the current approach'],
          goal: ['Quyết định có hay không dùng Temporal dựa trên nguyên mẫu và số đo, ghi trong ADR.', 'Decide whether to adopt Temporal based on a prototype and measurements, recorded in an ADR.'],
          learn: [
            ['Durable execution, replay và tính tất định của code workflow', 'Durable execution, replay and workflow code determinism'],
            ['Timer, signal, bù trừ, version hóa workflow đang chạy', 'Timers, signals, compensation, versioning running workflows'],
            ['Chỉ dùng khi workflow dài, phải sống qua sự cố, logic retry/bù trừ khó bảo trì', 'Adopt only for long workflows that must survive failures and retry/compensation logic that is hard to maintain'],
          ],
          ai: [['Claude', ['Viết ADR khách quan', 'Write an objective ADR'], ['Tôi đã làm nguyên mẫu luồng "chờ duyệt PR rồi merge rồi deploy" trên Temporal (.NET SDK) và so với Workflow API + hàng đợi hiện tại. Số đo của tôi: <dán số dòng code, độ phức tạp, thời gian phục hồi sau sự cố, chi phí vận hành>. Viết ADR theo 7 câu hỏi checkpoint, kết luận dựa trên số đo, không dựa trên độ phổ biến.', 'I prototyped the "wait for PR approval, then merge, then deploy" flow on Temporal (.NET SDK) and compared it with the current Workflow API + queue. My measurements: <paste lines of code, complexity, recovery time after failure, operating cost>. Write an ADR using the 7 checkpoint questions, concluding from the measurements, not from popularity.']]],
          res: [['Temporal docs', 'https://docs.temporal.io/']],
          pr: [
            ['Chạy Temporal local và viết lại một luồng dài bằng .NET SDK', 'Run Temporal locally and rewrite one long flow with the .NET SDK'],
            ['Giết worker giữa chừng ở cả hai cách và so thời gian phục hồi', 'Kill workers mid-flow in both approaches and compare recovery'],
            ['Viết ADR quyết định', 'Write the decision ADR'],
          ],
          done: [
            ['Có ADR với số đo so sánh và quyết định rõ', 'There is an ADR with comparison measurements and a clear decision'],
          ],
        },
        {
          id: 's14-2', kind: 'eval', h: '3–4',
          t: ['Đánh giá LangGraph cho một agent có trạng thái dạng đồ thị và ghi ADR', 'Evaluate LangGraph for an agent with graph-shaped state and record an ADR'],
          goal: ['Biết agent nào (nếu có) thực sự cần đồ thị, checkpoint và tương tác người bên trong vòng lặp.', 'You know which agent, if any, really needs a graph, checkpoints and in-loop human interaction.'],
          learn: [
            ['Khi vòng lặp công cụ đơn giản không còn đủ: nhiều nhánh, chu trình có kiểm soát, checkpoint', 'When a simple tool loop is no longer enough: branching, controlled cycles, checkpoints'],
            ['Chi phí thêm một runtime Python bên cạnh .NET', 'The cost of adding a Python runtime next to .NET'],
          ],
          ai: [['ChatGPT', ['So sánh có tiêu chí', 'Compare with criteria'], ['So sánh việc giữ vòng lặp agent tự viết bằng .NET với việc dùng LangGraph cho AI Developer agent của tôi theo các tiêu chí: số nhánh và chu trình, nhu cầu checkpoint, người tham gia giữa vòng lặp, chi phí vận hành thêm runtime Python, khả năng quan sát. Kết luận khi nào nên chuyển.', 'Compare keeping my hand-written .NET agent loop with using LangGraph for my AI Developer agent on: number of branches and cycles, checkpoint needs, human interaction mid-loop, operating cost of an extra Python runtime, observability. Conclude when switching would be worth it.']]],
          res: [['LangGraph docs', 'https://docs.langchain.com/oss/python/langgraph/overview']],
          pr: [
            ['Vẽ đồ thị trạng thái thật của AI Developer hiện tại', 'Draw the real state graph of the current AI Developer'],
            ['Làm nguyên mẫu nhỏ nếu đồ thị có chu trình phức tạp', 'Build a small prototype if the graph has complex cycles'],
            ['Viết ADR quyết định', 'Write the decision ADR'],
          ],
          done: [
            ['Có ADR về LangGraph với tiêu chí và quyết định', 'There is a LangGraph ADR with criteria and a decision'],
          ],
        },
      ],
    },
    {
      id: 's15', title: ['Quản trị và bảo mật nhiều nhóm', 'Multi-team governance and security'],
      items: [
        {
          id: 's15-1', kind: 'learn', h: '3',
          t: ['Học multi-tenancy, phân quyền theo chính sách, threat modeling và vẽ mô hình đe dọa cho nền tảng', 'Learn multi-tenancy, policy-based authorization, threat modeling, and draw the platform\'s threat model'],
          goal: ['Có mô hình đe dọa của nền tảng với các mối đe dọa chính và biện pháp.', 'There is a platform threat model with the main threats and countermeasures.'],
          learn: [
            ['Cô lập theo nhóm và theo repo', 'Isolation by team and by repo'],
            ['STRIDE hoặc cách tương tự để liệt kê mối đe dọa', 'STRIDE or a similar method to enumerate threats'],
            ['Phân loại dữ liệu, lưu giữ audit, yêu cầu riêng tư', 'Data classification, audit retention, privacy requirements'],
          ],
          ai: [['Claude', ['Cùng làm threat model', 'Threat-model together'], ['Cùng tôi làm threat model theo STRIDE cho nền tảng AI delivery gồm: n8n, Workflow API, agent service gọi LLM, runner chạy code, GitHub, Jira, cloud. Đặc biệt chú ý: prompt injection qua ticket và repo, agent bị lạm dụng công cụ, runner bị thoát ra, rò rỉ dữ liệu sang nhà cung cấp LLM, lạm dụng quyền duyệt. Mỗi mối đe dọa: mức độ và biện pháp.', 'Threat-model with me using STRIDE for the AI delivery platform: n8n, Workflow API, agent service calling LLMs, the code-running runner, GitHub, Jira, the cloud. Focus on: prompt injection via tickets and repos, agent tool abuse, runner escape, data leaking to the LLM provider, approval abuse. For each threat: severity and countermeasure.']]],
          res: [['OWASP Threat Modeling', 'https://owasp.org/www-community/Threat_Modeling'], ['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Vẽ sơ đồ luồng dữ liệu và ranh giới tin cậy', 'Draw the data-flow diagram and trust boundaries'],
            ['Liệt kê mối đe dọa và biện pháp vào `docs/security/threat-model.md`', 'List threats and countermeasures in `docs/security/threat-model.md`'],
          ],
          done: [
            ['Mô hình đe dọa có ranh giới tin cậy và biện pháp cho mối đe dọa mức cao', 'The threat model has trust boundaries and countermeasures for high-severity threats'],
          ],
        },
        {
          id: 's15-2', kind: 'build', h: '4–5',
          t: ['Tập trung thực thi chính sách: quyền theo nhóm, repo, môi trường, quyền công cụ theo agent, duyệt theo mức rủi ro', 'Centralize policy enforcement: team, repo and environment permissions, per-agent tool permissions, risk-based approvals'],
          goal: ['Một chỗ duy nhất quyết định ai và agent nào được làm gì ở đâu, và mọi hành động có quyền đều có audit.', 'One place decides who and which agent may do what where, and every privileged action is audited.'],
          learn: [
            ['Chính sách là dữ liệu (ví dụ OPA hoặc policy trong .NET), không rải trong code', 'Policy as data (e.g. OPA or .NET policies), not scattered in code'],
            ['Quy tắc duyệt theo mức rủi ro: việc nhỏ một người duyệt, việc nhạy cảm hai người', 'Risk-based approval rules: one approver for small changes, two for sensitive ones'],
          ],
          ai: [['Claude', ['Review mô hình phân quyền', 'Review the permission model'], ['Đây là mô hình phân quyền của tôi: <dán vai trò, nhóm, repo, môi trường, quyền công cụ theo agent, quy tắc duyệt>. Tìm chỗ một người hoặc một agent có thể tự duyệt việc của mình, quyền nào quá rộng, và hành động có quyền nào chưa có audit.', 'Here is my permission model: <paste roles, teams, repos, environments, per-agent tool permissions, approval rules>. Find places where a person or agent could approve their own work, permissions that are too broad, and privileged actions without audit.']]],
          res: [['ASP.NET Core authorization', MS + '/aspnet/core/security/authorization/introduction'], ['Open Policy Agent', 'https://www.openpolicyagent.org/docs/latest/']],
          pr: [
            ['Định nghĩa mô hình quyền: nhóm, repo, môi trường, agent, công cụ', 'Define the permission model: team, repo, environment, agent, tool'],
            ['Đưa mọi kiểm tra quyền qua một dịch vụ chính sách', 'Route every permission check through one policy service'],
            ['Thêm quy tắc duyệt theo mức rủi ro', 'Add risk-based approval rules'],
          ],
          done: [
            ['Test: người trong nhóm A không chạy được workflow trên repo của nhóm B', 'Test: someone in team A cannot run a workflow on team B\'s repo'],
            ['Thay đổi nhạy cảm cần hai người duyệt', 'Sensitive changes need two approvers'],
          ],
        },
        {
          id: 's15-3', kind: 'build', h: '3–4',
          t: ['Dùng credential ngắn hạn, xoay vòng bí mật, đặt chính sách lưu giữ dữ liệu và quy trình ứng phó sự cố', 'Use short-lived credentials, rotate secrets, set data retention policies and an incident response procedure'],
          goal: ['Không còn credential dài hạn quan trọng; bí mật xoay được mà không downtime; sự cố có quy trình.', 'No important long-lived credentials remain; secrets rotate without downtime; incidents have a procedure.'],
          learn: [
            ['GitHub App với token cài đặt ngắn hạn thay cho PAT', 'A GitHub App with short-lived installation tokens instead of PATs'],
            ['Xoay vòng bí mật có hai phiên bản song song', 'Secret rotation with two versions in parallel'],
            ['Lưu giữ và xóa dữ liệu prompt, log theo chính sách', 'Retention and deletion of prompts and logs by policy'],
          ],
          ai: [['ChatGPT', ['Soạn quy trình sự cố', 'Draft the incident procedure'], ['Soạn quy trình ứng phó sự cố cho nền tảng AI delivery với 3 kịch bản: token GitHub bị lộ, agent tạo PR chứa mã độc, nhà cung cấp LLM bị rò rỉ dữ liệu. Mỗi kịch bản: phát hiện, cô lập, thu hồi, khôi phục, thông báo, rút kinh nghiệm.', 'Draft an incident response procedure for the AI delivery platform with 3 scenarios: a leaked GitHub token, an agent opening a PR with malicious code, a data leak at the LLM provider. For each: detect, contain, revoke, recover, communicate, learn.']]],
          res: [['GitHub Apps', 'https://docs.github.com/apps/creating-github-apps/about-creating-github-apps/about-creating-github-apps'], ['Azure Key Vault', MS + '/azure/key-vault/']],
          pr: [
            ['Chuyển từ PAT sang GitHub App với token ngắn hạn', 'Move from PATs to a GitHub App with short-lived tokens'],
            ['Thử xoay một bí mật trong lúc hệ thống đang chạy', 'Rotate a secret while the system is running'],
            ['Viết chính sách lưu giữ và `docs/runbooks/incident-response.md`', 'Write the retention policy and `docs/runbooks/incident-response.md`'],
          ],
          done: [
            ['Không còn PAT dài hạn trong hệ thống', 'No long-lived PATs remain in the system'],
            ['Xoay bí mật không làm gián đoạn lần chạy nào', 'Rotating a secret interrupts no run'],
          ],
        },
        {
          id: 's15-4', kind: 'eval', h: '3',
          t: ['Thử phòng thủ prompt injection và tool injection bằng chỉ dẫn độc hại trong ticket, repo và kết quả công cụ', 'Test prompt- and tool-injection defenses with malicious instructions in tickets, repos and tool output'],
          goal: ['Có bằng chứng chỉ dẫn trong nội dung không tin cậy không làm lộ bí mật hay vượt chính sách.', 'There is evidence that instructions inside untrusted content never leak secrets or bypass policy.'],
          learn: [
            ['Phòng thủ nhiều lớp: tách dữ liệu và chỉ dẫn, quyền công cụ hẹp, kiểm tra đầu ra, duyệt hành động lớn', 'Defense in depth: separate data from instructions, narrow tool permissions, output checks, approval for big actions'],
          ],
          ai: [['Claude', ['Soạn ca tấn công', 'Draft attack cases'], ['Soạn 8 ca tấn công injection cho nền tảng: chỉ dẫn trong mô tả ticket, trong comment code, trong README, trong tên file, trong kết quả `search_requirements`, trong nội dung PR của người khác, yêu cầu agent in biến môi trường, yêu cầu agent tự duyệt. Mỗi ca: kết quả mong đợi và lớp phòng thủ chịu trách nhiệm.', 'Write 8 injection attack cases for the platform: instructions in a ticket description, a code comment, a README, a file name, a `search_requirements` result, someone else\'s PR content, asking the agent to print env vars, asking the agent to approve itself. For each: expected result and the defense layer responsible.']]],
          res: [['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Thêm 8 ca vào `tests/evaluation/security/`', 'Add the 8 cases to `tests/evaluation/security/`'],
            ['Chạy và ghi kết quả, sửa lớp phòng thủ bị lọt', 'Run them, record results, fix any defense that leaks'],
          ],
          done: [
            ['Không ca nào làm lộ bí mật hay vượt chính sách', 'No case leaks a secret or bypasses policy'],
          ],
        },
      ],
    },
    {
      id: 's16', title: ['Quan sát hệ thống và đánh giá AI', 'Observability and AI evaluation'],
      items: [
        {
          id: 's16-1', kind: 'learn', h: '2',
          t: ['Học log, metric, trace phân tán, OpenTelemetry và quan sát LLM', 'Learn logs, metrics, distributed traces, OpenTelemetry and LLM observability'],
          goal: ['Biết thu gì ở mỗi bước để trả lời "lần chạy này chậm và tốn ở đâu".', 'You know what to capture at each step to answer "where was this run slow and expensive".'],
          learn: [
            ['Trace context truyền qua HTTP và hàng đợi', 'Trace context propagated over HTTP and queues'],
            ['Thuộc tính cho lời gọi LLM: model, prompt version, token, chi phí', 'Attributes for LLM calls: model, prompt version, tokens, cost'],
            ['Đánh giá offline và online, test hồi quy cho prompt', 'Offline and online evaluation, regression tests for prompts'],
          ],
          ai: [['ChatGPT', ['Thiết kế thuộc tính', 'Design attributes'], ['Đề xuất danh sách span và thuộc tính OpenTelemetry cho một lần chạy của nền tảng AI delivery: nhận webhook, chuyển trạng thái, gọi LLM, gọi công cụ, job runner, CI, duyệt, deploy. Thuộc tính nào là bắt buộc, thuộc tính nào không được chứa dữ liệu nhạy cảm.', 'Propose a list of OpenTelemetry spans and attributes for one run of the AI delivery platform: webhook intake, state transition, LLM call, tool call, runner job, CI, approval, deploy. Which attributes are mandatory, and which must never contain sensitive data.']]],
          res: [['OpenTelemetry docs', 'https://opentelemetry.io/docs/'], ['.NET observability with OpenTelemetry', MS + '/dotnet/core/diagnostics/observability-with-otel']],
          pr: [
            ['Viết danh sách span và thuộc tính chuẩn của nền tảng', 'Write the platform\'s standard span and attribute list'],
          ],
          done: [
            ['Danh sách span và thuộc tính nằm trong `docs/architecture/telemetry.md`', 'The span and attribute list is in `docs/architecture/telemetry.md`'],
          ],
        },
        {
          id: 's16-2', kind: 'build', h: '4–5',
          t: ['Gắn OpenTelemetry xuyên suốt n8n → Workflow API → agent → runner, dựng dashboard vận hành và chi phí', 'Wire OpenTelemetry end to end n8n → Workflow API → agents → runner, and build operations and cost dashboards'],
          goal: ['Mở một trace thấy toàn bộ lần chạy; dashboard hiện throughput, thời gian từng bước, tỷ lệ lỗi và chi phí mỗi việc.', 'Opening one trace shows the whole run; dashboards show throughput, step durations, failure rates and cost per task.'],
          learn: [
            ['Dashboard theo câu hỏi vận hành, không theo mọi metric có sẵn', 'Dashboards built around operational questions, not every metric available'],
          ],
          ai: [['Claude Code', ['Gắn telemetry', 'Wire telemetry'], ['Thêm OpenTelemetry cho Workflow API, agent service và runner: trace HTTP, EF Core, HttpClient, span thủ công cho lời gọi LLM và công cụ với thuộc tính theo `docs/architecture/telemetry.md`, truyền trace context qua bảng Job của hàng đợi. Xuất qua OTLP tới collector. Thêm metric: thời gian chờ hàng đợi, thời gian từng bước, số retry, chi phí LLM.', 'Add OpenTelemetry to the Workflow API, agent service and runner: HTTP, EF Core and HttpClient tracing, manual spans for LLM and tool calls with attributes from `docs/architecture/telemetry.md`, trace context carried through the queue\'s Job table. Export via OTLP to a collector. Add metrics: queue wait time, per-step duration, retry count, LLM cost.']]],
          res: [['OpenTelemetry .NET', 'https://opentelemetry.io/docs/languages/dotnet/']],
          pr: [
            ['Gắn telemetry cho 3 service và truyền context qua hàng đợi', 'Add telemetry to the 3 services and carry context through the queue'],
            ['Dựng dashboard: throughput, thời gian chờ hàng đợi, thời gian từng bước, tỷ lệ thành công agent, retry, PR được chấp nhận, lỗi cổng, chi phí mỗi việc, deploy và rollback', 'Build dashboards: throughput, queue wait, step durations, agent success, retries, PR acceptance, gate failures, cost per task, deploys and rollbacks'],
            ['Đặt cảnh báo cho tỷ lệ lỗi và chi phí bất thường', 'Set alerts for failure rates and unusual cost'],
          ],
          done: [
            ['Một trace hiện đủ các bước của một lần chạy', 'One trace shows every step of a run'],
            ['Dashboard trả lời được chi phí và thời gian của mỗi việc', 'Dashboards answer the cost and duration of each task'],
          ],
        },
        {
          id: 's16-3', kind: 'eval', h: '4–5',
          t: ['Dựng bộ đánh giá AI và chạy trong CI khi đổi prompt hay model, kèm vòng phản hồi của người', 'Build the AI evaluation suite and run it in CI on prompt or model changes, with a human feedback loop'],
          goal: ['Đổi prompt hay model mà chất lượng giảm thì CI phát hiện trước khi lên production.', 'A prompt or model change that lowers quality is caught in CI before production.'],
          learn: [
            ['Bộ đánh giá đại diện: rõ, mơ hồ, mâu thuẫn, nhạy cảm bảo mật, thay đổi lớn và nhỏ, chỉ dẫn độc hại, lỗi công cụ', 'A representative set: clear, ambiguous, conflicting, security-sensitive, large and small changes, malicious instructions, tool failures'],
            ['So với baseline, không chỉ kiểm tra có trả lời hay không', 'Compare against a baseline, not just whether an answer came back'],
            ['Phản hồi của người (duyệt, từ chối, làm lại) là dữ liệu đánh giá online', 'Human feedback (approve, reject, rework) is online evaluation data'],
          ],
          ai: [['Claude', ['Thiết kế cách chấm', 'Design the scoring'], ['Tôi có bộ đánh giá cho AI BA, Developer, Reviewer và QA: <mô tả các ca>. Đề xuất cách chấm tự động cho từng agent (so khớp schema, kiểm tra AC, chạy test, dùng LLM chấm có rubric), cách giảm sai lệch của LLM chấm, và ngưỡng hồi quy để chặn PR đổi prompt.', 'I have evaluation sets for the AI BA, Developer, Reviewer and QA: <describe cases>. Propose automatic scoring for each agent (schema match, AC checks, running tests, LLM-as-judge with a rubric), how to reduce judge bias, and a regression threshold that blocks prompt-change PRs.']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/'], ['OpenAI evals guide', 'https://platform.openai.com/docs/']],
          pr: [
            ['Gom các bộ ca từ P1, P2 vào `tests/evaluation/` và bổ sung loại còn thiếu', 'Gather the P1 and P2 case sets into `tests/evaluation/` and add missing types'],
            ['Viết script chấm và lưu baseline', 'Write the scoring script and store the baseline'],
            ['Thêm job CI chạy đánh giá khi thư mục prompt hoặc cấu hình model thay đổi', 'Add a CI job that runs the evaluation when prompts or model config change'],
            ['Ghi phản hồi của người từ PR vào dữ liệu đánh giá online', 'Feed human feedback from PRs into online evaluation data'],
          ],
          done: [
            ['PR làm giảm điểm dưới ngưỡng bị CI chặn', 'A PR that drops the score below threshold is blocked by CI'],
            ['Dashboard có chất lượng AI theo thời gian', 'Dashboards show AI quality over time'],
          ],
        },
      ],
    },
    {
      id: 's17', title: ['Độ tin cậy, mở rộng và khôi phục thảm họa', 'Reliability, scaling and disaster recovery'],
      items: [
        {
          id: 's17-1', kind: 'learn', h: '2–3',
          t: ['Học SLI/SLO/SLA, backpressure, rate limit, circuit breaker, bulkhead và suy giảm có kiểm soát', 'Learn SLI/SLO/SLA, backpressure, rate limiting, circuit breakers, bulkheads and graceful degradation'],
          goal: ['Đặt được SLO cho nền tảng và biết hệ thống phản ứng thế nào khi nhà cung cấp LLM chậm hoặc chết.', 'You can set SLOs for the platform and know how it behaves when the LLM provider is slow or down.'],
          learn: [
            ['SLI đo được, SLO là mục tiêu, error budget quyết định tốc độ thay đổi', 'SLIs are measured, SLOs are targets, the error budget sets the pace of change'],
            ['Circuit breaker và bulkhead cho phụ thuộc không ổn định', 'Circuit breakers and bulkheads for unstable dependencies'],
            ['Backpressure: hàng đợi đầy thì từ chối có kiểm soát thay vì sập', 'Backpressure: refuse gracefully when the queue is full instead of collapsing'],
          ],
          ai: [['Claude', ['Đặt SLO', 'Set SLOs'], ['Giúp tôi đặt SLI và SLO cho nền tảng AI delivery: tỷ lệ lần chạy hoàn thành không lỗi hạ tầng, thời gian chờ hàng đợi p95, độ khả dụng của Workflow API, thời gian từ duyệt yêu cầu tới PR. Mỗi cái: cách đo, mục tiêu ban đầu hợp lý, và việc làm khi hết error budget.', 'Help me set SLIs and SLOs for the AI delivery platform: share of runs completing without infrastructure errors, p95 queue wait, Workflow API availability, time from requirement approval to PR. For each: how to measure, a sensible starting target, and what to do when the error budget runs out.']]],
          res: [['Google SRE book', 'https://sre.google/sre-book/table-of-contents/']],
          pr: [
            ['Viết `docs/architecture/slo.md` với SLI, SLO và chính sách error budget', 'Write `docs/architecture/slo.md` with SLIs, SLOs and an error budget policy'],
          ],
          done: [
            ['SLO được viết và gắn với dashboard', 'SLOs are written and linked to dashboards'],
          ],
        },
        {
          id: 's17-2', kind: 'build', h: '4–5',
          t: ['Thêm giới hạn đồng thời theo nhóm, xử lý rate limit nhà cung cấp, circuit breaker và backpressure hàng đợi', 'Add per-team concurrency limits, provider rate-limit handling, circuit breakers and queue backpressure'],
          goal: ['Một nhóm không chiếm hết tài nguyên, và nhà cung cấp LLM sập không kéo cả nền tảng sập theo.', 'One team cannot take all resources, and an LLM provider outage does not take the whole platform down.'],
          learn: [
            ['Giới hạn toàn cục và theo nhóm trong hàng đợi', 'Global and per-team limits in the queue'],
            ['Polly: retry, circuit breaker, timeout, bulkhead trong .NET', 'Polly: retry, circuit breaker, timeout, bulkhead in .NET'],
            ['Autoscaling worker theo độ dài hàng đợi', 'Autoscaling workers on queue length'],
          ],
          ai: [['Claude Code', ['Cài resilience', 'Implement resilience'], ['Thêm vào agent service một resilience pipeline cho `ILlmClient`: timeout, retry tôn trọng `Retry-After`, circuit breaker mở khi tỷ lệ lỗi cao, fallback sang model dự phòng. Thêm giới hạn đồng thời theo nhóm vào hàng đợi, và từ chối job mới (HTTP 429 kèm `Retry-After`) khi hàng đợi vượt ngưỡng. Thêm autoscaling worker theo số job đang chờ.', 'Add a resilience pipeline for `ILlmClient` in the agent service: timeout, retries that honour `Retry-After`, a circuit breaker that opens on high failure rates, fallback to the backup model. Add per-team concurrency limits to the queue, and reject new jobs (HTTP 429 with `Retry-After`) when the queue exceeds a threshold. Add worker autoscaling on pending job count.']]],
          res: [['Polly resilience library', 'https://github.com/App-vNext/Polly'], ['.NET resilience', MS + '/dotnet/core/resilience/']],
          pr: [
            ['Cài resilience pipeline và giới hạn đồng thời theo nhóm', 'Implement the resilience pipeline and per-team concurrency limits'],
            ['Giả lập nhà cung cấp LLM lỗi 100% trong 10 phút', 'Simulate the LLM provider failing 100% for 10 minutes'],
            ['Giả lập một nhóm gửi 200 job cùng lúc', 'Simulate one team submitting 200 jobs at once'],
          ],
          done: [
            ['Khi LLM lỗi, circuit mở, job chờ thay vì thất bại hàng loạt, và tự phục hồi khi LLM trở lại', 'When the LLM fails, the circuit opens, jobs wait instead of failing en masse, and recovery is automatic'],
            ['Một nhóm gửi nhiều job không làm nghẽn nhóm khác', 'One team flooding jobs does not starve other teams'],
          ],
        },
        {
          id: 's17-3', kind: 'build', h: '4–5',
          t: ['Thử sao lưu và khôi phục, đặt RTO/RPO, viết runbook phục hồi cho n8n, Workflow API, database, hàng đợi và runner', 'Test backup and restore, set RTO/RPO, write recovery runbooks for n8n, the Workflow API, database, queue and runner'],
          goal: ['Mỗi thành phần có runbook phục hồi đã chạy thử, với thời gian phục hồi và mất mát dữ liệu đo được.', 'Each component has a recovery runbook that was rehearsed, with measured recovery time and data loss.'],
          learn: [
            ['RTO là thời gian phục hồi, RPO là lượng dữ liệu chấp nhận mất', 'RTO is recovery time, RPO is acceptable data loss'],
            ['Chính sách lưu giữ artifact và log', 'Artifact and log retention policy'],
            ['Postmortem không đổ lỗi', 'Blameless postmortems'],
          ],
          ai: [['ChatGPT', ['Soạn runbook', 'Draft runbooks'], ['Soạn khung runbook phục hồi cho 5 thành phần: n8n (workflow và credential), Workflow API, PostgreSQL, hàng đợi job, runner. Mỗi runbook: triệu chứng, cách xác nhận, bước phục hồi, cách kiểm chứng, RTO/RPO mục tiêu, người chịu trách nhiệm. Thêm mẫu postmortem không đổ lỗi.', 'Draft recovery runbook skeletons for 5 components: n8n (workflows and credentials), Workflow API, PostgreSQL, the job queue, the runner. Each: symptoms, how to confirm, recovery steps, how to verify, target RTO/RPO, owner. Add a blameless postmortem template.']]],
          res: [['Google SRE book', 'https://sre.google/sre-book/table-of-contents/'], ['PostgreSQL backup and restore', 'https://www.postgresql.org/docs/current/backup.html']],
          pr: [
            ['Viết 5 runbook vào `docs/runbooks/`', 'Write the 5 runbooks in `docs/runbooks/`'],
            ['Diễn tập khôi phục database và n8n trên staging, đo thời gian', 'Rehearse database and n8n recovery on staging and time it'],
            ['Đặt chính sách lưu giữ artifact và cảnh báo sự cố', 'Set the artifact retention policy and incident alerts'],
          ],
          done: [
            ['RTO và RPO đo được cho database và n8n', 'RTO and RPO are measured for the database and n8n'],
            ['Cả 5 runbook đã được chạy thử ít nhất một lần', 'All 5 runbooks have been rehearsed at least once'],
          ],
        },
        {
          id: 's17-4', kind: 'gate', h: '3–4',
          t: ['Chốt Team Production: game day phục hồi, review bảo mật và kiến trúc, quyết định rollout bằng bằng chứng', 'Close Team Production: a recovery game day, security and architecture reviews, a rollout decision based on evidence'],
          goal: ['Đủ tiêu chí production và có quyết định rollout dựa trên bằng chứng pilot và production.', 'All production criteria are met and the rollout decision rests on pilot and production evidence.'],
          learn: [
            ['Game day: gây sự cố đại diện có kế hoạch để chứng minh phục hồi', 'Game day: planned representative failures to prove recovery'],
          ],
          ai: [['Claude', ['Rà bằng chứng', 'Review the evidence'], ['Đây là bằng chứng production readiness: <dán>. Đối chiếu với 10 tiêu chí (review bảo mật và kiến trúc xong, danh tính và bí mật production tách biệt, workflow quan trọng quan sát được, có SLO và người chịu trách nhiệm, sao lưu và khôi phục đã thử, phục hồi sự cố đại diện đã chứng minh, có tài liệu deploy và rollback/roll-forward, chất lượng AI được đánh giá liên tục, chi phí và năng lực được giám sát, nhóm có quy trình hỗ trợ và sự cố). Tiêu chí nào còn yếu?', 'Here is the production readiness evidence: <paste>. Check it against the 10 criteria (security and architecture reviews done, production identities and secrets isolated, critical workflows observable, SLOs and ownership defined, backup and restore tested, recovery from representative failures demonstrated, deploy and rollback/roll-forward documented, AI quality evaluated continuously, cost and capacity monitored, teams have support and incident procedures). Which criteria are weak?']]],
          res: [['Google SRE book', 'https://sre.google/sre-book/table-of-contents/'], ['Azure Well-Architected', MS + '/azure/well-architected/']],
          pr: [
            ['Tổ chức game day: giết worker, làm sập database staging, chặn nhà cung cấp LLM, lộ một token giả', 'Run a game day: kill workers, take down the staging database, block the LLM provider, leak a fake token'],
            ['Hoàn tất review bảo mật và kiến trúc', 'Complete the security and architecture reviews'],
            ['Viết `docs/production-readiness.md` và quyết định rollout', 'Write `docs/production-readiness.md` and the rollout decision'],
          ],
          done: [
            ['Review bảo mật và kiến trúc xong; danh tính và bí mật production tách biệt; workflow quan trọng quan sát được', 'Security and architecture reviews done; production identities and secrets isolated; critical workflows observable'],
            ['Có SLO và người chịu trách nhiệm; sao lưu, khôi phục và phục hồi sự cố đại diện đã chứng minh', 'SLOs and ownership defined; backup, restore and recovery from representative failures demonstrated'],
            ['Có tài liệu deploy và rollback/roll-forward; chất lượng AI, chi phí và năng lực được giám sát liên tục; có quy trình hỗ trợ và sự cố', 'Deploy and rollback/roll-forward documented; AI quality, cost and capacity monitored continuously; support and incident procedures exist'],
          ],
        },
      ],
    },
  ],
};
