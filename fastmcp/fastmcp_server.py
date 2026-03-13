from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Iterable, Optional

from fastmcp import FastMCP


# =============================================================================
# 환경 / 경로 설정
# =============================================================================
SERVER_FILE = Path(__file__).resolve()
DEFAULT_SERVER_DIR = SERVER_FILE.parent

# CuteIssac 전용 고정 Unity 프로젝트 루트
# 멀티 프로젝트 환경에서 자동 감지보다 훨씬 안전함
PROJECT_ROOT = Path(r"D:\GitHub\CuteIssac\CuteIssac").resolve()


def _is_unity_project(path: Path) -> bool:
    """Unity 프로젝트 루트 판별."""
    return (
        path.is_dir()
        and (path / "Assets").is_dir()
        and (path / "Packages").is_dir()
        and (path / "ProjectSettings").is_dir()
    )


def _candidate_roots() -> Iterable[Path]:
    """
    참고용 후보 목록.
    현재는 PROJECT_ROOT를 고정 사용하므로 진단용으로만 유지.
    """
    yield PROJECT_ROOT
    yield DEFAULT_SERVER_DIR.parent
    yield Path.cwd().resolve()

    env_root = os.getenv("UNITY_PROJECT_ROOT")
    if env_root:
        yield Path(env_root).expanduser().resolve()

    yield DEFAULT_SERVER_DIR
    yield DEFAULT_SERVER_DIR.parent.parent


# Unity 경로는 기존 사용 경로 유지
UNITY_PATH = os.getenv("UNITY_PATH", r"D:\Unity\6000.3.10f1\Editor\Unity.exe")

# Unity에서 배치 호출할 API 클래스명
UNITY_API_CLASS = os.getenv("UNITY_API_CLASS", "FastMCPUnityAPI")

# Editor.log 경로
UNITY_LOG_PATH = Path(
    os.getenv(
        "UNITY_EDITOR_LOG_PATH",
        str(Path.home() / "AppData" / "Local" / "Unity" / "Editor" / "Editor.log"),
    )
)

# 서버 이름
mcp = FastMCP("cuteissac-dev-server")


# =============================================================================
# 내부 유틸
# =============================================================================
@dataclass(frozen=True)
class ExecResult:
    returncode: int
    stdout: str
    stderr: str

    @property
    def merged(self) -> str:
        if self.stdout and self.stderr:
            return f"{self.stdout}\n{self.stderr}".strip()
        return (self.stdout or self.stderr).strip()


def _normalize_relative_path(relative_path: str) -> Path:
    """
    프로젝트 루트 내부 경로만 허용.
    외부 경로 접근 방지.
    """
    candidate = (PROJECT_ROOT / relative_path).resolve()

    try:
        candidate.relative_to(PROJECT_ROOT)
    except ValueError as exc:
        raise ValueError(f"프로젝트 루트 밖 경로는 허용되지 않습니다: {relative_path}") from exc

    return candidate


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def _backup_file(path: Path) -> Path:
    """
    수정 전 백업 생성.
    초 단위 충돌을 줄이기 위해 마이크로초까지 포함.
    """
    backup_dir = PROJECT_ROOT / ".fastmcp_backups"
    backup_dir.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    backup_name = f"{path.name}.{timestamp}.bak"
    backup_path = backup_dir / backup_name
    shutil.copy2(path, backup_path)
    return backup_path


def _run_process(command: list[str], cwd: Optional[Path] = None) -> ExecResult:
    """
    외부 프로세스 실행.
    Windows 환경에서 인코딩 깨짐을 줄이기 위해 utf-8 + replace 사용.
    """
    result = subprocess.run(
        command,
        cwd=str(cwd or PROJECT_ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    return ExecResult(
        returncode=result.returncode,
        stdout=result.stdout.strip(),
        stderr=result.stderr.strip(),
    )


def _build_unity_command(method: str, *args: str) -> list[str]:
    return [
        UNITY_PATH,
        "-projectPath",
        str(PROJECT_ROOT),
        "-batchmode",
        "-quit",
        "-executeMethod",
        method,
        *args,
    ]


def _run_unity_method(method_suffix: str, *args: str) -> str:
    """
    FastMCPUnityAPI.{method_suffix} 를 Unity batchmode로 실행.
    """
    unity_exe = Path(UNITY_PATH)
    if not unity_exe.exists():
        return f"❌ Unity 실행 파일을 찾지 못했습니다: {UNITY_PATH}"

    if not _is_unity_project(PROJECT_ROOT):
        return (
            "❌ PROJECT_ROOT 가 유효한 Unity 프로젝트가 아닙니다.\n"
            f"- project_root: {PROJECT_ROOT}"
        )

    method = f"{UNITY_API_CLASS}.{method_suffix}"
    result = _run_process(_build_unity_command(method, *args))

    if result.returncode != 0:
        return (
            f"❌ Unity 배치 실행 실패\n"
            f"- method: {method}\n"
            f"- exit code: {result.returncode}\n"
            f"- output:\n{result.merged or '(no output)'}"
        )

    return result.merged or f"✅ Unity method 완료: {method}"


def _json_ok(data: object) -> str:
    return json.dumps(data, ensure_ascii=False, indent=2)


def _parse_args_json(args_json: str) -> list[str]:
    try:
        parsed = json.loads(args_json)
    except json.JSONDecodeError as exc:
        raise ValueError(f"args_json 파싱 실패: {exc}") from exc

    if not isinstance(parsed, list) or not all(isinstance(item, str) for item in parsed):
        raise ValueError("args_json 은 문자열 배열 JSON 이어야 합니다.")

    return parsed


def _create_mono_behaviour_template(class_name: str, namespace: str = "") -> str:
    """
    기본 MonoBehaviour 템플릿.
    인스펙터 조절값은 SerializeField 로만 노출.
    """
    namespace_open = f"namespace {namespace}\n{{\n" if namespace else ""
    namespace_close = "}\n" if namespace else ""
    indent = "    " if namespace else ""

    return (
        "using UnityEngine;\n\n"
        f"{namespace_open}"
        f"{indent}/// <summary>\n"
        f"{indent}/// Generated by FastMCP.\n"
        f"{indent}/// 인스펙터 조절값만 SerializeField 로 노출합니다.\n"
        f"{indent}/// </summary>\n"
        f"{indent}public sealed class {class_name} : MonoBehaviour\n"
        f"{indent}{{\n"
        f"{indent}    [Header(\"References\")]\n"
        f"{indent}    [SerializeField] private Transform cachedTransform;\n\n"
        f"{indent}    [Header(\"Settings\")]\n"
        f"{indent}    [SerializeField, Min(0f)] private float moveSpeed = 5f;\n\n"
        f"{indent}    private void Reset()\n"
        f"{indent}    {{\n"
        f"{indent}        cachedTransform = transform;\n"
        f"{indent}    }}\n\n"
        f"{indent}    private void Awake()\n"
        f"{indent}    {{\n"
        f"{indent}        if (cachedTransform == null)\n"
        f"{indent}        {{\n"
        f"{indent}            cachedTransform = transform;\n"
        f"{indent}        }}\n"
        f"{indent}    }}\n\n"
        f"{indent}    private void Update()\n"
        f"{indent}    {{\n"
        f"{indent}        // TODO: 입력/상태머신과 연결\n"
        f"{indent}    }}\n"
        f"{indent}}}\n"
        f"{namespace_close}"
    )


def _create_scriptable_object_template(class_name: str, namespace: str = "") -> str:
    """기본 ScriptableObject 템플릿."""
    namespace_open = f"namespace {namespace}\n{{\n" if namespace else ""
    namespace_close = "}\n" if namespace else ""
    indent = "    " if namespace else ""

    return (
        "using UnityEngine;\n\n"
        f"{namespace_open}"
        f"{indent}[CreateAssetMenu(fileName = \"{class_name}\", menuName = \"Game/{class_name}\")]\n"
        f"{indent}public sealed class {class_name} : ScriptableObject\n"
        f"{indent}{{\n"
        f"{indent}    [Header(\"Settings\")]\n"
        f"{indent}    [SerializeField] private string description;\n"
        f"{indent}    [SerializeField, Min(0)] private int value = 1;\n"
        f"{indent}}}\n"
        f"{namespace_close}"
    )


def _create_plain_csharp_template(class_name: str, namespace: str = "") -> str:
    """기본 Plain C# 클래스 템플릿."""
    namespace_open = f"namespace {namespace}\n{{\n" if namespace else ""
    namespace_close = "}\n" if namespace else ""
    indent = "    " if namespace else ""

    return (
        "using System;\n\n"
        f"{namespace_open}"
        f"{indent}/// <summary>\n"
        f"{indent}/// Generated by FastMCP.\n"
        f"{indent}/// </summary>\n"
        f"{indent}public sealed class {class_name}\n"
        f"{indent}{{\n"
        f"{indent}}}\n"
        f"{namespace_close}"
    )


def _create_feature_readme(feature_name: str) -> str:
    return (
        f"# {feature_name}\n\n"
        "## 목적\n"
        "- 이 기능의 책임과 범위를 명확히 정의\n\n"
        "## 구성 요소\n"
        "- Runtime 스크립트\n"
        "- ScriptableObject 데이터\n"
        "- Editor 확장 필요 여부\n\n"
        "## 체크리스트\n"
        "- [ ] 런타임 클래스 생성\n"
        "- [ ] 데이터 에셋 정의\n"
        "- [ ] 씬 연결\n"
        "- [ ] 테스트\n"
    )


# =============================================================================
# 진단
# =============================================================================
@mcp.tool()
def health_check() -> str:
    """MCP 서버, Unity, 프로젝트 루트, 로그 경로 상태 점검."""
    candidate_roots = [str(p) for p in _candidate_roots()]

    data = {
        "server_name": "cuteissac-dev-server",
        "server_file": str(SERVER_FILE),
        "server_dir": str(DEFAULT_SERVER_DIR),
        "project_root": str(PROJECT_ROOT),
        "project_root_valid": _is_unity_project(PROJECT_ROOT),
        "candidate_roots": candidate_roots,
        "unity_path": UNITY_PATH,
        "unity_exists": Path(UNITY_PATH).exists(),
        "unity_api_class": UNITY_API_CLASS,
        "editor_log_path": str(UNITY_LOG_PATH),
        "editor_log_exists": UNITY_LOG_PATH.exists(),
    }
    return _json_ok(data)


@mcp.tool()
def get_project_root() -> str:
    """현재 감지된 Unity 프로젝트 루트 반환."""
    return str(PROJECT_ROOT)


@mcp.tool()
def summarize_project_structure(max_depth: int = 3) -> str:
    """프로젝트 구조 요약."""
    lines: list[str] = []

    def walk(path: Path, depth: int) -> None:
        if depth > max_depth:
            return

        try:
            entries = sorted(path.iterdir(), key=lambda p: (p.is_file(), p.name.lower()))
        except PermissionError:
            return
        except OSError:
            return

        for entry in entries:
            try:
                rel = entry.relative_to(PROJECT_ROOT)
            except ValueError:
                continue

            prefix = "  " * depth
            lines.append(f"{prefix}{rel}")

            if entry.is_dir():
                walk(entry, depth + 1)

    important_roots = ["Assets", "Packages", "ProjectSettings"]
    for root_name in important_roots:
        root = PROJECT_ROOT / root_name
        if root.exists():
            lines.append(root_name)
            walk(root, 1)

    return "\n".join(lines[:3000])


# =============================================================================
# 파일 작업
# =============================================================================
@mcp.tool()
def read_file(path: str) -> str:
    """프로젝트 내부 파일 읽기."""
    full_path = _normalize_relative_path(path)
    if not full_path.exists():
        return f"❌ 파일을 찾을 수 없습니다: {path}"
    return _read_text(full_path)


@mcp.tool()
def write_file(path: str, content: str, create_backup: bool = False) -> str:
    """프로젝트 내부 파일 새로 쓰기 / 덮어쓰기."""
    full_path = _normalize_relative_path(path)

    backup_path = None
    if create_backup and full_path.exists():
        backup_path = _backup_file(full_path)

    _write_text(full_path, content)

    if backup_path:
        return f"✅ 파일 기록 완료: {path}\nbackup: {backup_path}"
    return f"✅ 파일 기록 완료: {path}"


@mcp.tool()
def modify_file(path: str, search: str, replace: str, create_backup: bool = True) -> str:
    """문자열 기반 안전 치환."""
    full_path = _normalize_relative_path(path)
    if not full_path.exists():
        return f"❌ 파일을 찾을 수 없습니다: {path}"

    content = _read_text(full_path)
    if search not in content:
        return f"❌ 대상 문자열을 찾지 못했습니다: {search}"

    backup_path = _backup_file(full_path) if create_backup else None
    updated = content.replace(search, replace)
    _write_text(full_path, updated)

    if backup_path:
        return f"✅ 파일 수정 완료: {path}\nbackup: {backup_path}"
    return f"✅ 파일 수정 완료: {path}"


@mcp.tool()
def regex_patch_file(path: str, pattern: str, replacement: str, create_backup: bool = True) -> str:
    """정규식 기반 패치."""
    full_path = _normalize_relative_path(path)
    if not full_path.exists():
        return f"❌ 파일을 찾을 수 없습니다: {path}"

    content = _read_text(full_path)
    updated, count = re.subn(pattern, replacement, content, flags=re.MULTILINE)

    if count == 0:
        return f"❌ 패턴 일치 항목이 없습니다: {pattern}"

    backup_path = _backup_file(full_path) if create_backup else None
    _write_text(full_path, updated)

    if backup_path:
        return f"✅ 정규식 패치 완료: {path}\nreplacements: {count}\nbackup: {backup_path}"
    return f"✅ 정규식 패치 완료: {path}\nreplacements: {count}"


@mcp.tool()
def search_files(pattern: str) -> str:
    """프로젝트 루트 기준 glob 검색. 예: Assets/**/*.cs"""
    matches = [str(p.relative_to(PROJECT_ROOT)) for p in PROJECT_ROOT.glob(pattern)]
    matches.sort()
    return _json_ok(matches)


@mcp.tool()
def search_text_in_files(glob_pattern: str, keyword: str, max_results: int = 100) -> str:
    """파일들에서 텍스트 검색."""
    results: list[dict[str, object]] = []

    for file_path in PROJECT_ROOT.glob(glob_pattern):
        if not file_path.is_file():
            continue

        try:
            lines = file_path.read_text(encoding="utf-8", errors="ignore").splitlines()
        except Exception:
            continue

        for index, line in enumerate(lines, start=1):
            if keyword in line:
                results.append(
                    {
                        "file": str(file_path.relative_to(PROJECT_ROOT)),
                        "line": index,
                        "text": line.strip(),
                    }
                )
                if len(results) >= max_results:
                    return _json_ok(results)

    return _json_ok(results)


# =============================================================================
# 스크립트 생성
# =============================================================================
@mcp.tool()
def create_unity_script(
    name: str,
    relative_dir: str = "Assets/Scripts/Generated",
    namespace: str = "",
    template_type: str = "monobehaviour",
) -> str:
    """Unity 스크립트를 템플릿 기반으로 생성."""
    safe_dir = _normalize_relative_path(relative_dir)
    script_path = safe_dir / f"{name}.cs"

    if script_path.exists():
        return f"❌ 이미 존재하는 스크립트입니다: {script_path.relative_to(PROJECT_ROOT)}"

    template_type_lower = template_type.lower()
    if template_type_lower == "monobehaviour":
        code = _create_mono_behaviour_template(name, namespace)
    elif template_type_lower == "scriptableobject":
        code = _create_scriptable_object_template(name, namespace)
    elif template_type_lower == "plain":
        code = _create_plain_csharp_template(name, namespace)
    else:
        return f"❌ 지원하지 않는 template_type 입니다: {template_type}"

    _write_text(script_path, code)
    return f"✅ 스크립트 생성 완료: {script_path.relative_to(PROJECT_ROOT)}"


@mcp.tool()
def scaffold_feature(feature_name: str, base_dir: str = "Assets/Game") -> str:
    """
    기능 단위 폴더/파일 스캐폴딩.
    예: Combat, Inventory, Dialogue
    """
    root = _normalize_relative_path(base_dir) / feature_name
    runtime_dir = root / "Runtime"
    data_dir = root / "Data"
    editor_dir = root / "Editor"

    runtime_dir.mkdir(parents=True, exist_ok=True)
    data_dir.mkdir(parents=True, exist_ok=True)
    editor_dir.mkdir(parents=True, exist_ok=True)

    runtime_script = runtime_dir / f"{feature_name}Controller.cs"
    data_script = data_dir / f"{feature_name}Config.cs"
    readme_file = root / "README.md"

    if not runtime_script.exists():
        _write_text(runtime_script, _create_mono_behaviour_template(f"{feature_name}Controller", "Game"))
    if not data_script.exists():
        _write_text(data_script, _create_scriptable_object_template(f"{feature_name}Config", "Game"))
    if not readme_file.exists():
        _write_text(readme_file, _create_feature_readme(feature_name))

    return f"✅ 기능 스캐폴딩 완료: {root.relative_to(PROJECT_ROOT)}"


# =============================================================================
# Git / 터미널
# =============================================================================
@mcp.tool()
def git_status() -> str:
    """git 상태 조회."""
    result = _run_process(["git", "status", "--short", "--branch"])
    return result.merged or "(no output)"


@mcp.tool()
def git_commit(message: str) -> str:
    """전체 변경사항 add 후 commit."""
    add_result = _run_process(["git", "add", "."])
    if add_result.returncode != 0:
        return f"❌ git add 실패\n{add_result.merged}"

    commit_result = _run_process(["git", "commit", "-m", message])
    if commit_result.returncode != 0:
        return f"❌ git commit 실패\n{commit_result.merged or '(no output)'}"

    return f"✅ git commit 완료\n{commit_result.merged}"


@mcp.tool()
def run_terminal(executable: str, args_json: str = "[]") -> str:
    """쉘 없이 안전하게 명령 실행."""
    try:
        args = _parse_args_json(args_json)
    except ValueError as exc:
        return f"❌ {exc}"

    result = _run_process([executable, *args])
    if result.returncode != 0:
        return f"❌ 명령 실행 실패 (exit code {result.returncode})\n{result.merged or '(no output)'}"

    return result.merged or "✅ 명령 실행 완료"


# =============================================================================
# 로그 / 컴파일
# =============================================================================
@mcp.tool()
def get_unity_logs(line_count: int = 200) -> str:
    """Editor.log 마지막 일부 반환."""
    if not UNITY_LOG_PATH.exists():
        return f"❌ Unity Editor.log 를 찾지 못했습니다: {UNITY_LOG_PATH}"

    lines = UNITY_LOG_PATH.read_text(encoding="utf-8", errors="ignore").splitlines()
    sliced = lines[-max(1, line_count):]
    return "\n".join(sliced)


@mcp.tool()
def unity_console_errors(line_limit: int = 80) -> str:
    """최근 컴파일 에러/예외만 필터링."""
    if not UNITY_LOG_PATH.exists():
        return f"❌ Unity Editor.log 를 찾지 못했습니다: {UNITY_LOG_PATH}"

    lines = UNITY_LOG_PATH.read_text(encoding="utf-8", errors="ignore").splitlines()
    error_lines = [
        line
        for line in lines
        if "error CS" in line or "Exception:" in line or "NullReferenceException" in line
    ]
    if not error_lines:
        return "최근 로그에서 C# 컴파일 에러를 찾지 못했습니다."

    return "\n".join(error_lines[-max(1, line_limit):])


@mcp.tool()
def analyze_compile_errors() -> str:
    """로그 기반 간단 진단."""
    raw = unity_console_errors(200)
    if raw.startswith("최근 로그에서"):
        return raw
    if raw.startswith("❌"):
        return raw

    diagnostics = []
    if "CS0246" in raw:
        diagnostics.append("- CS0246: 타입/네임스페이스 누락. using 또는 asmdef 참조 확인")
    if "CS0103" in raw:
        diagnostics.append("- CS0103: 현재 컨텍스트에 이름이 없음. 오타/스코프 확인")
    if "CS0117" in raw:
        diagnostics.append("- CS0117: 타입에 해당 멤버 없음. API 버전 불일치 가능")
    if "NullReferenceException" in raw:
        diagnostics.append("- NullReferenceException: 인스펙터 참조 누락 또는 초기화 순서 문제")

    if not diagnostics:
        diagnostics.append("- 정형화된 패턴 외 오류. raw 로그 직접 확인 필요")

    return "최근 컴파일/런타임 오류 분석:\n" + "\n".join(diagnostics) + "\n\n원본 로그:\n" + raw


# =============================================================================
# Unity 제어
# =============================================================================
@mcp.tool()
def open_unity() -> str:
    """Unity Editor 실행."""
    unity_exe = Path(UNITY_PATH)
    if not unity_exe.exists():
        return f"❌ Unity 실행 파일을 찾지 못했습니다: {UNITY_PATH}"

    if not _is_unity_project(PROJECT_ROOT):
        return (
            "❌ PROJECT_ROOT 가 유효한 Unity 프로젝트가 아닙니다.\n"
            f"- project_root: {PROJECT_ROOT}"
        )

    subprocess.Popen([UNITY_PATH, "-projectPath", str(PROJECT_ROOT)], cwd=str(PROJECT_ROOT))
    return "✅ Unity Editor 실행 요청 완료"


@mcp.tool()
def run_unity_method(method_suffix: str, args_json: str = "[]") -> str:
    """FastMCPUnityAPI 의 정적 메서드를 배치모드로 실행."""
    try:
        args = _parse_args_json(args_json)
    except ValueError as exc:
        return f"❌ {exc}"

    return _run_unity_method(method_suffix, *args)


@mcp.tool()
def create_scene(scene_name: str) -> str:
    return _run_unity_method("CreateScene", scene_name)


@mcp.tool()
def open_scene(scene_name: str) -> str:
    return _run_unity_method("OpenScene", scene_name)


@mcp.tool()
def save_scene() -> str:
    return _run_unity_method("SaveScene")


@mcp.tool()
def get_scene_graph() -> str:
    return _run_unity_method("GetSceneGraph")


@mcp.tool()
def get_scene_graph_json() -> str:
    return _run_unity_method("GetSceneGraphJson")


@mcp.tool()
def create_gameobject(name: str) -> str:
    return _run_unity_method("CreateGameObject", name)


@mcp.tool()
def delete_gameobject(name: str) -> str:
    return _run_unity_method("DeleteGameObject", name)


@mcp.tool()
def set_parent(child: str, parent: str) -> str:
    return _run_unity_method("SetParent", child, parent)


@mcp.tool()
def set_position(name: str, x: float, y: float, z: float) -> str:
    return _run_unity_method("SetPosition", name, str(x), str(y), str(z))


@mcp.tool()
def set_local_scale(name: str, x: float, y: float, z: float) -> str:
    return _run_unity_method("SetLocalScale", name, str(x), str(y), str(z))


@mcp.tool()
def add_component(obj: str, component: str) -> str:
    return _run_unity_method("AddComponent", obj, component)


@mcp.tool()
def remove_component(obj: str, component: str) -> str:
    return _run_unity_method("RemoveComponent", obj, component)


@mcp.tool()
def create_prefab(obj: str, prefab_name: str) -> str:
    return _run_unity_method("CreatePrefab", obj, prefab_name)


@mcp.tool()
def instantiate_prefab(prefab: str) -> str:
    return _run_unity_method("InstantiatePrefab", prefab)


@mcp.tool()
def search_assets(filter_name: str, search_type: str = "t:Prefab") -> str:
    return _run_unity_method("SearchAssets", filter_name, search_type)


@mcp.tool()
def set_inspector_value(obj_name: str, comp_name: str, field: str, value: str) -> str:
    return _run_unity_method("SetInspectorValue", obj_name, comp_name, field, value)


@mcp.tool()
def refresh_assets() -> str:
    return _run_unity_method("RefreshAssets")


@mcp.tool()
def enter_playmode() -> str:
    return _run_unity_method("EnterPlayMode")


@mcp.tool()
def exit_playmode() -> str:
    return _run_unity_method("ExitPlayMode")


# =============================================================================
# 고수준 자동화
# =============================================================================
@mcp.tool()
def create_basic_3d_player_rig(player_name: str = "Player") -> str:
    """
    가장 기본적인 3D 플레이어 리그 생성.
    - Player 오브젝트
    - Main Camera
    - CharacterController
    - CapsuleCollider
    """
    results = [
        _run_unity_method("CreateBasic3DPlayerRig", player_name),
    ]
    return "\n\n".join(results)


@mcp.tool()
def create_game_feature_bundle(feature_name: str) -> str:
    """
    기능 단위로 폴더/스크립트/README 한번에 생성.
    씬 조작 전 구조부터 만들 때 사용.
    """
    scaffold_result = scaffold_feature(feature_name)
    refresh_result = refresh_assets()
    return f"{scaffold_result}\n{refresh_result}"


if __name__ == "__main__":
    if not _is_unity_project(PROJECT_ROOT):
        raise RuntimeError(
            "고정된 PROJECT_ROOT 가 유효한 Unity 프로젝트가 아닙니다.\n"
            f"- project_root: {PROJECT_ROOT}"
        )

    mcp.run()