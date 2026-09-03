# Linux Setup Guide

## Vorrausgesetzte Pakete installieren

Vor der Installation von **{{prefill_name}}** müssen die Pakete `curl`, `jq` und `unzip` auf dem System installiert sein. Abhängig von der Konfiguration des Systems, könnten diese Pakete bereits installiert sein. Trotzdem stellen die folgenden Schritte sicher, dass alles nötige erfolgreich instaliert wurde.

!!! Hinweis
    Diese Schritte gehen von einem Ubuntu/Debian System aus. Abhängig davon welche Linux Distribution du verwendest, musst du die Befehle ggf. leicht an dein System anpassen.

Um die folgenden Befehle auszuführen, öffne eine neue **Konsole** und gib die folgenden Befehle ein:

```bash
# Stellt sicher, dass die aktuellsten Versionen installiert werden
apt-get update

# Installiert die nötigen Pakete
apt-get install curl jq unzip wget -y
```

!!! Warning
    Beim ausführen von `apt-get install` könnte ein "Could not open lock file" Fehler auftreten. In dem Falle füge einfach `sudo` vorne an den Befehl an.

Sobald alle Pakete erfolgreich installiert wurden, können wir mit der Installation von **{{prefill_name}}** fortfahren.

---

## {{prefill_name}} installieren

Wir werden ein Script zur Unterstützung bei der Installation von **{{prefill_name}}** nutzen, das erspart uns mehrere manuelle Schritte.

Um anzufangen, öffne eine neue Konsole. Wir wollen einen neuen Ordner erstellen, in welchem wir anschließen **{{prefill_name}}** installieren können. Um einen neuen Ordner zu erstellen und dort hinein zu navigieren, füre die folgenden Befehle aus:

``` bash
mkdir {{prefill_name}}
cd {{prefill_name}}/
```

Nachdem die Befehle oben erfolgreich ausgeführt wurden solltest du eine Ausgabe ähnlich zu dieser in deiner Konsole sehen:

<div data-cli-player="../casts/make-new-directory.cast" data-rows=4></div>
<br>

Wir können nun fortfahren und das [Installationsskript](https://github.com/tpill90/{{repo_name}}/blob/master/scripts/update.sh) herunterladen und ausführen, um **{{prefill_name}}** zu installieren:

```bash
# Lädt das Installationsskript herunter
curl -o update.sh --location "https://raw.githubusercontent.com/tpill90/{{repo_name}}/master/scripts/update.sh"

# Macht das Installationsskript ausführbar
chmod +x update.sh

# Führt die Installation durch
./update.sh

# Macht {{prefill_name}} ausführbar
chmod +x ./{{prefill_name}}
```

Wenn alles wie erwartet funktioniert, solltest du in deiner Konsole eine Ausgabe ähnlich zu dieser sehen:

<div data-cli-player="../casts/successful-linux-install.cast" data-rows=7></div>
<br>


**{{prefill_name}}** ist nun erfolgreich auf deinem System installiert! Du kannst es mit `./{{prefill_name}}` ausführen.

---

## Nächste Schritte

Wenn du **{{prefill_name}}** das erste Mal benutzt und eine Anleitung haben willst, schaue dir [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) an.

Antworten auf häufig gestellte Fragen und Probleme können unter [Frequently Asked Questions](https://github.com/tpill90/{{repo_name}}#frequently-asked-questions) gefunden werden.
