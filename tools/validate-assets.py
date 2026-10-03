from pathlib import Path
from PIL import Image
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

required = [
    ROOT / "Info" / "GlobalInfo.xml",
    ROOT / "Creature" / "CreatureList" / "CaleHenituse.txt",
    ROOT / "Creature" / "CreatureGen" / "CaleHenituse.xml",
    ROOT / "Creature" / "Creatures" / "CaleHenituse.txt",
    ROOT / "Creature" / "CreatureInfo" / "en" / "CaleHenituse.xml",
    ROOT / "Equipment" / "EquipMods" / "94180419.txt",
    ROOT / "Equipment" / "EquipMods" / "94180420.txt",
    ROOT / "Equipment" / "EquipMods" / "94180421.txt",
]

missing = [str(p.relative_to(ROOT)) for p in required if not p.exists()]
if missing:
    raise SystemExit("Missing files:\n" + "\n".join(missing))

for p in ROOT.rglob("*.xml"):
    ET.parse(p)

for p in ROOT.rglob("*.png"):
    im = Image.open(p)
    print(f"{p.relative_to(ROOT)}: {im.size}, mode={im.mode}")

print("XML/asset structure: OK")
