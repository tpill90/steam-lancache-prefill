#TODO refactor
#TODO document here, as well as on the wiki.  Document what the workflow for this is, as well as the format of .ansi files
#TODO should this be located in lancacheprefill.common?
#TODO maybe calculate the file hash of each file to be processed, and use it to determine if there were any changes since the last run.
# Could then open the browser automatically only on the changed images
#TODO this should scan all folders recursively to find all .ansi files, and then generate svgs in the appropriate folder

#region imports

import sys
import RenderAnsiToSvg.functions as a2svg
import os
from pathlib import Path
from rich import print

#TODO this is the import to the file from another dir
sys.path.insert(0, './RenderAnsiToSvg')

# Resolves the directory where the script is located
script_dir = str(Path(__file__).resolve().parent)

#endregion

# Specifies the directory that is holding the input .ansi files to be processed
# inputDirectory = f"{script_dir}/img"
inputDirectory = f"{script_dir}/mkdocs/images"

# TODO document the output dir
a2svg.renderAnsiToSVG(inputDirectory)

# Opens the specified file in the browser automatically after each run, to display the changes immediately.  Helpful for debugging or updating a .ansi file
os.system(f'start {inputDirectory}/svg/overview.svg')