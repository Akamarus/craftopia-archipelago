# Item notification cards

The SCRC reference is adapted to Craftopia's existing Field Guide palette. The
existing F8 guide and received-item history are unchanged; there is no new F6
history view.

Cards appear at the upper right, with a light-blue sent-item heading or a
light-green received/found heading. Smaller white text below identifies the
source location. Both lines wrap, and card height follows the text. Cards use
the existing dark teal background and muted border.

Three cards can appear at once. Each has six seconds of visible time. Additional
cards wait in arrival order, with a queue count beneath the stack. Timers pause
when the game is unfocused or the Field Guide is open. The overlay does not
capture input or change the simulation clock.

The bridge resolves items using the recipient's game package and locations
using the sender's game package. ItemSend and ReceivedItems events share a
deduplication key, so the same find does not produce two cards. Initial item
replay still fills the existing received history without old popups. Generic
queued/completed-check notices and the separate Soul Orb receipt popup no
longer duplicate these cards. Blocked-action explanations remain separate.

Notification state is cosmetic and in memory; delivery receipts, session
binding, save verification and acknowledgement remain authoritative. No popup
claims that a game inventory mutation has already been saved.

Validation: notification model tests cover capacity, FIFO, six-second lifetime,
pause and deduplication. Bridge tests cover sent/received/own wording, foreign
game location names, initial replay, first live index-zero receipt and paired
network events. Actual placement over Craftopia's HUD still needs visual review
in the next playthrough. This change has not been installed into the live game.
