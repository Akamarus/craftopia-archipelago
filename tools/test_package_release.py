import tempfile
import unittest
from pathlib import Path
from package_release import validate_payload, stage_workshop

class ReleaseTests(unittest.TestCase):
    def test_workshop_is_discoverable_by_official_loader(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            mod = root / 'mod'
            (mod / 'bridge').mkdir(parents=True)
            (mod / 'CraftopiaArchipelago.dll').write_bytes(b'compiled')
            (mod / 'bridge' / 'connection_host.py').write_text('bridge')
            workshop = root / 'workshop'
            stage_workshop(mod, workshop)
            # Official MultiFolderLoader.AddMod indexes only <item>/plugins and patchers.
            plugin = workshop / 'plugins' / 'CraftopiaArchipelago'
            self.assertEqual((plugin / 'CraftopiaArchipelago.dll').read_bytes(), b'compiled')
            self.assertTrue((plugin / 'bridge' / 'connection_host.py').is_file())
            self.assertFalse((workshop / 'CraftopiaArchipelago.dll').exists())
    def test_refuses_player_state_and_game_assemblies(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            for filename in ('craftopia_mp_seed.json', 'craftopia_bridge_ledger_private.json', 'com.craftopia.archipelago.cfg',
                             'AD__Overcraft.dll', 'something.multidata', 'save.ocs'):
                p = root / filename
                p.write_text('private')
                with self.assertRaises(ValueError): validate_payload(root)
                p.unlink()
    def test_accepts_clean_runtime(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            (root / 'CraftopiaArchipelago.dll').write_bytes(b'compiled')
            validate_payload(root)

if __name__ == '__main__': unittest.main()
