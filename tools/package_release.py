"""Build a player ZIP and separate Workshop content from explicit clean inputs."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
from zipfile import ZipFile, ZIP_DEFLATED
from package_managed_bridge import ROOT, build, verify, validate_output
import asyncio

VERSION = '1.16.0-alpha.1'

def validate_payload(root):
    for p in root.rglob('*'):
        if not p.is_file(): continue
        name = p.name.lower()
        if (name.startswith(('craftopia_mp_', 'craftopia_bridge_ledger_', 'craftopia_bridge_authority_',
                             'craftopia-archipelago-', 'craftopia-currency-', 'craftopia-orb-character-',
                             'craftopia-milestone-', 'craftopia-observations-', 'craftopia_observations',
                             'unityengine', 'ad__overcraft', 'bepinex.', '0harmony', 'mirror.'))
            or p.suffix.lower() in ('.cfg', '.log', '.ocs', '.multidata', '.archipelago', '.pdb')
            or '__pycache__' in p.parts):
            raise ValueError('Private state or non-redistributable input in release: ' + str(p))

def manifest(root):
    validate_payload(root)
    files = {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
             for p in sorted(root.rglob('*')) if p.is_file() and p.name != 'manifest.json'}
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    (root / 'manifest.json').write_text(json.dumps(dict(version=VERSION, source_commit=revision, files=files), indent=2), encoding='utf-8')

def package(output):
    output = validate_output(ROOT, output)
    if output.exists(): raise ValueError('Use a new output directory; release builds never overwrite previous artifacts.')
    bridge = build(output / 'runtime')
    asyncio.run(verify(bridge))
    payload = output / 'player'
    mod = payload / 'mod'
    mod.mkdir(parents=True)
    shutil.copy2(ROOT / 'mod/CraftopiaArchipelago/bin/Release/netstandard2.1/CraftopiaArchipelago.dll', mod)
    shutil.copytree(bridge, mod / 'bridge')
    subprocess.run([sys.executable, str(ROOT / 'tools/package_apworld.py'), str(payload / 'craftopia.apworld')], check=True)
    shutil.copy2(ROOT / 'worlds/craftopia/test/regional_player.yaml', payload / 'Craftopia.yaml')
    for name in ('INSTALLATION.md', 'RELEASE_v1.16.0.md', 'WORKSHOP.md'):
        shutil.copy2(ROOT / 'docs' / name, payload / name)
    shutil.copy2(ROOT / 'tools/Install-Mod.ps1', payload)
    (payload / 'START_HERE.txt').write_text(
        'Craftopia Archipelago ' + VERSION + '\n\n'
        'Windows x64, Seamless World, BepInEx 5 Mono. Experimental alpha.\n'
        'Install BepInEx first. Close Craftopia. Run Install-Mod.ps1 with PowerShell,\n'
        'or copy mod/ into BepInEx/plugins/CraftopiaArchipelago/.\n'
        'Existing users: update the existing AP folder; do not create a second copy.\n'
        'Read INSTALLATION.md. F8 > Connection sets your server/slot/password.\n'
        'The included craftopia.apworld and Craftopia.yaml are for your AP host.\n'
        'No Python or compiler installation is required.\n'
        'AP records: %LOCALAPPDATA%/CraftopiaArchipelago. Back up with your saves.\n', encoding='utf-8')
    workshop = output / 'workshop'
    shutil.copytree(mod, workshop)
    # modInfo.txt is written only after Steam assigns the private item's ID.
    manifest(payload)
    manifest(workshop)
    archive = output / ('Craftopia-Archipelago-' + VERSION + '-Windows.zip')
    with ZipFile(archive, 'w', ZIP_DEFLATED) as z:
        for path in sorted(payload.rglob('*')):
            if path.is_file(): z.write(path, path.relative_to(payload).as_posix())
    with ZipFile(archive) as z:
        if z.testzip() is not None: raise RuntimeError('ZIP validation failed')
    shutil.copy2(payload / 'craftopia.apworld', output / 'craftopia.apworld')
    (output / 'SHA256SUMS.txt').write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.name + '\n'
                                          for p in (archive, output / 'craftopia.apworld')), encoding='ascii')
    print(archive)
    return output

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    package(parser.parse_args().output)
