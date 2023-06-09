# Check dead links
# muffett http://localhost:8000

pip install mkdocs mkdocs-awesome-pages-plugin mkdocs-static-i18n mkdocs-macros-plugin

cd docs
start http://127.0.0.1:8000
python3 -m mkdocs serve