# select-apps

<div data-cli-player="../casts/select-apps.cast" data-rows=30></div>
<br>

## Overview

<!-- TODO Write this at some point -->

---

## Status

Listet alle ausgewählten Apps und ihre Downloadgröße auf. Bitte beachte, dass die Downloadgröße kleiner als die finale Installationsgröße ist, da Steam Dateien beim Download komprimiert.

<div data-cli-player="../casts/select-apps-status.cast" data-rows=18></div>
<br>

---

<!-- TODO give this another pass -->

### Example usage

Um den aktuellen `status` einzusehen, gib einfach den folgenden Befehl im Terminal ein:

```powershell
./{{prefill_name}} select-apps status
```

#### Sortierung ändern

Die Ausgabe kann wie gewüscht mit dem folgenden Befehl sortiert werden:

```powershell
./{{prefill_name}} select-apps status --sort-order descending --sort-by size
```

---

### Optionen

| Option &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; | Werte                 | Standard      |     |
| ----------------------------------------- | --------------------- | ------------- | --- |
| --sort-order                              | ascending, descending | **ascending** | Gibt an, in welcher Reihenfolge die Daten sortiert werden sollen.  |
| --sort-by                                 | app, size             | **app**       | Gibt an, nach welcher Spalte sortiert werden soll.  |
| --os                                      | windows, linux, macos | **windows**   | Gibt an, nach welchen Betriebssystemen gefiltert werden soll.  |
| --no-ansi                                 |                       |               | Alle Ausgaben erfolgen in Klartext, anstatt den visuell ansprechenden Farben und Fortschrittsbalken. Sollte nur in Terminals verwendet werden, die ANSI nicht unterstützen, oder wenn die Ausgabe in eine Datei umgeleitet wird. Die meisten Terminals unterstützen ANSI.  |
