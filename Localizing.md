# Localization

## On github.dev

1. Fork this repository.
1. Open github.dev by pressing <kbd>.</kbd> on keyboard. \
![Open github.dev](https://user-images.githubusercontent.com/856858/130119109-4769f2d7-9027-4bc4-a38c-10f297499e8f.gif)
1. Install [ResX Viewer/Editor](https://marketplace.visualstudio.com/items?itemName=8LWXpg.code-resx) extension (yes, I made an extension for this).
1. Copy `./ProcessKiller/Properties/Resources.resx` to `./ProcessKiller/Properties/Resources.<locale>.resx`.
1. Change the `Value`s to the translated text.
1. Add `"$releasePath/<locale>"` to the `$items` array in `./ProcessKiller/release.ps1`.
   https://github.com/8LWXpg/PowerToysRun-ProcessKiller/blob/bc8a9932df1800b1577ac673ac10795047590b48/ProcessKiller/release.ps1#L19-L24
1. Commit the change and submit a PR.
