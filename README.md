# P3D Merge
A simple command-line tool to merge multiple P3D files.

## Basic Usage
The most basic execution of the tool takes 2+ input filepaths, and 1 output filepath:

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d"`

*Example*
```
P3DMerge.exe "C:\SHAR\art\chars\homer_m.p3d" "C:\SHAR\art\chars\h_fat_m.p3d" -o "Merged.p3d"
Inputs: C:\SHAR\art\chars\homer_m.p3d; C:\SHAR\art\chars\h_fat_m.p3d
Output: C:\Users\Josh\Dropbox\repos\P3DMerge\P3DMerge\bin\Debug\Merged.p3d

Parsing inputs...
Checking: 'C:\SHAR\art\chars\homer_m.p3d'...
Checking: 'C:\SHAR\art\chars\h_fat_m.p3d'...
Merging files into 'C:\Redacted\Merged.p3d'...
Done.
```

## Optional Arguments
P3D Merge provides a few optional arguments to change certain functionality. You can use any combination of the below to achieve the desired output.

*Note: `-append` and `-overwrite` are exclusive, as you can only choose one for the output file.*

### Append
By default, if the output file exists, you will be presented with a user-input prompt to choose an action:
```
Output file 'C:\Redacted\Merged.p3d' already exists. Choose an action:
        [A]ppend
        [O]verwrite
        [E]xit
```
You can use the `-append` argument to automatically choose the `Append` option, which will add the chunks from the input files to the existing file:

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -append`

### Deduplication (Dedupe)
You can use the `-dedupe` argument to check all chunks in the various input files for duplicates, and only add one of each.

*Note: This will only check root chunks, not children.*

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -dedupe`

### No History
By default, the tool will add a `History` chunk to the start of the output file, with details on the tool's execution (similar to the output you can see in some base game files from `p3dcompress`).

You can use the `-nohistory` argument to omit this chunk in the output file.

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -nohistory`

### Overwrite
By default, if the output file exists, you will be presented with a user-input prompt to choose an action:
```
Output file 'C:\Redacted\Merged.p3d' already exists. Choose an action:
        [A]ppend
        [O]verwrite
        [E]xit
```
You can use the `-overwrite` argument to automatically choose the `Overwrite` option, which will delete the original file, and rebuild from the input files:

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -overwrite`

### Pause
By default, the tool will close itself upon completion, unless it hits an error.

You can use the `-pause` argument to wait for user input after execution before closing. Useful if you want to see the tool's output.

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -pause`

### Sort
By default, the tool will add chunks to the output file in the order they were listed in the arguments, and the order they were in the original files.

You can use the `-sort` argument to sort chunks by their type, and attempting to prioritise chunks as required by *Simpsons: Hit & Run*. For example, `Texture` chunks before `Shader` chunks.

`P3DMerge.exe "Path\To\File 1.p3d" "Path\To\File 2.p3d" "Path\To\File 3.p3d" -o "Path\To\Output.p3d" -sort`
