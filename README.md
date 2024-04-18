# Shell-X

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://oleg-shilo.github.io/cs-script/Donation.html)
<img align="right" src="https://raw.githubusercontent.com/oleg-shilo/shell-x/master/images/shell_x_logo.png" height="128" width="128" alt="" style="float:right">


_**Dynamic customizable file context menu solution for Windows.
Allows creating context menus of any complexity without the need to compile COM shell extensions. The solution is based on the same concept as Windows Explorer native "Send to" context menu.**_

## Overview

In Windows Explorer, context menus are an extremely important part of the User Experience (UX). Just a single right-click on the file allows convenient access to the file type-specific operations.

Unfortunately, the creation and customization of context menus were always a pain point. The problem is that Windows implements explorer context menus as so-called _Shell Extensions_. They are a heavy-weight COM servers that is not trivial to implement. And what is even more important they are components that must be rebuilt/recompiled every time user wants to change the menu structure or the associated menu action. And this in turn dramatically affects the user adoption of context menus as an operating system feature.  

Interestingly enough Windows has introduced an alternative light way for managing a very specific context menu - "Send to".

![image](images/send_to.png)

The customisation of the "Send to" is dead simple. The user simply goes to the special folder and creates their shortcut(s) to the desired application. Then at runtime, the shortcut name will become the content menu item. And shortcut itself will be invoked (with the selected file path passed as an argument) when the user selects this menu item.

![image](images/send_to_files.png)

This means that the creation and customization of the "Send to" context menu is a simple file creation/editing activity that does not even require user to be an admin (elevated).

Shell-X applies the same simplified approach but extends it by allowing the creation of any context menu for any file type.

Below are some of Shell-X features that extend Windows "Send to" approach:

* Support for complex nested context menus.
* Support for console and Windows menu actions.
* Support for both batch files and PowerShell scripts as an action associated with a menu item.
* Support for custom icons in the menu items.
* The action definition is no longer a shortcut but a batch file so a menu action can have multiple steps.
* Definitive menu items order thanks to the use of the sortable prefixes in the file names.
* Individual context menu definitions for file types based on the file extension.

_Note, that intensive use of icons may lead to memory exhaustion. This is a Windows Explorer bug/design flaw. Thus don't overuse this feature. You can read more about this in [this therad](https://github.com/oleg-shilo/shell-x/issues/22)._

## Installation

_With Chocolatey_

Install package _Shell-X_:

```PS
choco install shell-x
```

_Manually_

- Download the release package and unzip its content in any location.
- Execute the following two commands in the command prompt
  ```
  shell-x -r
  shell-x -init
  ```
To uninstall just execute:
  ```
  shell-x -unregister
  ```
  Note, the explorer may lock the extension file so you may need to restart it before you can delete the file. 

_Configuration_

After the installation, the sample context menu (as described in the next section) will be created. Do modify and extend it as you wish by creating properly named batch files in the configuration folder as described in the next section.

You can open the configuration folder at any time by executing the _open_ command in the command prompt:

```
shell-x -open
```

There is also an option (v1.4.0+) for testing the configuration outside of the Windows explorer

```
shell-x -test [path]
```
It is helpful for refining the mapping of the configuration to the selected item (path) actions.

![image](https://user-images.githubusercontent.com/16729806/191431089-d71bbc08-1722-4cae-ae2d-b315129902eb.png)

## How it works

Shell-X maintains a global directory, whose file tree structure defines the complex context menu tree to be displayed at runtime on right click.

The root folders are named according to the file extension that the context menu is for. Thus the folder `txt` contains context menu definition for all text files, and the `dll` folder is for all DLLs. There are special folder names:
- `[any]` that defines the context menu for any selected file or folder.
- `[folder]` that defines the context menu for any selected folder.
- `[file]` that defines the context menu for any selected file.

**Note A**, if you want multiple extension files to be handled the same way (by a single handler) you can achieve this by naming the special folder with the comma-separated extension names enclosed in the square brackets. IE for menu item associated with editing JPEG and BMP Files the folder name should be `[jpeg,bmp]`: 

**Note B** All special folders have their name enclosed in square brackets (e.g. `[any]` or `[txt,md]`) and all folders for specific file extension have their names exactly matching the extension text (e.g. `txt`).

![image](https://github.com/oleg-shilo/shell-x/assets/16729806/21ad4206-2043-4d66-903c-ec881a84e95e)

Below is an example of the configuration for for text files (`txt` file extension).

![image](images/shell_x_files.png)

This is how the menu for text files looks at runtime.

![image](images/shell_x_menu.png)

In the example above the context menu for txt files has a complex structure containing sub-menus for opening the selected file with Notepad and other file handling operations.

The content of _00.Notepad.cmd_ file is an ordinary batch file content:
```
notepad.exe %*
```

Since the menu items are composed according to the configuration folder file structure naming the files it is vital the proper naming convention is followed:

* File name
  ```
  <two_digits_order_prefix>.<menu_item_name>[.c][.ms].<cmd|bat|ps1>
  ```

* By default the batch file is executed with the console window hidden. If you prefer console being visible include `.c` suffix before the batch file extension.

* `.ms` in the file name has special meaning. It indicates that the batch file supports a multi-select scenario. Thus if multiple files are selected and executed against the shell extension menu item then every file will be executed in its own process of the corresponding batch file. Otherwise, by default, all files are passed to a single batch file.


* If you want the menu item to have the icon then place the icon file in the same folder where the corresponding batch file is and give it the same file name as the batch file but with the _".ico"_ extension:
  ```
  05.Shell-X configure.cmd
  05.Shell-X configure.ico
  ```
  
Note, you can use wild card expression as the folder name that encodes the pattern for the file name (of the file that is right-clicked).
However, since the wild card characters are prohibited by the file system you will need to use special characters that look like the special wild card characters but are in fact special Unicode characters that are safe to use as folder names:

```C#
// The Unicode characters that look like ? and * but still allowed in dir and file names
 string safeQuestionMark = "？"; 
 string safeAsterisk = "⁎";
``` 
Simply copy the characters from this description, compose the desired pattern in the text editor and then paste the pattern in the file explorer as a folder name.

Thus your desired pattern for files cmn.ar.00, cmn.ar.01,. . .
will look like this: ⁎.ar.⁎.  

## Naming Convention

The naming convention for configuration folders:
Read more at https://github.com/oleg-shilo/shell-x/?tab=readme-ov-file#how-it-works
- `<extension>`<br>
   Any selected file, whose extension is the same as the name of the folder (e.g. `txt`).
- `[any]`<br>
  Any selected path
- `[file]`<br>
  Any selected file
- `[folder]`<br>
  Any selected folder
- `[<extension1>,<extension2>,..<extensionN>]`<br>
  A selected file, whose extension is one of the comma-delimited values in the folder name (e.g. `[png,bmp,jpeg]`).
  
## Limitations

* When the user right-clicks a file and the plugin is loaded for the very first time there is a noticeable delay (~3-5 seconds) before the menu pops up. This is a Windows Explorer one-off limitation and any subsequent right-clicks bring the context menu instantly.


