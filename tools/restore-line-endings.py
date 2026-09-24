# Usage: python tools/restore-line-endings.py <base-commit> <file>...
# Restores each unchanged line's original CRLF/LF ending from <base-commit> after
# an editor normalised a mixed-ending file (the repo uses `* -text`). New or
# changed lines take the ending of the preceding original line.
import subprocess,difflib,sys,os
base,files=sys.argv[1],sys.argv[2:]
G=['git','-c','safe.directory='+os.getcwd()]
for p in files:
    old=subprocess.run(G+['show',f'{base}:{p}'],capture_output=True,check=True).stdout
    new=open(p,'rb').read()
    ol=old.split(b'\n');nl=new.split(b'\n')
    ok=[l.rstrip(b'\r') for l in ol];nk=[l.rstrip(b'\r') for l in nl]
    out=[None]*len(nl)
    for tag,i1,i2,j1,j2 in difflib.SequenceMatcher(None,ok,nk,autojunk=False).get_opcodes():
        if tag=='equal':
            for k in range(i2-i1):out[j1+k]=ol[i1+k]
        else:
            cr=(ol[i1-1] if i1>0 else ol[0]).endswith(b'\r')
            for k in range(j1,j2):out[k]=nk[k]+(b'\r' if cr and k<len(nl)-1 else b'')
    open(p,'wb').write(b'\n'.join(out))
    print('restored',p)
