# Working With Project Documentation

## Installing Prerequisites

This project is using [mkdocs](https://www.mkdocs.org) to generate the static documentation that is then hosted on Github Pages. The only requirement for building **mkdocs** is to have **Python 3** installed, which can be done through [Chocolatey](https://community.chocolatey.org/packages/python).

```powershell
# Installs Python from Chocolatey.  Alternatively Python can be manually installed.
choco install python

# Installs required mkdocs package
pip install -r requirements.txt
```

---

## Project Layout

    mkdocs.yml    # Mkdocs root configuration file.
    mkdocs/
        index.md  # The documentation homepage.
        assets/   # Contains custom Javascript and CSS used on the docs site
        custom_theme/
    	img/
    	img/svg/   # The .ansi files in the parent directory will be rendered here.
        ...       # Other markdown pages, images and other files.

---

## Making Changes

**mkdocs** has a built in server that will watch for changes being made, and immediately display those changes.

You can launch the the live server using `mkdocs serve`, and open `http://127.0.0.1:8000/` in your browser. You can now make edits and have the page automatically refresh and display those changes!

---

## Helpful links

- [Writing your docs](https://www.mkdocs.org/user-guide/writing-your-docs)
- [Mkdocs Configuration](https://www.mkdocs.org/user-guide/configuration)
- [Python Markdown Extensions](https://python-markdown.github.io/extensions/)
- [Mkdocs Themes](https://github.com/mkdocs/mkdocs/wiki/MkDocs-Themes)
- [Projects using Mkdocs](https://github.com/mkdocs/mkdocs/wiki/MkDocs-Users)
- [Code highlighting demo](https://highlightjs.org/static/demo/)
- [Code Highlighting styles](https://github.com/highlightjs/highlight.js/tree/main/src/styles)
