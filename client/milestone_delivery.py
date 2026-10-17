"""Durable, allowlisted milestone checks. Native completion is observed by the mod."""
import importlib.util
from pathlib import Path
from delivery_state import candidate

_spec=importlib.util.spec_from_file_location('craftopia_milestone_catalog',Path(__file__).resolve().parents[1]/'worlds/craftopia/Milestones.py')
catalog=importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(catalog)

def fingerprint(value):
    return isinstance(value,str) and len(value)==64 and all(c in '0123456789ABCDEF' for c in value)

class MilestoneDeliveryMixin:
    async def register_milestone_character(self,msg,token):
        if (self.recovery_hold or self.multiplayer_mode!=0 or not self._conn_validated(token) or
            not self._valid_conn_token(msg.get('connection_token')) or
            msg.get('local_enrollment_verified') is not True or
            type(msg.get('epoch')) is not int or msg['epoch']!=self.ledger['epoch'] or
            msg.get('slot')!=self.args.slot or msg.get('seed_name')!=self.ledger.get('seed_name')): return
        try: catalog.validate_slot(self.slot_data)
        except (ValueError,KeyError,TypeError): return
        guid=self.normalize_guid(self._token_guid(token))
        if guid is None or msg.get('character_guid')!=guid: return
        if any(e['character_guid']!=guid or e['slot']!=self.args.slot for e in self.ledger.get('enrollments',[])): return
        if self.enroll_character(guid): await self.send_session(token=token)

    def milestone_location(self,family,source_id,stage):
        if self.recovery_hold or self.multiplayer_mode!=0: return None
        try:
            catalog.validate_slot(self.slot_data)
            return catalog.resolve_milestone(self.slot_data,family,source_id,stage)
        except (ValueError,KeyError,TypeError): return None

    async def report_milestone(self,msg,token):
        family,source,stage=msg.get('family'),msg.get('source_id'),msg.get('stage')
        loc=self.milestone_location(family,source,stage)
        guid=self.normalize_guid(self._token_guid(token))
        world=msg.get('world_fingerprint')
        if (loc is None or guid is None or msg.get('character_guid')!=guid or
            type(msg.get('epoch')) is not int or msg['epoch']!=self.ledger['epoch'] or
            msg.get('slot')!=self.args.slot or msg.get('seed_name')!=self.ledger.get('seed_name') or
            not fingerprint(world) or msg.get('observation_id')!=catalog.key(family,source,stage) or
            not any(e['character_guid']==guid and e['slot']==self.args.slot for e in self.ledger.get('enrollments',[]))): return False
        existing=self.ledger.get('milestone_checks',[])
        if any(r['world_fingerprint']!=world or r['character_guid']!=guid for r in existing): return False
        k=msg['observation_id']
        if not any(r['key']==k for r in existing):
            proposed=candidate(self.ledger)
            proposed.setdefault('milestone_checks',[]).append(dict(key=k,family=family,source_id=source,stage=stage,
                location_id=loc,character_guid=guid,world_fingerprint=world))
            if not self.commit(proposed): return False
        await self.reconcile_milestone_checks()
        return True

    async def reconcile_milestone_checks(self):
        if not self.ap_connected or self.recovery_hold: return
        history=self.ledger.get('milestone_checks',[])
        if any(self.milestone_location(r['family'],r['source_id'],r['stage'])!=r['location_id'] for r in history):
            self.recovery_hold=True
            print('[hold] Milestone history does not match the selected catalog; preserve the ledger for review.',flush=True)
            return
        locations=[r['location_id'] for r in history if r['location_id'] not in self.checked_locations]
        if locations: await self.send_ap({'cmd':'LocationChecks','locations':locations})

    def milestone_goal_complete(self):
        try:
            catalog.validate_slot(self.slot_data)
            return not self.recovery_hold and set(self.slot_data['goal_location_ids'])<=self.checked_locations
        except (ValueError,KeyError,TypeError): return False
