---
id: kd_builtin_memory_project_mistake_note
injectMode: full
injectAgents:
- unity
aiEditMode: auto
maintenanceRules: |-
  - Record only verified problems, rework causes, and avoidance steps
  - Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
  - Keep each entry short and focused on one lesson or constraint
  - Keep the list within 20 items and merge duplicates regularly
  - Remove outdated issues, non-reproducible issues, and unsupported guesses
---

- 运行 EditMode 测试：本项目 com.unity.test-framework 为 1.1.33，Locus 内置 UnityTestApi 需 ≥1.4.0，改用标准 TestRunnerApi（或手动 Unity Test Runner）执行。
