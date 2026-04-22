#!/usr/bin/env python3
"""
Generate WPF layout-animation GIFs for the WpfFastControls README.

Output
------
  docs/textblock-layout-ripple.gif
  docs/fixedwidthtextblock-repaint-only.gif
"""
import os
from PIL import Image, ImageDraw, ImageFont

# ── Canvas ────────────────────────────────────────────────────────────────────
W, H = 700, 530

# ── Catppuccin Mocha palette ──────────────────────────────────────────────────
BG        = (30,  30,  46)   # #1E1E2E  window background
NODE_IDLE = (49,  50,  68)   # #313244  resting node fill
NODE_TXT  = (205, 214, 244)  # #CDD6F4  node label / title
LINE_CLR  = (69,  71,  90)   # #45475A  tree connector lines
BLUE      = (137, 180, 250)  # #89B4FA  text-change event
RED       = (243, 139, 168)  # #F38BA8  InvalidateMeasure (dirty)
AMBER     = (250, 179, 135)  # #FAB387  layout pass (measuring)
GREEN     = (166, 227, 161)  # #A6E3A1  InvalidateVisual (repaint only)
SUBTEXT   = (166, 173, 200)  # #A6ADC8  annotation text
DARK_BG   = (24,  24,  37)   # code/annotation panel background

def dark(c, f=0.28):
    return tuple(int(x * f) for x in c)

# ── Tree geometry ─────────────────────────────────────────────────────────────
NW, NH, NR = 220, 40, 8     # node: width, height, corner radius
CX = 350                      # horizontal centre of tree
SPACING = 72                  # vertical distance between node centres
Y0 = 68                       # y of root node centre

def ny(i):
    return Y0 + i * SPACING   # centre-y of node i

# ── Fonts ─────────────────────────────────────────────────────────────────────
def load_fonts():
    candidates = [
        "C:/Windows/Fonts/consola.ttf",
        "C:/Windows/Fonts/cour.ttf",
        "C:/Windows/Fonts/arial.ttf",
    ]
    path = next((p for p in candidates if os.path.exists(p)), None)
    if path:
        return dict(
            title = ImageFont.truetype(path, 16),
            node  = ImageFont.truetype(path, 13),
            annot = ImageFont.truetype(path, 13),
            code  = ImageFont.truetype(path, 12),
            badge = ImageFont.truetype(path, 11),
        )
    f = ImageFont.load_default()
    return {k: f for k in ("title", "node", "annot", "code", "badge")}

FONTS = load_fonts()

# ── Node colours per state ────────────────────────────────────────────────────
STATE = {
    "idle":    (NODE_IDLE,  None,   NODE_TXT),
    "trigger": (dark(BLUE), BLUE,   BLUE),
    "dirty":   (dark(RED),  RED,    RED),
    "measure": (dark(AMBER),AMBER,  AMBER),
    "repaint": (dark(GREEN),GREEN,  GREEN),
}

# ── Drawing helpers ───────────────────────────────────────────────────────────
def draw_node(draw, idx, label, state):
    fill, outline, txt = STATE.get(state, STATE["idle"])
    x, y = CX, ny(idx)
    l, t, r, b = x - NW//2, y - NH//2, x + NW//2, y + NH//2
    if outline:
        draw.rounded_rectangle([l-2, t-2, r+2, b+2], radius=NR+2, fill=outline)
    draw.rounded_rectangle([l, t, r, b], radius=NR, fill=fill)
    draw.text((x, y), label, fill=txt, anchor="mm", font=FONTS["node"])


def draw_connectors(draw, n, states):
    for i in range(n - 1):
        x = CX
        y1 = ny(i) + NH//2 + 2
        y2 = ny(i+1) - NH//2 - 2

        sa, sb = states[i], states[i+1]
        if "dirty" in (sa, sb):
            clr = RED
        elif "measure" in (sa, sb):
            clr = AMBER
        elif "repaint" in (sa, sb):
            clr = GREEN
        else:
            clr = LINE_CLR

        draw.line([(x, y1), (x, y2)], fill=clr, width=2)
        # downward arrowhead at child end
        draw.polygon([(x, y2+1), (x-4, y2-7), (x+4, y2-7)], fill=clr)


def make_frame(labels, states, title, annot1, annot2=""):
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)
    n = len(labels)

    draw_connectors(draw, n, states)
    for i, (lbl, st) in enumerate(zip(labels, states)):
        draw_node(draw, i, lbl, st)

    # Title bar
    draw.text((W//2, 22), title, fill=NODE_TXT, anchor="mm", font=FONTS["title"])

    # Annotation panel (two lines, bottom of canvas)
    ay = H - 58
    draw.rounded_rectangle([14, ay, W-14, H-10], radius=7, fill=DARK_BG)
    draw.text((W//2, ay + 16), annot1, fill=SUBTEXT, anchor="mm", font=FONTS["annot"])
    if annot2:
        draw.text((W//2, ay + 35), annot2, fill=SUBTEXT, anchor="mm", font=FONTS["code"])

    return img


def save_gif(frames, durations, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    frames[0].save(
        path, save_all=True, append_images=frames[1:],
        loop=0, duration=durations, optimize=False,
    )
    print(f"  saved  {path}")


# ── TextBlock animation ───────────────────────────────────────────────────────
def make_textblock_gif(out_path):
    labels = ["Window", "DockPanel", "UniformGrid", "Border", "StackPanel", "TextBlock"]
    title  = "TextBlock — Text property update"
    n = len(labels)
    frames, durations = [], []

    def add(states, a1, a2="", ms=400):
        frames.append(make_frame(labels, states, title, a1, a2))
        durations.append(ms)

    idle = ["idle"] * n

    # ── Phase 1: resting ──────────────────────────────────────────────────────
    for _ in range(2):
        add(idle, "Resting state  —  10 Hz price feed incoming", ms=800)

    # ── Phase 2: text-change event hits TextBlock ─────────────────────────────
    trig = idle[:]
    trig[n-1] = "trigger"
    for _ in range(2):
        add(trig, 'textBlock.Text = "1.08547"',
            "→  InvalidateMeasure() fires on TextBlock", ms=500)

    # ── Phase 3: InvalidateMeasure ripples UP the visual tree ─────────────────
    for step in range(n):
        st = idle[:]
        for j in range(n - 1 - step, n):
            st[j] = "dirty"
        add(st, "InvalidateMeasure() walks UP the visual tree …",
            "every ancestor is marked layout-dirty", ms=230)

    # ── Phase 4: layout pass walks DOWN ──────────────────────────────────────
    for step in range(n):
        st = ["dirty"] * n
        for j in range(step):
            st[j] = "idle"
        st[step] = "measure"
        add(st, "WPF layout pass walks back DOWN …",
            "Measure() + Arrange() called on each element", ms=210)

    # final: all idle after layout
    add(idle, "WPF layout pass walks back DOWN …",
        "Measure() + Arrange() called on each element", ms=210)

    # ── Phase 5: done ─────────────────────────────────────────────────────────
    for _ in range(3):
        add(idle,
            "Result: full layout pass for a text-only change",
            "12 tiles × 10 Hz = 120 layout passes / sec  |  ~10% CPU", ms=700)

    save_gif(frames, durations, out_path)


# ── FixedWidthTextBlock animation ─────────────────────────────────────────────
def make_fixedwidth_gif(out_path):
    labels = ["Window", "DockPanel", "UniformGrid", "Border", "StackPanel", "FixedWidthTextBlock"]
    title  = "FixedWidthTextBlock — Text property update"
    n = len(labels)
    frames, durations = [], []

    def add(states, a1, a2="", ms=400):
        frames.append(make_frame(labels, states, title, a1, a2))
        durations.append(ms)

    idle = ["idle"] * n

    # ── Phase 1: resting ──────────────────────────────────────────────────────
    for _ in range(2):
        add(idle, "Resting state  —  10 Hz price feed incoming", ms=800)

    # ── Phase 2: text-change event ────────────────────────────────────────────
    trig = idle[:]
    trig[n-1] = "trigger"
    for _ in range(2):
        add(trig, 'fixedBlock.Text = "1.08547"',
            "→  InvalidateVisual() fires on FixedWidthTextBlock", ms=500)

    # ── Phase 3: repaint pulse — only this element ────────────────────────────
    for _ in range(4):
        st = idle[:]
        st[n-1] = "repaint"
        add(st, "InvalidateVisual() — this element only",
            "zero propagation, zero layout pass", ms=280)
        add(idle, "InvalidateVisual() — this element only",
            "zero propagation, zero layout pass", ms=140)

    # ── Phase 4: done ─────────────────────────────────────────────────────────
    for _ in range(3):
        add(idle,
            "Result: repaint only — no layout pass whatsoever",
            "CPU render overhead: ~10%  →  ~6%  (−4 pp)", ms=700)

    save_gif(frames, durations, out_path)


# ── Entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    base = os.path.join(os.path.dirname(__file__))
    print("Generating GIFs …")
    make_textblock_gif(os.path.join(base, "textblock-layout-ripple.gif"))
    make_fixedwidth_gif(os.path.join(base, "fixedwidthtextblock-repaint-only.gif"))
    print("Done.")
