"""Normalized AP receipts; callers must classify against a verified DataPackage."""
from dataclasses import dataclass
from coop_progress import decode, integer, require, text


UNLOCKS = ('unlock_equipment', 'unlock_production', 'unlock_skill_tree')
TEAM_ITEMS = ('physical', 'currency')


@dataclass(frozen=True)
class ItemReceipt:
    index: int
    item_id: int
    kind: str
    target: str
    amount: int


def validate_receipt(receipt):
    require(type(receipt) is ItemReceipt, 'invalid receipt type')
    require(integer(receipt.index) and integer(receipt.item_id, 1) and integer(receipt.amount, 1),
            'invalid receipt number')
    require(type(receipt.kind) is str and receipt.kind in UNLOCKS + TEAM_ITEMS + ('points',),
            'unsupported receipt kind')
    require(text(receipt.target), 'invalid receipt target')
    require(receipt.kind != 'points' or receipt.target == 'skill_points', 'invalid points target')
    require(receipt.kind not in ('unlock_production', 'unlock_skill_tree') or receipt.amount == 1,
            'invalid unlock amount')


def decode_receipt(row):
    receipt = decode(ItemReceipt, row)
    validate_receipt(receipt)
    return receipt
