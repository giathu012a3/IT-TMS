import re

def hex_to_rgb(h):
    h = h.strip().lstrip('#')
    if len(h) == 3:
        h = ''.join(c*2 for c in h)
    return tuple(int(h[i:i+2], 16)/255.0 for i in (0, 2, 4))

def rel_lum(rgb):
    lin = []
    for c in rgb:
        if c <= 0.03928:
            lin.append(c / 12.92)
        else:
            lin.append(((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]

def contrast(c1, c2):
    l1 = rel_lum(hex_to_rgb(c1))
    l2 = rel_lum(hex_to_rgb(c2))
    top, bot = max(l1, l2), min(l1, l2)
    return (top + 0.05) / (bot + 0.05)

def blend(fg_rgb, fg_alpha, bg_rgb):
    return tuple(fg_rgb[i] * fg_alpha + bg_rgb[i] * (1 - fg_alpha) for i in range(3))

print("=== WCAG 2.1 CONTRAST RATIO CHECKS ===")

# Light Mode quick-fill-btn
# bg: --raised = #f9fafb, text: --text = #111827
cr_light = contrast('#111827', '#f9fafb')
print(f"1. Quick-fill Light (Default): text #111827 on bg #f9fafb -> {cr_light:.2f}:1 (WCAG AAA >= 7.0: {cr_light >= 7.0})")

# Dark Mode quick-fill-btn
# bg: --raised = #18181c, text: --text = #f4f4f5
cr_dark = contrast('#f4f4f5', '#18181c')
print(f"2. Quick-fill Dark (Default): text #f4f4f5 on bg #18181c -> {cr_dark:.2f}:1 (WCAG AAA >= 7.0: {cr_dark >= 7.0})")

# Light Mode Hover: bg: --accent-soft = #eff6ff, color: --accent-ink = #1e40af
cr_light_hover = contrast('#1e40af', '#eff6ff')
print(f"3. Quick-fill Light (Hover/Active): text #1e40af on bg #eff6ff -> {cr_light_hover:.2f}:1 (WCAG AAA >= 7.0: {cr_light_hover >= 7.0})")

# Dark Mode Hover: bg: rgba(96, 165, 250, 0.14) over #18181c
bg_dark_rgb = hex_to_rgb('#18181c')
fg_accent_rgb = hex_to_rgb('#60a5fa')
blended_dark = blend(fg_accent_rgb, 0.14, bg_dark_rgb)
l_dark_blended = rel_lum(blended_dark)
l_accent_ink = rel_lum(hex_to_rgb('#93c5fd'))
cr_dark_hover = (max(l_dark_blended, l_accent_ink) + 0.05) / (min(l_dark_blended, l_accent_ink) + 0.05)
print(f"4. Quick-fill Dark (Hover/Active): text #93c5fd on blended bg -> {cr_dark_hover:.2f}:1 (WCAG AAA >= 7.0: {cr_dark_hover >= 7.0})")

# Pill bg-secondary-subtle:
# Light: text #374151 on bg #f3f4f6
cr_pill_light = contrast('#374151', '#f3f4f6')
print(f"5. Pill bg-secondary-subtle Light: text #374151 on bg #f3f4f6 -> {cr_pill_light:.2f}:1 (WCAG AAA >= 7.0: {cr_pill_light >= 7.0})")

# Dark: text #d4d4d8 on bg #222228
cr_pill_dark = contrast('#d4d4d8', '#222228')
print(f"6. Pill bg-secondary-subtle Dark: text #d4d4d8 on bg #222228 -> {cr_pill_dark:.2f}:1 (WCAG AAA >= 7.0: {cr_pill_dark >= 7.0})")

# Note text in 5th button: text-muted
# Light: --muted #4b5563 on #f9fafb
cr_muted_light = contrast('#4b5563', '#f9fafb')
print(f"7. Button note Light: text #4b5563 on #f9fafb -> {cr_muted_light:.2f}:1 (WCAG AAA >= 7.0: {cr_muted_light >= 7.0})")

# Dark: --muted #a1a1aa on #18181c
cr_muted_dark = contrast('#a1a1aa', '#18181c')
print(f"8. Button note Dark: text #a1a1aa on #18181c -> {cr_muted_dark:.2f}:1 (WCAG AA >= 4.5: {cr_muted_dark >= 4.5})")

# Rule for .table > tbody > tr.table-light > td
# Light: text var(--text) #111827 on bg var(--subtle) #f3f4f6
cr_table_light_l = contrast('#111827', '#f3f4f6')
print(f"9. .table-light override Light: text #111827 on bg #f3f4f6 -> {cr_table_light_l:.2f}:1 (WCAG AAA >= 7.0: {cr_table_light_l >= 7.0})")

# Dark: text var(--text) #f4f4f5 on bg var(--subtle) #1f1f24
cr_table_light_d = contrast('#f4f4f5', '#1f1f24')
print(f"10. .table-light override Dark: text #f4f4f5 on bg #1f1f24 -> {cr_table_light_d:.2f}:1 (WCAG AAA >= 7.0: {cr_table_light_d >= 7.0})")
