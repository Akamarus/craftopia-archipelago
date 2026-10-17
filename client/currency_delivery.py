"""Typed, character-bound Soul Orb currency delivery; distinct from SP/bundles."""
from delivery_state import candidate, integer, text


class CurrencyDeliveryMixin:
    def currency_contract_ready(self):
        if not any(k in self.slot_data for k in ('soul_orb_currency_protocol','soul_orb_catalog','soul_orb_locations')):
            return True
        protocol = self.slot_data.get('soul_orb_currency_protocol')
        if type(protocol) is not int or protocol != 1 or self.multiplayer_mode != 0:
            return False
        capabilities = getattr(self, 'currency_capabilities', {})
        tokens = self._all_conn_tokens()
        return bool(tokens) and all(len(capabilities.get(t, ())) == 2 and
            all(type(v) is int and v == 1 for v in capabilities[t]) for t in tokens)

    def currency_ready(self):
        protocol = self.slot_data.get('soul_orb_currency_protocol')
        return (self.currency_contract_ready() and not self.recovery_hold and type(protocol) is int and protocol == 1
                and self.multiplayer_mode == 0 and text(self.ledger.get('seed_name')))

    async def receive_currency(self, index, original):
        if not self.currency_ready():
            if original not in self.retry_item_messages:
                self.retry_item_messages.append(original)
            return
        if not any(r['receipt_id'] == index for r in self.ledger.get('currency_receipts', [])):
            proposed = candidate(self.ledger)
            proposed.setdefault('currency_receipts', []).append({'receipt_id': index,
                'amount': 1, 'character_guid': None, 'acknowledged': False})
            if not self.commit(proposed):
                if original not in self.retry_item_messages:
                    self.retry_item_messages.append(original)
                return
        self.seen_non_grant.add(index)
        await self.flush_currency()

    async def flush_currency(self):
        if not self.currency_ready():
            return
        for token, conn in self._iter_conns():
            guid = self.normalize_guid(conn.get('guid'))
            if not self._conn_validated(token) or guid is None:
                continue
            if not any(e.get('character_guid') == guid and e.get('slot') == self.args.slot
                       for e in self.ledger.get('enrollments', [])):
                continue
            for record in list(self.ledger.get('currency_receipts', [])):
                if record['acknowledged'] or record['character_guid'] not in (None, guid):
                    continue
                if record['character_guid'] is None:
                    proposed = candidate(self.ledger)
                    next(r for r in proposed['currency_receipts'] if r['receipt_id'] == record['receipt_id'])['character_guid'] = guid
                    if not self.commit(proposed):
                        return
                await self.send_mod({'cmd': 'grant_currency', 'currency_protocol': 1,
                    'seed_name': self.ledger['seed_name'], 'slot': self.args.slot,
                    'epoch': self.ledger['epoch'], 'connection_token': token,
                    'character_guid': guid, 'receipt_id': record['receipt_id'],
                    'item_id': 3840, 'amount': 1}, token=token)

    async def accept_currency_ack(self, msg, token=None):
        actual_token = token if token is not None else self.connection_token
        guid = self.normalize_guid(self._token_guid(token))
        if (not self.currency_ready() or not self._conn_validated(token) or guid is None
                or msg.get('saved') is not True
                or msg.get('connection_token') != actual_token
                or msg.get('character_guid') != guid or msg.get('seed_name') != self.ledger['seed_name']
                or msg.get('slot') != self.args.slot
                or type(msg.get('currency_protocol')) is not int or msg['currency_protocol'] != 1
                or type(msg.get('epoch')) is not int or msg['epoch'] != self.ledger['epoch']
                or type(msg.get('item_id')) is not int or msg['item_id'] != 3840
                or type(msg.get('amount')) is not int or msg['amount'] != 1
                or not integer(msg.get('receipt_id'))):
            return
        proposed = candidate(self.ledger)
        record = next((r for r in proposed.get('currency_receipts', [])
                       if r['receipt_id'] == msg['receipt_id'] and r['character_guid'] == guid), None)
        if record is None or record['acknowledged']:
            return
        record['acknowledged'] = True
        if self.commit(proposed):
            await self.send_activity()
