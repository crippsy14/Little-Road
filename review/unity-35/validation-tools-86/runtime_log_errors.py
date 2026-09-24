"""Unity may exit normally after repeated Update exceptions; reject those runs."""
import re
from pathlib import Path

def runtime_error_lines(folder: Path):
    # Match emitted exception/assertion headers, not stack helpers whose method
    # names contain 'Exception'. Missing logs are an evidence error, not a pass.
    text = (folder/'run.log').read_text(encoding='utf-8-sig', errors='replace')
    return re.findall(r'(?m)^(?:[\w.]*Exception:.*|Assertion failed\b.*)', text)
