"""Builds the New LvlUp Effect artwork into Release/ and Release/Artwork/.

    python Release/make_artwork.py

Requires Python 3 with Pillow and numpy. Inputs live in Release/Artwork/src:
scene.jpg (an in-game 2560x1440 capture of the notification) and Gelasio.ttf
(SIL Open Font License, see Gelasio-OFL.txt). Outputs:
  icon.png                    256x256 Thunderstore icon
  Artwork/icon-master.png     1024x1024 icon master
  Artwork/cover.png           1600x900 Nexus/GitHub cover
  Artwork/header.png          1300x372 Nexus header (header@2x.png 2600x744)
"""
import math, random, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "Artwork"); SRC = os.path.join(ART, "src")
FONT = os.path.join(SRC, "Gelasio.ttf")
BG = (12, 19, 24); GOLD = (232, 190, 110); GOLD2 = (255, 214, 140); CREAM = (250, 240, 215)
PLAQUE = (1280, 330)  # notification centre in scene.jpg

def serif(size):
    f = ImageFont.truetype(FONT, size); f.set_variation_by_name("Bold"); return f

def sans(size):
    for path in ("C:/Windows/Fonts/seguisb.ttf", "C:/Windows/Fonts/segoeui.ttf",
                 "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"):
        if os.path.exists(path): return ImageFont.truetype(path, size)
    return ImageFont.load_default()

def medallion(N, new="6", old="5", embers=True, bg=True):
    C = N / 2; s = N / 1024
    img = Image.new("RGBA", (N, N), BG + (255,) if bg else (0, 0, 0, 0))
    if bg:
        g = Image.new("L", (N, N), 0); gd = ImageDraw.Draw(g)
        for r in range(int(470 * s), 0, -3):
            gd.ellipse((C - r, C - r, C + r, C + r), fill=int(120 * (1 - r / (470 * s)) ** 1.6))
        img = Image.composite(Image.new("RGBA", (N, N), (150, 95, 40, 255)), img, g)
    d = ImageDraw.Draw(img); R = 330 * s
    def diamond(x, y, k, fill=None, outline=None, w=0):
        d.polygon([(x - k * 1.6, y), (x, y - k), (x + k * 1.6, y), (x, y + k)], fill=fill, outline=outline, width=w)
    for sg in (-1, 1):
        d.line([(C + sg * (R + 30 * s), C), (C + sg * (R + 160 * s), C)], fill=GOLD, width=max(2, int(9 * s)))
        diamond(C + sg * (R + 120 * s), C, 26 * s, outline=GOLD, w=max(2, int(9 * s)))
    d.ellipse((C - R, C - R, C + R, C + R), outline=GOLD, width=int(26 * s))
    r2 = R - 50 * s
    d.ellipse((C - r2, C - r2, C + r2, C + r2), outline=GOLD + (150,), width=max(2, int(8 * s)))
    for a in range(4):
        diamond(C + math.cos(a * math.pi / 2) * R, C + math.sin(a * math.pi / 2) * R, 22 * s, fill=GOLD2)
    num = Image.new("RGBA", (N, N), (0, 0, 0, 0)); f = serif(int(400 * s))
    ImageDraw.Draw(num).text((C, C - 300 * s), old, font=f, fill=CREAM + (85,), anchor="mm")
    glow = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    ImageDraw.Draw(glow).text((C, C + 25 * s), new, font=f, fill=(255, 180, 70, 255), anchor="mm")
    num = Image.alpha_composite(num, glow.filter(ImageFilter.GaussianBlur(28 * s)))
    ImageDraw.Draw(num).text((C, C + 25 * s), new, font=f, fill=GOLD2, anchor="mm")
    clip = Image.new("L", (N, N), 0)
    ImageDraw.Draw(clip).ellipse((C - r2 + 6 * s, C - r2 + 6 * s, C + r2 - 6 * s, C + r2 - 6 * s), fill=255)
    num.putalpha(ImageChops.multiply(num.getchannel("A"), clip))
    img = Image.alpha_composite(img, num); d = ImageDraw.Draw(img)
    if embers:
        random.seed(4)
        for _ in range(14):
            a = random.uniform(0, 2 * math.pi); rr = random.uniform(R + 40 * s, R + 150 * s); k = random.uniform(4, 9) * s
            x = C + math.cos(a) * rr; y = C + math.sin(a) * rr * 0.9 - 40 * s
            d.ellipse((x - k, y - k, x + k, y + k), fill=(255, 205, 120, random.randint(90, 200)))
    return img

def banner(scene, W, H, plaque_x, title, sub, chip, src_w, chips=True):
    sw = src_w; sh = sw * H / W
    x0 = max(0, PLAQUE[0] - plaque_x * sw); y0 = max(0, PLAQUE[1] - 0.42 * sh)
    bg = scene.crop((int(x0), int(y0), int(x0 + sw), int(y0 + sh))).resize((W, H), Image.LANCZOS)
    bg = ImageEnhance.Brightness(bg).enhance(0.92).convert("RGBA")
    xs = np.linspace(0, 1, W); left = np.clip((0.66 - xs) / 0.30, 0, 1) * 255
    ys = np.linspace(0, 1, H); bottom = np.clip((ys - 0.72) / 0.28, 0, 1) ** 1.3 * 235
    mask = np.maximum(np.tile(left, (H, 1)), np.tile(bottom[:, None], (1, W))).astype("uint8")
    img = Image.composite(Image.new("RGBA", (W, H), (10, 15, 20, 255)), bg, Image.fromarray(mask, "L"))
    x = int(W * 0.06)
    med = medallion(512, bg=False, embers=False).resize((int(title * 1.25),) * 2, Image.LANCZOS)
    y = int(H * 0.5) - int(title * 1.95) - (int(sub * 1.4) if chips else 0)
    img.alpha_composite(med, (x - int(title * 0.12), y)); img = img.convert("RGB")
    d = ImageDraw.Draw(img, "RGBA"); y += int(title * 1.3)
    d.text((x, y), "NEW LVLUP", font=serif(title), fill=CREAM); y += int(title * 1.05)
    d.text((x, y), "EFFECT", font=serif(title), fill=GOLD); y += int(title * 1.25)
    d.text((x, y), "Animated skill level-up notifications for Valheim", font=sans(sub), fill=(225, 215, 195))
    y += int(sub * 2.0)
    if chips:
        cx = x
        for t in ("CLIENT-SIDE", "VANILLA PROGRESSION", "CONFIGURABLE"):
            f = sans(chip); bb = d.textbbox((cx, y), t, font=f); p = int(chip * 0.55)
            d.rounded_rectangle((bb[0] - p, bb[1] - p, bb[2] + p, bb[3] + p), radius=p, outline=GOLD + (200,), width=2, fill=(232, 190, 110, 28))
            d.text((cx, y), t, font=f, fill=GOLD); cx = bb[2] + p * 3
        y += int(chip * 2.6)
        d.text((x, y), "by Crevitka  ·  v1.2.1", font=sans(chip), fill=(170, 165, 150))
    return img

if __name__ == "__main__":
    icon = medallion(1024).convert("RGB")
    icon.save(os.path.join(ART, "icon-master.png"))
    icon.resize((256, 256), Image.LANCZOS).save(os.path.join(HERE, "icon.png"))
    scene = Image.open(os.path.join(SRC, "scene.jpg")).convert("RGB")
    banner(scene, 1600, 900, 0.70, 118, 30, 22, 1120).save(os.path.join(ART, "cover.png"))
    h = banner(scene, 2600, 744, 0.72, 150, 40, 0, 1700, chips=False)
    h.save(os.path.join(ART, "header@2x.png")); h.resize((1300, 372), Image.LANCZOS).save(os.path.join(ART, "header.png"))
    print("Artwork written to", HERE)
