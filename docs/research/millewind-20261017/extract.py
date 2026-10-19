exec(open('artifacts/remaining-equipment/extract-fixed.py').read().split('rows=[];errors=[]')[0])
out=Path('public-repo/craftopia-archipelago/docs/research/millewind-20261017')
rows=[];errors=[]
for fn in ['sharedassets0.assets','sharedassets6.assets','resources.assets']:
 e=UnityPy.load(str(base/fn));e.typetree_generator=g
 for o in e.objects:
  if o.assets_file.name!=fn or o.type.name!='MonoBehaviour':continue
  try:
   b=o.parse_monobehaviour_head();n=b.m_Script.deref().read().m_ClassName
   if not ('Dungeon' in n or 'LanguageSource' in n or 'Placement' in n or 'AgeEvo' in n):continue
   d=o.read_typetree(check_read=True)
   if 'Language' in n:
    print('LANGUAGE',fn,o.path_id,list(d),flush=True)
    (out/('localization-'+fn+'.json')).write_text(json.dumps(d,ensure_ascii=False),encoding='utf8')
   else:rows.append(dict(asset=fn,path_id=o.path_id,cls=n,data=d))
  except Exception as ex:errors.append([fn,o.path_id,str(ex)[:100]])
 print(fn,len(rows),len(errors),flush=True)
 (out/'sources.json').write_text(json.dumps(dict(rows=rows,errors=errors),ensure_ascii=False,indent=2),encoding='utf8')
