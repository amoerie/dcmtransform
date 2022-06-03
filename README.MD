🔍 DICOM TRANSFORM
----------
[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2dcmtransform%2Fbadge%3Fref%3Dmaster&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/dcmtransform/goto?ref=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DcmTransform?label=DcmTransform&style=for-the-badge)](https://www.nuget.org/packages/DcmTransform)

Transforms DICOM files using JavaScript.

Installation
------------

```
    dotnet tool install dcmtransform --global
```

Usage
-----

```powershell
    dcmfind sample.dcm --script sample.js
```

Options 

```
  files pos. 0         Required. (Default: .) Transform these DICOM files. When missing, this option will be read from the piped input.
  
  -s, --script         Required. Script that transforms the provided DICOM file 

  -p, --parallelism    (Default: 8) Process this many files in parallel

  --help               Display this help screen.

  --version            Display version information.
```

The script can be provided inline or as a path to a file. It is presumed that the script exposes one top level "main" function that accepts a single argument "dicom".
This argument is a C# object exposed to JavaScript with the following signature:

```csharp
public interface IJintDicom
{
    string? GetString(ushort group, ushort element);
    string[]? GetMultipleStrings(ushort group, ushort element);
    void UpdateString(ushort group, ushort element, string value);
    void DeleteTag(ushort group, ushort element);
    void DeleteGroup(ushort group);
    bool Contains(ushort group, ushort element);
    bool HasValue(ushort group, ushort element);
    int CountSequenceItems(ushort group, ushort element);
    JintDicom GetSequenceItem(ushort group, ushort element, int index);
    JintDicom[] GetSequenceItems(ushort group, ushort element);
    JintDicom CreateAndReturnSequenceItem(ushort group, ushort element);
}
```

This is a sample transform script setting the accession number to 'TRANSFORMED'

```
function main(dicom) {
    dicom.UpdateString(0x0008,0x0050,"TRANSFORMED");
}
```
