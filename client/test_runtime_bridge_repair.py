import contextlib
import io
import json
import os
import subprocess
import sys
import unittest
from pathlib import Path
from test_multiplayer import MPBase, cb, run


class RuntimeRepair(MPBase):
    def update(self, b, guid, token=None, **overrides):
        msg = dict(cmd='identity_update', character_guid=guid,
                   epoch=b.ledger['epoch'], slot=b.args.slot)
        msg.update(overrides)
        run(b.on_mod_message(msg, token))

    def test_menu_to_character_and_switch(self):
        b = self.new_bridge()
        self.connect_mod(b)
        history = json.dumps(b.ledger, sort_keys=True)
        self.update(b, -123)
        self.assertEqual(b.primary_guid, '-123')
        self.assertTrue(b.mod_validated)
        self.update(b, '00123')
        self.assertEqual(b.primary_guid, '123')
        self.assertEqual(json.dumps(b.ledger, sort_keys=True), history)
        self.update(b, -456, token=b.connection_token)
        self.assertEqual(b.primary_guid, '-456')

    def test_invalid_or_stale_update_preserves_binding(self):
        b = self.new_bridge()
        self.connect_mod(b, 123)
        for guid, overrides in [(0, {}), (True, {}), (456, {'epoch': 0}),
                                (456, {'slot': 'wrong'})]:
            self.update(b, guid, **overrides)
            self.assertEqual(b.primary_guid, '123')
        self.update(b, 456, token='stale-token')
        self.assertEqual(b.primary_guid, '123')

    def test_extra_identity_does_not_change_primary(self):
        b = self.new_shared()
        self.connect_mod(b, 123)
        _, token = self.connect_extra(b)
        self.update(b, -456, token)
        self.assertEqual(b.extra_mod_conns[token]['guid'], '-456')
        self.assertEqual(b.primary_guid, '123')

    def test_real_lock_heartbeat_and_contender(self):
        path = cb.claim_authority('lock-probe', False)
        try:
            b = self.new_bridge()
            b.authority_path = path
            output = io.StringIO()
            with contextlib.redirect_stdout(output):
                b._authority_beat()
                b._authority_beat()
            self.assertEqual(b.authority_path, path)
            self.assertNotIn('took over', output.getvalue())
            # A real second process must remain excluded after the heartbeat.
            script = ('import craftopia_bridge as c; '
                      'c.LEDGER_DIR=' + repr(self.tmp.name) + '; '
                      'c.claim_authority("lock-probe", False)')
            contender = subprocess.run([sys.executable, '-c', script],
                cwd=Path(__file__).parent, capture_output=True, timeout=15)
            self.assertEqual(contender.returncode, 2, contender.stderr)
        finally:
            cb._authority_lock_file.close()
            cb._authority_lock_file = None
        successor = subprocess.run([sys.executable, '-c', script],
            cwd=Path(__file__).parent, capture_output=True, timeout=15)
        self.assertEqual(successor.returncode, 0, successor.stderr)


if __name__ == '__main__':
    unittest.main()
