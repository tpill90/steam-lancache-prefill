# Configuring a Nightly Job

!!! Note
    This guide assumes that you have already installed **{{app_name}}** on your system.  If you have not yet installed **{{app_name}}**, see [Linux Setup Guide](../Linux-Setup-Guide)


## Configuring The Schedule

We will first need to configure a `timer` which will configure the schedule that **{{app_name}}** will run on.  In this example, we will setup a schedule that will run nightly at 4am local time.

You should create a new file named `/etc/systemd/system/{{app_name.lower()}}.timer`, and save the following configuration into that file.

```ini
[Unit]
Description={{app_name}} run daily
Requires={{app_name.lower()}}.service

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

Next, well setup the job that will be triggered nightly by the `timer` that we previously setup.  Create a new file `/etc/systemd/system/{{app_name.lower()}}.service`, and save the following configuration into the file.

!!! Note
    The values of `User`, `WorkingDirectory`, and `ExecStart` will need to be configured to point to your **{{app_name}}** install location.

```ini
[Unit]
Description={{app_name}}
After=remote-fs.target
Wants=remote-fs.target

[Service]
User=# Replace with your username

# Set this to the directory where {{app_name}} is installed. 
WorkingDirectory=# Example : /home/tim/{{app_name}}

# This should be the full path to {{app_name}}, as well as any additional option flags
ExecStart= # Example: /home/tim/{{app_name}}/{{app_name}} prefill --no-ansi

Type=oneshot
Nice=19

[Install]
WantedBy=multi-user.target
```

Once these two files are setup, you can enable the scheduled job with:
```
sudo systemctl daemon-reload
sudo systemctl enable --now {{app_name.lower()}}.timer
sudo systemctl enable {{app_name.lower()}}
```

If everything was configured correctly, you should see similar output from running `sudo systemctl status {{app_name.lower()}}.timer`

<div data-cli-player="../casts/systemd-timer-status.cast" data-rows=8></div>
<br>

-----

## Checking Service Logs

It is possible to check on the status of the service using `sudo systemctl status {{app_name.lower()}}`, which will display both the service's status as well as its most recent logs.

<div data-cli-player="../casts/systemd-service-logs.cast" data-rows=17></div>
<br>

