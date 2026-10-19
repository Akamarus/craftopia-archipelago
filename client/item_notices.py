"""Cosmetic item events, independent of grant acknowledgement and game saves."""
class ItemNotices:
    def __init__(self):
        self.slot = None
        self.names, self.games, self.data = {}, {}, {}
        self.seen = set()

    def bind(self, slot, names, games):
        self.slot, self.names, self.games = slot, names, games

    def packages(self, games):
        self.data.update(games)

    def name(self, player, kind, value):
        mapping = self.data.get(self.games.get(player), {}).get(kind + '_name_to_id', {})
        return next((name for name, code in mapping.items() if code == value), f'{kind.title()} #{value}')

    def event(self, item, recipient, silent=False):
        try:
            if isinstance(item, dict):
                code, location, sender = item['item'], item['location'], item['player']
            else:
                code, location, sender = item[:3]
            if any(type(v) is not int for v in (code, location, sender, recipient)):
                return None
        except (KeyError, TypeError, ValueError):
            return None
        if self.slot not in (sender, recipient):
            return None
        key = f'{sender}:{location}:{recipient}:{code}'
        if key in self.seen:
            return None
        self.seen.add(key)
        item_name = self.name(recipient, 'item', code)
        player = lambda p: self.names.get(p, f'Player {p}')
        kind = 'sent' if recipient != self.slot else 'received'
        title = (f'Sent {item_name} to {player(recipient)}' if kind == 'sent' else
                 f'Found {item_name}' if sender == self.slot else f'Received {item_name} from {player(sender)}')
        row = dict(id=key, title=title, location=self.name(sender, 'location', location), kind=kind, silent=silent)
        return row
