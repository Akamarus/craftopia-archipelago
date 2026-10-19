#!/usr/bin/env python3
"""Build an isolated Windows CPython bridge from already installed components.

Run with the approved AP venv Python (for its websockets dependency):
  python tools/package_managed_bridge.py --verify
No downloads, pip, venv launcher, user packages, or runtime ledgers are shipped.
"""
import argparse
import asyncio
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
CLIENT_FILES = ('connection_host.py', 'craftopia_bridge.py', 'delivery_state.py',
                'currency_delivery.py', 'soul_orb_delivery.py', 'milestone_delivery.py', 'item_notices.py', 'orb_map.py')
EXCLUDE = {'site-packages', '__pycache__', 'test', 'tests', 'idlelib', 'tkinter', 'turtledemo', 'ensurepip', 'venv'}
CUSTOM = {'sitecustomize.py', 'usercustomize.py'}


def stdlib_files(root):
    for current, directories, files in os.walk(root):
        current = Path(current)
        directories[:] = sorted(name for name in directories if name not in EXCLUDE
                                and (current != root or name in sys.stdlib_module_names))
        for name in sorted(files):
            path = current / name
            if path.suffix != '.py' or name in CUSTOM:
                continue
            if current != root or path.stem in sys.stdlib_module_names:
                yield path


def validate_output(root, output):
    root, output = root.resolve(), output.resolve()
    artifacts = root / 'artifacts'
    if output == artifacts or not output.is_relative_to(artifacts):
        raise ValueError('Output must be a named directory inside this repository artifacts directory.')
    return output


def copy_file(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def build(output, overwrite=False):
    if sys.platform != 'win32': raise RuntimeError('Build with Windows CPython.')
    output = validate_output(ROOT, output)
    if output.exists():
        if not overwrite: raise ValueError('Output already exists; use --overwrite to replace this build artifact.')
        # Resolved, repository-contained target checked above; never touch an installation.
        shutil.rmtree(output)
    base = Path(sys.base_prefix)
    dist = importlib.metadata.distribution('websockets')
    dependency = Path(dist.locate_file('websockets'))
    bridge = output / 'bridge'
    runtime = bridge / 'python'
    version_tag = f'python{sys.version_info.major}{sys.version_info.minor}'
    for name in ('python.exe', 'python3.dll', version_tag + '.dll', 'vcruntime140.dll', 'vcruntime140_1.dll'):
        copy_file(base / name, runtime / name)
    for source in stdlib_files(base / 'Lib'):
        copy_file(source, runtime / 'Lib' / source.relative_to(base / 'Lib'))
    for source in sorted((base / 'DLLs').iterdir()):
        if ((source.suffix == '.pyd' and source.stem in sys.stdlib_module_names
             and not source.stem.startswith('_test') and source.stem not in ('_ctypes_test', '_tkinter'))
                or source.name in ('libcrypto-3.dll', 'libssl-3.dll', 'libffi-8.dll', 'sqlite3.dll', 'zlib1.dll')):
            copy_file(source, runtime / 'DLLs' / source.name)
    for source in sorted(dependency.rglob('*')):
        if source.is_file() and '__pycache__' not in source.parts and source.suffix in ('.py', '.pyd'):
            copy_file(source, runtime / 'vendor' / 'websockets' / source.relative_to(dependency))
    # _pth disables registry/environment paths and site initialization, including
    # sitecustomize and usercustomize. The client path is explicit for local imports.
    (runtime / (version_tag + '._pth')).write_text('Lib\nDLLs\nvendor\n../client\n', encoding='ascii')
    for name in CLIENT_FILES:
        copy_file(ROOT / 'client' / name, bridge / 'client' / name)
    catalogs = ROOT / 'worlds' / 'craftopia'
    copy_file(catalogs / 'Milestones.py', bridge / 'worlds' / 'craftopia' / 'Milestones.py')
    for source in sorted(catalogs.glob('*.json')):
        copy_file(source, bridge / 'worlds' / 'craftopia' / source.name)
    copy_file(base / 'LICENSE.txt', bridge / 'licenses' / 'CPython-LICENSE.txt')
    license_files = [Path(dist.locate_file(p)) for p in dist.files or []
                     if p.name.upper() in ('LICENSE', 'LICENSE.TXT')]
    if not license_files: raise RuntimeError('websockets license is missing.')
    copy_file(license_files[0], bridge / 'licenses' / 'websockets-LICENSE.txt')
    for name in ('METADATA', 'WHEEL'):
        for record in dist.files or []:
            if record.name == name:
                copy_file(Path(dist.locate_file(record)), bridge / 'licenses' / ('websockets-' + name))
    provenance = {
        'format': 1, 'platform': 'Windows x64' if sys.maxsize > 2**32 else 'Windows x86',
        'python': {'version': sys.version.split()[0], 'distribution': 'CPython official Windows installation',
                   'source': 'https://www.python.org/', 'license': 'licenses/CPython-LICENSE.txt'},
        'websockets': {'version': dist.version, 'source': 'https://github.com/python-websockets/websockets',
                       'license': 'licenses/websockets-LICENSE.txt'},
        'isolation': 'python._pth; no site initialization, registry paths, PYTHONPATH, or user packages',
        'certificates': 'CPython ssl uses Windows system certificate stores; no private certificate bundle copied.',
        'files': {p.relative_to(bridge).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in sorted(bridge.rglob('*')) if p.is_file()},
    }
    (bridge / 'PROVENANCE.json').write_text(json.dumps(provenance, indent=2) + '\n', encoding='utf-8')
    return bridge


async def verify(bridge):
    import websockets
    executable = bridge / 'python' / 'python.exe'
    script = bridge / 'client' / 'connection_host.py'
    env = dict(os.environ, PYTHONPATH=str(ROOT), PYTHONHOME=str(ROOT))
    inspection = subprocess.run([str(executable), '-c',
        'import sys,ssl,websockets,json; print(json.dumps(dict(isolated=sys.flags.isolated,site="site" in sys.modules,paths=sys.path,openssl=ssl.OPENSSL_VERSION)))'],
        capture_output=True, text=True, env=env, timeout=15, check=True)
    info = json.loads(inspection.stdout)
    if not info['isolated'] or info['site'] or any(not Path(p).resolve().is_relative_to(bridge.resolve()) for p in info['paths']):
        raise RuntimeError('Packaged runtime path isolation failed.')
    invalid = subprocess.run([str(executable), str(script)], input='{}\n', capture_output=True, text=True, env=env, timeout=15, check=True)
    if json.loads(invalid.stdout)['state'] != 'error': raise RuntimeError('Invalid-config smoke test failed.')
    received = []
    async def peer(ws, *unused):
        await ws.send(json.dumps([{'cmd':'RoomInfo', 'seed_name':'portable-build-verification'}]))
        try:
            received.extend(json.loads(await ws.recv()))
            await ws.send(json.dumps([{'cmd':'ConnectionRefused', 'errors':['package-verification-secret']}]))
            await ws.wait_closed()
        except websockets.ConnectionClosed:
            pass
    before = set((bridge / 'client').iterdir())
    async with websockets.serve(peer, '127.0.0.1', 0) as server:
        import socket
        with socket.socket() as probe:
            probe.bind(('127.0.0.1', 0))
            local_port = probe.getsockname()[1]
        config = {'server':f"127.0.0.1:{server.sockets[0].getsockname()[1]}",
                  'slot':'PackageVerification', 'password':'package-verification-secret', 'port':local_port}
        process = subprocess.Popen([str(executable), str(script)], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, text=True, env=env)
        try:
            process.stdin.write(json.dumps(config) + '\n')
            process.stdin.flush()
            prepared = await asyncio.wait_for(asyncio.to_thread(process.stdout.readline), 15)
            if json.loads(prepared)['state'] != 'prepared' or received:
                raise RuntimeError('Packaged discovery handshake failed.')
            process.stdin.write('{"cmd":"start"}\n')
            process.stdin.flush()
            refusal = await asyncio.wait_for(asyncio.to_thread(process.stdout.readline), 15)
            if json.loads(refusal)['state'] != 'error': raise RuntimeError('Packaged refusal status failed.')
            remaining, errors = await asyncio.wait_for(asyncio.to_thread(process.communicate), 15)
            if not received or received[0].get('password') != config['password']:
                raise RuntimeError('Packaged authentication failed.')
            if config['password'] in prepared + refusal + remaining + errors:
                raise RuntimeError('Packaged credential privacy failed.')
        finally:
            if process.poll() is None:
                process.kill()
                await asyncio.to_thread(process.wait)
            # Delete only fresh verification state in this disposable build.
            for path in set((bridge / 'client').iterdir()) - before:
                if path.is_file() and path.name.startswith(('craftopia_bridge_authority_managed-', 'craftopia_bridge_ledger_managed-')):
                    path.unlink()
    # CPython may cache imports; compiled cache paths aren't redistribution inputs.
    for cache in bridge.rglob('__pycache__'):
        if cache.resolve().is_relative_to(bridge.resolve()): shutil.rmtree(cache)
    return info


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts' / 'managed-bridge')
    parser.add_argument('--overwrite', action='store_true')
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    bridge = build(args.output, args.overwrite)
    if args.verify:
        info = asyncio.run(verify(bridge))
        print('Verified isolated runtime, SSL, invalid settings, seed handshake, authentication refusal, credential privacy, and process exit.')
        print(info['openssl'])
    size = sum(p.stat().st_size for p in bridge.rglob('*') if p.is_file())
    print(f'Built {bridge} ({size / 1024 / 1024:.1f} MiB)')


if __name__ == '__main__': main()
