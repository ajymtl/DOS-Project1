# DOS-Project1
Project as a part of curriculum for Distributed Operating Systems Principles -COP5615  (Fall'20)

## Run instructions:
Unzip the file mittal.zip.
### To run locally:
- dotnet fsi --langversion:preview .\proj1.fsx
### To run locally with timing stats:
- time dotnet fsi --langversion:preview .\proj1.fsx
### To run on remote:
- Deploy remote.fsx on remote server. Put your IP in configuration.
- Update IP of remote machine in proj1.fsx.
- Dotnet fsi --langversion:preview .\proj1.fsx true
### Max parallelization achieved:
I noticed that as the value of the number gets higher, the parallelization factor i.e.
cpu_time/real_time approaches 8. The number of logical cores in my personal machine were
also 8.
### Size of the work unit:
Because this problem has work which doesn’t have any blocking operations e.g. IO, requests
etc, I observed that having a large number of workers (i.e. more than 8) doesn’t make sense
and doesn’t actually increase the parallelization factor. Thus optimal work size (for each worker)
for this particular problem was, N/8.
The result of running your program for dotnet fsi proj1.fsx 1000000 4
It yields nothing.
### Things to be noted
- For problems of size 10^6 and less, I am not achieving a parallelization factor of 2+. The
reason is the time taken by “Akka.Fsharp” library to load is higher than the actual computation
time because of the optimization approach (using sliding windows approach for calculation of
sum of squares) used.
For e.g.
Library load happens in synchronous fashion.
Computation is done parallely.
PF =( library load time + 8*(computation time)) / (library load time + computation time)
Here because library load time >> 8* computation time, the parallelization factor is close to 1.1
or 1.2.
- The max number that we are able to handle
10^11.
