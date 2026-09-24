# Byte-preserving multi-line replace for mixed CRLF/LF files (see restore-line-endings.py).
# Usage from Python: from importlib... or exec; replace(path, [(old, new, count)]) tries the
# old text with CRLF and with LF line breaks and writes the new text with the same style.
def replace(path,pairs):
    s=open(path,'rb').read().decode('utf-8')
    for item in pairs:
        old,new=item[0],item[1];count=item[2] if len(item)>2 else 1
        for nl in ('\r\n','\n'):
            a=old.replace('\n',nl)
            if s.count(a)==count:s=s.replace(a,new.replace('\n',nl));break
        else:raise AssertionError(f'{path}: expected {count} match(es) for {old[:70]!r}')
    open(path,'wb').write(s.encode('utf-8'))
