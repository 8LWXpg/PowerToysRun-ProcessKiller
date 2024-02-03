# PowerToysRun Plugin Template

This a template for Visual Studio to create a PowerToys Run plugin.

## How to use

1. Download the latest release.
2. Move the zip to `~\Documents\Visual Studio 2022\Templates\ProjectTemplates`.
3. Open Visual Studio 2022 and select the template named `PowerToys Run Plugin Template`.
4. Copy `PowerToys.Settings.UI.Lib.dll`, `PowerToys.ManagedCommon.dll`, `PowerToys.Common.UI.dll` ,`Wox.Plugin.dll`, `Wox.Infrastructure.dll` from the PowerToys Run folder to `.\Lib` (using symlink or running `copyLib.ps1` is recommended).
5. Right-click on `Properties\Resources.resx` and select `Run Custom Tool` (`Resources.Designer.cs` changes completely for unknown reasons).

## How to debug

1. Build the project.
2. Run `debug.ps1`.
3. Attach to the process `PowerToys.PowerLauncher`.

## Update the template

Visual Studio 2022 caches the templates, so you can't simply replace the zip file.

1. Remove previous versions of the template.
2. Start Visual Studio 2022 then select `Create a new project`, this loads the templates.
3. Close Visual Studio 2022.
4. Move the new zip to `~\Documents\Visual Studio 2022\Templates\ProjectTemplates`.

## Help improve this template

If you have any suggestions or improvements, feel free to open an issue or a pull request.

### Useful links

- [Template parameters](https://learn.microsoft.com/en-us/visualstudio/ide/template-parameters?view=vs-2022)
- [Template schema reference](https://learn.microsoft.com/en-us/visualstudio/extensibility/visual-studio-template-schema-reference?view=vs-2022)
