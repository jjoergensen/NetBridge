This project is inteded for bridging between 2 different and incompatible
versions of .NET.

It can be used both for calling .NET core from .NET framework and vice versa.
But it needs to be compiled for each version of .NET separately.

You also need to make sure, that you don't mix the versions of .NET in your
assemblies, so one way of making sure of that is to place it and the related
assemblies in a separate folder.
