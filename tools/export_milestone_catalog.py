"""Export category membership for review; never automatically enable AP checks."""
import argparse
import hashlib
import json
from pathlib import Path


def category_evidence(capture, known_missions):
    section=capture.get('world_evidence',{}).get('mission_categories',{})
    if (capture.get('schema_version')!=5 or section.get('status')!='collected'
            or section.get('truncated') is not False
            or capture.get('missions_status')!='collected'):
        raise ValueError('Incomplete category/mission capture')
    members={}; seen=set()
    for mission in capture.get('missions',[]):
        mid=mission.get('id'); category=mission.get('category')
        if type(mid) is not int or type(category) is not int or mid in seen:
            raise ValueError('Invalid or duplicate mission identity')
        if mission.get('observed') is not True: raise ValueError('Unobserved membership')
        seen.add(mid);members.setdefault(category,[]).append(mid)
    rows=[];categories=set()
    for row in section.get('rows',[]):
        fields=row.get('fields',{});cid=fields.get('id')
        if type(cid) is not int or cid in categories: raise ValueError('Invalid category identity')
        categories.add(cid); ids=sorted(members.get(cid,[]))
        complete=bool(ids) and all(mid in known_missions for mid in ids)
        rows.append(dict(source_id=cid,title=fields.get('title'),members=ids,
                         membership_complete=complete, enabled=False,
                         reason='requires regional prerequisite review' if complete else 'missing or unknown membership'))
    if set(members)-categories: raise ValueError('Membership lacks a category definition')
    return sorted(rows,key=lambda row:row['source_id'])


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture',type=Path);parser.add_argument('mission_catalog',type=Path)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    raw=args.capture.read_bytes();capture=json.loads(raw)
    catalog=json.loads(args.mission_catalog.read_text(encoding='utf-8'))
    known={row['id'] for row in catalog['missions']}
    report=dict(schema=1,capture_sha256=hashlib.sha256(raw).hexdigest(),
                assembly_sha256=capture.get('game_assembly_sha256'),
                research=category_evidence(capture,known),enabled=[])
    args.output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

