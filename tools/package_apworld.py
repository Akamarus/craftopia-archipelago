"""Package repository world files, never local runtime state or diagnostics."""
import argparse
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / 'worlds' / 'craftopia'
    args.output.parent.mkdir(parents=True, exist_ok=True)
    files = sorted(p for p in root.rglob('*') if p.is_file()
                   and not {'test', '__pycache__'}.intersection(p.relative_to(root).parts)
                   and p.suffix in {'.py', '.json', '.yaml', '.md'})
    with ZipFile(args.output, 'w', ZIP_DEFLATED) as archive:
        for path in files:
            archive.write(path, (Path('craftopia') / path.relative_to(root)).as_posix())
    with ZipFile(args.output) as archive:
        assert archive.testzip() is None
        assert 'craftopia/RegionalRuns.py' in archive.namelist()
    print(f'Packaged {len(files)} files: {args.output}')


if __name__ == '__main__': main()
