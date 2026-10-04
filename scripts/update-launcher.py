import re

with open('launcher-src/Program.cs', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('public const string VERSION = "1.0.1";', 'public const string VERSION = "1.1.0";')
text = text.replace('btnNavMods = CreateNavButton("📦  Modpack (25)", btnY);', 'btnNavMods = CreateNavButton("📦  Modpack (30)", btnY);')
text = text.replace('lblModsHeader.Text = "Included Adventure Mods (25 Total)";', 'lblModsHeader.Text = "Included Adventure Mods (30 Total)";')
text = text.replace('"📦 DOWNLOAD & INSTALL GAME (1.6 GB)"', '"📦 DOWNLOAD & INSTALL GAME (1.8 GB)"')

# Add new mods
old_mods = '''            AddModItem(lv, "ModernFix", "Performance", "Supercharged launch times & memory fixes");'''
new_mods = '''            AddModItem(lv, "ModernFix", "Performance", "Supercharged launch times & memory fixes");
            AddModItem(lv, "Simple Voice Chat", "Audio", "Proximity 3D positional voice chat (V)");
            AddModItem(lv, "JourneyMap", "Map & Radar", "Live real-time minimap, waypoints, friend radar (J)");
            AddModItem(lv, "Artifacts", "Exploration", "Rare accessories and baubles found in dungeon chests");
            AddModItem(lv, "Farmer\'s Delight", "Cooking", "Cooking pots, hearty meals, feasts, and knife slicing");
            AddModItem(lv, "SkinRestorer", "Visuals", "Change your in-game skin anytime via /skin command");
            AddModItem(lv, "Sodium / Embeddium", "Performance", "Next-gen graphics engine for ultra-high FPS");'''

if old_mods in text:
    text = text.replace(old_mods, new_mods)
    print("Added new mods to Modpack tab.")
else:
    print("Warning: old_mods not found")

# Update Guide / CheatSheet
old_guide = '''            sb.AppendLine("=== IN-GAME AUTHENTICATION ===");'''
new_guide = '''            sb.AppendLine("=== SERVER & WHITELIST ===");
            sb.AppendLine("• Server IP: mc.primordial.my (Port: 25565)");
            sb.AppendLine("• Web Hub: https://minecraft.primordial.my");
            sb.AppendLine("• Self-whitelist: Use invite code 'adventure' on the website!");
            sb.AppendLine();
            sb.AppendLine("=== PROXIMITY VOICE CHAT ===");
            sb.AppendLine("• Press 'V' to open voice settings, volume & mic testing");
            sb.AppendLine("• Push-to-Talk or Voice Activation available in 'V' menu");
            sb.AppendLine("• Create private whisper groups with friends");
            sb.AppendLine();
            sb.AppendLine("=== CUSTOM SKINS (OFFLINE SUPPORT) ===");
            sb.AppendLine("• /skin <player>  : Copy any player's skin (e.g. /skin Technoblade)");
            sb.AppendLine("• /skin url <url> : Set custom skin from direct image link");
            sb.AppendLine("• /skin clear     : Reset to default character skin");
            sb.AppendLine();
            sb.AppendLine("=== IN-GAME AUTHENTICATION ===");'''

if old_guide in text:
    text = text.replace(old_guide, new_guide)
    print("Added voice chat, skins, and server details to CheatSheet.")
else:
    print("Warning: old_guide not found")

with open('launcher-src/Program.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Saved updated Program.cs!")
