@./skills/uitest/SkILL.md

## Mandatory post-change review

After every code change in this repository, before reporting the work as complete, spawn the `code-reviewer` subagent to review all uncommitted changes; it runs `dotnet build` and the full test suite (`dotnet test`) itself. If its verdict is REQUEST CHANGES, fix the findings and re-run `code-reviewer`; repeat until APPROVE (max 3 rounds — then report remaining findings). Never report a code change as complete without an APPROVE from `code-reviewer`.
