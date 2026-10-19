from pathlib import Path
import json,struct,hashlib
out=Path('public-repo/craftopia-archipelago/docs/research/millewind-20261017')
sources=json.loads((out/'sources.json').read_text(encoding='utf8'))['rows']
selected=[r for r in sources if r['data'].get('sourceId') in [2,202] or r['data'].get('m_Name')=='So--OcDungeonEntrance_002_Islanddungeon']
for r in sources:
 if r['cls']=='OcDungeonEntranceSo' and r['data']['defaultDungeon']['m_PathID']==40808 and r not in selected:selected.append(r)
b=Path('G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data/resources.assets').read_bytes();loc=[]
for sid in [2,202]:
 term=('800_DungeonName/'+str(sid)).encode();p=b.find(term+b'\0');q=(p+len(term)+3)//4*4
 # Term is 17 bytes including null for source2; align past terminator.
 q=(p+len(term)+1+3)//4*4
 typ,count,length=struct.unpack_from('<iii',b,q);name=b[q+12:q+12+length].decode().rstrip('\0')
 loc.append(dict(term=term.decode(),term_offset=p,term_type=typ,language_count=count,english=name,english_offset=q+12,serialized_record_hex=b[p-4:q+12+length].hex()))
assert loc[0]['english']=='Old Garden of Undead',loc
(out/'identity.json').write_text(json.dumps(dict(localization_asset='G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data/resources.assets',localization=loc,native_sources=selected,workbook=dict(path='outputs/logic-review-20261016/Craftopia - Outstanding Logic and Items.xlsx',sheet_xml='xl/worksheets/sheet1.xml',question_id='Q025',region_cell='C31',region='Millewind Hill Tower',answer_cell='E31',answer='Old Garden of Undead. No quest, items, or special requirements to access. The only limitation is combat gear.',confidence_cell='G31',confidence='Remembered')),indent=2),encoding='utf8')
lines=Path('artifacts/harvesting-research/all.cs').read_text(encoding='utf8').splitlines()
(out/'native-code.txt').write_text('\n'.join(f'{i+1}: {lines[i]}' for a,z in [(550970,550973),(569294,569309),(354093,354106)] for i in range(a,z)),encoding='utf8')
print(loc);print([(r['cls'],r['path_id'],r['data']['m_Name']) for r in selected])
