"""
Generates cute and funny Minecraft 64x64 skin textures and 80x144 front preview renders.
All skins follow standard Java Edition 1.8+ 64x64 format.
"""
import os
import struct
import zlib
import binascii

OUT_DIR = r"D:\minecraft\adventure\pack\skins"
PORTABLE_DIR = r"D:\minecraft\adventure\portable-build\Primordial-Adventures-Portable\pack\skins"
PORTABLE_SKINS_DIR = r"D:\minecraft\adventure\portable-build\Primordial-Adventures-Portable\skins"

def make_png(width, height, rgba_bytes):
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', binascii.crc32(tag + data) & 0xffffffff)
    ihdr = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        raw.extend(rgba_bytes[y*width*4 : (y+1)*width*4])
    idat = zlib.compress(bytes(raw), 9)
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr) + chunk(b'IDAT', idat) + chunk(b'IEND', b'')

class SkinCanvas:
    def __init__(self):
        self.width = 64
        self.height = 64
        self.pixels = [[(0, 0, 0, 0) for _ in range(64)] for _ in range(64)]

    def set_pixel(self, x, y, rgba):
        if 0 <= x < 64 and 0 <= y < 64:
            self.pixels[y][x] = rgba

    def fill_rect(self, x, y, w, h, rgba):
        for py in range(y, y + h):
            for px in range(x, x + w):
                self.set_pixel(px, py, rgba)

    def to_bytes(self):
        buf = bytearray()
        for y in range(64):
            for x in range(64):
                r, g, b, a = self.pixels[y][x]
                buf.extend([r, g, b, a])
        return bytes(buf)

    def render_preview(self):
        # 80x144 canvas
        pw, ph = 80, 144
        preview = [[(0, 0, 0, 0) for _ in range(pw)] for _ in range(ph)]

        def blit_part(src_x, src_y, src_w, src_h, dst_x, dst_y, dst_w, dst_h):
            for dy in range(dst_h):
                sy = src_y + int(dy * src_h / dst_h)
                for dx in range(dst_w):
                    sx = src_x + int(dx * src_w / dst_w)
                    r, g, b, a = self.pixels[sy][sx]
                    if a > 0:
                        preview[dst_y + dy][dst_x + dx] = (r, g, b, a)

        # Base Body Parts
        # Head: src(8,8,8,8) -> dst(20, 0, 40, 40)
        blit_part(8, 8, 8, 8, 20, 0, 40, 40)
        # Head 2nd layer: src(40,8,8,8)
        blit_part(40, 8, 8, 8, 20, 0, 40, 40)

        # Body: src(20,20,8,12) -> dst(20, 40, 40, 60)
        blit_part(20, 20, 8, 12, 20, 40, 40, 60)
        # Body 2nd layer: src(20,36,8,12)
        blit_part(20, 36, 8, 12, 20, 40, 40, 60)

        # Right Arm: src(44,20,4,12) -> dst(0, 40, 20, 60)
        blit_part(44, 20, 4, 12, 0, 40, 20, 60)
        # Right Arm 2nd layer: src(44,36,4,12)
        blit_part(44, 36, 4, 12, 0, 40, 20, 60)

        # Left Arm: src(36,52,4,12) -> dst(60, 40, 20, 60)
        blit_part(36, 52, 4, 12, 60, 40, 20, 60)
        # Left Arm 2nd layer: src(52,52,4,12)
        blit_part(52, 52, 4, 12, 60, 40, 20, 60)

        # Right Leg: src(4,20,4,12) -> dst(20, 100, 20, 44)
        blit_part(4, 20, 4, 12, 20, 100, 20, 44)
        # Left Leg: src(20,52,4,12) -> dst(40, 100, 20, 44)
        blit_part(20, 52, 4, 12, 40, 100, 20, 44)

        buf = bytearray()
        for y in range(ph):
            for x in range(pw):
                r, g, b, a = preview[y][x]
                buf.extend([r, g, b, a])
        return pw, ph, bytes(buf)

# ----------------- SKIN CREATORS -----------------

def create_gentleman_duck():
    # Yellow duck with formal tuxedo and red bow tie
    c = SkinCanvas()
    DY = (255, 214, 10, 255)     # Duck Yellow
    DO = (255, 140, 0, 255)     # Duck Orange (beak)
    EY = (30, 30, 35, 255)      # Eye Black
    EW = (255, 255, 255, 255)   # Eye White
    BK = (24, 24, 30, 255)      # Tuxedo Black
    WH = (245, 245, 250, 255)   # Shirt White
    RD = (220, 38, 38, 255)     # Red Bow Tie

    # Head (8..15, 8..15)
    c.fill_rect(0, 0, 32, 16, DY)
    # Beak (10..13, 12..14)
    c.fill_rect(10, 12, 4, 2, DO)
    # Eyes
    c.set_pixel(9, 10, EW); c.set_pixel(9, 11, EY)
    c.set_pixel(14, 10, EW); c.set_pixel(14, 11, EY)

    # Body (Tuxedo + Shirt + Bow Tie)
    c.fill_rect(16, 16, 24, 16, BK)
    c.fill_rect(20, 20, 8, 12, BK)
    # Shirt V
    c.fill_rect(22, 20, 4, 6, WH)
    c.set_pixel(23, 26, WH); c.set_pixel(24, 26, WH)
    # Bow Tie
    c.set_pixel(22, 21, RD); c.set_pixel(23, 21, RD); c.set_pixel(24, 21, RD); c.set_pixel(25, 21, RD)

    # Right Arm & Left Arm (Tuxedo sleeves + orange hands)
    c.fill_rect(40, 16, 16, 16, BK)
    c.fill_rect(44, 29, 4, 3, DO) # hand
    c.fill_rect(32, 48, 16, 16, BK)
    c.fill_rect(36, 61, 4, 3, DO) # hand

    # Legs (Black trousers + orange feet)
    c.fill_rect(0, 16, 16, 16, BK)
    c.fill_rect(4, 29, 4, 3, DO) # foot
    c.fill_rect(16, 48, 16, 16, BK)
    c.fill_rect(20, 61, 4, 3, DO) # foot
    return c

def create_cozy_frog():
    # Green frog hoodie with cute blushing face
    c = SkinCanvas()
    FG = (74, 222, 128, 255)    # Frog Pastel Green
    FD = (34, 197, 94, 255)     # Darker Green
    SK = (255, 237, 213, 255)   # Skin tone
    PK = (251, 146, 60, 255)    # Blush Peach
    EY = (30, 41, 59, 255)      # Dark eye
    EW = (255, 255, 255, 255)   # Eye highlight
    BL = (254, 240, 138, 255)   # Pastel yellow belly

    # Head & Hood
    c.fill_rect(0, 0, 32, 16, FG)
    # Face opening
    c.fill_rect(10, 10, 4, 4, SK)
    c.set_pixel(9, 11, SK); c.set_pixel(14, 11, SK)
    # Eyes & Blush
    c.set_pixel(10, 11, EY); c.set_pixel(13, 11, EY)
    c.set_pixel(9, 12, PK); c.set_pixel(14, 12, PK)
    # Big Frog Eyes on Hood (top layer or head top)
    c.fill_rect(40, 8, 8, 8, (0, 0, 0, 0))
    # Frog hood eyes on overlay
    c.fill_rect(41, 7, 2, 2, EW); c.set_pixel(42, 8, EY)
    c.fill_rect(45, 7, 2, 2, EW); c.set_pixel(45, 8, EY)

    # Body (Hoodie with yellow belly patch)
    c.fill_rect(16, 16, 24, 16, FG)
    c.fill_rect(22, 22, 4, 8, BL)

    # Arms
    c.fill_rect(40, 16, 16, 16, FG)
    c.fill_rect(44, 29, 4, 3, SK)
    c.fill_rect(32, 48, 16, 16, FG)
    c.fill_rect(36, 61, 4, 3, SK)

    # Legs (Shorts + white socks + frog slippers)
    c.fill_rect(0, 16, 16, 16, FD)
    c.fill_rect(4, 28, 4, 4, FG)
    c.fill_rect(16, 48, 16, 16, FD)
    c.fill_rect(20, 60, 4, 4, FG)
    return c

def create_toasty_bread():
    # Golden toasted bread slice with happy smile
    c = SkinCanvas()
    CR = (180, 83, 9, 255)      # Bread Crust Brown
    BR = (254, 243, 199, 255)   # Bread Dough Cream
    BU = (253, 224, 71, 255)    # Butter Yellow
    PK = (244, 114, 182, 255)   # Pink Cheeks
    EY = (69, 26, 3, 255)       # Dark eye brown

    # Head (Bread slice face)
    c.fill_rect(0, 0, 32, 16, BR)
    # Crust border
    c.fill_rect(8, 8, 8, 1, CR)
    c.fill_rect(8, 8, 1, 8, CR)
    c.fill_rect(15, 8, 1, 8, CR)
    # Butter melting on head
    c.fill_rect(11, 9, 3, 2, BU)
    # Happy eyes and blush
    c.set_pixel(10, 12, EY); c.set_pixel(13, 12, EY)
    c.set_pixel(9, 13, PK); c.set_pixel(14, 13, PK)
    # Smile
    c.set_pixel(11, 14, EY); c.set_pixel(12, 14, EY)

    # Body (Toasted torso with big butter slab)
    c.fill_rect(16, 16, 24, 16, BR)
    c.fill_rect(20, 20, 8, 12, BR)
    c.fill_rect(20, 20, 1, 12, CR)
    c.fill_rect(27, 20, 1, 12, CR)
    c.fill_rect(22, 23, 4, 4, BU) # big butter square

    # Arms
    c.fill_rect(40, 16, 16, 16, BR)
    c.fill_rect(32, 48, 16, 16, BR)

    # Legs
    c.fill_rect(0, 16, 16, 16, CR)
    c.fill_rect(16, 48, 16, 16, CR)
    return c

def create_derpy_banana():
    # Goofy banana suit with silly eyes
    c = SkinCanvas()
    BY = (250, 204, 21, 255)    # Banana Yellow
    BD = (234, 179, 8, 255)     # Deep Yellow
    ST = (101, 163, 13, 255)    # Greenish stem
    BW = (120, 53, 15, 255)     # Brown tip
    EY = (15, 23, 42, 255)      # Eye
    EW = (255, 255, 255, 255)   # Eye White

    # Head
    c.fill_rect(0, 0, 32, 16, BY)
    # Stem on top
    c.fill_rect(11, 8, 2, 2, ST)
    # Goofy / Derpy eyes (one up, one down!)
    c.fill_rect(9, 10, 2, 2, EW); c.set_pixel(9, 10, EY)
    c.fill_rect(13, 11, 2, 2, EW); c.set_pixel(14, 12, EY)
    # Silly tongue smile
    c.set_pixel(11, 14, (239, 68, 68, 255))
    c.set_pixel(12, 14, (239, 68, 68, 255))

    # Body
    c.fill_rect(16, 16, 24, 16, BY)
    c.fill_rect(20, 20, 8, 12, BY)
    c.fill_rect(23, 20, 2, 12, BD) # seam

    # Arms
    c.fill_rect(40, 16, 16, 16, BY)
    c.fill_rect(32, 48, 16, 16, BY)

    # Legs (Banana peel bottom with brown tips)
    c.fill_rect(0, 16, 16, 16, BY)
    c.fill_rect(4, 29, 4, 3, BW)
    c.fill_rect(16, 48, 16, 16, BY)
    c.fill_rect(20, 61, 4, 3, BW)
    return c

def create_gentleman_penguin():
    # Chubby penguin with tuxedo, bowtie & golden monocle
    c = SkinCanvas()
    BK = (15, 23, 42, 255)      # Penguin Black
    WH = (248, 250, 252, 255)   # White Belly
    OR = (249, 115, 22, 255)    # Orange Beak
    GD = (234, 179, 8, 255)     # Gold Monocle
    RD = (220, 38, 38, 255)     # Red Bowtie
    EY = (2, 6, 23, 255)

    # Head (Black hood with white face and orange beak)
    c.fill_rect(0, 0, 32, 16, BK)
    c.fill_rect(9, 9, 6, 5, WH)
    c.fill_rect(11, 12, 2, 2, OR) # Beak
    # Left eye normal, Right eye with Gold Monocle!
    c.set_pixel(10, 10, EY)
    c.fill_rect(13, 9, 3, 3, GD)  # Monocle frame
    c.set_pixel(14, 10, (186, 230, 253, 255)) # Glass shine
    c.set_pixel(14, 10, EY)

    # Body (Tuxedo with White Belly + Red Bowtie)
    c.fill_rect(16, 16, 24, 16, BK)
    c.fill_rect(22, 21, 4, 11, WH)
    # Red Bowtie
    c.set_pixel(22, 20, RD); c.set_pixel(23, 20, RD); c.set_pixel(24, 20, RD); c.set_pixel(25, 20, RD)

    # Arms (Flipper sleeves)
    c.fill_rect(40, 16, 16, 16, BK)
    c.fill_rect(32, 48, 16, 16, BK)

    # Legs (Black with orange webbed penguin feet)
    c.fill_rect(0, 16, 16, 16, BK)
    c.fill_rect(4, 29, 4, 3, OR)
    c.fill_rect(16, 48, 16, 16, BK)
    c.fill_rect(20, 61, 4, 3, OR)
    return c

def create_neko_kitty():
    # Pastel lavender cat hoodie with cute whiskers and paws
    c = SkinCanvas()
    LV = (192, 132, 252, 255)   # Lavender Hoodie
    LD = (168, 85, 247, 255)    # Deep Lavender
    SK = (255, 237, 213, 255)   # Soft Skin
    PK = (244, 114, 182, 255)   # Pink Cheeks & Ears
    EY = (147, 51, 234, 255)    # Purple Eyes
    WH = (255, 255, 255, 255)

    # Head
    c.fill_rect(0, 0, 32, 16, LV)
    c.fill_rect(9, 9, 6, 5, SK)
    # Eyes & Blush
    c.set_pixel(10, 10, EY); c.set_pixel(13, 10, EY)
    c.set_pixel(9, 12, PK); c.set_pixel(14, 12, PK)
    # Whiskers
    c.set_pixel(8, 11, LD); c.set_pixel(15, 11, LD)
    # Cat ears on overlay
    c.set_pixel(40, 6, PK); c.set_pixel(47, 6, PK)

    # Body (Hoodie with cute kitty face on pocket)
    c.fill_rect(16, 16, 24, 16, LV)
    c.fill_rect(22, 24, 4, 4, WH) # Kitty paw print
    c.set_pixel(22, 24, PK); c.set_pixel(25, 24, PK)

    # Arms (Lavender with white paw mittens)
    c.fill_rect(40, 16, 16, 16, LV)
    c.fill_rect(44, 29, 4, 3, WH)
    c.fill_rect(32, 48, 16, 16, LV)
    c.fill_rect(36, 61, 4, 3, WH)

    # Legs (Pleated skirt + socks)
    c.fill_rect(0, 16, 16, 16, LD)
    c.fill_rect(4, 25, 4, 4, SK)
    c.fill_rect(4, 29, 4, 3, WH)
    c.fill_rect(16, 48, 16, 16, LD)
    c.fill_rect(20, 57, 4, 4, SK)
    c.fill_rect(20, 61, 4, 3, WH)
    return c

def create_baby_dino():
    # Green baby dinosaur onesie with cute face and teeth trim
    c = SkinCanvas()
    DG = (34, 197, 94, 255)     # Dino Green
    DL = (134, 239, 172, 255)   # Light Dino Green
    SK = (255, 237, 213, 255)   # Face
    EY = (22, 101, 52, 255)     # Eye
    WH = (255, 255, 255, 255)   # Teeth
    OR = (251, 146, 60, 255)    # Spikes

    # Head
    c.fill_rect(0, 0, 32, 16, DG)
    c.fill_rect(9, 10, 6, 4, SK)
    # Teeth trim above face
    c.set_pixel(10, 9, WH); c.set_pixel(12, 9, WH); c.set_pixel(14, 9, WH)
    # Eyes
    c.set_pixel(10, 11, EY); c.set_pixel(13, 11, EY)

    # Body (Green with pastel belly)
    c.fill_rect(16, 16, 24, 16, DG)
    c.fill_rect(22, 22, 4, 8, DL)

    # Arms
    c.fill_rect(40, 16, 16, 16, DG)
    c.fill_rect(32, 48, 16, 16, DG)

    # Legs (Dino feet)
    c.fill_rect(0, 16, 16, 16, DG)
    c.fill_rect(4, 30, 4, 2, DL)
    c.fill_rect(16, 48, 16, 16, DG)
    c.fill_rect(20, 62, 4, 2, DL)
    return c

def create_strawberry_cow():
    # Pastel pink cow with cream patches and strawberry accents
    c = SkinCanvas()
    PK = (244, 114, 182, 255)   # Pastel Pink
    PL = (253, 242, 248, 255)   # Cream White Patch
    RD = (225, 29, 72, 255)     # Strawberry Red
    SN = (251, 207, 232, 255)   # Snout
    EY = (136, 19, 55, 255)     # Berry Eye
    HF = (80, 7, 36, 255)       # Hoof

    # Head (Pink with cream cow spot and snout)
    c.fill_rect(0, 0, 32, 16, PK)
    c.fill_rect(8, 8, 3, 3, PL)
    c.fill_rect(10, 12, 4, 3, SN) # Snout
    c.set_pixel(11, 13, EY); c.set_pixel(12, 13, EY) # Nostrils
    # Eyes
    c.set_pixel(9, 10, EY); c.set_pixel(14, 10, EY)
    # Strawberry horn accents
    c.set_pixel(8, 7, RD); c.set_pixel(15, 7, RD)

    # Body (Pink with cream cow spots)
    c.fill_rect(16, 16, 24, 16, PK)
    c.fill_rect(21, 22, 3, 4, PL)
    c.fill_rect(24, 26, 3, 4, PL)

    # Arms (Pink with hoof)
    c.fill_rect(40, 16, 16, 16, PK)
    c.fill_rect(44, 29, 4, 3, HF)
    c.fill_rect(32, 48, 16, 16, PK)
    c.fill_rect(36, 61, 4, 3, HF)

    # Legs (Pink with hoof)
    c.fill_rect(0, 16, 16, 16, PK)
    c.fill_rect(4, 29, 4, 3, HF)
    c.fill_rect(16, 48, 16, 16, PK)
    c.fill_rect(20, 61, 4, 3, HF)
    return c

SKINS_TO_CREATE = [
    ("gentleman_duck", "Gentleman Duck", "funny", create_gentleman_duck()),
    ("toasty_bread", "Toasty Bread", "funny", create_toasty_bread()),
    ("derpy_banana", "Derpy Banana", "funny", create_derpy_banana()),
    ("gentleman_penguin", "Gentleman Penguin", "funny", create_gentleman_penguin()),
    ("cozy_frog", "Cozy Froggy", "cute", create_cozy_frog()),
    ("neko_kitty", "Neko Kitty", "cute", create_neko_kitty()),
    ("baby_dino", "Baby Dino", "cute", create_baby_dino()),
    ("strawberry_cow", "Strawberry Cow", "cute", create_strawberry_cow()),
]

def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(PORTABLE_DIR, exist_ok=True)
    os.makedirs(PORTABLE_SKINS_DIR, exist_ok=True)

    for skin_id, name, cat, canvas in SKINS_TO_CREATE:
        # Texture 64x64
        tex_bytes = make_png(64, 64, canvas.to_bytes())
        # Preview 80x144
        pw, ph, prev_raw = canvas.render_preview()
        prev_bytes = make_png(pw, ph, prev_raw)

        for d in [OUT_DIR, PORTABLE_DIR, PORTABLE_SKINS_DIR]:
            with open(os.path.join(d, f"{skin_id}.png"), "wb") as f:
                f.write(tex_bytes)
            with open(os.path.join(d, f"{skin_id}-preview.png"), "wb") as f:
                f.write(prev_bytes)
        print(f"Generated {skin_id} (texture: {len(tex_bytes)} B, preview: {len(prev_bytes)} B)")

if __name__ == "__main__":
    main()
