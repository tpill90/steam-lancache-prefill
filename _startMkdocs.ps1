# Check dead links
# muffett http://localhost:8000

Set-Location docs

pip install -r requirements.txt
Start-Process http://127.0.0.1:8000
python -m mkdocs serve