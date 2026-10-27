"""Package repository world files, never local runtime state or diagnostics."""
import argparse
import json
import os
from pathlib import Path
import sys
from zipfile import ZipFile, ZIP_DEFLATED


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    if args.output.name != 'craftopia.apworld':
        parser.error('The archive must be named craftopia.apworld to match its package directory.')
    root = Path(__file__).resolve().parents[1] / 'worlds' / 'craftopia'
    source = Path(os.environ.get('CRAFTOPIA_AP_SOURCE', root.parents[3] / 'ap-0.6.8')).resolve()
    sys.path.insert(0, str(source))
    import unittest  # Scope AP discovery while accessing its official container writer.
    os.environ.setdefault('AP_TEST_WORLDS', 'apquest')
    from worlds.Files import APWorldContainer
    from Utils import tuplize_version
    metadata = json.loads((root / 'archipelago.json').read_text(encoding='utf-8'))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    files = sorted(p for p in root.rglob('*') if p.is_file()
                   and not {'test', '__pycache__'}.intersection(p.relative_to(root).parts)
                   and p.suffix in {'.py', '.json', '.yaml', '.md'})
    class CraftopiaContainer(APWorldContainer):
        def write_contents(self, archive):
            super().write_contents(archive)
            for path in files:
                if path.name != 'archipelago.json':
                    archive.write(path, (Path('craftopia') / path.relative_to(root)).as_posix())
        def get_manifest(self):
            return {**super().get_manifest(), 'authors': metadata['authors']}
    container = CraftopiaContainer(str(args.output))
    container.game = metadata['game']
    container.world_version = tuplize_version(metadata['world_version'])
    container.minimum_ap_version = tuplize_version(metadata['minimum_ap_version'])
    container.write()
    container.read()
    with ZipFile(args.output) as archive:
        assert archive.testzip() is None
        assert 'craftopia/RegionalRuns.py' in archive.namelist()
    print(f'Packaged {len(files)} files: {args.output}')


if __name__ == '__main__': main()
