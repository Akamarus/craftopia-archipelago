"""Read-only loader for tests against an external AP 0.6.8 source checkout."""
import importlib.util
import os
from pathlib import Path
import sys
import unittest  # enables AP's optional scoped test loader

def load_world():
    source = os.environ.get('CRAFTOPIA_AP_SOURCE')
    if not source: raise RuntimeError('Set CRAFTOPIA_AP_SOURCE to an Archipelago 0.6.8 source directory')
    sys.path.insert(0, str(Path(source).resolve()))
    os.environ['AP_TEST_WORLDS']='apquest'
    import worlds
    import Utils
    if tuple(Utils.version_tuple) != (0,6,8):
        raise RuntimeError('Tests require AP 0.6.8; got '+str(Utils.version_tuple))
    folder=Path(__file__).resolve().parents[1]
    existing = sys.modules.get('worlds.craftopia')
    if existing is not None:
        if Path(existing.__file__).resolve() != folder / '__init__.py':
            raise RuntimeError('Another Craftopia source is already loaded')
        return existing.CraftopiaWorld
    spec=importlib.util.spec_from_file_location('worlds.craftopia',folder/'__init__.py',submodule_search_locations=[str(folder)])
    module=importlib.util.module_from_spec(spec)
    sys.modules['worlds.craftopia']=module
    spec.loader.exec_module(module)
    return module.CraftopiaWorld
