# prefill

<div data-cli-player="../casts/prefill.cast" data-rows=13></div>
<br>

## Übersicht

Füllt einen Lancache-Server autmatisch mit {{ gaming_service_name }} Apps, sodass zukünftige Downloads von Lancache direkt bearbeitet können, was Geschwindigkeiten erhöhen und die Internetauslastung reduzieren kann.

Speichert zuvor heruntergeladene Spiele und lädt beim erneuten Ausführen nur Spiele herunter, für die neue Versionen verfügbar sind.

---

## Beispiel

!!! Hinweis
    Dieser Befehl lädt automatisch alle Apps herunter, die mit dem Befehl `select-apps` ausgewählt wurden, unabhängig von zusätzlichen Optionen.

Um den `prefill` Prozess zu starten, muss einfach nur der folgende Befehl in der Konsole eingegeben werden:
```powershell
./{{prefill_name}} prefill
```

Nach Beginn des `prefill` Befehls, prüft **{{prefill_name}}**, welche Apps seit dem letzten ausführen neue Versionen erhalten haben, oder noch nie erfolgreich heruntergeladen wurden. Wenn `prefill` eine solche App erkennt, beginnt es mit dem Download. Falls keine Apps heruntergeladen werden müssen, beendet der Prozess sich selbst.


### Deine komplette {{gaming_service_name}} Bibliothek herunterladen

Je nachdem wie groß deine Bibliothek ist und wie viele Apps du herunterladen willst, könnte es einfacher sein, die komplette Bibliothek herunterzuladen, anstatt die Apps einzeln mit dem Befehl `select-apps` auszuwählen. Dadurch werden auch neu gekaufte Apps direkt mit aufgenommen und müssen nicht manuell mit `select-apps` nach dem Kauf hinzugefügt werden.

```powershell
./{{prefill_name}} prefill --all
```

### Sicherstellen, dass der Cache vollständig gefüllt ist

Wenn beispielsweise bald ein Event ansteht und du zu 100% sicher sein willst, dass alle Apps und alle Updates heruntegeladen und im Lancache zwischengespeichert wurden, kannst du diesen Befehl verwenden. Normalerweise sollte `prefill` alle ausgewählten apps immer mit allen aktuellen Updates herunterladen, durch verschiedene Gründe könnten apps aber nicht mehr zwischengespeichert sein, auch wenn sie das eigentlich sollten. Mit der zusätzlichen Option `--force` wird **{{prefill_name}}** gezwungen, alle Apps erneut herunterzuladen, auch wenn sie schon zwischengespeichert sind. Dadurch dass alle Apps vollständig neu heruntergeladen werden, werden sämtliche fehlenden Dateien ergänzt.

```powershell
./{{prefill_name}} prefill --force
```

### Mehrere Optionen kombinieren

Es ist möglich mehrere Optionen in einem einzelnen Befehl zu kombinieren, anstatt jede einzeln zu verwenden. Der folgende Befehl wählt beispielsweise die meistgespielten Spiele von Steam aus, lädt nur die Linux version herunter und gibt zusätzlich mehr Details aus:

```powershell
./{{prefill_name}} prefill --top --os linux --verbose
```

---

## Optionen

| Option &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; | &nbsp; &nbsp; | Werte                 | Standard    |     |
| ------------------------------------------------------------------------------------------------- | ------------- | --------------------- | ----------- | --- |
| --os                                                                                              |               | windows, linux, macos | **windows** | Gibt an, für welches Betriebssystem Spiele heruntergleaden werden sollen. Fast alle Spiele unterstützen Windows, es gibt jedoch immer mehr Spiele die eigene native Versionen für Linux anbieten. Diese können ähnlich groß wie die Windows Version sein. |
| --all                                                                                             |               |                       |             | Lädt alle verfügbaren Apps herunter, nützlich um einen leeren Cache zu füllen.  |
| --recent                                                                                          |               |                       |             | Lädt alle Apps herunter, die innerhalb der letzten 2 Wochen geöffnet wurden.  |
| --recently-purchased                                                                              |               |                       |             | Lädt alle Apps herunter, die dem Konto in den letzten 2 Wochen hinzugefügt wurden.  |
| --top                                                                                             |               | 1-100                 | **50**      | Lädt die beliebtesten Spiele der letzten 2 Wochen nach Spielerzahl herunter.  |
| --force                                                                                           | -f            |                       |             | **{{prefill_name}}** speichert standartmäßig die zuletzt heruntergeladenen Apps und lädt nur Apps erneut herunter, für die ein Update verfügbar ist. Dieses Verhalten ist gut für die meisten Fälle, da so unnötige Downloads von bereits heruntergladenen Apps vermieden werden.  <br><br> Die Option `--force` überschreibt dieses Verhalten und lädt alle Apps erneut herunter. Kann für Diagnose, Tests, und messen der Geschwindigkeit nützlich sein.  |
| --verbose                                                                                         |               |                       |             | Produziert detailliertere Ausgaben. Standartmäßig werden Apps welche übersprungen werden können nicht angezeigt, mit dieser Option werden alle Apps immer protokolliert.  |
| --unit                                                                                            |               | bits, bytes           | **bits**    | Gibt an in welcher Einheit Geschwindigkeiten angezeigt werden.  |
| --no-ansi                                                                                         |               |                       |             | Alle Ausgaben erfolgen in Klartext, anstatt den visuell ansprechenden Farben und Fortschrittsbalken. Sollte nur in Terminals verwendet werden, die ANSI nicht unterstützen, oder wenn die Ausgabe in eine Datei umgeleitet wird. Die meisten Terminals unterstützen ANSI.  |
