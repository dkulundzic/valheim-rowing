from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import KeepTogether, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

import os

# Rebuild with: python3 docs/make_voice_recording_guide.py (needs reportlab)
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "voice-recording-guide.pdf")

ACCENT = colors.HexColor("#1F4E79")
LIGHT = colors.HexColor("#E8EEF5")
GRID = colors.HexColor("#9AA9BA")

styles = getSampleStyleSheet()
title = ParagraphStyle("title", parent=styles["Title"], fontName="Helvetica-Bold", fontSize=20, textColor=ACCENT, spaceAfter=2 * mm, alignment=TA_LEFT)
subtitle = ParagraphStyle("subtitle", parent=styles["Normal"], fontName="Helvetica", fontSize=10, textColor=colors.HexColor("#555555"), spaceAfter=5 * mm)
h2 = ParagraphStyle("h2", parent=styles["Heading2"], fontName="Helvetica-Bold", fontSize=13, textColor=ACCENT, spaceBefore=4 * mm, spaceAfter=2 * mm)
body = ParagraphStyle("body", parent=styles["Normal"], fontName="Helvetica", fontSize=10.5, leading=14)
cell = ParagraphStyle("cell", parent=body, fontSize=9.5, leading=12)
cellBold = ParagraphStyle("cellBold", parent=cell, fontName="Helvetica-Bold")
head = ParagraphStyle("head", parent=cell, fontName="Helvetica-Bold", textColor=colors.white)
note = ParagraphStyle("note", parent=body, fontSize=9.5, leading=12.5, textColor=colors.HexColor("#444444"))


def tips_table(rows):
    data = [[Paragraph(f"<b>{n}</b>", cell), Paragraph(text, cell)] for n, text in rows]
    t = Table(data, colWidths=[9 * mm, 160 * mm])
    t.setStyle(TableStyle([
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LINEBELOW", (0, 0), (-1, -1), 0.4, GRID),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("TEXTCOLOR", (0, 0), (0, -1), ACCENT),
    ]))
    return t


def tips_section(heading, rows):
    return KeepTogether([Paragraph(heading, h2), tips_table(rows)])


def sounds_table(rows):
    header = ["#", "Memo name", "When the game plays it", "What it sounds like", "Length", "Takes", "Done"]
    data = [[Paragraph(h, head) for h in header]]
    for r in rows:
        data.append([Paragraph(r[0], cellBold), Paragraph(f"<b>{r[1]}</b>", cell), Paragraph(r[2], cell),
                     Paragraph(r[3], cell), Paragraph(r[4], cell), Paragraph(r[5], cell), tick_box()])
    t = Table(data, colWidths=[7 * mm, 25 * mm, 37 * mm, 50 * mm, 18 * mm, 14 * mm, 17 * mm], repeatRows=1)
    style = [
        ("BACKGROUND", (0, 0), (-1, 0), ACCENT),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.4, GRID),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("ALIGN", (-1, 1), (-1, -1), "CENTER"),
    ]
    for i in range(1, len(data)):
        if i % 2 == 0:
            style.append(("BACKGROUND", (0, i), (-2, i), LIGHT))
    t.setStyle(TableStyle(style))
    return t


def tick_box():
    box = Table([[""]], colWidths=[5 * mm], rowHeights=[5 * mm])
    box.setStyle(TableStyle([("BOX", (0, 0), (-1, -1), 0.8, ACCENT)]))
    return box


story = [
    Paragraph("RowingMod voice recording guide", title),
    Paragraph("Effort sounds for the Valheim rowing mod, recorded on an iPhone with Voice Memos.", subtitle),

    tips_section("Phone settings", [
        ("1", "<b>Settings &gt; Apps &gt; Voice Memos &gt; Audio Quality &gt; Lossless.</b> This avoids compression."),
        ("2", "<b>Leave \"Enhance Recording\" off.</b> It can smear short sounds; noise gets cleaned up afterwards."),
    ]),
    tips_section("Room", [
        ("3", "<b>Pick a quiet room:</b> away from fans, the fridge, the washing machine and traffic."),
        ("4", "<b>Prefer soft furnishings:</b> a bedroom or a living room with a sofa and curtains. Avoid tiled bathrooms and kitchens, which echo."),
    ]),
    tips_section("Mic position", [
        ("5", "<b>Hold the phone 20-30 cm from your mouth.</b> Too close distorts and picks up spit; too far picks up the room."),
        ("6", "<b>Aim slightly to the side</b> of your mouth, not straight at it, so breath pops don't hit the mic."),
        ("7", "<b>Keep the phone still:</b> rest it on something rather than holding it, since handling noise shows up on recordings."),
    ]),
    tips_section("Performance", [
        ("8", "<b>Really make the effort:</b> pull on a towel, push against a doorframe, or lift something heavy while you grunt. Acted grunts sound fake."),
        ("9", "<b>Vary every take:</b> change the vowel (\"hnh\", \"hup\", \"uhh\", \"hah\"), the length and the strength. Variety matters more than perfection."),
        ("10", "<b>Leave about 1 second of silence between takes</b>, so they can be split automatically."),
        ("11", "<b>One memo per sound type</b>, named as in the list on the next page."),
        ("12", "<b>For breathing, do real exercise first</b> (jumping jacks, stairs), then record yourself catching your breath."),
        ("13", "<b>Record more than you need.</b> Bad takes get thrown out, and spares add variety."),
    ]),
    tips_section("Sending", [
        ("14", "<b>AirDrop the memos to the Mac</b> (they land in Downloads) and say the file names."),
    ]),

    PageBreak(),
    Paragraph("What to record", title),
    Paragraph("One memo per row. Record a set per voice: male, and female if someone can help. Tick each memo off when it's done.", subtitle),

    Paragraph("Needed", h2),
    sounds_table([
        ("1", "grunt-light", "Normal strong strokes", "Short, controlled effort: pulling something moderately heavy", "0.2-0.6 s", "15-20"),
        ("2", "grunt-hard", "Strokes under heavy load: headwind, crew pushing hard", "Full-effort pull, as if hauling an oar against the wind", "0.3-0.8 s", "15-20"),
        ("3", "brake", "The blade digging in when you start braking", "A strained, held \"nnngh\", bracing against something pulling away", "0.5-1.0 s", "8-10"),
    ]),
    Spacer(1, 3 * mm),

    Paragraph("Nice to have", h2),
    sounds_table([
        ("4", "breathing", "Between strokes when stamina is low", "Real panting: do jumping jacks first, then just breathe", "30-60 s in one go", "1"),
        ("5", "tired", "Running out of stamina (\"Too tired to row\")", "An exhausted groan or sigh", "0.5-1.5 s", "5-8"),
        ("6", "clash", "Your stroke clashes with the crew's rhythm", "A short annoyed grunt or \"tch\", oar jarring in your hands", "0.2-0.5 s", "5-8"),
    ]),
    Spacer(1, 3 * mm),

    Paragraph("Ideas for later (not planned yet)", h2),
    sounds_table([
        ("7", "calls", "The beat, when the crew is in sync", "A rhythm call: \"Row!\", \"Heave!\", \"Pull!\" in a Norse-ish voice", "0.3-1.0 s", "5-10"),
        ("8", "crew-sync", "Several rowers hitting the same beat", "A shared, punchy \"hup!\"", "0.2-0.5 s", "8-10"),
    ]),
    Spacer(1, 5 * mm),
    Paragraph("Every take is cleaned (trimmed, denoised, levelled, EQ'd), and varied in pitch; you listen to them and pick the keepers before anything goes into the mod.", note),
]


def footer(canvas_obj, doc):
    canvas_obj.saveState()
    canvas_obj.setFont("Helvetica", 8)
    canvas_obj.setFillColor(colors.HexColor("#777777"))
    canvas_obj.drawString(20 * mm, 12 * mm, "RowingMod - voice recording guide")
    canvas_obj.drawRightString(A4[0] - 20 * mm, 12 * mm, f"Page {doc.page}")
    canvas_obj.restoreState()


doc = SimpleDocTemplate(OUT, pagesize=A4, leftMargin=20 * mm, rightMargin=20 * mm, topMargin=18 * mm, bottomMargin=20 * mm,
                        title="RowingMod voice recording guide", author="RowingMod")
doc.build(story, onFirstPage=footer, onLaterPages=footer)
print(OUT)
