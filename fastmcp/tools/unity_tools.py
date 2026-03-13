from __future__ import annotations

import os
import subprocess
from pathlib import Path


def register(mcp, project_root):
    project_root = Path(project_root).resolve()
    unity_path = os.getenv("UNITY_PATH", r"D:\Unity\6000.3.10f1\Editor\Unity.exe")
    editor_log_path = Path(
        os.getenv(
            "UNITY_EDITOR_LOG_PATH",
            str(Path.home() / "AppData" / "Local" / "Unity" / "Editor" / "Editor.log"),
        )
    )

    def create_script_template(class_name: str, namespace: str = "") -> str:
        namespace_open = f"namespace {namespace}\n{{\n" if namespace else ""
        namespace_close = "}\n" if namespace else ""
        indent = "    " if namespace else ""
        return (
            "using UnityEngine;\n\n"
            f"{namespace_open}"
            f"{indent}public sealed class {class_name} : MonoBehaviour\n"
            f"{indent}{{\n"
            f"{indent}    [Header(\"Settings\")]\n"
            f"{indent}    [SerializeField, Min(0f)] private float speed = 5f;\n\n"
            f"{indent}    private Transform cachedTransform;\n\n"
            f"{indent}    private void Awake()\n"
            f"{indent}    {{\n"
            f"{indent}        cachedTransform = transform;\n"
            f"{indent}    }}\n\n"
            f"{indent}    private void Update()\n"
            f"{indent}    {{\n"
            f"{indent}        // TODO: 구현\n"
            f"{indent}    }}\n"
            f"{indent}}}\n"
            f"{namespace_close}"
        )

    @mcp.tool()
    def create_unity_script(name: str, relative_dir: str = "Assets/Scripts", namespace: str = "") -> str:
        scripts_dir = (project_root / relative_dir).resolve()
        try:
            scripts_dir.relative_to(project_root)
        except ValueError as exc:
            raise ValueError("프로젝트 루트 밖 폴더는 허용되지 않습니다.") from exc

        scripts_dir.mkdir(parents=True, exist_ok=True)
        script_path = scripts_dir / f"{name}.cs"

        if script_path.exists():
            return f"Script already exists: {script_path.relative_to(project_root)}"

        script_path.write_text(create_script_template(name, namespace), encoding="utf-8", newline="\n")
        return f"✅ {script_path.relative_to(project_root)} created"

    @mcp.tool()
    def update_script(relative_path: str, new_code: str) -> str:
        script_path = (project_root / relative_path).resolve()
        try:
            script_path.relative_to(project_root)
        except ValueError as exc:
            raise ValueError("프로젝트 루트 밖 파일은 허용되지 않습니다.") from exc

        if not script_path.exists():
            return f"Script not found: {relative_path}"

        script_path.write_text(new_code, encoding="utf-8", newline="\n")
        return f"✅ {relative_path} updated"

    @mcp.tool()
    def unity_console_errors(line_limit: int = 50) -> str:
        if not editor_log_path.exists():
            return f"Unity Editor.log not found: {editor_log_path}"

        lines = editor_log_path.read_text(encoding="utf-8", errors="ignore").splitlines()
        errors = [line for line in lines if "error CS" in line or "Exception:" in line]
        if not errors:
            return "No recent C# compile errors found"

        return "\n".join(errors[-max(1, line_limit) :])

    @mcp.tool()
    def open_unity() -> str:
        unity_exe = Path(unity_path)
        if not unity_exe.exists():
            return f"Failed to launch Unity: {unity_path}"

        subprocess.Popen([unity_path, "-projectPath", str(project_root)], cwd=str(project_root))
        return "Unity Editor launched"
