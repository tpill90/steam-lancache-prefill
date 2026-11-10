# Configuring a Nightly Job

!!! Note
    This guide assumes that you have already installed **{{prefill_name}}** on your system.  If you have not yet installed **{{prefill_name}}**, see [Linux Setup Guide](../Linux-Setup-Guide)

## Configuring The Schedule

We will first need to configure a `timer` which will configure the schedule that **{{prefill_name}}** will run on. In this example, we will setup a schedule that will run nightly at 4am local time.

You should create a new file named `/etc/systemd/system/{{prefill_name.lower()}}.timer`, and save the following configuration into that file.

```ini
[Unit]
Description={{prefill_name}} run daily
Requires={{prefill_name.lower()}}.service

[Timer]
# Runs every day at 4am (local time)
OnCalendar=*-*-* 4:00:00

# Set to true so we can store when the timer last triggered on disk.
Persistent=true

[Install]
WantedBy=timers.target
```

---

## Configuring The Job

Next, we will setup the job that will be triggered nightly by the `timer` that we previously setup.  Create a new file `/etc/systemd/system/{{prefill_name.lower()}}.service`, and save the following configuration into the file.

!!! Note
    The values of `User`, `WorkingDirectory`, and `ExecStart` will need to be configured to point to your **{{prefill_name}}** install location.

```ini
[Unit]
Description={{prefill_name}}
After=remote-fs.target
Wants=remote-fs.target

[Service]
# Replace with your username
User=

# Set this to the directory where {{prefill_name}} is installed.
# Example : /home/tim/{{prefill_name}}
WorkingDirectory=

# This should be the full path to {{prefill_name}}, as well as any additional option flags
# Example: /home/tim/{{prefill_name}}/{{prefill_name}} prefill --no-ansi
ExecStart=

Type=oneshot
Nice=19

[Install]
WantedBy=multi-user.target
```

Once these two files are setup, you can enable the scheduled job with:

```
sudo systemctl daemon-reload
sudo systemctl enable --now {{prefill_name.lower()}}.timer
sudo systemctl enable {{prefill_name.lower()}}
```

If everything was configured correctly, you should see similar output from running `sudo systemctl status {{prefill_name.lower()}}.timer`

<div data-cli-player="../casts/systemd-timer-status.cast" data-rows=8></div>
<br>

---

## Checking Service Logs

It is possible to check on the status of the service using `sudo systemctl status {{prefill_name.lower()}}`, which will display both the service's status as well as its most recent logs.

<div data-cli-player="../casts/systemd-service-logs.cast" data-rows=17></div>
<br>

---

## Getting Notified When Re-Login Is Required

Steam login tokens expire after roughly 6 months, or immediately if your Steam password changes.  Once the token is no longer valid, a scheduled run can't log in, because nobody is there to type in the password.

**{{prefill_name}}** helps you catch this before and after it happens:

- **Automatic renewal** : starting 30 days before the token expires, **{{prefill_name}}** asks Steam for a renewed token on every run.  Steam decides whether to issue one, so this is best effort.
- **Early warning** : during that same 30 days, every run logs a warning like `Warning: The saved Steam login token expires in 12 day(s)`.
- **Dedicated exit code** : when re-authentication is required and the app is running non-interactively, it stops immediately with exit code `10` (instead of waiting for a password prompt to time out).

To fix it, run **{{prefill_name}}** once in an interactive terminal, and enter your password (and Steam Guard code, if requested) :

```bash
./{{prefill_name}} prefill
```

### Sending a notification on failure

systemd can start another unit whenever the prefill job fails.  Add an `OnFailure=` line to the `[Unit]` section of `/etc/systemd/system/{{prefill_name.lower()}}.service` :

```ini
[Unit]
Description={{prefill_name}}
After=remote-fs.target
Wants=remote-fs.target
OnFailure={{prefill_name.lower()}}-failed.service
```

Then create `/etc/systemd/system/{{prefill_name.lower()}}-failed.service`.  This example sends a push notification with [ntfy](https://ntfy.sh); replace the `ExecStart` line with whatever notification method you prefer (mail, Discord/Slack webhook, Gotify, etc.) :

```ini
[Unit]
Description=Notify when {{prefill_name}} fails

[Service]
Type=oneshot
ExecStart=/usr/bin/curl -s -H "Title: {{prefill_name}} failed" -d "Check 'journalctl -u {{prefill_name.lower()}}'. If the Steam login expired, run {{prefill_name}} interactively once." https://ntfy.sh/<your-topic>
```

If you only want to be notified about the expired login, and not about other failures, check the exit code in a wrapper script instead, and point `ExecStart` at that script :

```bash
#!/bin/bash
./{{prefill_name}} prefill --no-ansi
if [ $? -eq 10 ]; then
    curl -s -d "{{prefill_name}}: Steam login expired, please log in again" https://ntfy.sh/<your-topic>
fi
```

Finally, reload systemd so it picks up the changes :

```bash
sudo systemctl daemon-reload
```
