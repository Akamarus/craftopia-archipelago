#!/usr/bin/env python3
"""Managed bridge. Newline JSON stdin: config, start, disconnect (or EOF).

Stdout is exclusively structured status. Credentials never enter arguments,
identity, ledger, or diagnostics. A daemon reader makes parent EOF cancellable
throughout discovery and runtime, including on Windows redirected pipes.
"""
import argparse
import asyncio
import contextlib
import hashlib
import json
import os
import sys
import threading
import time
from urllib.parse import urlsplit


def normalize_server(value):
    if not isinstance(value, str) or not value.strip():
        raise ValueError('Server address is required.')
    value = value.strip()
    parsed = urlsplit(value if '://' in value else 'ws://' + value)
    if (parsed.scheme != 'ws' or not parsed.hostname or parsed.username is not None
            or parsed.password is not None or parsed.path not in ('', '/')
            or parsed.query or parsed.fragment or any(c.isspace() for c in value)):
        raise ValueError('Use a server hostname and port without credentials or a path.')
    port = parsed.port or 38281
    if parsed.port == 0 or not 1 <= port <= 65535:
        raise ValueError('Invalid server port.')
    hostname = parsed.hostname.lower()
    if ':' in hostname: hostname = '[' + hostname + ']'
    return f'{hostname}:{port}'


def session_identity(server, seed, slot):
    canonical = json.dumps([normalize_server(server), seed, slot], ensure_ascii=False, separators=(',', ':'))
    return 'managed-' + hashlib.sha256(canonical.encode('utf-8')).hexdigest()


def parse_config(value):
    if not isinstance(value, dict): raise ValueError('Invalid connection settings.')
    server = normalize_server(value.get('server'))
    slot = value.get('slot')
    if not isinstance(slot, str) or not slot.strip() or len(slot) > 256:
        raise ValueError('A slot name is required.')
    password = value.get('password')
    if password is not None and not isinstance(password, str):
        raise ValueError('Invalid password value.')
    port = value.get('port', 11341)
    if type(port) is not int or not 1 <= port <= 65535:
        raise ValueError('Invalid local bridge port.')
    choice = value.get('starter_choice') or None
    from delivery_state import COMBAT
    if choice is not None and choice not in COMBAT:
        raise ValueError('Invalid starter choice.')
    return dict(server=server, slot=slot, password=password or None, port=port, starter_choice=choice)


async def discover_seed(server):
    import websockets
    async def receive():
        async with websockets.connect('ws://' + server, max_size=2 ** 20, open_timeout=10, close_timeout=1) as ws:
            async for raw in ws:
                packets = json.loads(raw)
                if not isinstance(packets, list): continue
                for packet in packets:
                    if isinstance(packet, dict) and packet.get('cmd') == 'RoomInfo':
                        seed = packet.get('seed_name')
                        if not isinstance(seed, str) or not seed.strip():
                            raise ValueError('Missing seed identity.')
                        return seed
            raise ValueError('Server closed before sending seed identity.')
    return await asyncio.wait_for(receive(), 15)


async def run_bridge(config, session, seed, emit):
    import craftopia_bridge as module
    args = argparse.Namespace(**config, session=session, game_id_dump=False, expected_seed=seed,
                              state_callback=lambda state, message: emit(dict(state=state, message=message)))
    try:
        authority = module.claim_authority(session, False)
        bridge = module.Bridge(args)
        bridge.authority_path = authority
        bridge.authority_last_beat = time.monotonic()
        await bridge.run()
    except SystemExit:
        emit(dict(state='error', message='Another bridge owns this session, or its authority file is unavailable.'))
    finally:
        if module._authority_lock_file is not None:
            module._authority_lock_file.close()
            module._authority_lock_file = None


async def run_host(raw_config, commands, emit, discover=discover_seed, runner=run_bridge):
    work = None
    command = None
    failure = asyncio.Event()
    def status(event):
        if failure.is_set(): return
        emit(event)
        if event.get('state') == 'error': failure.set()
    try:
        config = parse_config(raw_config)
        work = asyncio.create_task(discover(config['server']))
        command = asyncio.create_task(commands.get())
        done, _ = await asyncio.wait([work, command], return_when=asyncio.FIRST_COMPLETED)
        if command in done:
            emit(dict(state='disconnected', message='Connection cancelled.'))
            return
        seed = work.result()
        session = session_identity(config['server'], seed, config['slot'])
        emit(dict(state='prepared', session=session, seed=seed, slot=config['slot'], server=config['server']))
        start = await asyncio.wait_for(command, 60)
        if not isinstance(start, dict) or start.get('cmd') != 'start':
            emit(dict(state='disconnected', message='Connection cancelled.'))
            return
        work = asyncio.create_task(runner(config, session, seed, status))
        command = asyncio.create_task(commands.get())
        failed = asyncio.create_task(failure.wait())
        try:
            await asyncio.wait([work, command, failed], return_when=asyncio.FIRST_COMPLETED)
            if work.done(): work.result()
        finally:
            failed.cancel()
            await asyncio.gather(failed, return_exceptions=True)
        if not failure.is_set(): emit(dict(state='disconnected', message='Bridge stopped.'))
    except Exception:
        emit(dict(state='error', message='Unable to connect. Check the server, slot, password, and local bridge port.'))
    finally:
        for task in (work, command):
            if task is not None: task.cancel()
        await asyncio.gather(*(task for task in (work, command) if task is not None), return_exceptions=True)


async def main_async(output):
    commands = asyncio.Queue()
    loop = asyncio.get_running_loop()
    def read_input():
        while True:
            try:
                line = sys.stdin.readline(65537)
                value = json.loads(line) if line and len(line) <= 65536 else None
            except Exception:
                value = None
            try: loop.call_soon_threadsafe(commands.put_nowait, value)
            except RuntimeError: return
            if value is None: return
    threading.Thread(target=read_input, daemon=True).start()
    def emit(event):
        output.write(json.dumps(event, ensure_ascii=True) + '\n')
        output.flush()
    config = await commands.get()
    if config is None: return
    await run_host(config, commands, emit)


def main():
    output = sys.stdout
    # Legacy AP chat/error logs can include arbitrary remote text. Managed mode
    # deliberately discards them; only fixed host diagnostics leave the process.
    with open(os.devnull, 'w') as quiet, contextlib.redirect_stdout(quiet), contextlib.redirect_stderr(quiet):
        try: asyncio.run(main_async(output))
        except (Exception, SystemExit):
            output.write(json.dumps(dict(state='error', message='Bridge runtime unavailable or unable to start.')) + '\n')
            output.flush()


if __name__ == '__main__': main()
