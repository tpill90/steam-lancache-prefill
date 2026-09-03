# Häufig gestellte Fragen

## Ich muss mich mit meinem Passwort anmelden? Woher weiß ich, dass das sicher ist?

Genauso wie Steam, speichert **SteamPrefill** dein Passwort niemals. Dein Passwort wird nur einmalig temporär zur Anmeldung genutzt und nirgends auf dem System gespeichert. Nach der Anmeldung erhält **SteamPrefill** einen "Zugangs-Token" von Steam, welcher für zukünftige Anmeldungen verwendet wird, ohne dass ein Password nötig ist. Da der Quellcode von **{{prefill_name}}** öffentlich einsehbar ist, kannst du selbst verifizieren, wie dein Passwort im [Quellcode](https://github.com/tpill90/steam-lancache-prefill/blob/919ee58ead1458778b121933bbde02cc16d03837/SteamPrefill/Handlers/Steam/Steam3Session.cs#L106) verwendet wird.

Für zusätzliche Sicherheit wird empfohlen die 2-Faktor-Authentifizierung über den **Steam Guard Mobile Authenticator** für dein Konto zu aktivieren. Die App generiert einen Code den du beim anmelden eingeben musst, welcher sich alle 30 Sekunden ändert und nur einmal verwendet werden kann. Um das einzurichten, folge der Anleitung [Einrichtung des Steam-Mobile-Authentifikators](https://help.steampowered.com/de/faqs/view/6891-E071-C9D9-0134).

---

## Kann ich {{prefill_name}} auf dem Lancache-Server ausführen?

Ja, kannst du! Du musst einfach nur **{{prefill_name}}** auf dem Server installieren und es regulär ausführen.

Wenn alles wie erwartet funktioniert, solltest du beim ausführen von `prefill` eine Nachricht sehen, dass ein Server unter der IP `127.0.0.1` gefunden wurde.

<img src="../images/svg/AutoDns-Server.svg" alt="Prefill running on Lancache Server">

Du kannst auch den Docker Container nutzen, in dem Fall sollte der Server stattdessen unter der IP `172.17.0.1` gefunden werden.

<img src="../images/svg/AutoDns-Docker.svg" alt="Prefill running on Lancache Server in Docker">

**{{prefill_name}}** auf dem Lancache Server selbst zu installieren bringt einige Vorteile gegenüber der Installation auf einem Client-Gerät mit sich, primär die Geschwindigkeit mit der Apps heruntergeladen werden können.
Dadurch, dass keine Daten über das Netzwerk transportiert werden, sollte der `prefill` Befehl nur von der Geschwindigkeit der Datenträger und der CPU-Leistung abhängig sein.
Wenn man beispielsweise eine **SK Hynix Gold P31 2TB NVME SSD** nutzt und mit `prefill --force` Apps aus dem Cache herunterlädt, sind solche Geschwindigkeiten möglich:

<img src="../images/svg/AutoDns-ServerPerf.svg" alt="Prefill running on Lancache Server in Docker">

---

## Kann {{prefill_name}} automatisch nach Zeitplan ausgeführt werden?

Ja, kann es! Zeitabhängige Jobs können unter Linux einfach über `systemd` services eingerichtet werden und laufen flexibel nach dem von dir konfigurierten Zeitplan. Lies dir [Einen Nightly Job einrichten](https://tpill90.github.io/{{repo_name}}/install-guides/Scheduled-Job/) für eine Anleitung dazu durch.

---

## Kann ich meinen Cache mit bereits installierten {{gaming_service_name}} Apps füllen?

Es ist leider nicht möglich einen Lancache direkt mit bereits installierten {{gaming_service_name}} Apps zu füllen. Die installierten Apps sind in einem anderen Format als dem, welches Lancache zwischenspeichert, da die Dateien nach dem Download dekomprimiert und entschlüsselt werden. Dieser Prozess ist nicht umkehrbar, daher ist der einzige Weg Apps richtig im Cache zu speichern, sie über **{{prefill_name}}** oder **{{gaming_service_name}}** erneut herunterzuladen.

---

## Wo speichert {{prefill_name}} downloads?

**{{prefill_name}}** speichert gar keine Downloads! Es lädt die Daten einfach nur so schnell wie möglich über Lancache herunter, ohne die Daten jemals auf der Festplatte zu speichern. Lancache ist das Programm, welches beim herunterladen von Dateien diese zwischenspeichert, egal ob durch **{{gaming_service_name}}** oder **{{prefill_name}}**.

---

## Wie pausiere ich laufende Downloads?

Du kannst downloads jederzeit pausieren, indem du den Prozess einfach mit `STRG + C` beendest. Dadurch geht nichts kaputt, **{{prefill_name}}** macht beim nächsten ausführen einfach da weiter, wo es aufgehört hat.

---

## Ist es möglich Apps die ich nicht besitze herunterzuladen?

Auch wenn es definitiv nützlich (und günstiger!) wäre, Apps die du nicht besitzt herunterzuladen, ist es leider nicht möglich. Um Apps von {{gaming_service_name}} herunterzuladen, ist es nötig sich mit seinem Nutzernamen und Passwort zu Authentifizieren. {{gaming_service_name}} weiß welche Apps du besitzt, wodurch **{{prefill_name}}** die Liste der Apps die du besitzt in `select-apps` anzeigen kann. Wenn **{{prefill_name}}** versucht eine App (ob im Besitz oder nicht) herunterzuladen, überprüft {{gaming_service_name}} ob dein Konto dazu berechtigt ist und verweigert den Download einfach, wenn du nicht dazu berechtigt bist.

---

## Wie kann ich die Bandbreite beim Herunterladen begrenzen?

Du möchtest vielleicht die Geschwindigkeit beim herunterladen von Spielen durch **{{prefill_name}}** begrenzen, um zu verhindern dass dadurch die komplette Internetleitung ausgelastet wird, worunter die Verbindung anderer Geräte leiden könnte. Dieses Problem ist bekannt als Bufferbloat, mehr Informationen zu diesem Problem kannst du hier finden: [What is bufferbloat?](https://waveform.com/tools/bufferbloat)

**{{prefill_name}}** hat aktuell keine Funktion zur Limitierung der Bandbreite und wird eine solche Funktion durch die Art wie downloads implementierrt sind auch nie haben. Selbst wenn **{{prefill_name}}** seine Bandbreite begrenzen könnte, würden die gleichen Probleme vermutlich bei Downloads durch **{{gaming_service_name}}** auftreten.

Eine Möglichkeit die Brandbreite zu begrenzen wäre _Quality of Service (QOS)_ auf deinem Router einzurichten, wodurch die Bandbreite des Lancache-Servers begrenzt wird, oder andere Geräte priorisiert werden. Eine Anleitung für Anfänger kann hier gefunden werden: [Beginners guide to QOS](https://www.howtogeek.com/75660/the-beginners-guide-to-qos-on-your-router/)

Anleitungen für bestimmte Router können hier gefunden werden:

- [Asus](https://asus.com/support/FAQ/1013333/)
- [Netgear](https://kb.netgear.com/25613/How-do-I-enable-Dynamic-QoS-on-my-Nighthawk-router)
- [Linksys](https://linksys.com/support-article?articleNum=50216)
- [TP-Link](https://tp-link.com/us/support/faq/557/)

---

## Meine Logs haben komische Zeichen und sind dadurch schwer zu lesen. Kann ich die Zeichen entfernen?

Abhängig von der verwendeten Konsole und den vom System unterstützten Farben, könnten die Logs so aussehen:

```text
[6:20:46 PM] Starting [38;5;80mCounter-Strike: Global Offensive[0m
[6:20:46 PM] Downloading [38;5;170m12.91 GiB[0m
```

Einer der Gründe weshalb die Logs so aussehen, könnte sein, dass deine Konsole falsche Angaben zur Kompatibilität dieser Zeichen macht, wodurch sie Ausgaben erhält, die nicht verarbeitet werden können. Um diese Zeichen zu entfernen, nutze einfach die Option `--no-ansi`, wodurch alle möglicherweise nicht unterstützten Zeichen entfernt werden.

---

## Kann ich mehrere {{gaming_service_name}} Konten gleichzeitig verwenden?

Die Verwendung von mehreren Konten wird aktuell leider nicht von {{prefill_name}} unterstützt, da es als Programm für einen einzelnen Nutzer geschrieben wurde. Allerdings gibt es eine einfache Methode, das zu umgehen. Da {{prefill_name}} als komplett eigenständiges Programm entworfen wurde, also alle Konfigurationen im Installationsverzeichnis bleiben, kannst du einfach mehrere Instanzen unter verschiedenen Pfaden installieren und dadurch mehrere Konten verwenden.

Du installierst {{prefill_name}} also einmal pro Konto, welches du anmelden willst. Wenn du beispielsweise 2 Konten verwenden willst, könnte deine Ordnerstruktur ungefähr so aussehen:

<img src="../images/svg/Multi-User-File-Structure.svg" alt="Structure required for two {{gaming_service_name}} accounts">

Nachdem mehrere Instanzen installiert wurden, können sie wie üblich unabhängig von einander über die Konsole verwendet werden.
