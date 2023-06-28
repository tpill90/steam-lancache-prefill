#region imports

import re
from io import StringIO
from pathlib import Path
import base64
from rich import text
from rich.console import Console
from rich.terminal_theme import TerminalTheme
from yaml import safe_load
from bs4 import BeautifulSoup
import sys

#endregion

#TODO refactor
THISDIR = str(Path(__file__).resolve().parent)
sys.path.insert(0, str(Path(THISDIR).parent))

def ansiToSVG(
	ansiText: str,
	fileName: str,
	theme: str | None = None
):
	console = _doRichRender(ansiText)
	console.save_svg(fileName, theme=_doRichTerminalTheme(theme), title="")

def _doRichRender(ansiText: str) -> Console:
	#TODO move this variable to be a global setting
	CONSOLE_WIDTH = 80
	console = Console(width=CONSOLE_WIDTH, record=True, file=StringIO())

	richText = text.Text.from_ansi(ansiText)
	console.print(richText)
	console.height = len(richText.wrap(console, width=CONSOLE_WIDTH))
	return console

def _doRichTerminalTheme(theme: str | None) -> TerminalTheme:
	base24 = safe_load(Path(theme or f"{THISDIR}/img/assets/onedark.yml").read_text(encoding="utf-8"))

	return TerminalTheme(
		background=_hexToRGB(base24["base00"]),
		foreground=_hexToRGB(base24["base05"]),
		normal=[
			_hexToRGB(base24["base01"]),
			_hexToRGB(base24["base08"]),
			_hexToRGB(base24["base0B"]),
			_hexToRGB(base24["base09"]),
			_hexToRGB(base24["base0D"]),
			_hexToRGB(base24["base0E"]),
			_hexToRGB(base24["base0C"]),
			_hexToRGB(base24["base06"]),
		],
		bright=[
			_hexToRGB(base24["base02"]),
			_hexToRGB(base24["base12"]),
			_hexToRGB(base24["base14"]),
			_hexToRGB(base24["base13"]),
			_hexToRGB(base24["base16"]),
			_hexToRGB(base24["base17"]),
			_hexToRGB(base24["base15"]),
			_hexToRGB(base24["base07"]),
		],
	)

def _hexToRGB(colourCode: str) -> tuple[int, int, int]:
	return tuple(int(colourCode[i : i + 2], base=16) for i in (0, 2, 4))

def encode_file_to_base64(file_path):
	with open(file_path, "rb") as file:
		encoded_bytes = base64.b64encode(file.read())
		encoded_string = encoded_bytes.decode('utf-8')
	return encoded_string

def replace_file_content(file_path, old_string, new_string):
	with open(file_path, 'r', encoding='utf-8') as file:
		file_contents = file.read()

	updated_contents = file_contents.replace(old_string, new_string)

	with open(file_path, 'w', encoding='utf-8') as file:
		file.write(updated_contents)
	
def remove_title_bar(svgOutputPath):
	shift_distance = 28

	with open(svgOutputPath, 'r', encoding='utf-8') as file:
		svg_data = file.read()

	soup = BeautifulSoup(svg_data, 'xml')

	# Find the <g> element with the specified transform attribute value
	g_elements = soup.find_all('g', attrs={'transform': 'translate(26,22)'})
	removed_g_element = None
	for g_element in g_elements:
		removed_g_element = g_element
		break  # Remove only the first matching <g> element

	if removed_g_element is not None:
		# Calculate the vertical shift distance
		
		# Remove the <g> element
		removed_g_element.decompose()

		# Shift the remaining elements upwards
		for element in soup.find_all():
			if 'transform' in element.attrs:
				transform_value = element['transform']
				match = re.search(r"translate\((\d+),\s*(\d+)\)", transform_value)
				x_value = int(match.group(1))
				y_value = int(match.group(2))
				element['transform'] = f"translate({x_value},{y_value - shift_distance})"

	# Shrinking background rectangle
	rect_elements = soup.find_all('rect', attrs={'fill': '#282c34'})
	for rect_element in rect_elements:
		parsed = float(rect_element['height'])
		rect_element['height'] = str(parsed - shift_distance + 4)
		break  # Remove only the first matching <g> element

	# Shrinking overall viewbox box
	root_svg_tag = soup.find('svg')
	viewbox = root_svg_tag['viewBox'].split(' ')

	# Calculate and update new height
	current_height = float(viewbox[3])
	new_height = current_height - shift_distance + 4
	viewbox[3] = str(new_height)

	# Save new height
	root_svg_tag['viewBox'] = ' '.join(viewbox)

	with open(svgOutputPath, 'w', encoding='utf-8') as file:
		file.write(str(soup))