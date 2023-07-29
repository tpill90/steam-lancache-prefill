# Check dead links
# muffett http://localhost:8000

cd docs

pip install -r requirements.txt
start http://127.0.0.1:8000
python3 -m mkdocs serve