# Configuring a Nightly Job

!!! Note
    This guide assumes that you have already installed **{{prefillName}}** on your system.  If you have not yet installed **{{prefillName}}**, see [Linux Setup Guide](../Linux-Setup-Guide)


## Configuring The Schedule

We will first need to configure a `timer` which will configure the schedule that **{{prefillName}}** will run on.  In this example, we will setup a schedule that will run nightly at 4am local time.  This schedule was chosen

You should create a new file named `/etc/systemd/system/{{prefillName.lower()}}.timer`, and save the following configuration into that file.

```ini
[Unit]
Description={{prefillName}} run daily
Requires={{prefillName}}.service

[Timer]
# Runs every day at 4am (local time)
OnCalendar=*-*-* 4:00:00

# Set to true so we can store when the timer last triggered on disk.
Persistent=true

[Install]
WantedBy=timers.target
```

-----

## Configuring The Job

Next, well setup the job that will be triggered nightly by the `timer` that we previously setup.  Create a new file `/etc/systemd/system/{{prefillName.lower()}}.service`, and save the following configuration into the file.

!!! Note
    The values of `User`, `WorkingDirectory`, and `ExecStart` will need to be configured to point to your **{{prefillName}}** install location.

```ini
[Unit]
Description={{prefillName}}
After=remote-fs.target
Wants=remote-fs.target

[Service]
Type=oneshot
# Sets the job to the lowest priority
Nice=19
User=# Replace with your username
WorkingDirectory=# Set this to the directory where {{prefillName}} is installed.  E.g /home/tim/Prefills
ExecStart=# Set this to match your working directory from the line above.  E.g. /home/tim/Prefills/{{prefillName}} prefill --no-ansi

[Install]
WantedBy=multi-user.target
```

Once these two files are setup, you can enable the scheduled job with:
```
sudo systemctl daemon-reload
sudo systemctl start {{prefillName.lower()}}.timer
sudo systemctl enable {{prefillName.lower()}}
```

If everything was configured correctly, you should see similar output from running `sudo systemctl status {{prefillName.lower()}}.timer`

<div data-cli-player="../casts/systemd-timer-status.cast" data-rows=8></div>
<br>

-----

## Checking Service Logs

It is possible to check on the status of the service using `sudo systemctl status {{prefillName.lower()}}`, which will display both the service's status as well as its most recent logs.

<div data-cli-player="../casts/systemd-service-logs.cast" data-rows=17></div>
<br>

