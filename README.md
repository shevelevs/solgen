### Overview

Solgen is a command line tool that generates Microsoft Visual Studio solution file (.sln) from a project or a set of projects. 
Generated solution file includes all the projects and their dependencies with directory structure recreated within the solution.
The reason why this tool might be useful is because solution files are not normally edited by people manually and thus in a relatively big project it's difficult to synchronize changes made by several people. Alternative is to maintain only project files (such as .csproj) and some kind of directory structure (potentially with .proj files explicitly linking them together), but generate solution file on the fly.

### Usage

A few simple examples:

> solgen myproject.csproj

> solgen /config:x64,x86 myproject.csproj

> solgen myproject1.csproj myproject2.vcxproj myproject.proj
