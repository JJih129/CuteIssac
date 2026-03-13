from __future__ import annotations

import subprocess
from pathlib import Path


def register(mcp, project_root):
    project_root = Path(project_root).resolve()

    def run_git(args: list[str]) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["git", *args],
            cwd=str(project_root),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )

    @mcp.tool()
    def git_commit(message: str) -> str:
        add_result = run_git(["add", "."])
        if add_result.returncode != 0:
            return f"❌ git add 실패\n{(add_result.stdout + add_result.stderr).strip()}"

        commit_result = run_git(["commit", "-m", message])
        output = (commit_result.stdout + commit_result.stderr).strip()
        if commit_result.returncode != 0:
            return f"❌ git commit 실패\n{output or '(no output)'}"

        return f"✅ git commit complete\n{output}"

    @mcp.tool()
    def git_status() -> str:
        result = run_git(["status", "--short", "--branch"])
        return (result.stdout + result.stderr).strip() or "(no output)"
