from pathlib import Path
import io,zipfile
from copy import deepcopy
from lxml import etree
R=Path(__file__).resolve().parents[1]
original=(R/'forms/source/Приложение к отчету.pptx').read_bytes()
ns={'a':'http://schemas.openxmlformats.org/drawingml/2006/main','p':'http://schemas.openxmlformats.org/presentationml/2006/main'}
z=zipfile.ZipFile(io.BytesIO(original));path='ppt/slides/slide4.xml';root=etree.fromstring(z.read(path))
shape=next(s for s in root.findall('.//p:sp',ns) if ''.join(s.xpath('.//a:t/text()',namespaces=ns))=='Экспериментально-практическая работа')
shape.find('p:spPr/a:xfrm/a:off',ns).set('y',str(15*12700))
shape.find('p:spPr/a:xfrm/a:ext',ns).set('cy',str(72*12700))
shape.find('p:txBody/a:bodyPr',ns).set('anchor','t')
for run in shape.findall('.//a:rPr',ns):run.set('sz','2800')
target=R/'forms/template/Приложение к отчету.pptx'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as out:
    for entry in z.infolist():out.writestr(deepcopy(entry),etree.tostring(root,xml_declaration=True,encoding='UTF-8',standalone=True) if entry.filename==path else z.read(entry.filename))
with zipfile.ZipFile(target) as out:
    changed=[n for n in z.namelist() if z.read(n)!=out.read(n)]
assert changed==[path],changed

print('Corrected slide heading, one template part only')
