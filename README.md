GitDir
======

A Windows console application to list the status of all Git subdirectories.

![Screenshot](http://i.imgur.com/tgaaKJI.png)


GitDir is a Windows tool that helps manage directories full of Git-subdirectories that you may (or may not) not the status of. The output follows the formatting the Posh-git provides in it's CLI integration for PowerShell, although PowerShell is not required.

Usage
------
```
gitdir <path> [options...]

[options]
   -r    Recusively search subdirectories
```


Git Alias
------
Add the following lines to your global `.gitconfig` file to call an alias directly from `git` like `git dir`. Make sure the executable is accesible from your PATH.

```
[alias]
        dir = "!gitdir"
```
