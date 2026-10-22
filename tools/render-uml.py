#!/usr/bin/env python3
"""Render the complete class diagram with an opaque background in all SVG viewers."""
import argparse
import os
from pathlib import Path
import re
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--jar", type=Path, help="PlantUML jar; otherwise use plantuml on PATH")
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
source = root / "docs/uml/01-current-classes.puml"
command = ["java", "-Djava.awt.headless=true", "-jar", str(args.jar.resolve())] if args.jar else ["plantuml"]
environment = dict(os.environ, PLANTUML_LIMIT_SIZE="40000")
environment["JAVA_TOOL_OPTIONS"] = environment.get("JAVA_TOOL_OPTIONS", "") + " -Djava.awt.headless=true"
subprocess.run(command + ["-tsvg", "-failfast2", str(source)], env=environment, check=True)
svg = source.with_suffix(".svg")
text = svg.read_text()
# Some image viewers ignore CSS background on the SVG root. Paint a real canvas
# behind all diagram elements so dark viewer themes cannot show through.
match = re.search(r"<svg\b[^>]*>", text)
if not match:
    raise RuntimeError("PlantUML did not produce an SVG root")
text = text[:match.end()] + '<rect id="diagram-background" x="0" y="0" width="100%" height="100%" fill="#FFFFFF"/>' + text[match.end():]
svg.write_text(text)
print(f"Rendered opaque SVG: {svg}")
