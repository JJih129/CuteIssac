import os

def register(mcp, PROJECT_ROOT):

    @mcp.tool()
    def read_file(path: str) -> str:
        full = os.path.join(PROJECT_ROOT, path)
        with open(full, "r", encoding="utf-8") as f:
            return f.read()

    @mcp.tool()
    def write_file(path: str, content: str) -> str:
        full = os.path.join(PROJECT_ROOT, path)

        os.makedirs(os.path.dirname(full), exist_ok=True)

        with open(full, "w", encoding="utf-8") as f:
            f.write(content)

        return f"written {path}"

    @mcp.tool()
    def modify_file(path: str, search: str, replace: str) -> str:

        full = os.path.join(PROJECT_ROOT, path)

        with open(full, "r", encoding="utf-8") as f:
            data = f.read()

        data = data.replace(search, replace)

        with open(full, "w", encoding="utf-8") as f:
            f.write(data)

        return f"modified {path}"