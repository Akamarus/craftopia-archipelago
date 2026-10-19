"""Dedicated GUID routing for selected, first-time soul orb observations."""
import json
from pathlib import Path
from functools import lru_cache
from delivery_state import candidate, integer

CATALOG='soul_orbs_20261015_r1'


@lru_cache(maxsize=1)
def frozen_orbs():
    data=json.loads((Path(__file__).resolve().parents[1]/'worlds/craftopia'/f'{CATALOG}.json').read_text(encoding='utf-8'))
    if data.get('catalog_id')!=CATALOG or data.get('frozen_ids') is not True:
        raise ValueError('Unknown orb catalog')
    return {r['guid']:r['ap_location_id'] for r in data['orbs']}


class SoulOrbDeliveryMixin:
    def orb_location(self,guid):
        d=self.slot_data
        if (not self.currency_ready() or d.get('soul_orb_catalog')!=CATALOG or
                type(d.get('soul_orb_schema')) is not int or d['soul_orb_schema']!=1 or
                not integer(guid,-2147483648,2147483647)):
            return None
        allowed=d.get('soul_orb_locations')
        if not isinstance(allowed,dict): return None
        loc=allowed.get(str(guid))
        if not integer(loc,2000,2999): return None
        return loc if frozen_orbs().get(guid)==loc else None

    async def report_orb(self,msg,token):
        guid=self.normalize_guid(self._token_guid(token))
        orb=msg.get('orb_guid')
        loc=self.orb_location(orb)
        if (loc is None or guid is None or msg.get('character_guid')!=guid or
                msg.get('seed_name')!=self.ledger.get('seed_name') or
                msg.get('observation_id')!='orb:'+str(orb) or
                not any(e.get('character_guid')==guid and e.get('slot')==self.args.slot
                        for e in self.ledger.get('enrollments',[]))):
            return False
        proposed=candidate(self.ledger)
        rows=proposed.setdefault('orb_checks',[])
        if not any(r['orb_guid']==orb for r in rows):
            rows.append({'orb_guid':orb,'location_id':loc,'character_guid':guid})
            if not self.commit(proposed): return False
        await self.reconcile_orb_checks()
        await self.send_activity(token)
        return True

    async def reconcile_orb_checks(self):
        if not self.currency_ready() or not self.ap_connected: return
        pending=[r['location_id'] for r in self.ledger.get('orb_checks',[])
                 if self.orb_location(r['orb_guid'])==r['location_id'] and r['location_id'] not in self.checked_locations]
        if pending: await self.send_ap({'cmd':'LocationChecks','locations':pending})
