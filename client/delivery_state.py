"""Strict bridge ledger format. Legacy histories require reviewed migration."""
import copy

COMBAT = ('Sword', 'Bow', 'Shield', 'Lance', 'SwordTwoHand', 'Katana', 'MagicStaff')

def integer(value, minimum=0, maximum=2**63-1):
    return type(value) is int and minimum <= value <= maximum

def text(value):
    return isinstance(value, str) and bool(value.strip()) and value == value.strip()

def valid_unlock(record):
    if not isinstance(record, dict) or not integer(record.get('receipt_id'), -8):
        return False
    if record['receipt_id'] == -8:
        return record == {'cmd':'unlock_skill_tree','tree_id':'Magic','receipt_id':-8}
    cmd = record.get('cmd')
    if record['receipt_id'] < 0 and (cmd != 'unlock_equipment' or record.get('tier') != 1 or record.get('category') not in COMBAT):
        return False
    if cmd == 'unlock_equipment':
        return text(record.get('category')) and integer(record.get('tier'), 1, 2147483647)
    if cmd == 'unlock_production':
        return text(record.get('station_id'))
    if cmd == 'unlock_skill_tree':
        return text(record.get('tree_id'))
    return False

def _char_grants(value):
    if not isinstance(value, dict):
        raise ValueError('invalid char_grants')
    for guid, cg in value.items():
        if not text(guid) or not isinstance(cg, dict):
            raise ValueError('invalid char_grants entry')
        for key in ('delivered', 'pending'):
            lst = cg.get(key)
            if not isinstance(lst, list) or not all(integer(v) for v in lst) \
                    or len(set(lst)) != len(lst):
                raise ValueError('invalid char_grants indices')
    return value


def validate(ledger):
    if not isinstance(ledger, dict) or type(ledger.get('format_version')) is not int or ledger['format_version'] != 2:
        raise ValueError('legacy or unsupported ledger: reviewed migration required')
    if not all(text(ledger.get(k)) for k in ('server','slot','game','session')):
        raise ValueError('missing ledger identity')
    if not integer(ledger.get('epoch'),1): raise ValueError('invalid epoch')
    milestones=ledger.get('milestone_checks',[])
    if not isinstance(milestones,list): raise ValueError('invalid milestone checks')
    seen_milestones=set()
    for r in milestones:
        if (not isinstance(r,dict) or r.get('family') not in ('tower','age','dungeon','category') or
            not integer(r.get('source_id'),-2147483648,2147483647) or not integer(r.get('stage'),0,99) or
            not integer(r.get('location_id'),3000,4999) or not text(r.get('character_guid')) or
            not isinstance(r.get('world_fingerprint'),str) or len(r['world_fingerprint'])!=64 or
            any(c not in '0123456789ABCDEF' for c in r['world_fingerprint']) or
            r.get('key')!=f"milestone:{r['family']}:{r['source_id']}:{r['stage']}" or r['key'] in seen_milestones):
            raise ValueError('invalid milestone history')
        seen_milestones.add(r['key'])
    currency = ledger.get('currency_receipts', [])
    orbs=ledger.get('orb_checks',[])
    if not isinstance(orbs,list): raise ValueError('invalid orb checks')
    seen_orbs=set()
    for r in orbs:
        if (not isinstance(r,dict) or not integer(r.get('orb_guid'),-2147483648,2147483647) or
                not integer(r.get('location_id'),2000,2999) or not text(r.get('character_guid')) or r['orb_guid'] in seen_orbs):
            raise ValueError('invalid orb check')
        seen_orbs.add(r['orb_guid'])
    if not isinstance(currency, list): raise ValueError('invalid currency history')
    seen_currency = set()
    for r in currency:
        if (not isinstance(r,dict) or not integer(r.get('receipt_id')) or
                type(r.get('amount')) is not int or r['amount'] != 1 or
                type(r.get('acknowledged')) is not bool or 'character_guid' not in r or r['receipt_id'] in seen_currency):
            raise ValueError('invalid currency receipt')
        guid = r.get('character_guid')
        if guid is not None:
            try:
                if not text(guid) or str(int(guid)) != guid or not -2147483648 <= int(guid) <= 2147483647 or int(guid)==0:
                    raise ValueError('invalid currency character')
            except (ValueError,TypeError): raise ValueError('invalid currency character')
        if r['acknowledged'] and guid is None: raise ValueError('unassigned currency ack')
        seen_currency.add(r['receipt_id'])
    for key in ('delivered','pending_checks','unlocks','pending_unlocks'):
        if not isinstance(ledger.get(key),list): raise ValueError('missing list: '+key)
    # --- multiplayer/enrollment keys: migrate with defaults, never corrupt ---
    if 'enrollments' not in ledger:
        ledger['enrollments'] = []
    enrollments = ledger['enrollments']
    if not isinstance(enrollments, list):
        raise ValueError('invalid enrollments')
    seen_guids = set()
    for e in enrollments:
        if not isinstance(e, dict) or not text(e.get('character_guid')) \
                or not text(e.get('slot')) or not text(e.get('enrolled_at')):
            raise ValueError('invalid enrollment entry')
        if e['character_guid'] in seen_guids:
            raise ValueError('duplicate enrollment')
        seen_guids.add(e['character_guid'])
    if 'char_grants' not in ledger:
        ledger['char_grants'] = {}
    _char_grants(ledger['char_grants'])
    if 'unassigned_points' not in ledger:
        ledger['unassigned_points'] = []
    unassigned = ledger['unassigned_points']
    if not isinstance(unassigned, list) or not all(integer(v) for v in unassigned) \
            or len(set(unassigned)) != len(unassigned):
        raise ValueError('invalid unassigned_points')
    if 'pending_bundles' not in ledger:
        ledger['pending_bundles'] = []
    pending_bundles = ledger['pending_bundles']
    if not isinstance(pending_bundles, list):
        raise ValueError('invalid pending_bundles')
    bundle_receipts = set()
    for b in pending_bundles:
        if not isinstance(b, dict) or not text(b.get('kind')) \
                or not integer(b.get('amount'), 1) or not integer(b.get('receipt_id')):
            raise ValueError('invalid pending bundle')
        if b['receipt_id'] in bundle_receipts:
            raise ValueError('duplicate pending bundle')
        bundle_receipts.add(b['receipt_id'])
    if 'team_consumables' not in ledger:
        ledger['team_consumables'] = []
    team = ledger['team_consumables']
    if not isinstance(team, list) or not all(integer(v) for v in team) \
            or len(set(team)) != len(team):
        raise ValueError('invalid team_consumables')
    if bundle_receipts & set(team):
        raise ValueError('bundle receipt both pending and team-delivered')
    if 'seen_events' not in ledger:
        ledger['seen_events'] = []
    seen_events = ledger['seen_events']
    if not isinstance(seen_events, list) or not all(text(v) for v in seen_events) \
            or len(set(seen_events)) != len(seen_events):
        raise ValueError('invalid seen_events')
    if not isinstance(ledger, dict) or type(ledger.get('format_version')) is not int or ledger['format_version'] != 2:
        raise ValueError('legacy or unsupported ledger: reviewed migration required')
    if not all(text(ledger.get(k)) for k in ('server','slot','game','session')):
        raise ValueError('missing ledger identity')
    if not integer(ledger.get('epoch'),1): raise ValueError('invalid epoch')
    for key in ('delivered','pending_checks','unlocks','pending_unlocks'):
        if not isinstance(ledger.get(key),list): raise ValueError('missing list: '+key)
    delivered = ledger['delivered']
    if not all(integer(v) for v in delivered) or len(set(delivered)) != len(delivered):
        raise ValueError('invalid delivered indices')
    history = {}
    for item in ledger['unlocks']:
        if not valid_unlock(item) or item['receipt_id'] in history:
            raise ValueError('invalid/duplicate unlock history')
        history[item['receipt_id']] = item
    pending = set()
    for item in ledger['pending_unlocks']:
        if not valid_unlock(item) or history.get(item['receipt_id']) != item or item['receipt_id'] in pending:
            raise ValueError('pending unlock differs from history')
        pending.add(item['receipt_id'])
    for item in ledger['pending_checks']:
        if not isinstance(item,dict) or not integer(item.get('mission_id'),-2147483648,2147483647):
            raise ValueError('invalid pending check')
        if not text(item.get('mission_class')) and item.get('mission_class') is not None:
            raise ValueError('invalid mission class')
        if item.get('location_id') is not None and not integer(item['location_id']):
            raise ValueError('invalid location')
    if type(ledger.get('starters_applied')) is not bool: raise ValueError('missing starter state')
    choice = ledger.get('starter_choice')
    if choice is not None and choice not in COMBAT and choice != 'all_basic':
        raise ValueError('invalid starter choice')
    if ledger['starters_applied'] and (choice is None or not any(k < 0 for k in history)):
        raise ValueError('starter flag without durable history')
    events = ledger.get('goal_events')
    if not isinstance(events,dict): raise ValueError('missing goal events')
    if type(events.get('boss_defeated')) is not bool or not integer(events.get('max_age_reached')):
        raise ValueError('invalid goal values')
    if events['boss_defeated'] and not text(events.get('boss_id')):
        raise ValueError('missing boss identity')
    objectives=events.get('completed_objectives')
    if not isinstance(objectives,list) or not all(text(v) for v in objectives) or len(set(objectives)) != len(objectives):
        raise ValueError('invalid objective history')
    return ledger

def candidate(ledger):
    return copy.deepcopy(ledger)
