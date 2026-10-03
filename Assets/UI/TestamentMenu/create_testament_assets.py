import os
import wave
import math
import struct
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

TEXTURES_DIR = r"Assets\UI\TestamentMenu\Textures"
AUDIO_DIR = r"Assets\UI\TestamentMenu\Audio"
os.makedirs(TEXTURES_DIR, exist_ok=True)
os.makedirs(AUDIO_DIR, exist_ok=True)

# ==========================================
# 1. TEXTURE GENERATION
# ==========================================

def create_desk_background():
    w, h = 1920, 1080
    img = Image.new("RGBA", (w, h), (24, 16, 12, 255))
    draw = ImageDraw.Draw(img)
    
    # Generate wood grain texture
    np.random.seed(42)
    base_wood = np.zeros((h, w, 3), dtype=np.float32)
    
    # Planks and horizontal grain
    y_coords, x_coords = np.mgrid[0:h, 0:w]
    
    # Wood grain wavy pattern
    grain = np.sin(y_coords * 0.15 + np.sin(x_coords * 0.015) * 5.0) * 0.5 + 0.5
    fine_grain = np.sin(y_coords * 0.8 + np.sin(x_coords * 0.05) * 2.0) * 0.5 + 0.5
    noise = np.random.normal(0, 0.04, (h, w))
    
    wood_val = grain * 0.35 + fine_grain * 0.15 + noise * 0.1 + 0.4
    
    # Mahogany tones: Dark reddish-brown
    r = np.clip((wood_val * 42.0 + 18.0), 0, 255)
    g = np.clip((wood_val * 28.0 + 12.0), 0, 255)
    b = np.clip((wood_val * 20.0 + 8.0), 0, 255)
    
    # Center warm light glow (overhead lamp over the desk)
    cx, cy = w / 2.0, h / 2.0 - 50.0
    dist_sq = ((x_coords - cx) / (w * 0.55)) ** 2 + ((y_coords - cy) / (h * 0.55)) ** 2
    vignette = np.clip(1.0 - dist_sq * 0.65, 0.25, 1.3)
    
    r = np.clip(r * vignette * 1.1, 0, 255).astype(np.uint8)
    g = np.clip(g * vignette * 1.05, 0, 255).astype(np.uint8)
    b = np.clip(b * vignette * 0.95, 0, 255).astype(np.uint8)
    
    img_wood = Image.fromarray(np.dstack([r, g, b]), mode="RGB").convert("RGBA")
    
    # Add subtle plank seam lines
    draw_wood = ImageDraw.Draw(img_wood)
    plank_heights = [0, 260, 520, 780, 1080]
    for py in plank_heights:
        draw_wood.line([(0, py), (w, py)], fill=(12, 8, 6, 180), width=2)
        draw_wood.line([(0, py + 1), (w, py + 1)], fill=(45, 30, 20, 100), width=1)
        
    out_path = os.path.join(TEXTURES_DIR, "Testament_Desk_Bg.png")
    img_wood.save(out_path, "PNG")
    print(f"Created: {out_path}")

def create_parchment_paper():
    w, h = 1100, 1480
    np.random.seed(101)
    
    y_coords, x_coords = np.mgrid[0:h, 0:w]
    
    # Base parchment color: Cream / aged sepia
    # Center: (245, 236, 215), Edges: (195, 172, 135) to burned (135, 105, 70)
    cx, cy = w / 2.0, h / 2.0
    dist_norm = np.sqrt(((x_coords - cx) / (w * 0.5)) ** 2 + ((y_coords - cy) / (h * 0.5)) ** 2)
    edge_darkening = np.clip((dist_norm - 0.7) * 2.2, 0.0, 1.0)
    
    # Subtle fiber / paper texture noise
    paper_noise = np.random.normal(0, 4.5, (h, w))
    stain_noise = np.sin(x_coords * 0.01) * np.cos(y_coords * 0.012) * 8.0
    
    base_r = 245.0 - edge_darkening * 90.0 + paper_noise + stain_noise
    base_g = 236.0 - edge_darkening * 105.0 + paper_noise + stain_noise * 0.9
    base_b = 215.0 - edge_darkening * 125.0 + paper_noise + stain_noise * 0.7
    
    # Burned corners
    corner_dist = (((x_coords - cx) / (w * 0.48)) ** 4 + ((y_coords - cy) / (h * 0.48)) ** 4)
    corner_burn = np.clip((corner_dist - 0.75) * 3.5, 0.0, 1.0)
    base_r = base_r * (1.0 - corner_burn * 0.45)
    base_g = base_g * (1.0 - corner_burn * 0.55)
    base_b = base_b * (1.0 - corner_burn * 0.65)
    
    r = np.clip(base_r, 40, 255).astype(np.uint8)
    g = np.clip(base_g, 30, 255).astype(np.uint8)
    b = np.clip(base_b, 20, 255).astype(np.uint8)
    a = np.full((h, w), 255, dtype=np.uint8)
    
    # Slightly rough border transparency (deckle edge)
    border_mask = np.ones((h, w), dtype=np.float32)
    border_margin = 12
    for i in range(border_margin):
        factor = (i + 1) / border_margin
        border_mask[i, :] = np.minimum(border_mask[i, :], factor)
        border_mask[h - 1 - i, :] = np.minimum(border_mask[h - 1 - i, :], factor)
        border_mask[:, i] = np.minimum(border_mask[:, i], factor)
        border_mask[:, w - 1 - i] = np.minimum(border_mask[:, w - 1 - i], factor)
    
    a = np.clip(border_mask * 255, 0, 255).astype(np.uint8)
    
    paper_img = Image.fromarray(np.dstack([r, g, b, a]), mode="RGBA")
    draw = ImageDraw.Draw(paper_img)
    
    # Draw double ornamental border
    border_color_outer = (85, 55, 30, 200)
    border_color_inner = (120, 85, 50, 170)
    
    m1 = 36
    draw.rectangle([m1, m1, w - m1, h - m1], outline=border_color_outer, width=3)
    
    m2 = 44
    draw.rectangle([m2, m2, w - m2, h - m2], outline=border_color_inner, width=1)
    
    # Ornate corner brackets
    c_len = 35
    for (ox, oy, dx, dy) in [(m1, m1, 1, 1), (w - m1, m1, -1, 1), (m1, h - m1, 1, -1), (w - m1, h - m1, -1, -1)]:
        draw.line([(ox, oy), (ox + dx * c_len, oy)], fill=border_color_outer, width=4)
        draw.line([(ox, oy), (ox, oy + dy * c_len)], fill=border_color_outer, width=4)
        # Decorative small circle
        draw.ellipse([ox + dx * 10 - 4, oy + dy * 10 - 4, ox + dx * 10 + 4, oy + dy * 10 + 4], fill=border_color_outer)

    out_path = os.path.join(TEXTURES_DIR, "Testament_Paper.png")
    paper_img.save(out_path, "PNG")
    print(f"Created: {out_path}")
    
    # Also create torn top and bottom halves
    create_torn_halves(paper_img, w, h)

def create_torn_halves(paper_img, w, h):
    # Rip line around y = 840 (between text and signature area)
    rip_y_base = int(h * 0.58)
    np.random.seed(777)
    
    # Generate jagged tear line
    xs = np.arange(w)
    # Low frequency tear + high frequency jagged fibers
    tear_offsets = (np.sin(xs * 0.015) * 22.0 + 
                    np.sin(xs * 0.04) * 12.0 + 
                    np.random.normal(0, 3.5, w))
    tear_ys = (rip_y_base + tear_offsets).astype(int)
    
    # Top Half
    img_top = paper_img.copy()
    arr_top = np.array(img_top)
    for x in range(w):
        y_split = tear_ys[x]
        arr_top[y_split + 4:, x, 3] = 0 # Transparent below tear
        # Whitish/fibrous torn edge
        if y_split < h:
            for f in range(4):
                if y_split + f < h:
                    arr_top[y_split + f, x, :3] = [230, 220, 205] # White paper fibers
                    arr_top[y_split + f, x, 3] = int(255 * (1.0 - f * 0.25))
    
    torn_top = Image.fromarray(arr_top, mode="RGBA")
    out_top = os.path.join(TEXTURES_DIR, "Testament_Torn_Top.png")
    torn_top.save(out_top, "PNG")
    print(f"Created: {out_top}")
    
    # Bottom Half
    img_bottom = paper_img.copy()
    arr_bottom = np.array(img_bottom)
    for x in range(w):
        y_split = tear_ys[x]
        arr_bottom[:y_split - 2, x, 3] = 0 # Transparent above tear
        if y_split >= 2:
            for f in range(4):
                if y_split - 2 + f < h:
                    arr_bottom[y_split - 2 + f, x, :3] = [230, 220, 205]
                    arr_bottom[y_split - 2 + f, x, 3] = int(255 * ((f + 1) * 0.25))
                    
    torn_bottom = Image.fromarray(arr_bottom, mode="RGBA")
    out_bottom = os.path.join(TEXTURES_DIR, "Testament_Torn_Bottom.png")
    torn_bottom.save(out_bottom, "PNG")
    print(f"Created: {out_bottom}")

def create_red_seal():
    size = 512
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = size // 2, size // 2
    r_outer = 220
    r_inner = 180
    r_center = 120
    
    stamp_color = (175, 28, 28, 235) # Deep crimson red ink
    
    # Outer serrated ring (notary stamp teeth)
    num_teeth = 72
    for i in range(num_teeth):
        angle = i * (2 * math.pi / num_teeth)
        x1 = cx + (r_outer - 6) * math.cos(angle)
        y1 = cy + (r_outer - 6) * math.sin(angle)
        x2 = cx + (r_outer + 8) * math.cos(angle)
        y2 = cy + (r_outer + 8) * math.sin(angle)
        draw.line([(x1, y1), (x2, y2)], fill=stamp_color, width=4)
        
    # Main outer ring
    draw.ellipse([cx - r_outer, cy - r_outer, cx + r_outer, cy + r_outer], outline=stamp_color, width=7)
    draw.ellipse([cx - r_inner, cy - r_inner, cx + r_inner, cy + r_inner], outline=stamp_color, width=3)
    draw.ellipse([cx - r_center, cy - r_center, cx + r_center, cy + r_center], outline=stamp_color, width=4)
    
    # Center Star
    def draw_star(center_x, center_y, r_out, r_in, points=5):
        pts = []
        for i in range(points * 2):
            r = r_out if i % 2 == 0 else r_in
            angle = i * math.pi / points - math.pi / 2
            pts.append((center_x + r * math.cos(angle), center_y + r * math.sin(angle)))
        draw.polygon(pts, fill=stamp_color)
        
    draw_star(cx, cy, 65, 26, 5)
    
    # Stars around inner ring
    for i in range(6):
        ang = i * (math.pi / 3) + 0.2
        sx = cx + (r_inner + (r_outer - r_inner) / 2) * math.cos(ang)
        sy = cy + (r_inner + (r_outer - r_inner) / 2) * math.sin(ang)
        draw_star(sx, sy, 12, 5, 5)
        
    # Add subtle ink wear / stamp texture
    arr = np.array(img)
    noise = np.random.normal(1.0, 0.22, (size, size))
    noise = np.clip(noise, 0.4, 1.2)
    arr[:, :, 3] = (arr[:, :, 3] * noise).astype(np.uint8)
    
    final_stamp = Image.fromarray(arr, mode="RGBA")
    out_path = os.path.join(TEXTURES_DIR, "Testament_WaxSeal.png")
    final_stamp.save(out_path, "PNG")
    print(f"Created: {out_path}")

def create_signatures():
    # 1. An's Signature: Crimson red fountain pen cursive
    w, h = 600, 220
    img_an = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_an = ImageDraw.Draw(img_an)
    pen_color = (190, 20, 25, 245)
    
    # Render elegant flowing signature strokes
    # Stroke 1: N
    pts_N = [
        (60, 160), (75, 70), (90, 45), (115, 45), (135, 120), (145, 165),
        (150, 95), (160, 60), (180, 50), (200, 75), (210, 140)
    ]
    draw_an.line(pts_N, fill=pen_color, width=5, joint="curve")
    
    # Stroke 2: guyen
    pts_guyen = [
        (210, 140), (225, 155), (245, 130), (255, 175), (240, 205), (215, 200), (230, 165),
        (265, 140), (285, 160), (300, 135), (315, 160), (330, 135), (345, 155), (365, 130),
        (380, 155), (395, 140)
    ]
    draw_an.line(pts_guyen, fill=pen_color, width=4, joint="curve")
    
    # Stroke 3: An
    pts_An = [
        (420, 155), (445, 50), (465, 40), (480, 80), (495, 155),
        (435, 115), (485, 110),
        (510, 155), (525, 130), (540, 155), (555, 130), (570, 150)
    ]
    draw_an.line(pts_An, fill=pen_color, width=4, joint="curve")
    
    # Flowing underline flourish
    pts_flourish = [
        (50, 175), (120, 185), (260, 195), (420, 190), (560, 175), (580, 165)
    ]
    draw_an.line(pts_flourish, fill=pen_color, width=4, joint="curve")
    
    # Smooth slight ink bleed
    img_an = img_an.filter(ImageFilter.SMOOTH)
    out_an = os.path.join(TEXTURES_DIR, "Testament_Signature_An.png")
    img_an.save(out_an, "PNG")
    print(f"Created: {out_an}")
    
    # 2. Father's Signature: Faded aged black-sepia ink
    img_father = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_f = ImageDraw.Draw(img_father)
    f_color = (48, 36, 28, 220)
    
    pts_T = [
        (70, 60), (220, 50), (140, 55), (135, 165), (120, 175), (105, 160)
    ]
    draw_f.line(pts_T, fill=f_color, width=5, joint="curve")
    
    pts_Viet = [
        (180, 135), (200, 165), (220, 100), (235, 165), (250, 135), (265, 165), (285, 135), (310, 165)
    ]
    draw_f.line(pts_Viet, fill=f_color, width=4, joint="curve")
    
    pts_Nghiem = [
        (340, 130), (355, 60), (370, 165), (385, 130), (405, 165), (425, 130), (445, 165),
        (470, 110), (490, 165), (520, 135), (540, 165), (560, 120), (575, 155)
    ]
    draw_f.line(pts_Nghiem, fill=f_color, width=4, joint="curve")
    
    pts_f_flourish = [
        (80, 185), (250, 195), (450, 185), (570, 170)
    ]
    draw_f.line(pts_f_flourish, fill=f_color, width=4, joint="curve")
    
    img_father = img_father.filter(ImageFilter.SMOOTH)
    out_father = os.path.join(TEXTURES_DIR, "Testament_Signature_Father.png")
    img_father.save(out_father, "PNG")
    print(f"Created: {out_father}")

def create_ui_icons():
    # 1. Fountain Pen Icon
    size = 128
    img_pen = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_pen = ImageDraw.Draw(img_pen)
    
    gold = (212, 165, 65, 255)
    dark_gold = (140, 100, 30, 255)
    nib_tip = (235, 200, 120, 255)
    body_dark = (35, 30, 28, 255)
    
    # Diagonal pen body from top-right to bottom-left
    pen_body = [(95, 20), (115, 40), (45, 110), (25, 90)]
    draw_pen.polygon(pen_body, fill=body_dark)
    
    # Gold collar
    collar = [(45, 110), (25, 90), (35, 80), (55, 100)]
    draw_pen.polygon(collar, fill=gold)
    
    # Gold Nib
    nib = [(30, 95), (40, 105), (14, 120)]
    draw_pen.polygon(nib, fill=nib_tip)
    
    # Ink slit
    draw_pen.line([(35, 100), (14, 120)], fill=(20, 15, 12, 255), width=2)
    # Breathing hole
    draw_pen.ellipse([27, 98, 33, 104], fill=(20, 15, 12, 255))
    
    out_pen = os.path.join(TEXTURES_DIR, "Icon_FountainPen.png")
    img_pen.save(out_pen, "PNG")
    print(f"Created: {out_pen}")
    
    # 2. Torn Paper Icon
    img_tear = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_tear = ImageDraw.Draw(img_tear)
    paper_col = (230, 215, 190, 255)
    edge_col = (130, 95, 60, 255)
    
    # Left torn piece
    left_piece = [(20, 20), (55, 20), (65, 45), (45, 70), (60, 95), (48, 115), (20, 115)]
    draw_tear.polygon(left_piece, fill=paper_col, outline=edge_col)
    
    # Right torn piece (shifted down and right)
    right_piece = [(75, 28), (108, 28), (108, 123), (70, 123), (80, 103), (62, 78), (82, 53)]
    draw_tear.polygon(right_piece, fill=paper_col, outline=edge_col)
    
    out_tear = os.path.join(TEXTURES_DIR, "Icon_TearPaper.png")
    img_tear.save(out_tear, "PNG")
    print(f"Created: {out_tear}")
    
    # 3. Legal Scroll / Terms Icon
    img_terms = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_terms = ImageDraw.Draw(img_terms)
    
    scroll_col = (235, 222, 198, 255)
    draw_terms.rounded_rectangle([25, 18, 103, 110], radius=10, fill=scroll_col, outline=(120, 85, 50, 255), width=3)
    
    # Lines representing legal text
    for y in [35, 50, 65, 80, 95]:
        width_line = 60 if y != 95 else 35
        draw_terms.line([(38, y), (38 + width_line, y)], fill=(100, 70, 45, 220), width=3)
        
    out_terms = os.path.join(TEXTURES_DIR, "Icon_Terms.png")
    img_terms.save(out_terms, "PNG")
    print(f"Created: {out_terms}")

    # 4. Button Background Plaque (9-sliceable frame)
    w_btn, h_btn = 380, 80
    for state, color_bg, color_border in [
        ("Normal", (42, 28, 20, 230), (180, 140, 80, 255)),
        ("Hover", (65, 42, 30, 245), (240, 195, 110, 255)),
        ("Pressed", (28, 18, 12, 250), (140, 100, 50, 255)),
    ]:
        img_btn = Image.new("RGBA", (w_btn, h_btn), (0, 0, 0, 0))
        draw_b = ImageDraw.Draw(img_btn)
        draw_b.rounded_rectangle([2, 2, w_btn - 3, h_btn - 3], radius=8, fill=color_bg, outline=color_border, width=2)
        # Inner decorative line
        draw_b.rounded_rectangle([6, 6, w_btn - 7, h_btn - 7], radius=6, outline=(color_border[0], color_border[1], color_border[2], 120), width=1)
        out_btn = os.path.join(TEXTURES_DIR, f"Btn_Frame_{state}.png")
        img_btn.save(out_btn, "PNG")
        print(f"Created: {out_btn}")


# ==========================================
# 2. AUDIO GENERATION (16-bit PCM WAV)
# ==========================================

def write_wav(file_path, sample_rate, samples):
    samples = np.clip(samples, -1.0, 1.0)
    int_samples = (samples * 32767.0).astype(np.int16)
    with wave.open(file_path, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        wf.writeframes(int_samples.tobytes())
    print(f"Created WAV: {file_path}")

def generate_sfx():
    sr = 44100
    
    # 1. SFX_Pen_Sign: Pen nib scratching across paper
    dur = 2.4
    n_samples = int(dur * sr)
    t = np.linspace(0, dur, n_samples, endpoint=False)
    
    np.random.seed(99)
    # Scratching bursts at different times (representing cursive strokes: N - gu - yen - An)
    stroke_starts = [0.1, 0.45, 0.85, 1.25, 1.6, 1.95]
    stroke_durs =   [0.28, 0.32, 0.35, 0.28, 0.30, 0.38]
    stroke_speeds = [1.2, 0.9, 1.1, 1.3, 1.0, 1.4]
    
    pen_audio = np.zeros(n_samples)
    white_noise = np.random.normal(0, 1.0, n_samples)
    
    for start, d, spd in zip(stroke_starts, stroke_durs, stroke_speeds):
        i_start = int(start * sr)
        i_end = min(n_samples, int((start + d) * sr))
        sub_len = i_end - i_start
        sub_t = np.linspace(0, d, sub_len)
        
        # Envelope: sharp attack, rough sustain, rapid decay
        env = np.sin(sub_t / d * np.pi) ** 1.4
        
        # Metallic friction resonance (2.5 kHz to 4.2 kHz)
        f0 = 2800.0 * spd
        resonance = np.sin(2.0 * np.pi * f0 * sub_t + np.sin(sub_t * 80.0) * 4.0) * 0.35
        friction = white_noise[i_start:i_end] * (0.65 + resonance)
        
        pen_audio[i_start:i_end] += friction * env * 0.75
        
    write_wav(os.path.join(AUDIO_DIR, "SFX_Pen_Sign.wav"), sr, pen_audio)
    
    # 2. SFX_Paper_Tear: Violent tearing and ripping of thick paper
    dur_tear = 1.1
    n_tear = int(dur_tear * sr)
    t_tear = np.linspace(0, dur_tear, n_tear, endpoint=False)
    
    np.random.seed(202)
    tear_audio = np.zeros(n_tear)
    raw_noise = np.random.normal(0, 1.0, n_tear)
    
    # Tear envelope: sudden initial rip at t=0.05, peak friction at 0.15 - 0.4, fluttering flutter at 0.5 - 0.9
    tear_env = np.zeros(n_tear)
    for i, ti in enumerate(t_tear):
        if ti < 0.05:
            tear_env[i] = 0.0
        elif ti < 0.15:
            tear_env[i] = (ti - 0.05) / 0.10 # fast attack
        elif ti < 0.55:
            tear_env[i] = 1.0 - (ti - 0.15) * 0.5
        elif ti < 1.0:
            tear_env[i] = 0.8 * np.exp(-(ti - 0.55) * 6.0)
            
    # Sharp impulse fiber snaps
    snaps = np.zeros(n_tear)
    snap_indices = np.random.randint(int(0.08 * sr), int(0.55 * sr), 35)
    for idx in snap_indices:
        snap_len = min(n_tear - idx, 120)
        snaps[idx:idx+snap_len] += (np.random.normal(0, 1.0, snap_len) * 
                                    np.exp(-np.linspace(0, 5, snap_len))) * 2.2
        
    # Low frequency rumble (paper fluttering in hand)
    flutter = np.sin(2.0 * np.pi * 65.0 * t_tear + np.sin(t_tear * 25.0) * 3.0) * 0.25
    
    tear_audio = (raw_noise * 0.7 + snaps + flutter) * tear_env * 0.85
    write_wav(os.path.join(AUDIO_DIR, "SFX_Paper_Tear.wav"), sr, tear_audio)
    
    # 3. SFX_Paper_Hover: Gentle paper rustle on mouse hover
    dur_hover = 0.32
    n_hover = int(dur_hover * sr)
    t_hover = np.linspace(0, dur_hover, n_hover, endpoint=False)
    
    h_env = np.sin(t_hover / dur_hover * np.pi) ** 1.8
    h_noise = np.random.normal(0, 1.0, n_hover)
    # High-pass filter simulation for delicate whispery paper rustle
    h_audio = (h_noise[1:] - h_noise[:-1]) * 0.5
    h_audio = np.append(h_audio, 0) * h_env * 0.4
    write_wav(os.path.join(AUDIO_DIR, "SFX_Paper_Hover.wav"), sr, h_audio)
    
    # 4. SFX_Stamp_Thud: Red notary wax seal impact
    dur_stamp = 0.65
    n_stamp = int(dur_stamp * sr)
    t_stamp = np.linspace(0, dur_stamp, n_stamp, endpoint=False)
    
    # Deep bass thud (85 Hz) + wood desk knock + paper slap
    bass = np.sin(2.0 * np.pi * 85.0 * t_stamp * np.exp(-t_stamp * 14.0)) * np.exp(-t_stamp * 8.0) * 0.8
    knock = np.sin(2.0 * np.pi * 320.0 * t_stamp) * np.exp(-t_stamp * 25.0) * 0.45
    slap = np.random.normal(0, 1.0, n_stamp) * np.exp(-t_stamp * 40.0) * 0.5
    
    stamp_audio = bass + knock + slap
    write_wav(os.path.join(AUDIO_DIR, "SFX_Stamp_Thud.wav"), sr, stamp_audio)
    
    # 5. BGM_Testament_Ambience: Dark, atmospheric ambient loop with tension drone and distant wind
    dur_bgm = 12.0
    n_bgm = int(dur_bgm * sr)
    t_bgm = np.linspace(0, dur_bgm, n_bgm, endpoint=False)
    
    # Low C minor drone (65.4 Hz / 130.8 Hz)
    drone1 = np.sin(2.0 * np.pi * 65.4 * t_bgm) * 0.28
    drone2 = np.sin(2.0 * np.pi * 77.78 * t_bgm) * 0.18 # Minor third tension
    drone3 = np.sin(2.0 * np.pi * 130.8 * t_bgm + np.sin(t_bgm * 0.4) * 2.0) * 0.15
    
    # Eerie wind whoosh
    wind_mod = (np.sin(2.0 * np.pi * 0.12 * t_bgm) * 0.5 + 0.5) ** 2.0
    wind_noise = np.random.normal(0, 0.08, n_bgm) * wind_mod
    
    # Subtle ticking of antique clock (every 1.0 second)
    clock_ticks = np.zeros(n_bgm)
    for sec in range(int(dur_bgm)):
        tick_idx = int(sec * sr)
        if tick_idx + 300 < n_bgm:
            clock_ticks[tick_idx:tick_idx+300] += np.sin(np.linspace(0, 8, 300) * 1200.0) * np.exp(-np.linspace(0, 6, 300)) * 0.12
            
    # Seamless loop window at ends
    fade_len = int(0.8 * sr)
    fade_in = np.linspace(0, 1, fade_len)
    fade_out = np.linspace(1, 0, fade_len)
    window = np.ones(n_bgm)
    window[:fade_len] = fade_in
    window[-fade_len:] = fade_out
    
    bgm_audio = (drone1 + drone2 + drone3 + wind_noise + clock_ticks) * window * 0.65
    write_wav(os.path.join(AUDIO_DIR, "BGM_Testament_Ambience.wav"), sr, bgm_audio)

if __name__ == "__main__":
    print("Generating Testament Menu Visual Textures...")
    create_desk_background()
    create_parchment_paper()
    create_red_seal()
    create_signatures()
    create_ui_icons()
    
    print("\nGenerating Testament Menu Audio Assets...")
    generate_sfx()
    print("\nAll assets successfully generated!")
