import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

class UserStorageTests(unittest.TestCase):
    def test_managed_bridge_uses_external_state_directory(self):
        with tempfile.TemporaryDirectory() as root:
            env = dict(os.environ, CRAFTOPIA_AP_DATA_DIR=root)
            code = ('import craftopia_bridge as c; from pathlib import Path; '
                    'p=Path(c.ledger_path("storage-test")); '
                    'assert p.parent==Path(__import__("os").environ["CRAFTOPIA_AP_DATA_DIR"]); '
                    'c.save_ledger(str(p), {"test":True}); assert p.exists()')
            result = subprocess.run([sys.executable, '-c', code], cwd=Path(__file__).parent,
                                    env=env, capture_output=True, text=True, timeout=20)
            self.assertEqual(result.returncode, 0, result.stderr)

if __name__ == '__main__': unittest.main()
