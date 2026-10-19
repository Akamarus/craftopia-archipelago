import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('package_managed_bridge', Path(__file__).with_name('package_managed_bridge.py'))
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)

class PackageTests(unittest.TestCase):
    def test_stdlib_is_allowlisted_and_excludes_customization(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ['json/__init__.py', 'json/tests/test_secret.py', 'site-packages/private.py', 'sitecustomize.py', 'usercustomize.py', 'personal.py', 'encodings/utf_8.py', 'json/__pycache__/cached.pyc']:
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text('')
            files = [p.relative_to(root).as_posix() for p in package.stdlib_files(root)]
            self.assertEqual(sorted(files), ['encodings/utf_8.py', 'json/__init__.py'])
    def test_output_refuses_repository_and_outside_paths(self):
        root = Path(__file__).resolve().parents[1]
        for path in [root, root / 'artifacts', root.parent / 'elsewhere']:
            with self.assertRaises(ValueError): package.validate_output(root, path)
        self.assertEqual(package.validate_output(root, root / 'artifacts' / 'managed-bridge'), root / 'artifacts' / 'managed-bridge')

if __name__ == '__main__': unittest.main()
