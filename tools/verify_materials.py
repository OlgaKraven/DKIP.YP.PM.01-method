"""Verify links, data, printable geometry and the student code assembly route."""
from pathlib import Path
from html.parser import HTMLParser
from decimal import Decimal, ROUND_HALF_UP
import csv
import io
import json
import shutil
import zipfile
import xml.etree.ElementTree as ET
from urllib.parse import urlsplit,unquote
import pdfplumber
from pypdf import PdfReader

root=Path(__file__).resolve().parents[1]
class Page(HTMLParser):
    def __init__(self,text):
        super().__init__();self.refs=[];self.ids=set();self.feed(text)
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id'in a:self.ids.add(a['id'])
        for k in ['href','src']:
            if k in a:self.refs.append(a[k])
count=0
for name in ['index.html','areas.html']:
    page=Page((root/name).read_text(encoding='utf-8'))
    for ref in page.refs:
        u=urlsplit(ref)
        if u.scheme:continue
        target=root/(unquote(u.path) or name)
        assert target.is_file(),(name,ref)
        if u.fragment:
            assert u.fragment in Page(target.read_text(encoding='utf-8')).ids,(name,ref)
        count+=1
print('Local links and resources:',count)
course=json.loads((root/'content/course.json').read_text(encoding='utf-8'))
assert len(course['areas'])==30
for i,p in enumerate(course['areas'],1):
    assert p['title'].startswith(f'Вариант {i:02d} · ')
    assert (root/'downloads'/f'v{i:02d}.zip').is_file()
    data=json.loads((root/'data'/f'v{i:02d}'/'catalog.json').read_text(encoding='utf-8'),parse_float=Decimal)
    assert len(data['products'])==4 and len(data['orders'])==4
    for n,prod in enumerate(data['products']):
        assert prod['id']==f'V{i:02d}-{n+1:02d}'
    with zipfile.ZipFile(root/'downloads'/f'v{i:02d}.zip') as z:
        assert z.read('catalog.json')==(root/'data'/f'v{i:02d}'/'catalog.json').read_bytes()
print('All 30 variant cards and their archives agree')
for name in ['assignment.pdf','guide.pdf']:
    r=PdfReader(root/'downloads'/name)
    assert all(len(p.extract_text().strip())>20 for p in r.pages),'Blank page: '+name
    assert len(r.outline)>0,'No outline'
    outside=[]
    with pdfplumber.open(root/'downloads'/name) as pdf:
        for i,p in enumerate(pdf.pages,1):
            assert abs(p.width-595.276)<1 and abs(p.height-841.89)<1
            continuation='продолжение' in p.extract_text().splitlines()[0]
            for c in p.chars:
                # Page numbers are within the bottom margin.
                if c['top']>790:continue
                if continuation and c['top']<55:continue
                if c['x0']<84 or c['x1']>540 or c['top']<55 or c['bottom']>786:
                    outside.append((i,c['text'],round(c['x0'],1),round(c['x1'],1)))
    assert not outside,(name,outside[:20])
    print(name,':',len(r.pages),'A4 pages; no blank pages or text outside margins')

route=root/'output/route-check'
route.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(root/'downloads/starter.zip') as z:z.extractall(route)
methods=[]
for p in course['guide']:
    for block in p['blocks']:
        if block['kind']=='code' and block['title'] in ['Код метода','Реализация Calculate','Код StockSummary','Код CreateCard']:
            methods.append(block['text'])
assert len(methods)==4
(route/'app/Pricing.cs').write_text('namespace CatalogDesk;\npublic static class Pricing\n{\n'+'\n'.join(methods)+'\n}\n',encoding='utf-8')
shutil.copyfile(root/'data/v07/catalog.json',route/'app/Data/catalog.json')
for p in (root/'app').glob('*.xaml'):ET.parse(p)
print('Student route assembled from the four published code blocks in output/route-check')
