"""Keep each changed text file's line endings as they are in HEAD (run before committing).

The repository keeps exact bytes (.gitattributes: * -text), and files differ: most C# is LF, some CRLF. Editors and
Windows Python's text mode can flip a whole file, which then shows as rewritten in the diff. For every modified file this
puts back HEAD's dominant ending; new files are written LF. Usage: python tools/keep-eol.py [--check]
"""
import os, subprocess, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
G = ['git', '-c', 'safe.directory=' + ROOT.replace('\\', '/'), '-C', ROOT]
TEXT = ('.cs', '.shader', '.ps1', '.py', '.md', '.json', '.txt', '.hlsl', '.cginc')
check = '--check' in sys.argv

def run(*a):
    return subprocess.run(G + list(a), capture_output=True).stdout

changed = run('diff', '--name-only').decode().split() + run('ls-files', '--others', '--exclude-standard').decode().split()
fixed = 0
for f in changed:
    path = os.path.join(ROOT, f)
    if not f.endswith(TEXT) or not os.path.isfile(path) or f.startswith('review/'):
        continue
    head = run('show', 'HEAD:' + f)
    crlf = head.count(b'\r\n') * 2 > head.count(b'\n') if head else False
    data = open(path, 'rb').read()
    body = data.replace(b'\r\n', b'\n')
    want = body.replace(b'\n', b'\r\n') if crlf else body
    if want != data:
        fixed += 1
        print(('would fix ' if check else 'fixed ') + f + (' (CRLF)' if crlf else ' (LF)'))
        if not check:
            open(path, 'wb').write(want)
print(f'keep-eol: {fixed} file(s) {"to fix" if check else "fixed"}')
