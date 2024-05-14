Push-Location
Set-Location $PSScriptRoot

$name = 'ProcessKiller'
$assembly = "Community.PowerToys.Run.Plugin.$name"
$version = "v$((Get-Content ./plugin.json | ConvertFrom-Json).Version)"
$archs = @('x64', 'arm64')

git tag $version
git push --tags

Remove-Item ./out/*.zip -Recurse -Force -ErrorAction Ignore
foreach ($arch in $archs) {
	$releasePath = "./bin/$arch/Release/net8.0-windows"

	dotnet build -c Release /p:Platform=$arch

	Remove-Item "./out/$name/*" -Recurse -Force -ErrorAction Ignore
	$items = @(
		"$releasePath/$assembly.dll",
		"$releasePath/plugin.json",
		"$releasePath/Images",
		"$releasePath/$assembly.deps.json",
		"$releasePath/pl-PL"
	)
	Copy-Item $items "./out/$name" -Recurse -Force
	Compress-Archive "./out/$name" "./out/$name-$version-$arch.zip" -Force
}

gh release create $version (Get-ChildItem ./out/*.zip)
Pop-Location
