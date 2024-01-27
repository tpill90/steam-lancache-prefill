# benchmark

## Overview

Intended for use in identifying potential bottlenecks both server side (usually disk IO), as well as client side.

<!-- TODO give this whole file another run through.  Maybe write a more in depth workflow explanation.  --!>
<!-- TODO don't like this line -->
`benchmark` uses the same download logic as `prefill`, however it offers the following instead:

<!-- TODO touch this up -->
- Portable, no need to login to Steam in order to start download.
- Able to be used across multiple machines at the same time, without logging in
- Continuous sustained download, combines multiple apps into a single download
- Repeatable, will perform the same download every time
- Randomized, requests will be completed in a random order

-----

## setup

<div data-cli-player="../casts/benchmark-setup.cast" data-rows=22></div>
<br>

Creates a benchmark "workload" comprised of multiple apps, that will then be benchmarked using the `run` sub-command.  Generally, the ideal benchmark will be the one that most closely matches the apps that you will usually be downloaded.  This can be setup for example with `./SteamPrefill benchmark setup --use-selected`

<!-- Markdown columns determine width based on the the longest cell.  &nbsp; forces the length to be longer, so --use-selected doesn't get broken into two lines  -->
| Option   &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; | Values |     |
| ---------------- | --- | --- |
| --use-selected |  | Creates a workload file using apps previously specified with `select-apps`.  Ideal for most use cases, since it likely aligns with games that will be downloaded by real event clients. |
| --all          |  | Benchmark workload will be created using all currently owned apps.  |
| --appid        |  | The id of one or more apps to include in benchmark workload file.  Useful for testing a specific app, without having to modify previously selected apps.  AppIds can be found using [SteamDB](https://steamdb.info/)  |
| --no-ansi      |  |     Application output will be in plain text, rather than using the visually appealing colors and progress bars.  Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file. |
| --preset    | Dota2, Destiny2 |  Can be used to quickly setup a benchmark with a predefined workload of differing characteristics.  Destiny2 represents a best case scenario where chunk sizes are close to 1Mib, whereas Dota2 is a worst case scenario of small files. |

-----

## run

Runs multiple iterations of the benchmark workload created with `benchmark setup`.  Useful for measuring the throughput for the Lancache server, and diagnosing any potential performance issues.

<u>** Warmup **</u>

<div data-cli-player="../casts/benchmark-warmup.cast" data-rows=5></div>
<br>

The first part of the benchmark run will be the initialization + warmup of the workload.  The workload file previously created with `benchmark setup` will be loaded from disk, and the ordering of the requests to be made will be randomized.  

Next, the warmup run will download all of the workload's requests, which is necessary for a few reasons:

- It ensures that all of the data has been downloaded, and is cached by the Lancache.
- Allows for data that has been cached in the server's memory to be flushed by the new requests, ensuring that we are testing disk I/O.
- Gives the CPU a chance to physically warm up,  minimizing potential fluctuations between runs.

<u>** Running **</u>

After the warmup, `benchmark run` will begin downloading the same workload in a loop, for as many iterations as specified with `--iterations` (default: 5).  After each iteration, it will display the overall average download rate for that iteration.

<div data-cli-player="../casts/benchmark-iterations.cast" data-rows=5></div>
<br>

Once all the iterations have been completed, a summary table displaying the min/max/average will be shown:

<div data-cli-player="../casts/benchmark-run-summary.cast" data-rows=6></div>
<br>

<u>** Identifying bottlenecks **</u>

While `benchmark run` is useful for getting an overall idea of your server's performance, it won't however identify bottlenecks in the system by itself.  It is instead primarily intended to be another tool to help with identifying bottlenecks,  by providing a constant and even load on the server.

It is recommended that you run some sort of system monitoring software on the server while running your benchmarks, so that you can get an idea of how your server is handling the load.  There are many monitoring tools available,  such as [Glances](https://github.com/nicolargo/glances), that provide a visual overview of the system.

Two important measurements to keep an eye on, are the overall `CPU` usage, as well as `iowait`.  The majority of bottlenecks for servers will be either the speed of the CPU, or the speed at which the disk(s) can read.

![benchmark-run-glances](images/benchmark-run-glances.png){: style="width:350px"}

| Option        |     | Values        | Default |     |
| ------------- | --- | ------------- | ------- | --- |
| --concurrency | -c  | 1-100         | **30**  | The maximum number of concurrent requests in flight at one time.  A higher number may improve maximum throughput, but may possibly have a negative effect if the cache server cannot process the concurrent requests fast enough. |
| --iterations  | -i  | 1-25          | **5**   | The number of runs to do before calculating overall results.  |
| --unit        |     | bits, bytes   | **bits** | Specifies which unit to use to display download speed.  |
| --no-ansi     |     |               |          | Application output will be in plain text, rather than using the visually appealing colors and progress bars.  Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file. |
