from __future__ import annotations

import json
import subprocess
from pathlib import Path


def register(mcp, project_root):
    project_root = Path(project_root).resolve()

    @mcp.tool()
    def run_terminal(executable: str, args_json: str = "[]") -> str:
        try:
            args = json.loads(args_json)
        except json.JSONDecodeError as exc:
            return f"❌ args_json 파싱 실패: {exc}"

        if not isinstance(args, list) or not all(isinstance(item, str) for item in args):
            return "❌ args_json 은 문자열 배열 JSON 이어야 합니다."

        result = subprocess.run(
            [executable, *args],
            cwd=str(project_root),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )

        output = (result.stdout + result.stderr).strip()
        if result.returncode != 0:
            return f"❌ 명령 실행 실패 (exit code {result.returncode})\n{output or '(no output)'}"

        return output or "✅ 명령 실행 완료"
