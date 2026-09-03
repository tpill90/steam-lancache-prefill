# clear-temp

<div data-cli-player="../casts/clear-temp.cast" data-rows=6></div>
<br>

## Übersicht
Löscht temporäre Daten von {{prefill_name}}, wie gespeicherte Manifest-Dateien, um Speicherplatz frei zu machen.

Diese Dateien werden lokal gespeichert, um zukünfitge `prefill` Befehle deutlich zu beschleunigen (bis zu 3 Mal schneller), was jedoch etwas Speicherplatz benötigt. Diese Manifest-Dateien sammeln sich über die Zeit an, wenn neue Versionen von Spielen veröffentlich werden, was alte Manifest-Dateien ungenutzt zurücklässt.

Für die meisten Nutzer ist dieser Befehl nicht nötig, er kann allerdings hilfreich sein, um ein wenig Speicherplatz freizugeben, oder um temporäre Dateien zu entfernen und bei der Fehlerbehebung von {{prefill_name}} zu unterstützen.

---

## Optionen

| Option      |     |                                                              |
| ----------- | --- | ------------------------------------------------------------ |
| --yes       | -y  | Löscht temporäre Dateien ohne Fragen nach einer Bestätigung. |
