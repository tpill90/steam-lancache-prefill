# Docker Setup Guide

!!! Hinweis
    Docker ist nicht zwingend erforderlich, um **{{prefill_name}}** zu nutzen, da es eine komplett eigenständige App ist.

Diese Anleitung soll dabei helfen **{{prefill_name}}** als Docker Image ([DockerHub](https://hub.docker.com/r/tpill90/{{repo_name}}/tags)) aufzusetzen und zu verstehen, wie man damit interagiert. Diese Anleitung enthält keine genauen Anweisung zur Verwendung von **{{prefill_name}}** selbst, diese kann unter [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) gefunden werden.

---

## Allgemeine Interaktion über die Konsole

!!! Hinweis
    Arm64 wird ebenfalls unterstützt.  Nutze den Tag `latest-arm64` statt `latest` in den folgenden Beispielen.

Um die aktuellste Version des Containers herunterzuladen, öffne eine Konsole und führe den folgenden Befehl aus:

```bash
docker run -it --rm --net=host \
  --volume ~/.config/{{prefill_name}}:/Config \
  tpill90/{{repo_name}}:latest
```

Dieser Befehl macht das gleiche wie **{{prefill_name}}** direkt ohne Optionen über die Konsole auszuführen. Falls erfolgreich, sollte das folgende die Ausgabe sein:

<div data-cli-player="../casts/docker-pull.cast"></div>
<br>

Jetzt kannst du alle der in der Ausgabe aufgelisteten `COMMANDS` (Befehle) ausführen, indem du ihn einfach anhängst. Hier ein Beispiel mit `select-apps`:

```Bash
docker run -it --rm --net=host  \
  --volume ~/.config/{{prefill_name}}:/Config \
  tpill90/{{repo_name}}:latest \
  select-apps
```

---

## Nächste Schritte

Wenn du **{{prefill_name}}** das erste Mal benutzt und eine Anleitung haben willst, schaue dir [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) an.

Antworten auf häufig gestellte Fragen und Probleme können unter [Frequently Asked Questions](https://github.com/tpill90/{{repo_name}}#frequently-asked-questions) gefunden werden.
