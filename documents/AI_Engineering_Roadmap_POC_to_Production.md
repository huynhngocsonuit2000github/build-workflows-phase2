# AI Engineering Roadmap

## AI-Powered Software Delivery Platform --- POC → Team Pilot → Team Production

**Primary stack:** C# / .NET, Angular, n8n, PostgreSQL or SQL Server,
GitHub, Docker, Kubernetes, cloud services, and LLM/agent tooling.

**Project:** Build an AI-powered software delivery system for a Task
Management application. The system takes a Jira requirement and supports
the journey from analysis through implementation, review, testing,
documentation, and deployment---with human approval and auditable
controls.

**Purpose:** A learning and implementation plan. Progress through exit
criteria, not just estimated weeks.

------------------------------------------------------------------------

# 1. Vision and engineering principles

## 1.1 Target outcome

Build a platform that can: 1. Receive a Jira issue or approved feature
request. 2. Validate and clarify requirements. 3. Generate user stories
and acceptance criteria. 4. Produce an implementation plan. 5. Inspect
an existing .NET and Angular repository. 6. Implement a feature in an
isolated workspace. 7. Run build, unit, integration, security, and
quality checks. 8. Request independent AI review. 9. Create a pull
request with traceable evidence. 10. Wait for human approval where
required. 11. Deploy to approved environments. 12. Run smoke and
business-flow tests. 13. Update Jira and technical documentation. 14.
Report execution status, duration, model usage, and cost. 15. Recover
safely from retries, crashes, duplicate events, and partial failures.

## 1.2 Core architecture principle

**Use deterministic software to control the process; use AI agents for
tasks that require reasoning.**

-   Workflow orchestration controls sequence, state, routing, retries,
    timeouts, and approvals.
-   AI agents interpret requirements, plan changes, write code, review
    changes, and help design tests.
-   .NET services enforce business rules, permissions, state
    transitions, execution policies, and audit.
-   Isolated runners execute untrusted code and tools with restricted
    permissions.
-   CI/CD provides objective build, test, scan, and deployment evidence.
-   Humans retain control over important decisions, especially merge and
    production release.

Do not make every step an autonomous agent. Do not let an LLM decide
whether a security gate or approval requirement can be bypassed.

## 1.3 Technology selection

  -----------------------------------------------------------------------
  Concern                 Initial choice          Later evaluation
  ----------------------- ----------------------- -----------------------
  Main application API    ASP.NET Core / .NET     Keep

  Frontend                Angular                 Keep

  Workflow orchestration  n8n                     Keep for business-level
                                                  orchestration

  Workflow state and      .NET service +          Durable workflow engine
  policy                  relational DB           if needed

  Database                PostgreSQL or SQL       Managed database
                          Server                  

  AI model access         Provider abstraction in Multiple providers and
                          .NET                    model routing

  Agent implementation    .NET tools / SDKs or    LangGraph where
                          small agent runtime     graph-based agent state
                                                  is valuable

  Tool integration        Function/tool calling;  Governed MCP servers
                          MCP where appropriate   

  Source control          GitHub                  GitHub organization
                                                  controls

  CI/CD                   GitHub Actions          Organization-level
                                                  reusable workflows

  Execution               Isolated Docker-based   Ephemeral sandbox /
                          runner                  Kubernetes jobs

  Local development       Docker Compose          Cloud environments

  Production runtime      Not required for POC    Managed Kubernetes or
                                                  suitable container
                                                  platform

  Observability           Structured logs and     Centralized logs,
                          OpenTelemetry           metrics, traces, alerts

  Secrets                 Local development       Cloud secret manager
                          secrets for POC         and workload identity
  -----------------------------------------------------------------------

**Important:** n8n, LangGraph, and Temporal solve different problems.

-   n8n: visual integration and business workflow orchestration.
-   LangGraph: stateful, graph-based agent orchestration when an agent
    needs controlled loops, checkpoints, and human interaction.
-   Temporal: durable execution for long-running distributed workflows
    that must survive process and infrastructure failures.

Start with n8n + .NET. Introduce LangGraph or Temporal only when a
concrete requirement justifies the added operational complexity.

------------------------------------------------------------------------

# 2. Product scope: Task Management application

## 2.1 Core business modules

### Identity and access

-   User registration/invitation.
-   Login and logout.
-   Role-based authorization.
-   Organization or workspace membership.
-   Audit of important actions.

### Project management

-   Create and update projects.
-   Assign project members.
-   Project status and ownership.
-   Project-level permissions.

### Task management

-   Create, read, update, and delete tasks.
-   Assign users.
-   Priority, status, due date, labels.
-   Comments and activity history.
-   Search, filter, and pagination.

### Workflow and notifications

-   Configurable task status transitions.
-   Notifications for assignment and due dates.
-   Basic email or in-app notification.
-   Background processing.

### Reporting

-   Tasks by status and assignee.
-   Overdue tasks.
-   Project progress.
-   Sprint or iteration summary.

## 2.2 Non-functional requirements

-   Authentication and authorization.
-   Input validation.
-   API versioning where appropriate.
-   Structured logging.
-   Database migrations.
-   Automated tests.
-   Secure configuration and secrets.
-   Containerized local development.
-   CI/CD.
-   Backup/restore for production.
-   Performance and reliability targets for production.

## 2.3 Suggested initial domain model

-   User
-   Organization
-   OrganizationMember
-   Project
-   ProjectMember
-   Task
-   TaskComment
-   TaskActivity
-   Notification
-   WorkflowDefinition (optional)
-   WorkflowExecution (for the AI delivery platform)

Keep the business application and AI delivery platform logically
separated. They may initially live in one repository for learning, but
should have clear boundaries.

------------------------------------------------------------------------

# 3. End-to-end AI Software Delivery workflow

## 3.1 Target process

``` text
Jira Issue / Feature Request
          |
          v
   Intake and Validation
          |
          v
      AI BA Agent
          |
          v
 Requirements + User Story + AC
          |
          v
      Human Approval
          |
          v
   Implementation Planning
          |
          v
    AI Developer Agent
          |
          v
 Isolated Workspace / Runner
          |
          v
 Build + Unit + Integration Tests
          |
          v
    AI Reviewer Agent
          |
          v
 Security and Quality Gates
          |
          v
       Pull Request
          |
          v
      Human Approval
          |
          v
       Merge to Main
          |
          v
       CI/CD Pipeline
          |
          v
      Deploy to Dev / QA
          |
          v
 Smoke + E2E + Business Tests
          |
          v
   Human Production Approval
          |
          v
      Production Deploy
          |
          v
 Monitoring + Documentation
          |
          v
      Jira Update / Report
```

## 3.2 Responsibility boundaries

  -----------------------------------------------------------------------
  Component               Owns                    Must not own
  ----------------------- ----------------------- -----------------------
  n8n                     Triggers, routing,      Unrestricted host
                          notifications,          execution
                          integrations            

  .NET Workflow API       Workflow state,         LLM reasoning
                          transition rules,       
                          policy, permissions,    
                          execution records       

  AI BA                   Requirement             Final business approval
                          interpretation,         
                          questions, stories,     
                          acceptance criteria     

  AI Developer            Repository analysis,    Permission to merge or
                          implementation plan,    deploy production
                          code changes, tests     

  AI Reviewer             Independent review and  Replacing CI or human
                          findings                review

  AI QA                   Test design, test data  Declaring gates passed
                          suggestions, failure    without evidence
                          analysis                

  Runner                  Restricted execution of Broad access to secrets
                          approved commands and   or production
                          tools                   infrastructure

  CI/CD                   Repeatable build, test, Trusting agent
                          scan, artifact,         self-reported success
                          deployment              

  Human                   Requirement decisions,  Manually repeating
                          code approval, release  every deterministic
                          approval                task
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 4. Phase 0 --- Foundations and project setup

**Estimated duration:** 1--2 weeks\
**Goal:** Establish the application, repositories, development
environment, and engineering baseline before introducing agents.

## Learn

-   ASP.NET Core architecture and dependency injection.
-   Angular application structure.
-   REST API design.
-   Relational database design and migrations.
-   Git branching, pull requests, and code review.
-   Docker and Docker Compose.
-   Unit and integration testing.
-   Basic CI with GitHub Actions.

## Build

1.  Create the Task Management API in .NET.
2.  Create the Angular application.
3.  Configure PostgreSQL or SQL Server.
4.  Implement one vertical slice: create and list projects/tasks.
5.  Add authentication and authorization basics.
6.  Add database migrations.
7.  Add unit and API integration tests.
8.  Create Dockerfiles and Docker Compose.
9.  Add GitHub Actions for build and test.
10. Create Jira project and issue conventions.

## Deliverables

-   Working .NET API and Angular UI.
-   Database schema and migration process.
-   Docker Compose local environment.
-   CI build and test workflow.
-   README with local setup.
-   Initial architecture diagram.

## Exit criteria

-   A developer can clone the repository and run the application.
-   CI builds the API and frontend.
-   Tests run automatically.
-   A feature can be delivered through a normal human-led PR.

------------------------------------------------------------------------

# 5. Phase 1 --- POC: Workflow orchestration and first AI agents

**Estimated duration:** 4--6 weeks\
**Goal:** Demonstrate one complete Jira-to-PR workflow with controlled
AI assistance.

## Step 1. Workflow orchestration and state machines

### Learn

-   Deterministic workflows.
-   State machines and state transitions.
-   Workflow orchestration versus agent orchestration.
-   Events, triggers, webhooks, and polling.
-   Workflow execution IDs and correlation IDs.
-   Retry, timeout, and error handling basics.
-   Idempotency and duplicate event handling.

### Build

Create a .NET Workflow API.

Suggested entities:

**WorkflowExecution** - Id - JiraIssueKey - CurrentState - Status -
CreatedAt - UpdatedAt - CorrelationId - Version

**WorkflowHistory** - Id - WorkflowExecutionId - FromState - ToState -
ChangedAt - ChangedBy - Reason

Suggested states:

``` text
Created
RequirementAnalysis
AwaitingRequirementApproval
ImplementationPlanning
Coding
BuildAndTest
CodeReview
AwaitingCodeApproval
PullRequestCreated
ReadyForDeployment
Completed
Failed
Cancelled
```

Rules: - Validate every transition in .NET. - Reject invalid
transitions. - Persist every state change. - Expose execution status and
history APIs. - Use optimistic concurrency or another safe concurrency
mechanism. - Do not use Jira labels as the only source of execution
state.

### n8n responsibilities

-   Receive Jira webhook or poll for eligible issues.
-   Call the .NET Workflow API.
-   Route based on returned state.
-   Invoke approved agent/runner endpoints.
-   Send notifications.
-   Update Jira after confirmed outcomes.

### Exit criteria

-   Workflow state is persisted.
-   Invalid transitions are rejected.
-   Duplicate webhook events do not create duplicate executions.
-   Execution history can be inspected.
-   A failed workflow can be identified and safely retried.

## Step 2. LLM engineering

### Learn

-   Tokens, context windows, latency, and cost.
-   System, developer, and user instructions.
-   Prompt design and prompt versioning.
-   Structured outputs and JSON schema.
-   Temperature and model selection.
-   Context construction and truncation.
-   Hallucinations and uncertainty.
-   Provider abstraction and model fallback.
-   Basic prompt injection risks.

### Build

Create a .NET LLM service abstraction.

Responsibilities: - Model provider interface. - Prompt template
management. - Structured output validation. - Timeout and retry
policy. - Token and cost recording. - Request/response metadata
logging. - Model and prompt version tracking.

First AI use case: AI BA generates a structured requirement analysis.

Example output:

``` json
{
  "summary": "Allow project members to create tasks",
  "userStory": "As a project member, I want to create a task...",
  "acceptanceCriteria": [
    "A project member can create a task",
    "A task must have a title",
    "Only authorized project members can create tasks"
  ],
  "openQuestions": [],
  "assumptions": []
}
```

Validate the output against a schema. Treat generated content as a
proposal, not trusted truth.

### Exit criteria

-   Valid structured output is produced.
-   Invalid output is rejected or safely retried.
-   Prompt and model versions are traceable.
-   Token usage and approximate cost are recorded.
-   BA output can be reviewed by a human.

## Step 3. Tool calling and MCP

### Learn

-   Function calling and tool schemas.
-   Tool selection and argument validation.
-   Tool authorization.
-   Read-only versus write tools.
-   MCP concepts: host, client, server, tools, resources, and prompts.
-   Tool output validation.
-   Tool timeout and failure handling.
-   Untrusted tool results and prompt injection.

### Build

Give the BA agent a limited set of tools: - Get Jira issue. - Get
project metadata. - Search existing requirements. - Read approved
documentation. - Add a draft Jira comment.

Start with read-only tools. Require approval for write operations.

Build one small MCP server only after understanding the basic
function/tool-calling flow. Do not expose unrestricted shell,
filesystem, Git, or production tools to the agent.

### Exit criteria

-   Agent can select an appropriate tool.
-   Arguments are validated.
-   Unauthorized actions are rejected.
-   Tool failures are handled.
-   Tool calls are auditable.

## Step 4. Agent architecture

### Learn

-   Agent loop: observe, reason, act, observe.
-   Tool-enabled agents.
-   Context and conversation state.
-   Planning versus execution.
-   Bounded iteration and stopping conditions.
-   Human-in-the-loop.
-   Memory types and their risks.
-   Agent permissions and sandboxing.

### Build

Create an AI BA Agent that: 1. Reads a Jira issue. 2. Retrieves relevant
project context. 3. Identifies missing information. 4. Generates a user
story and acceptance criteria. 5. Records assumptions and open
questions. 6. Requests human approval. 7. Saves the approved requirement
artifact.

Keep the agent's tools limited to the BA role.

### Exit criteria

-   BA agent handles representative requirements.
-   Missing information is surfaced rather than invented.
-   Output is schema-validated.
-   Human approval is required before implementation.
-   All agent actions are traceable.

## Step 5. AI Developer and AI Reviewer

### Learn

-   Repository context gathering.
-   Codebase indexing and search.
-   Implementation planning.
-   Patch-based changes.
-   Git branches and worktrees.
-   Test-driven development concepts.
-   Independent code review.
-   Review findings and severity.
-   Agent iteration limits.

### Build

AI Developer: 1. Receives approved requirement and AC. 2. Inspects
repository. 3. Produces an implementation plan. 4. Creates an isolated
branch/worktree. 5. Makes changes. 6. Runs approved build and test
commands. 7. Produces a change summary and evidence.

AI Reviewer: 1. Receives the diff and approved AC. 2. Reviews
correctness, maintainability, security, and test coverage. 3. Produces
structured findings with file and line references. 4. Does not modify
code by default. 5. Requests bounded fixes only for actionable findings.

### Exit criteria

-   One small feature can be implemented by the agent.
-   Changes are isolated from other executions.
-   Build and tests run independently of agent claims.
-   Review findings are structured and actionable.
-   A human reviews and merges the PR.

## Step 6. POC end-to-end integration

Connect Jira → n8n → .NET Workflow API → BA → human approval → Developer
→ build/test → Reviewer → PR.

### POC exit criteria

-   One Jira ticket completes the full flow.
-   Workflow state and history persist.
-   Every run has a correlation ID.
-   Agent output is validated.
-   Build/test evidence is retained.
-   PR is created successfully.
-   Human approval controls merge.
-   Failures are visible and can be retried safely.
-   No unrestricted runner or production credentials are exposed.

------------------------------------------------------------------------

# 6. Phase 2 --- Team Pilot: Reliability, concurrency, QA, and security

**Estimated duration:** 6--8 weeks\
**Goal:** Support a small development team using the workflow on
selected repositories and low-risk tasks.

## Step 7. Runner architecture and isolation

### Learn

-   Worker and job execution models.
-   Process isolation and container security.
-   Ephemeral execution environments.
-   Filesystem and network restrictions.
-   Least privilege.
-   Secret isolation.
-   Resource limits.
-   Artifact storage.
-   Command allowlists and policy enforcement.

### Build

Create a dedicated .NET Runner service.

Responsibilities: - Accept authenticated execution requests. - Validate
job type and policy. - Create an isolated workspace. - Checkout the
approved repository and revision. - Run allowlisted commands. - Capture
logs, exit codes, and artifacts. - Enforce CPU, memory, and time
limits. - Destroy the environment after completion.

Security requirements: - Do not run untrusted code directly on the
orchestration host. - Do not expose Docker socket to the coding agent. -
Do not mount production secrets into agent workspaces. - Restrict
outbound network access. - Use short-lived, scoped credentials. - Record
command execution and actor identity.

## Step 8. Queue, concurrency, and distributed systems

### Learn

-   Queue and worker patterns.
-   At-least-once delivery.
-   Idempotent consumers.
-   Job leases and lease expiration.
-   Retry with exponential backoff and jitter.
-   Dead-letter queues.
-   Optimistic concurrency.
-   Distributed locks and their limitations.
-   Outbox and inbox patterns.
-   Eventual consistency.
-   Saga and compensation.
-   Reconciliation jobs.

### Build

Introduce a queue only when concurrent execution is needed.

Implement: - Job table or message broker. - Worker registration. - Lease
acquisition and renewal. - Lease expiration and recovery. - Retry count
and retry classification. - Dead-letter handling. - Idempotency keys. -
Execution deduplication. - Reconciliation for partial failures.

Potential broker choices: - RabbitMQ. - Azure Service Bus. - AWS SQS. -
Another managed queue that fits the deployment platform.

Do not add a broker just for fashion. A database-backed queue can be
enough for a small pilot if its limits are understood.

### Exit criteria

-   Multiple jobs execute concurrently within configured limits.
-   Duplicate events do not create duplicate side effects.
-   Crashed jobs can be recovered.
-   Expired leases are reclaimed.
-   Permanent failures are visible.
-   Partial integration failures can be reconciled.

## Step 9. AI QA and test engineering

### Learn

-   Test pyramid.
-   Unit, integration, contract, E2E, and regression tests.
-   Test data management.
-   Test isolation.
-   Flaky tests.
-   API and UI test automation.
-   Requirement-to-test traceability.
-   AI-generated test validation.

### Build

AI QA agent: - Converts approved acceptance criteria into test
scenarios. - Identifies positive, negative, boundary, and authorization
cases. - Suggests test data. - Generates test skeletons where useful. -
Analyzes failures and groups likely causes.

Automated execution: - .NET unit tests. - API integration tests. -
Angular unit tests. - E2E tests using a suitable framework. - Smoke
tests for deployed environments.

AI may propose tests, but CI must execute them and retain the evidence.

### Exit criteria

-   Every accepted AC maps to one or more test scenarios.
-   Critical business flows have automated tests.
-   Test results are retained.
-   Flaky and failed tests are distinguishable.
-   AI does not mark a test as passed without execution evidence.

## Step 10. Code quality and supply-chain security

### Learn

-   SAST.
-   SCA and dependency vulnerabilities.
-   Secret scanning.
-   Container image scanning.
-   SBOM.
-   License policy.
-   Code coverage.
-   Quality gates.
-   Branch protection.
-   Signed artifacts and provenance concepts.

### Build

Add CI gates: - Build and test. - Coverage thresholds appropriate to the
repository. - Static analysis. - Dependency vulnerability checks. -
Secret scanning. - Container scanning. - License policy where
required. - Artifact and scan result retention.

Use GitHub branch protection: - Require pull requests. - Require CI
checks. - Require code owner or reviewer approval where applicable. -
Dismiss stale approvals after relevant changes. - Require approval of
the latest reviewable push where supported.

A changed PR head must be reviewed and approved according to repository
protection policy. Do not preserve approval merely because an earlier
version was approved.

## Step 11. Database migration and deployment safety

### Learn

-   Database migration versioning.
-   Expand-and-contract migrations.
-   Backward compatibility.
-   Rolling deployment.
-   Blue/green and canary deployment.
-   Readiness and liveness probes.
-   Rollback versus roll-forward.
-   Backup and restore.
-   Deployment verification.

### Build

-   Version database migrations.
-   Run migrations through a controlled deployment step.
-   Test migration against representative data.
-   Keep application versions compatible during rolling deployment.
-   Define rollback and roll-forward procedures.
-   Add post-deployment smoke and business-flow tests.

Do not assume rolling back application containers reverses database
changes.

## Step 12. Team Pilot governance and feedback

### Learn

-   Role-based access control.
-   Separation of duties.
-   Approval policies.
-   Audit trails.
-   AI usage policies.
-   Human review design.
-   Developer feedback and adoption metrics.

### Build

Pilot with a small team and a restricted set of repositories.

Policies: - AI can create branches and PRs. - AI cannot bypass branch
protection. - Human approval required for merge. - Human approval
required for production release. - Sensitive repositories and tasks can
be excluded. - Developers can stop or cancel a run. - All AI-generated
changes are clearly identified.

Measure: - Workflow completion rate. - Time to PR. - PR acceptance and
rework rate. - Build/test pass rate. - Escaped defects. - Agent failure
rate. - Cost per completed task. - Developer satisfaction.

### Team Pilot exit criteria

-   Multiple developers use the system successfully.
-   Concurrent execution is safe.
-   Runner is isolated and permissioned.
-   Retry and recovery are demonstrated.
-   QA and security gates run automatically.
-   Audit evidence is available.
-   Pilot metrics show useful value without unacceptable quality or
    security regression.
-   Team has a documented operating procedure.

------------------------------------------------------------------------

# 7. Phase 3 --- Team Production: Scale, governance, and operational excellence

**Estimated duration:** 8--12+ weeks\
**Goal:** Operate the platform reliably across multiple teams and
repositories with production-grade security and controls.

## Step 13. Production architecture and cloud infrastructure

### Learn

-   Cloud networking and identity.
-   Managed container platforms.
-   Kubernetes architecture and operations.
-   Autoscaling.
-   Ingress and load balancing.
-   Managed databases.
-   Object and artifact storage.
-   Network policies.
-   Infrastructure as Code.
-   Environment separation.
-   Disaster recovery design.

### Build

Select a cloud platform based on organization requirements.

Possible Azure-oriented implementation: - Azure Kubernetes Service or an
appropriate managed container service. - Azure Database for PostgreSQL
or SQL Server offering. - Azure Container Registry. - Azure Key Vault. -
Managed identity / workload identity. - Azure Monitor and Application
Insights. - Private networking where required.

Equivalent services can be selected on AWS or GCP.

Production environments: - Development. - QA / staging. - Production.

Separate identities, secrets, network access, and deployment permissions
by environment.

## Step 14. Durable workflow execution

### Learn

-   Durable execution.
-   Workflow replay and determinism.
-   Checkpointing.
-   Long-running workflows.
-   Timers and signals.
-   Compensation.
-   Workflow versioning.
-   Upgrade and migration of running workflows.

### Evaluate

Keep n8n for integrations and business-level routing where appropriate.

Evaluate Temporal when: - Workflows run for a long time. - Execution
must survive process and infrastructure failures. - Durable timers and
signals are needed. - Complex retry and compensation logic becomes
difficult to maintain. - Workflow versioning and replay become
important.

Evaluate LangGraph when: - An individual agent requires graph-based
state. - The agent has controlled cycles and checkpoints. - Human
interaction occurs inside the agent graph. - Agent state and branching
become too complex for a simple tool loop.

Do not migrate merely to use a fashionable framework. Record the
requirement, prototype, operational cost, and measurable benefit before
adopting it.

## Step 15. Multi-team governance and security

### Learn

-   Multi-tenancy.
-   RBAC and policy-based authorization.
-   Tenant and repository isolation.
-   Identity federation.
-   Workload identity.
-   Secret rotation.
-   Threat modeling.
-   Audit retention.
-   Data classification.
-   Privacy and compliance requirements.
-   Incident response.

### Build

-   Team and repository-level permissions.
-   Per-agent tool permissions.
-   Per-environment deployment permissions.
-   Centralized policy enforcement.
-   Short-lived credentials.
-   Secret rotation.
-   Approval rules by risk level.
-   Audit trail for every privileged action.
-   Prompt and tool injection defenses.
-   Data retention and deletion policies.
-   Security incident response procedures.

Treat Jira descriptions, source code, documentation, issue comments, PR
content, and tool output as untrusted input. Never treat instructions
found in repository content as authorization to reveal secrets or bypass
policy.

## Step 16. Observability and AI evaluation

### Learn

-   Logs, metrics, and distributed traces.
-   OpenTelemetry.
-   Correlation and trace context.
-   LLM observability.
-   Prompt and model version tracking.
-   Evaluation datasets.
-   Offline and online evaluation.
-   Regression testing for prompts.
-   Human feedback loops.
-   Cost and latency analysis.

### Build

Capture: - Workflow ID and correlation ID. - Jira issue and repository
reference. - Agent and model version. - Prompt version. - Tool calls and
outcomes. - Token usage and estimated cost. - Runner duration and exit
code. - Build, test, and scan results. - Approval events. - Deployment
version and outcome.

Build dashboards for: - Workflow throughput. - Queue wait time. -
Execution duration by step. - Agent success and failure rate. - Retry
and recovery rate. - PR acceptance and rework. - Test and security gate
failures. - Model cost per task. - Deployment success and rollback rate.

Create a representative evaluation dataset: - Clear requirements. -
Ambiguous requirements. - Conflicting requirements. - Security-sensitive
requests. - Large and small code changes. - Invalid or malicious
repository instructions. - Tool failure and timeout scenarios.

Evaluate quality, not only whether an agent returned a response.

## Step 17. Reliability, scaling, and disaster recovery

### Learn

-   SLI, SLO, and SLA.
-   Capacity planning.
-   Backpressure.
-   Rate limiting.
-   Circuit breakers.
-   Bulkheads.
-   Graceful degradation.
-   High availability.
-   Backup and restore.
-   Disaster recovery.
-   Incident management.
-   Runbooks and postmortems.

### Build

-   Per-team and global concurrency limits.
-   Model provider rate-limit handling.
-   Queue backpressure.
-   Circuit breakers for unstable dependencies.
-   Worker autoscaling.
-   Database backup and restore tests.
-   Artifact retention policy.
-   Recovery procedures for n8n, workflow API, database, queue, and
    runner.
-   Incident alerts and runbooks.
-   Tested recovery time and recovery point objectives.

### Production exit criteria

-   Security and architecture reviews are complete.
-   Production identities and secrets are isolated.
-   Critical workflows are observable.
-   SLOs and operational ownership are defined.
-   Backup and restore are tested.
-   Recovery from representative failures is demonstrated.
-   Deployment and rollback/roll-forward procedures are documented.
-   AI quality is evaluated continuously.
-   Cost and capacity are monitored.
-   Teams have support and incident procedures.

------------------------------------------------------------------------

# 8. Cross-cutting engineering practices

These apply in every phase. Their maturity increases as the system
grows.

## 8.1 State and execution data

Keep a durable record of: - Workflow execution. - Current state and
version. - State transition history. - Agent invocation. - Tool
invocation. - Runner job. - Artifact and log references. - Approval. -
CI result. - Deployment result. - Error classification.

Do not store secrets or unnecessary sensitive content in execution logs.

## 8.2 Idempotency

Every event-driven operation that may be retried should define its
idempotency behavior.

Examples: - Jira webhook delivered twice. - n8n times out after the API
has already created an execution. - PR creation succeeds but the
response is lost. - Jira update fails after merge succeeds.

Use idempotency keys, unique constraints, stored operation results, and
reconciliation. Do not assume a network timeout means the remote
operation failed.

## 8.3 Retry policy

  Failure                    Typical handling
  -------------------------- --------------------------------------
  Validation error           Do not retry automatically
  Unauthorized               Stop and alert
  Rate limit                 Retry with backoff
  Temporary network error    Bounded retry
  Runner crash               Recover or recreate job
  Test failure               Analyze; bounded fix loop
  Invalid AI output          Bounded regeneration or human review
  Permanent provider error   Fail safely and alert

Never retry indefinitely. Define maximum attempts, deadlines, and a
manual recovery path.

## 8.4 AI quality and guardrails

-   Version prompts.
-   Validate structured outputs.
-   Keep evaluation datasets.
-   Compare model changes against a baseline.
-   Bound agent steps and tool calls.
-   Restrict tools by role.
-   Keep sensitive data out of prompts where possible.
-   Require approval for high-impact actions.
-   Retain evidence for decisions.
-   Never use an LLM response as proof that a command or deployment
    succeeded.

## 8.5 Cost management

Track: - Model and provider. - Input and output tokens. - Tool usage. -
Runner compute time. - CI minutes. - Artifact storage. - Cost per
workflow and per successful task.

Use smaller/cheaper models for classification and simple extraction when
quality is adequate. Use stronger models for complex reasoning and code
changes. Evaluate the quality/cost trade-off with actual tasks.

------------------------------------------------------------------------

# 9. Learning curriculum and deliverables

  ------------------------------------------------------------------------
                  Order Topic            What to          Practical
                                         understand       deliverable
  --------------------- ---------------- ---------------- ----------------
                      1 Workflow         Deterministic    .NET Workflow
                        orchestration    process, events, API + n8n
                                         state machine    

                      2 LLM engineering  Prompts,         Structured AI BA
                                         schemas,         
                                         context, cost    

                      3 Tool calling and Tool contracts,  Jira/document
                        MCP              permissions,     tools
                                         validation       

                      4 Agent            Agent loop,      AI BA agent
                        architecture     planning,        
                                         bounded          
                                         execution        

                      5 Multi-agent      Handoffs, role   BA → Dev →
                        workflow         boundaries,      Reviewer
                                         human approval   

                      6 Runner           Isolation,       Secure execution
                        engineering      command policy,  runner
                                         artifacts        

                      7 Distributed      Queue, leases,   Concurrent job
                        systems          idempotency,     execution
                                         recovery         

                      8 AI evaluation    Evaluation sets, AI QA +
                        and QA           test design, CI  evaluation suite
                                         evidence         

                      9 CI/CD and        Quality gates,   Secure delivery
                        security         scans,           pipeline
                                         migrations,      
                                         release          

                     10 Observability    Logs, metrics,   Operations
                                         traces, model    dashboard
                                         cost             

                     11 Cloud and        Identity,        Cloud deployment
                        scaling          networking,      
                                         autoscaling, HA  

                     12 Production       SLO, DR,         Production
                        reliability      incident         runbooks
                                         response,        
                                         governance       
  ------------------------------------------------------------------------

------------------------------------------------------------------------

# 10. Suggested weekly study method

For each learning unit:

1.  **Understand:** Learn the concept and its trade-offs.
2.  **Design:** Draw component and sequence diagrams.
3.  **Implement:** Build a small feature in the project.
4.  **Test:** Include success, failure, timeout, duplicate, and
    unauthorized cases.
5.  **Review:** Inspect code, architecture, security, and cost.
6.  **Document:** Record decisions and operational instructions.
7.  **Demonstrate:** Show the feature working end-to-end.

Suggested time split: - 20% concepts and documentation. - 60%
implementation. - 20% testing, review, and documentation.

Do not move to the next major topic only because the code compiles.
Verify the exit criteria.

------------------------------------------------------------------------

# 11. Architecture decision checkpoints

Before introducing a new framework or service, answer:

1.  What concrete problem are we solving?
2.  Can the current .NET + n8n architecture solve it safely?
3.  What failure modes does the new component address?
4.  What operational burden does it add?
5.  How will we test and observe it?
6.  What is the migration and rollback plan?
7.  What measurable result justifies adoption?

  -----------------------------------------------------------------------
  Proposed technology                 Adopt when
  ----------------------------------- -----------------------------------
  LangGraph                           Agent state, cycles, checkpointing,
                                      and human interaction justify a
                                      graph runtime

  Temporal                            Long-running distributed workflows
                                      need durable execution beyond
                                      current orchestration capabilities

  Message broker                      Job volume, concurrency,
                                      decoupling, or backpressure justify
                                      asynchronous messaging

  Kubernetes                          Deployment scale, isolation,
                                      availability, or platform
                                      requirements justify its
                                      operational cost

  Vector database                     Retrieval requirements and
                                      evaluation show semantic retrieval
                                      is beneficial

  Multiple LLM providers              Resilience, cost, capability, or
                                      data residency requirements justify
                                      provider abstraction
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 12. Metrics and success criteria

## POC

-   One end-to-end workflow completes.
-   State and execution history are persisted.
-   AI outputs are validated.
-   Build and tests execute independently.
-   Human controls merge.
-   Failures can be diagnosed.

## Team Pilot

-   Multiple developers can use the system.
-   Concurrent runs are isolated.
-   Duplicate and failed jobs recover safely.
-   Security and quality gates execute.
-   PR quality and rework are measured.
-   Team feedback demonstrates practical value.

## Team Production

-   Multi-team permissions and isolation work.
-   Security controls and audit are operational.
-   SLOs and incident ownership exist.
-   Observability covers workflow, agent, runner, and deployment.
-   Recovery procedures are tested.
-   Cost and capacity are managed.
-   AI quality regressions are detected.

Suggested metrics to establish baselines for:

  -----------------------------------------------------------------------
  Metric                              Meaning
  ----------------------------------- -----------------------------------
  Workflow success rate               Percentage of executions completed
                                      successfully

  Time to PR                          Time from approved requirement to
                                      PR

  PR acceptance rate                  Percentage accepted without major
                                      rework

  Rework rate                         Changes required after AI
                                      implementation/review

  Escaped defect rate                 Defects found after the relevant
                                      quality gate

  Recovery rate                       Failed executions safely recovered

  Cost per successful task            Total AI and infrastructure cost
                                      per completed task

  Developer adoption                  Active usage and developer feedback

  Deployment success rate             Successful deployments divided by
                                      attempts
  -----------------------------------------------------------------------

Do not set arbitrary targets before collecting a baseline. Set
thresholds with the pilot team and risk profile.

------------------------------------------------------------------------

# 13. Recommended repository structure

A possible starting structure:

``` text
ai-software-delivery/
├── apps/
│   ├── task-management-api/
│   ├── task-management-web/
│   └── workflow-api/
├── services/
│   ├── runner/
│   ├── agent-service/
│   └── notification-service/
├── agents/
│   ├── ba/
│   ├── developer/
│   ├── reviewer/
│   └── qa/
├── workflows/
│   └── n8n/
├── contracts/
│   ├── events/
│   ├── tools/
│   └── schemas/
├── tests/
│   ├── integration/
│   ├── e2e/
│   └── evaluation/
├── infra/
│   ├── docker/
│   ├── kubernetes/
│   └── terraform/
├── docs/
│   ├── architecture/
│   ├── decisions/
│   ├── runbooks/
│   └── security/
└── README.md
```

This is a target structure, not a requirement to create every folder in
the POC. Start small and split components when boundaries and deployment
needs become clear.

------------------------------------------------------------------------

# 14. Official learning resources

Use official documentation as the primary source. Verify product
capabilities and versions before implementation because AI SDKs and
cloud services change quickly.

## .NET and application engineering

-   ASP.NET Core: https://learn.microsoft.com/aspnet/core/
-   .NET architecture: https://learn.microsoft.com/dotnet/architecture/
-   EF Core: https://learn.microsoft.com/ef/core/
-   Angular: https://angular.dev/
-   Microsoft .NET testing:
    https://learn.microsoft.com/dotnet/core/testing/

## Workflow and agents

-   n8n documentation: https://docs.n8n.io/
-   LangGraph documentation:
    https://docs.langchain.com/oss/python/langgraph/overview
-   Temporal documentation: https://docs.temporal.io/
-   Model Context Protocol: https://modelcontextprotocol.io/

## LLM and AI engineering

-   OpenAI platform docs: https://platform.openai.com/docs/
-   Microsoft AI and .NET: https://learn.microsoft.com/dotnet/ai/
-   Azure AI Foundry documentation:
    https://learn.microsoft.com/azure/ai-foundry/
-   Anthropic documentation: https://docs.anthropic.com/

## DevOps and security

-   Docker documentation: https://docs.docker.com/
-   Kubernetes documentation: https://kubernetes.io/docs/
-   GitHub Actions: https://docs.github.com/actions
-   GitHub repository security: https://docs.github.com/code-security
-   OWASP Top 10 for LLM Applications: https://genai.owasp.org/

## Observability and cloud

-   OpenTelemetry: https://opentelemetry.io/docs/
-   Azure Architecture Center:
    https://learn.microsoft.com/azure/architecture/
-   AWS Well-Architected:
    https://docs.aws.amazon.com/wellarchitected/latest/framework/welcome.html
-   Google Cloud Architecture Framework:
    https://cloud.google.com/architecture/framework

------------------------------------------------------------------------

# 15. Immediate implementation backlog

Start with these tasks. Do not build the entire production architecture
on day one.

## Foundation

-   [ ] Create repository and solution structure.
-   [ ] Build Task Management API.
-   [ ] Build Angular application.
-   [ ] Add database and migrations.
-   [ ] Add Docker Compose.
-   [ ] Add CI build and test.

## POC workflow

-   [ ] Create Workflow API.
-   [ ] Define workflow states and transitions.
-   [ ] Persist workflow state and history.
-   [ ] Add correlation and idempotency keys.
-   [ ] Create n8n Jira intake workflow.
-   [ ] Add LLM provider abstraction.
-   [ ] Implement structured AI BA output.
-   [ ] Add human requirement approval.
-   [ ] Implement limited Jira/document tools.
-   [ ] Implement AI Developer for one small feature.
-   [ ] Add isolated branch/worktree handling.
-   [ ] Run build and tests through controlled execution.
-   [ ] Implement independent AI Reviewer.
-   [ ] Create PR.
-   [ ] Require human merge approval.
-   [ ] Add end-to-end failure tests.

## Pilot hardening

-   [ ] Add isolated runner.
-   [ ] Add queue and concurrency control when required.
-   [ ] Add lease, retry, dead-letter, and recovery.
-   [ ] Add AI QA.
-   [ ] Add integration and E2E tests.
-   [ ] Add SAST, SCA, secret, and container scans.
-   [ ] Add migration safety.
-   [ ] Add audit and cost tracking.
-   [ ] Run a limited team pilot and collect metrics.

## Production readiness

-   [ ] Select cloud platform and deployment architecture.
-   [ ] Add managed secrets and workload identity.
-   [ ] Add centralized OpenTelemetry.
-   [ ] Define SLOs and alerting.
-   [ ] Add multi-team permissions and governance.
-   [ ] Evaluate durable workflow execution.
-   [ ] Add backup/restore and disaster recovery.
-   [ ] Complete threat model and security review.
-   [ ] Document runbooks and incident procedures.
-   [ ] Approve production rollout based on pilot evidence.

------------------------------------------------------------------------

# 16. Final recommendation

Build the system in this order:

1.  A working Task Management application.
2.  A deterministic .NET Workflow API with n8n orchestration.
3.  One structured-output AI BA agent.
4.  A controlled AI Developer and independent Reviewer.
5.  A safe runner and reliable job execution.
6.  AI QA, CI quality gates, and security checks.
7.  Team pilot with measurement and feedback.
8.  Cloud, governance, observability, scaling, and production
    reliability.

The most important engineering rule:

> Deterministic code controls state, permissions, quality gates, and
> side effects. AI handles reasoning within explicitly granted
> boundaries. Humans approve high-impact decisions.

The goal is not to maximize the number of agents. The goal is to deliver
software faster without sacrificing security, quality, traceability, or
operational reliability.
