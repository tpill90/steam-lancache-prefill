# Einen Nightly Job einrichten

!!! Hinweis
    Diese Anleitung geht davon aus dass du **{{prefill_name}}** bereits auf deinem System installiert hast. Solltest du das noch nicht getan haben, schaue dir die [Linux Installationsanleitung](../Linux-Setup-Guide) an.

## Zeitplan einrichten

Zuerst müssen wir einen `timer` konfigurieren, welcher den Zeitplan einrichtet, nach dem **{{prefill_name}}** ausgeführt werden soll. In diesem Beispiel richten wir einen Zeitplan ein, welcher nächtlich um 04:00 Uhr lokaler Zeit ausgeführt wird.

Erstelle eine neue Datei namens `/etc/systemd/system/{{prefill_name.lower()}}.timer` mit der folgenden Konfiguration und speichere sie:

```ini
[Unit]
Description={{prefill_name}} nächtliche Ausführung
Requires={{prefill_name.lower()}}.service

[Timer]
# Wird jeden Tag um 04:00 Uhr lokaler Zeit ausgeführt.
OnCalendar=*-*-* 4:00:00

# Auf true setzen um zu speichern, wann der Job zuletzt ausgeführt wurde.
Persistent=true

[Install]
WantedBy=timers.target
```

---

## Job konfigurieren

Als nächstes konfigurieren wir den Job, der täglich von dem zuvor eingerichteten `timer` aus dem vorheringen Schritt ausgeführt wird. Erstelle eine neue Datei `/etc/systemd/system/{{prefill_name.lower()}}.service` mit dem folgenden Inhalt und speichere sie:

!!! Hinweis
    Die Werte für `User`, `WorkingDirectory` und `ExecStart` müssen so eingerichtet werden, dass sie auf deinen **{{prefill_name}}** Installationsort verweisen.

```ini
[Unit]
Description={{prefill_name}}
After=remote-fs.target
Wants=remote-fs.target

[Service]
# Füge hier den Nutzernamen ein
User=

# Füge hier den Installationsort von {{prefill_name}} ein.
# Beispiel: /home/tim/{{prefill_name}}
WorkingDirectory=

# Füge hier den vollen Pfad zur ausfühbaren Datei von {{prefill_name}} inklusive aller gewünschten Optionen ein
# Beispiel: /home/tim/{{prefill_name}}/{{prefill_name}} prefill --no-ansi
ExecStart=

Type=oneshot
Nice=19

[Install]
WantedBy=multi-user.target
```

Sobald diese beiden Dateien eingerichtet wurden, kannst du den Job mit den folgenden Befehlen aktivieren:

```
sudo systemctl daemon-reload
sudo systemctl enable --now {{prefill_name.lower()}}.timer
sudo systemctl enable {{prefill_name.lower()}}
```

Wenn alles korrekt konfiguriert wurde, solltest du eine Ausgabe ähnlich wie die folgende sehen, wenn du den Befehl `sudo systemctl status {{prefill_name.lower()}}.timer` ausführst:

<div data-cli-player="../casts/systemd-timer-status.cast" data-rows=8></div>
<br>

---

## Logs einsehen

Der aktuelle Status kann mit dem Befehl `sudo systemctl status {{prefill_name.lower()}}` eingesehen werden, welcher sowohl den aktuellen Status, sowie kürzliche Logs anzeigt.

<div data-cli-player="../casts/systemd-service-logs.cast" data-rows=17></div>
<br>
