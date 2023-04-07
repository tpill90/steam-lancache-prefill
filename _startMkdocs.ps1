# Check dead links
# npm install -g markdown-link-check@3.10.3
# get-childitem *.md -Recurse | ForEach-Object { markdown-link-check --quiet $_.FullName }

pip install mkdocs-awesome-pages-plugin
pip install mkdocs-section-index

cd docs
start http://127.0.0.1:8000
python3 -m mkdocs serve