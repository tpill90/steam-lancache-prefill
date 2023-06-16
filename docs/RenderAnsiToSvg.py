#TODO refactor
#TODO document
#TODO should this be located in lancacheprefill.common?
#TODO maybe calculate the file hash of each file to be processed, and use it to determine if there were any changes since the last run.  
# Could then open the browser automatically only on the changed images

#region imports

import sys
import functions
import xml.etree.ElementTree as ET
import os
from pathlib import Path
from rich import print

# Resolves the directory where the script is located
script_dir = str(Path(__file__).resolve().parent)

#endregion

# Finding all .ansi files so we can render them
inputDirectory = f"{script_dir}/img"

# Font will be embedded directly into the SVG.  This is necessary since Github doesn't allow loading fonts from different domains
fontFilePath = f"{script_dir}/img/assets/CascadiaMono-Regular.woff2"
base64_string = functions.encode_file_to_base64(fontFilePath)

for inputFile in os.listdir(inputDirectory):
	if not inputFile.endswith(".ansi"):
		continue
	
	print(f"Processing [magenta]{inputFile}[/magenta]")
	fullFilePath = os.path.join(inputDirectory, inputFile)

	inputText = ''
	f = open(fullFilePath, "r", encoding='utf-8')
	for line in f:
		# need to manually remove newlines
		temp = line.replace(r'\r\n', "")
		
		# handle escaping unicode characters
		temp = temp.replace(r'\u001b', "\u001b".encode().decode('unicode-escape'))
		inputText += temp

	f.close()
	
	svgOutputPath = f"{inputDirectory}/svg/{os.path.splitext(inputFile)[0]}.svg"
	functions.ansiToSVG(inputText, svgOutputPath)
	functions.replace_file_content(svgOutputPath, 'Fira Code', 'Cascadia Mono')
	functions.replace_file_content(svgOutputPath, 'local("FiraCode-Regular")', f"url(\"data:application/font-woff;charset=utf-8;base64,{base64_string}\") format(\"woff2\")")

	functions.remove_title_bar(svgOutputPath)
	

# Opens the specified file in the browser automatically after each run, to display the changes immediately.  Helpful for debugging or updating a .ansi file
os.system(f'start {inputDirectory}/svg/overview.svg')