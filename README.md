Command line tool that generates visual studio solution file from a set of projects. 
Generated solution file includes all the projects and their dependencies with directory structure recreated within the solution.
Useful for complex repositories consisting of many projects files.

A few examples:

> solgen myproject.csproj

> solgen /config:x64,x86 myproject.csproj

> solgen myproject1.csproj myproject2.vcxproj myproject.proj
