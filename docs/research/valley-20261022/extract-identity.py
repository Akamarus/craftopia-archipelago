from pathlib import Path
import hashlib,json,mmap,re,struct,openpyxl,sys
root=Path.cwd();repo=root/'public-repo/craftopia-archipelago';out=repo/'docs/research/valley-20261022'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
wbpath=root/'outputs/logic-review-20261016/Craftopia - Outstanding Logic and Items.xlsx';before=sha(wbpath)
w=openpyxl.load_workbook(wbpath,read_only=True,data_only=True)
answers=[dict(sheet=s.title,row=n,values=row) for s in w for n,row in enumerate(s.values,1) if row and str(row[0]) in ('Q028','Q029')];w.close();assert sha(wbpath)==before
exec((root/'artifacts/remaining-equipment/extract-fixed.py').read_text().split('rows=[];errors=[]')[0])
assembly=sha(base/'Managed/AD__Overcraft.dll');assert assembly=='88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea'
e=UnityPy.load(str(base/'sharedassets6.assets'));e.typetree_generator=g
rows=[];errors=[]
for obj in e.objects:
 if obj.assets_file.name!='sharedassets6.assets' or obj.type.name!='MonoBehaviour':continue
 try:
  cls=obj.parse_monobehaviour_head().m_Script.deref().read().m_ClassName
  if cls in ('OcDungeonSo','OcDungeonEntranceSo'):rows.append(dict(asset=obj.assets_file.name,path_id=obj.path_id,cls=cls,data=obj.read_typetree(check_read=True)))
 except Exception as ex:errors.append(dict(path_id=obj.path_id,error=str(ex)[:160]))
loc=[]
with (base/'resources.assets').open('rb') as f,mmap.mmap(f.fileno(),0,access=mmap.ACCESS_READ) as data:
 for m in re.finditer(rb'800_DungeonName/[0-9]+\x00',data):
  end=(m.end()+3)//4*4
  try:
   typ,count,length=struct.unpack_from('<iii',data,end)
   if typ!=0 or not 1<=count<=30 or not 1<=length<=256:continue
   name=data[end+12:end+12+length].decode('utf-8').rstrip('\0')
   if name not in ('Labyrinth of Torrent','Old Garden of Massacre'):continue
   loc.append(dict(term=m.group()[:-1].decode(),english=name,offset=m.start(),record_hex=data[m.start()-4:end+12+length].hex()))
  except (ValueError,UnicodeDecodeError,struct.error):pass
ids={int(x['term'].split('/')[-1]) for x in loc};dungeons=[r for r in rows if r['cls']=='OcDungeonSo' and r['data'].get('sourceId') in ids];paths={r['path_id'] for r in dungeons};entrances=[r for r in rows if r['cls']=='OcDungeonEntranceSo' and r['data']['defaultDungeon']['m_FileID']==0 and r['data']['defaultDungeon']['m_PathID'] in paths]
result=dict(assembly_sha256=assembly,workbook_sha256=before,workbook_answers=answers,localization=loc,dungeons=dungeons,entrances=entrances,decode_errors=errors)
(out/'identity-evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(localization=loc,dungeons=dungeons,entrances=entrances,errors=len(errors)),ensure_ascii=True))
