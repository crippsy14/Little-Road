"""Keep each changed text file's line endings as they are in HEAD (run before committing).

The repository keeps exact bytes (.gitattributes: * -text), and files differ: most C# is LF, some CRLF, a few mixed.
Editors and Windows Python's text mode can flip a whole file, which then shows as rewritten in the diff. For every
modified file this puts back HEAD's endings: a single-style file gets that style throughout; a mixed file keeps each
unchanged line's own ending, and new or changed lines take the ending of the line before them. New files are written LF.
Usage: python tools/keep-eol.py [--check]
(For edits from Python, tools/edit-preserving.py replaces text in either line-ending style.)
"""
import difflib, os, subprocess, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
G = ['git', '-c', 'safe.directory=' + ROOT.replace('\\', '/'), '-C', ROOT]
TEXT = ('.cs', '.shader', '.ps1', '.py', '.md', '.json', '.txt', '.hlsl', '.cginc')
check = '--check' in sys.argv

def run(*a):
    return subprocess.run(G + list(a), capture_output=True).stdout

def per_line(old, new):
    """HEAD's ending for every unchanged line; changed lines take the ending of the original line before them."""
    ol = old.split(b'\n'); nl = new.split(b'\n')
    ok = [l.rstrip(b'\r') for l in ol]; nk = [l.rstrip(b'\r') for l in nl]
    out = [None] * len(nl)
    for tag, i1, i2, j1, j2 in difflib.SequenceMatcher(None, ok, nk, autojunk=False).get_opcodes():
        if tag == 'equal':
            for k in range(i2 - i1): out[j1 + k] = ol[i1 + k]
        else:
            cr = (ol[i1 - 1] if i1 > 0 else ol[0]).endswith(b'\r')
            for k in range(j1, j2): out[k] = nk[k] + (b'\r' if cr and k < len(nl) - 1 else b'')
    return b'\n'.join(out)

changed = run('diff', '--name-only').decode().split() + run('ls-files', '--others', '--exclude-standard').decode().split()
fixed = 0
for f in changed:
    path = os.path.join(ROOT, f)
    if not f.endswith(TEXT) or not os.path.isfile(path) or f.startswith('review/'):
        continue
    head = run('show', 'HEAD:' + f)
    data = open(path, 'rb').read()
    crlfs, lines = head.count(b'\r\n'), head.count(b'\n')
    if head and 0 < crlfs < lines:
        style, want = 'mixed', per_line(head, data)
    else:
        crlf = bool(head) and crlfs * 2 > lines
        body = data.replace(b'\r\n', b'\n')
        style, want = ('CRLF' if crlf else 'LF'), (body.replace(b'\n', b'\r\n') if crlf else body)
    if want != data:
        fixed += 1
        print(('would fix ' if check else 'fixed ') + f + f' ({style})')
        if not check:
            open(path, 'wb').write(want)
print(f'keep-eol: {fixed} file(s) {"to fix" if check else "fixed"}')
