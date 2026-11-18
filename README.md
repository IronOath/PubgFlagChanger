# PUBGFlagChanger

`PUBGFlagChanger` is a Windows WPF utility for managing and applying PUBG observer flag packages.

## Features

- Import packages from `.zip`
- Add package folders directly
- Remove and rename packages
- Apply selected package to a destination folder
- Automatic backup before overwrite
- Track the currently used package per destination
- Launch PUBG (Steam) and terminate `TslGame.exe`

## Package format

Each package must contain:

- `TeamInfo.csv`
- `TeamIcon/` folder

Example:

- `MyPackage/TeamInfo.csv`
- `MyPackage/TeamIcon/...`

## Requirements

- Windows
- .NET 8 (for building from source)

## Run from source

```powershell
dotnet restore
dotnet build
dotnet run
```

## Usage

1. Start the app.
2. Set the destination folder (where `TeamInfo.csv` and `TeamIcon` should be applied).
3. Import a `.zip` package or add a package folder.
4. Select a package and click **Apply Package**.

## Data folders

The app stores data next to the executable:

- `Packages/` - imported and added packages
- `Packages/settings.json` - saved destination folder
- `Packages/used-packages.json` - destination-to-package mapping
- `Backups/` - timestamped backups created before apply

## Publish

```powershell
dotnet publish -c Release
```

The project is configured for self-contained single-file publishing (`win-x64`).
