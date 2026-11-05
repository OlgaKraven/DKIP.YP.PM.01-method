from pathlib import Path
import io,zipfile,re
from copy import deepcopy
from lxml import etree
R=Path(__file__).resolve().parents[1]
raw=(R/'forms/source/Аттестационный лист.docx').read_bytes()
z=zipfile.ZipFile(io.BytesIO(raw));root=etree.fromstring(z.read('word/document.xml'));ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
count=0
for p in root.findall('.//w:p',ns):
    nodes=p.findall('.//w:t',ns);text=''.join(t.text or '' for t in nodes)
    matches=list(re.finditer('производственной',text,re.I))
    for match in reversed(matches):
        start,end=match.span();replacement='Учебной' if match.group()[0].isupper() else 'учебной';offset=0
        for node in nodes:
            value=node.text or '';a,b=offset,offset+len(value);offset=b
            if b<=start or a>=end:continue
            left=max(0,start-a);right=min(len(value),end-a)
            node.text=value[:left]+(replacement if a<=start<b else '')+value[right:]
        count+=1
assert count==5,count
target=R/'forms/template/Аттестационный лист.docx'
with zipfile.ZipFile(target,'w') as out:
    for entry in z.infolist():out.writestr(deepcopy(entry),etree.tostring(root,xml_declaration=True,encoding='UTF-8',standalone=True) if entry.filename=='word/document.xml' else z.read(entry.filename))
with zipfile.ZipFile(target) as out:
    assert [n for n in z.namelist() if z.read(n)!=out.read(n)]==['word/document.xml']
assert 'производственной' not in ''.join(root.itertext()).lower()

print('Corrected five production-practice labels in the UP card copy')
